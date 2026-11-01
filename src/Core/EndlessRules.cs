using System;

namespace Hmi.Core
{
    public sealed class SimulationLimits
    {
        public int EntityCapacity { get; }
        public int EnemyCapacity { get; }
        public int CompanionCapacity { get; }
        public float EnemyHealthMultiplierCap { get; }
        public float EnemySpeedMultiplierCap { get; }
        public SimulationLimits(int entityCapacity = 512, int enemyCapacity = 256, int companionCapacity = 4,
            float enemyHealthMultiplierCap = 12, float enemySpeedMultiplierCap = 1.5f)
        {
            if (entityCapacity < 32 || entityCapacity > 4096 || enemyCapacity < 1 || enemyCapacity > entityCapacity - 7
                || companionCapacity < 1 || companionCapacity > 4
                || float.IsNaN(enemyHealthMultiplierCap) || float.IsInfinity(enemyHealthMultiplierCap) || enemyHealthMultiplierCap < 1 || enemyHealthMultiplierCap > 100
                || float.IsNaN(enemySpeedMultiplierCap) || float.IsInfinity(enemySpeedMultiplierCap) || enemySpeedMultiplierCap < 1 || enemySpeedMultiplierCap > 10)
                throw new ArgumentException("Invalid simulation limits");
            EntityCapacity = entityCapacity;
            EnemyCapacity = enemyCapacity;
            CompanionCapacity = companionCapacity;
            EnemyHealthMultiplierCap = enemyHealthMultiplierCap;
            EnemySpeedMultiplierCap = enemySpeedMultiplierCap;
        }
    }
    public enum RunTerminationReason { None, FinishedByPlayer, WaveLimit, LedgerLimit, HandleLimit }
    public enum OfferKind { PermanentCard, NextWaveSupply, Unavailable, TemporaryContract }
    public enum OfferUnavailableReason { None, Capped, RiskLimit, ContractLimit }
    public sealed class OfferSlotView
    {
        public string Slot { get; }
        public OfferKind Kind { get; }
        public CardDefinition Card { get; }
        public OfferUnavailableReason UnavailableReason { get; }
        public bool Enabled => Kind != OfferKind.Unavailable;
        internal OfferSlotView(string slot, OfferKind kind, CardDefinition card, OfferUnavailableReason reason = OfferUnavailableReason.None)
        {
            Slot = slot; Kind = kind; Card = card; UnavailableReason = reason;
        }
    }
    internal static class EndlessProgressionSystem
    {
        internal const string SupplyId = "supply.trait-next-wave";
        internal static float HealthScale(int wave) => 1 + Math.Min(990, wave <= 6 ? 0 : wave - 6) * .1f;
        internal static float SpeedScale(int wave) => 1 + Math.Min(900, wave <= 6 ? 0 : wave - 6) * .01f;
        internal static float HealthFactor(GameSimulation g, int wave, float raw) => g.IsEndless
            ? Math.Min(g.Limits.EnemyHealthMultiplierCap, raw * HealthScale(wave)) : raw;
        internal static float SpeedFactor(GameSimulation g, int wave, float raw) => g.IsEndless
            ? Math.Min(g.Limits.EnemySpeedMultiplierCap, raw * SpeedScale(wave)) : raw;
        internal static bool RiskFits(GameSimulation g, int card)
        {
            if (!g.IsEndless || g.Catalog.Cards[card].Slot != "risk") return true;
            int nextWave = g.Phase == RunPhase.Prepared || g.Wave == int.MaxValue ? g.Wave : g.Wave + 1;
            foreach (var effect in g.Catalog.CardEffects[card])
            {
                if (effect.Stat != Stat.EnemyHealth && effect.Stat != Stat.EnemySpeed) continue;
                float predicted = StatEffectSystem.ProjectStat(g, effect.Stat, card, false);
                float scale = effect.Stat == Stat.EnemyHealth ? HealthScale(nextWave) : SpeedScale(nextWave);
                float applied = effect.Stat == Stat.EnemyHealth ? HealthFactor(g, nextWave, predicted) : SpeedFactor(g, nextWave, predicted);
                if (applied + .00001f < predicted * scale) return false;
            }
            return true;
        }
        internal static void ExpireSupply(GameSimulation g)
        {
            if (g.Run.SupplyHealthBonus <= 0 && g.Run.ActiveContract < 0) return;
            g.Run.ActiveContract = -1;
            g.Run.SupplyHealthBonus = 0;
            for (int i = 0; i < g.Wallet.ActiveItems.Length; i++)
                if (g.Catalog.Items[i].Lifetime == "wave") g.Wallet.ActiveItems[i] = 0;
            StatEffectSystem.Recalculate(g);
            StatEffectSystem.SynchronizePlayerHealthLimit(g);
        }
        internal static void Terminate(GameSimulation g, RunTerminationReason reason)
        {
            ExpireSupply(g);
            g.Run.PendingContract = -1;
            g.Run.PendingSupply = false;
            g.Run.TerminationReason = reason;
            g.Run.Phase = RunPhase.Finished;
            g.MoveInput = default;
            g.Commands.Clear();
        }
    }
}
