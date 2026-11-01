using System.Security.Cryptography;
using System.Text.Json;

namespace Hmi.Adapters.Persistence;

public enum StoreStatus { PendingStart, Active, Closed }
public enum RecoveryKind { Empty, PendingStart, Active, RolledBack, Closed, Uncertain }
public sealed record SnapshotRef(string Name, string Run, long Sequence, string Hash, int Length);
public sealed record Control(int Schema, string Catalog, string Rules, long Sequence, string Run,
    StoreStatus Status, string Operation, string RequestHash, SnapshotRef[] Snapshots);
public sealed record Envelope(string Payload, string Hash);
public sealed record Recovery(RecoveryKind Kind, Control? Control, byte[]? Payload, string Detail);
public sealed record SnapshotIdentity(string Run, long Sequence, string Catalog, string Rules);
public sealed record CommitResult(long Sequence, bool AlreadyCommitted);

// Filesystem operations are an adapter port, not simulation authority. Implementations must
// serialize writers and keep atomic writes within this directory/filesystem.
public interface ICheckpointFiles
{
    IDisposable AcquireWriter();
    string[] Names();
    long Length(string name);
    byte[] Read(string name, int maximumBytes);
    void AtomicWrite(string name, byte[] bytes, bool overwrite);
    void Delete(string name);
}

