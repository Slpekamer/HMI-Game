using Hmi.Adapters.Persistence;
using System.Text;

static class Program
{
    static int checks;
    static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    static string Id() => Guid.NewGuid().ToString("N");
    static byte[] Data(int wave) => Encoding.UTF8.GetBytes("wave=" + wave);
    static CheckpointStore Store(ICheckpointFiles files) => new(files, "catalog-test", "rules-test", (p, identity) => Encoding.UTF8.GetString(p).StartsWith("wave="));
    static void Reject(Action action, string message)
    { bool rejected = false; try { action(); } catch (Exception e) when (e is IOException or InvalidDataException or InvalidOperationException or ArgumentException or System.Text.Json.JsonException) { rejected = true; } Check(rejected, message); }
    static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "kill-worker")
        {
            var childFiles = new PhysicalCheckpointFiles(args[1]);
            Store(new PauseFiles(childFiles, int.Parse(args[4]), args[5])).Save(args[2], args[3], 2, Data(2));
            return;
        }
        var files = new MemoryFiles(); var store = Store(files); string run = Id();
        Check(store.Recover().Kind == RecoveryKind.Empty, "empty");
        string start = Id(); store.BeginRun(run, start, 0);
        Check(store.BeginRun(run, start, 0).AlreadyCommitted, "start ack idempotent");
        Check(store.Recover().Kind == RecoveryKind.PendingStart, "pending no old Continue");
        string save = Id(); store.Save(run, save, 1, Data(1));
        Check(store.Save(run, save, 1, Data(1)).AlreadyCommitted, "save ack idempotent");
        Reject(() => store.Save(run, save, 1, Data(2)), "operation cannot change payload");
        Reject(() => store.Save(Id(), Id(), 2, Data(2)), "wrong run");
        Reject(() => store.Save(run, Id(), 1, Data(2)), "stale sequence");
        store.Save(run, Id(), 2, Data(2));
        var current = store.Recover(); Check(current.Kind == RecoveryKind.Active, "active");
        files.Values[current.Control!.Snapshots[0].Name] = [1, 2];
        Check(store.Recover().Kind == RecoveryKind.RolledBack, "only explicit prior same-run backup");
        store.Save(run, Id(), 3, Data(3));
        var afterRollback = store.Recover();
        files.Values[afterRollback.Control!.Snapshots[0].Name] = [0];
        Check(Encoding.UTF8.GetString(store.Recover().Payload!) == "wave=1", "rollback-save keeps last valid backup");
        store.Close(run, Id(), 4);
        Check(store.Recover().Kind == RecoveryKind.Closed, "closed does not revive backup");
        Reject(() => store.Save(run, Id(), 5, Data(3)), "late old write cannot revive closed");
        string newer = Id(); store.BeginRun(newer, Id(), 5);
        Check(store.Recover().Kind == RecoveryKind.PendingStart, "new run invalidates prior identity");
        for (int i = 0; i < 1000; i++) store.Save(newer, Id(), 6 + i, Data(i));
        Check(files.Names().Count(n => n.StartsWith("snapshot-")) == 2, "1000 commits bounded snapshots");
        Check(files.Values.Values.Sum(x => x.Length) < CheckpointStore.StoreLimit, "bounded disk bytes");
        // Interrupt each snapshot/control write stage; reopen must choose old or fully committed new.
        for (int fail = 1; fail <= 6; fail++)
        {
            var f = new MemoryFiles(); var s = Store(f); string r = Id();
            s.BeginRun(r, Id(), 0); s.Save(r, Id(), 1, Data(1));
            string op = Id(); f.FailAt = fail; f.Stage = 0;
            Reject(() => s.Save(r, op, 2, Data(2)), "fault injected " + fail);
            f.FailAt = 0;
            var recovery = Store(f).Recover();
            Check(recovery.Kind == RecoveryKind.Active, "crash has known committed state " + fail);
            string payload = Encoding.UTF8.GetString(recovery.Payload!);
            Check(payload == (fail == 6 ? "wave=2" : "wave=1"), "commit point " + fail);
            var retry = s.Save(r, op, 2, Data(2));
            Check(retry.Sequence == 3 && retry.AlreadyCommitted == (fail == 6), "retry exact once " + fail);
        }
        // Missing/corrupt authority never scans orphan snapshots to resurrect history.
        files.Values["control.json"] = [0];
        Check(store.Recover().Kind == RecoveryKind.Uncertain, "corrupt authority fail closed");
        Reject(() => store.BeginRun(Id(), Id(), 0), "cannot silently overwrite uncertain history");
        Reject(() => Store(new MemoryFiles()).BeginRun("ABCDEFABCDEFABCDEFABCDEFABCDEFAB", Id(), 0), "noncanonical uppercase UUID rejected");
        Reject(() => Store(new MemoryFiles()).Save(Id(), Id(), long.MaxValue, Data(1)), "sequence overflow rejected");
        var ignored = new MemoryFiles(); ignored.Values["temp-note"] = [1]; ignored.Values["personal.txt"] = [2];
        var clean = Store(ignored); string cleanRun = Id(); clean.BeginRun(cleanRun, Id(), 0); clean.Save(cleanRun, Id(), 1, Data(1));
        Check(ignored.Values.ContainsKey("temp-note") && ignored.Values.ContainsKey("personal.txt"), "unrelated files not cleaned");
        // Every close/new-run atomic boundary: before rename old identity remains; after rename never revives.
        foreach (bool closing in new[] { false, true })
        for (int fail = 1; fail <= 3; fail++)
        {
            var f = new MemoryFiles(); var s = Store(f); string oldRun = Id(), newRun = Id(), operation = Id();
            s.BeginRun(oldRun, Id(), 0); s.Save(oldRun, Id(), 1, Data(1)); f.Stage = 0; f.FailAt = fail;
            Reject(() => { if (closing) s.Close(oldRun, operation, 2); else s.BeginRun(newRun, operation, 2); }, "state transition fault");
            f.FailAt = 0; var state = Store(f).Recover();
            Check(state.Kind == (fail < 3 ? RecoveryKind.Active : closing ? RecoveryKind.Closed : RecoveryKind.PendingStart), "termination/newrun commit boundary");
            var result = closing ? s.Close(oldRun, operation, 2) : s.BeginRun(newRun, operation, 2);
            Check(result.Sequence == 3 && result.AlreadyCommitted == (fail == 3), "state retry exact once");
        }
        var huge = new MemoryFiles(); huge.Values["control.json"] = new byte[CheckpointStore.ControlLimit + 1];
        Check(Store(huge).Recover().Kind == RecoveryKind.Uncertain, "oversize control bounded failclosed");
        var locksOnly = new MemoryFiles(); locksOnly.Values["writer.lock"] = []; locksOnly.Values["temp-" + Id()] = [1];
        Check(Store(locksOnly).Recover().Kind == RecoveryKind.Empty, "lock and temp not historical run");
        Reject(() => Store(new MemoryFiles()).BeginRun(Guid.Empty.ToString("N"), Id(), 0), "default empty run identity rejected");
        Reject(() => Store(new MemoryFiles()).BeginRun(Id(), Guid.Empty.ToString("N"), 0), "default empty operation identity rejected");
        string temp = Path.Combine(Path.GetTempPath(), "hmi-checkpoint-" + Id());
        var physical = new PhysicalCheckpointFiles(temp); var disk = Store(physical); string diskRun = Id();
        disk.BeginRun(diskRun, Id(), 0); disk.Save(diskRun, Id(), 1, Data(7));
        Check(Store(new PhysicalCheckpointFiles(temp)).Recover().Kind == RecoveryKind.Active, "actual filesystem reopen");
        using (physical.AcquireWriter()) Reject(() => physical.AcquireWriter(), "exclusive writer lock");
        Reject(() => physical.Read("../escape", 1024), "path traversal rejected");
        for (int fail = 1; fail <= 4; fail++)
        {
            string directory = Path.Combine(Path.GetTempPath(), "hmi-checkpoint-crash-" + Id());
            var real = new PhysicalCheckpointFiles(directory); var injected = new FaultFiles(real); var s = Store(injected); string r = Id(), op = Id();
            s.BeginRun(r, Id(), 0); s.Save(r, Id(), 1, Data(1)); injected.FailAt = fail; injected.Stage = 0;
            Reject(() => s.Save(r, op, 2, Data(2)), "actual filesystem fault boundary");
            injected.FailAt = 0; var recovered = Store(new PhysicalCheckpointFiles(directory)).Recover();
            Check(recovered.Kind == RecoveryKind.Active && Encoding.UTF8.GetString(recovered.Payload!) == (fail == 4 ? "wave=2" : "wave=1"), "actual filesystem committed recovery");
            Check(s.Save(r, op, 2, Data(2)).AlreadyCommitted == (fail == 4), "actual filesystem lost ack retry");
        }
        // Descriptor validation is independent of envelope checksum correctness.
        var malformed = new MemoryFiles(); var valid = Store(malformed); string vr = Id(); valid.BeginRun(vr, Id(), 0); valid.Save(vr, Id(), 1, Data(1));
        var original = valid.Recover().Control!;
        foreach (var bad in new[] {
            original with { Snapshots = [original.Snapshots[0] with { Name = "../escape" }] },
            original with { Snapshots = [original.Snapshots[0], original.Snapshots[0]] },
            original with { Snapshots = [original.Snapshots[0] with { Run = Id() }] },
            original with { Catalog = "other-catalog" }, original with { Schema = 2 },
            original with { Status = StoreStatus.Closed }, original with { Sequence = -1 } })
        {
            byte[] bytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(bad);
            malformed.Values["control.json"] = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new Envelope(Convert.ToBase64String(bytes), Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes))));
            Check(Store(malformed).Recover().Kind == RecoveryKind.Uncertain, "checksummed malformed control rejected");
        }
        // Real child process termination around actual immutable/control filesystem commits.
        for (int phase = 1; phase <= 4; phase++)
        {
            string directory = Path.Combine(Path.GetTempPath(), "hmi-store-kill-" + Id()), marker = Path.Combine(directory, "test-boundary-marker");
            var fs = new PhysicalCheckpointFiles(directory); var s = Store(fs); string r = Id(), op = Id();
            s.BeginRun(r, Id(), 0); s.Save(r, Id(), 1, Data(1));
            var info = new System.Diagnostics.ProcessStartInfo("dotnet") { UseShellExecute = false };
            foreach (var argument in new[] { typeof(Program).Assembly.Location, "kill-worker", directory, r, op, phase.ToString(), marker }) info.ArgumentList.Add(argument);
            using var child = System.Diagnostics.Process.Start(info)!;
            bool reached = System.Threading.SpinWait.SpinUntil(() => File.Exists(marker) || child.HasExited, 15000);
            if (!reached || !File.Exists(marker)) { if (!child.HasExited) child.Kill(true); throw new Exception("Child did not reach kill boundary"); }
            child.Kill(true); child.WaitForExit();
            var resumed = Store(new PhysicalCheckpointFiles(directory)); var recovered = resumed.Recover();
            Check(recovered.Kind == RecoveryKind.Active && Encoding.UTF8.GetString(recovered.Payload!) == (phase == 4 ? "wave=2" : "wave=1"), "real killed process committed state");
            Check(resumed.Save(r, op, 2, Data(2)).AlreadyCommitted == (phase == 4), "real killed process retry and released lock");
        }
        IndependentStoreTests.Run(Check);
        Console.WriteLine($"CHECKPOINT_STORE PASS {checks} checks; process-crash model, not power-loss/mobile certification");
    }
}
sealed class MemoryFiles : ICheckpointFiles
{
    public readonly Dictionary<string, byte[]> Values = new(StringComparer.Ordinal);
    public int FailAt, Stage; private bool locked;
    public IDisposable AcquireWriter() { if (locked) throw new IOException("locked"); locked = true; return new Unlock(() => locked = false); }
    public string[] Names() => Values.Keys.ToArray();
    public long Length(string name) => Values.TryGetValue(name, out var b) ? b.Length : throw new FileNotFoundException(name);
    public byte[] Read(string name, int maximumBytes)
    {
        if (!Values.TryGetValue(name, out var b)) throw new FileNotFoundException(name);
        if (b.Length > maximumBytes) throw new IOException("read limit"); return b.ToArray();
    }
    public void Delete(string name) => Values.Remove(name);
    void Boundary() { if (++Stage == FailAt) throw new IOException("injected crash"); }
    public void AtomicWrite(string name, byte[] bytes, bool overwrite)
    {
        string temp = "temp-" + Guid.NewGuid().ToString("N"); Values[temp] = bytes.ToArray(); Boundary();
        Boundary(); // flushed/readback, before atomic rename
        if (!overwrite && Values.ContainsKey(name)) throw new IOException("immutable collision");
        Values[name] = Values[temp]; Values.Remove(temp); Boundary(); // commit happened but ack may be lost
    }
    sealed class Unlock(Action action) : IDisposable { public void Dispose() => action(); }
}

