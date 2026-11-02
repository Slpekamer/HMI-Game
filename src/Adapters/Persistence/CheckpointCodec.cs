using System.Text.Json;
using System.Text.Json.Serialization;
using Hmi.Core;

namespace Hmi.Adapters.Persistence;

/// <summary>Strict bounded JSON boundary. Validation never restores or advances simulation.</summary>
public sealed class CheckpointCodec
{
    private readonly GameSimulation simulation;
    private static readonly JsonSerializerOptions Options = new()
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        MaxDepth = 16
    };
    private static readonly HashSet<string> Fields = typeof(WaveCheckpointDto).GetProperties()
        .Select(p => p.Name).ToHashSet(StringComparer.Ordinal);

    public CheckpointCodec(GameSimulation simulation) => this.simulation = simulation;

    public byte[] Encode(WaveCheckpointDto checkpoint) => JsonSerializer.SerializeToUtf8Bytes(checkpoint, Options);

    public bool TryDecode(byte[] bytes, SnapshotIdentity identity, out WaveCheckpointDto? checkpoint)
        => TryDecodeCore(bytes, identity, false, out checkpoint);

    public bool TryDecodeArchived(byte[] bytes, SnapshotIdentity identity, out WaveCheckpointDto? checkpoint)
        => TryDecodeCore(bytes, identity, true, out checkpoint);

    private bool TryDecodeCore(byte[] bytes, SnapshotIdentity identity, bool archived, out WaveCheckpointDto? checkpoint)
    {
        checkpoint = null;
        if (bytes == null || bytes.Length == 0 || bytes.Length > CheckpointStore.SnapshotLimit
            || identity.Catalog != simulation.CatalogHash || (archived ? identity.Rules != WaveRules.PriorVersion && identity.Rules != WaveRules.LegacyVersion : identity.Rules != GameSimulation.CheckpointRulesVersion)
            || !Guid.TryParseExact(identity.Run, "N", out var run) || run == Guid.Empty || run.ToString("N") != identity.Run) return false;
        try
        {
            using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 16 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return false;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in root.EnumerateObject())
                if (!Fields.Contains(property.Name) || !seen.Add(property.Name)) return false;
            if (seen.Count != Fields.Count) return false;
            foreach (string name in new[] { "Cards", "Items" })
            {
                if (root.GetProperty(name).ValueKind != JsonValueKind.Array) return false;
                foreach (var item in root.GetProperty(name).EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object) return false;
                    var names = item.EnumerateObject().Select(p => p.Name).ToArray();
                    if (names.Length != 2 || !names.Contains("Id") || !names.Contains("Count")) return false;
                }
            }
            var dto = JsonSerializer.Deserialize<WaveCheckpointDto>(bytes, Options);
            if (dto == null || dto.RulesVersion != identity.Rules) return false;
            bool valid = archived
                ? simulation.TryValidateArchivedCheckpoint(dto, new CheckpointIdentity(run, identity.Sequence), out _)
                : simulation.TryValidateCheckpoint(dto, new CheckpointIdentity(run, identity.Sequence), out _);
            if (!valid) return false;
            checkpoint = dto;
            return true;
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException or InvalidOperationException or OverflowException)
        { return false; }
    }

    public bool Validate(byte[] bytes, SnapshotIdentity identity) => TryDecode(bytes, identity, out _);
}