public sealed class CheckpointStore
{
    public const int SnapshotLimit = 256 * 1024, ControlLimit = 16 * 1024, StoreLimit = 2 * 1024 * 1024;
    private const string ControlName = "control.json";
    private readonly ICheckpointFiles files;
    private readonly string catalog, rules;
    private readonly Func<byte[], SnapshotIdentity, bool> validatePayload;
    public CheckpointStore(ICheckpointFiles files, string catalog, string rules, Func<byte[], SnapshotIdentity, bool> validatePayload)
    {
        this.files = files; this.catalog = catalog; this.rules = rules; this.validatePayload = validatePayload;
        if (string.IsNullOrWhiteSpace(catalog) || string.IsNullOrWhiteSpace(rules)) throw new ArgumentException("Missing compatibility identity");
    }
    private static string Hash(byte[] data) => Convert.ToHexString(SHA256.HashData(data));
    private static bool IsUuid(string text) => Guid.TryParseExact(text, "N", out var id) && id != Guid.Empty && id.ToString("N") == text;
    private static bool Owned(string name) => name == ControlName || System.Text.RegularExpressions.Regex.IsMatch(name, @"^(snapshot-[0-9a-f]{32}-[1-9][0-9]*-[0-9a-f]{32}\.json|temp-[0-9a-f]{32})$");
    private static byte[] Encode(Control c)
    {
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(c);
        return JsonSerializer.SerializeToUtf8Bytes(new Envelope(Convert.ToBase64String(payload), Hash(payload)));
    }
    private Control? ReadControl()
    {
        var names = files.Names();
        if (!names.Contains(ControlName))
        {
            if (names.Any(n => n.StartsWith("snapshot-", StringComparison.Ordinal))) throw new InvalidDataException("Missing authority with historical snapshots");
            return null;
        }
        byte[] raw = files.Read(ControlName, ControlLimit);
        if (raw.Length > ControlLimit) throw new InvalidDataException("Oversize control");
        Envelope e = JsonSerializer.Deserialize<Envelope>(raw) ?? throw new InvalidDataException("Missing envelope");
        byte[] payload = Convert.FromBase64String(e.Payload);
        if (Hash(payload) != e.Hash) throw new InvalidDataException("Control checksum");
        Control c = JsonSerializer.Deserialize<Control>(payload) ?? throw new InvalidDataException("Missing control");
        if (c.Schema != 1 || c.Catalog != catalog || c.Rules != rules || c.Sequence < 1 || !IsUuid(c.Run)
            || !IsUuid(c.Operation) || !Enum.IsDefined(c.Status) || c.Snapshots == null || c.Snapshots.Length > 2
            || c.RequestHash?.Length != 64) throw new InvalidDataException("Invalid control identity/schema");
        if ((c.Status == StoreStatus.Active) != (c.Snapshots.Length > 0)) throw new InvalidDataException("Invalid active snapshot list");
        if (c.Status == StoreStatus.Active && c.Snapshots[0]?.Sequence != c.Sequence) throw new InvalidDataException("Latest snapshot sequence must equal committed control");
        long previous = long.MaxValue;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var s in c.Snapshots)
        {
            if (s == null || s.Run != c.Run || s.Sequence < 1 || s.Sequence > c.Sequence || s.Sequence >= previous
                || s.Length < 1 || s.Length > SnapshotLimit || s.Hash?.Length != 64 || !ValidSnapshotName(s.Name, s)
                || !seen.Add(s.Name)) throw new InvalidDataException("Invalid snapshot descriptor");
            previous = s.Sequence;
        }
        return c;
    }
    private static bool ValidSnapshotName(string name, SnapshotRef s)
    {
        string prefix = $"snapshot-{s.Run}-{s.Sequence}-";
        return name != null && name.StartsWith(prefix, StringComparison.Ordinal) && name.EndsWith(".json", StringComparison.Ordinal)
            && IsUuid(name.Substring(prefix.Length, name.Length - prefix.Length - 5));
    }
    public Recovery Recover()
    {
        using var writer = files.AcquireWriter();
        try
        {
            var c = ReadControl();
            if (c == null) return new(RecoveryKind.Empty, null, null, "No committed run");
            if (c.Status != StoreStatus.Active)
                return new(c.Status == StoreStatus.Closed ? RecoveryKind.Closed : RecoveryKind.PendingStart, c, null, "No resumable snapshot");
            for (int i = 0; i < c.Snapshots.Length; i++)
            {
                try
                {
                    var s = c.Snapshots[i]; byte[] data = files.Read(s.Name, SnapshotLimit);
                    if (data.Length == s.Length && Hash(data) == s.Hash && validatePayload(data, new(s.Run, s.Sequence, catalog, rules)))
                        return new(i == 0 ? RecoveryKind.Active : RecoveryKind.RolledBack, c, data,
                            i == 0 ? "Latest safe point" : "Latest damaged; previous committed same-run safe point restored");
                }
                catch (Exception ex) when (ex is IOException or InvalidDataException or JsonException or ArgumentException) { }
            }
            return new(RecoveryKind.Uncertain, c, null, "No valid committed snapshot; no arbitrary backup recovery");
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or JsonException or ArgumentException or FormatException)
        { return new(RecoveryKind.Uncertain, null, null, ex.Message); }
    }
    public CommitResult BeginRun(string run, string operation, long expectedSequence) => Commit(StoreStatus.PendingStart, run, operation, expectedSequence, null);
    public CommitResult Save(string run, string operation, long expectedSequence, byte[] payload) => Commit(StoreStatus.Active, run, operation, expectedSequence, payload);
    public CommitResult Close(string run, string operation, long expectedSequence) => Commit(StoreStatus.Closed, run, operation, expectedSequence, null);
    private CommitResult Commit(StoreStatus status, string run, string operation, long expectedSequence, byte[]? payload)
    {
        if (!IsUuid(run) || !IsUuid(operation) || expectedSequence < 0) throw new ArgumentException("Invalid request identity");
        if (expectedSequence == long.MaxValue) throw new InvalidOperationException("Sequence exhausted");
        // Own a defensive copy before validation/hash/write; caller mutation cannot change committed bytes.
        payload = payload?.ToArray();
        if (status == StoreStatus.Active && (payload == null || payload.Length == 0 || payload.Length > SnapshotLimit || !validatePayload(payload, new(run, checked(expectedSequence + 1), catalog, rules))))
            throw new InvalidDataException("Invalid prepared payload");
        string requestHash = Hash(JsonSerializer.SerializeToUtf8Bytes(new { status, run, expectedSequence, payloadHash = payload == null ? "" : Hash(payload) }));
        using var writer = files.AcquireWriter();
        Control? old = ReadControl();
        if (old?.Operation == operation)
        {
            if (old.RequestHash != requestHash) throw new InvalidOperationException("Operation identity reused with different request");
            return new(old.Sequence, true);
        }
        if ((old?.Sequence ?? 0) != expectedSequence) throw new InvalidOperationException("Stale commit sequence");
        if (expectedSequence == long.MaxValue) throw new InvalidOperationException("Sequence exhausted");
        if (status == StoreStatus.PendingStart)
        {
            if (old?.Run == run) throw new InvalidOperationException("New run requires a new UUID");
        }
        else if (old == null || old.Run != run || old.Status == StoreStatus.Closed)
            throw new InvalidOperationException("Run is not active/pending");
        long next = expectedSequence + 1;
        Cleanup(old);
        SnapshotRef[] refs = [];
        if (payload != null)
        {
            string name = $"snapshot-{run}-{next}-{Guid.NewGuid():N}.json";
            refs = [new(name, run, next, Hash(payload), payload.Length)];
            if (old!.Status == StoreStatus.Active)
            {
                foreach (var previous in old.Snapshots)
                {
                    try
                    {
                        byte[] data = files.Read(previous.Name, SnapshotLimit);
                        if (data.Length == previous.Length && Hash(data) == previous.Hash && validatePayload(data, new(previous.Run, previous.Sequence, catalog, rules)))
                        { refs = [refs[0], previous]; break; }
                    }
                    catch (Exception ex) when (ex is IOException or InvalidDataException or JsonException or ArgumentException) { }
                }
            }
        }
        var control = new Control(1, catalog, rules, next, run, status, operation, requestHash, refs);
        byte[] bytes = Encode(control);
        if (bytes.Length > ControlLimit) throw new InvalidDataException("Control quota exceeded");
        long used = files.Names().Where(Owned).Sum(n => (long)files.Length(n));
        if (used + (payload?.Length ?? 0) + bytes.Length > StoreLimit) throw new IOException("Checkpoint store quota exceeded");
        if (payload != null) files.AtomicWrite(refs[0].Name, payload, false);
        files.AtomicWrite(ControlName, bytes, true);
        Cleanup(control);
        return new(next, false);
    }
    private void Cleanup(Control? c)
    {
        // Only a validated authority allows cleanup; lock/temp-only fresh stores are safe.
        var keep = c?.Snapshots.Select(s => s.Name).ToHashSet(StringComparer.Ordinal) ?? [];
        foreach (string name in files.Names())
            if (Owned(name) && name != ControlName && !keep.Contains(name))
                try { files.Delete(name); } catch (IOException) { /* quota check prevents unbounded growth */ }
    }
}
