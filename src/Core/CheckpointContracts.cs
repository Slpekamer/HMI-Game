using System;

namespace Hmi.Core
{
    public readonly struct PreparedWaveToken : IEquatable<PreparedWaveToken>
    {
        public int RunToken { get; }
        public int Wave { get; }
        public long Ordinal { get; }
        public PreparedWaveToken(int runToken, int wave, long ordinal)
        { RunToken = runToken; Wave = wave; Ordinal = ordinal; }
        public bool Equals(PreparedWaveToken other) => RunToken == other.RunToken && Wave == other.Wave && Ordinal == other.Ordinal;
        public override bool Equals(object? obj) => obj is PreparedWaveToken other && Equals(other);
        public override int GetHashCode() => unchecked((RunToken * 397 ^ Wave) * 397 ^ Ordinal.GetHashCode());
    }
    public sealed class CheckpointIdentity
    {
        public Guid RunId { get; }
        public long Sequence { get; }
        public CheckpointIdentity(Guid runId, long sequence) { RunId = runId; Sequence = sequence; }
    }
    public enum CheckpointError
    {
        None, NotPrepared, MissingCatalogIdentity, InvalidIdentity, CatalogMismatch, RulesMismatch,
        InvalidSchema, InvalidProgression, InvalidRandom, InvalidCharacter, InvalidCards, InvalidItems,
        InvalidWallet, InvalidHistory, InvalidTemporaryState, HandleLimit, InvalidData
    }
    public sealed class CheckpointCount
    {
        public string Id { get; set; } = "";
        public int Count { get; set; }
    }
    // Detached transport data; restore validates and copies every field before touching authority.
    public sealed class WaveCheckpointDto
    {
        public int SchemaVersion { get; set; } = 1;
        public string CatalogHash { get; set; } = "";
        public string RulesVersion { get; set; } = "";
        public Guid RunId { get; set; }
        public long Sequence { get; set; }
        public int EntityCapacity { get; set; }
        public int EnemyCapacity { get; set; }
        public int CompanionCapacity { get; set; }
        public float EnemyHealthCap { get; set; }
        public float EnemySpeedCap { get; set; }
        public string CharacterId { get; set; } = "";
        public uint Seed { get; set; }
        public uint RandomState { get; set; }
        public int Wave { get; set; }
        public bool FiniteCompleted { get; set; }
        public bool IsEndless { get; set; }
        public int EndlessWavesCompleted { get; set; }
        public bool CompanionsStationary { get; set; }
        public uint EndlessOfferHistory { get; set; }
        public int Coins { get; set; }
        public int Earned { get; set; }
        public int Spent { get; set; }
        public int Kills { get; set; }
        public CheckpointCount[] Cards { get; set; } = Array.Empty<CheckpointCount>();
        public CheckpointCount[] Items { get; set; } = Array.Empty<CheckpointCount>();
        public string[] FinitePicks { get; set; } = Array.Empty<string>();
        public string? PendingContractId { get; set; }
        public bool PendingLegacySupply { get; set; }
    }
}
