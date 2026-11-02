namespace Hmi.Core
{
    /// <summary>Versioned authority timing. Presentation reads this policy; it never ends a wave.</summary>
    public static class WaveRules
    {
        public const int TicksPerSecond = 60;
        public const int DurationTicks = 60 * TicksPerSecond;
        public const int SpawnPreviewTicks = TicksPerSecond;
        public const float DurationSeconds = (float)DurationTicks / TicksPerSecond;
        // Structural checkpoint DTO remains schema1; this identifies changed gameplay semantics.
        public const string Version = "hmi-core-wave60-preview1-2";
        public const string LegacyVersion = "hmi-core-wave-start-1";
    }
}