sealed class FaultFiles(ICheckpointFiles inner) : ICheckpointFiles
{
    public int Stage, FailAt;
    public IDisposable AcquireWriter() => inner.AcquireWriter();
    public string[] Names() => inner.Names();
    public long Length(string name) => inner.Length(name);
    public byte[] Read(string name, int maximumBytes) => inner.Read(name, maximumBytes);
    public void Delete(string name) => inner.Delete(name);
    private void Boundary() { if (++Stage == FailAt) throw new IOException("actual filesystem injected interruption"); }
    public void AtomicWrite(string name, byte[] bytes, bool overwrite) { Boundary(); inner.AtomicWrite(name, bytes, overwrite); Boundary(); }
}

sealed class PauseFiles(ICheckpointFiles inner, int pauseAt, string marker) : ICheckpointFiles
{
    private int stage;
    public IDisposable AcquireWriter() => inner.AcquireWriter();
    public string[] Names() => inner.Names();
    public long Length(string name) => inner.Length(name);
    public byte[] Read(string name, int maximumBytes) => inner.Read(name, maximumBytes);
    public void Delete(string name) => inner.Delete(name);
    private void Boundary() { if (++stage == pauseAt) { File.WriteAllText(marker, stage.ToString()); Thread.Sleep(Timeout.Infinite); } }
    public void AtomicWrite(string name, byte[] bytes, bool overwrite) { Boundary(); inner.AtomicWrite(name, bytes, overwrite); Boundary(); }
}
