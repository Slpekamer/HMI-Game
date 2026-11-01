using System;

namespace Hmi.Core
{
    internal static class StatDefaults
    {
        internal static void Fill(float[] values, int companions)
        {
            Array.Clear(values, 0, values.Length);
            values[(int)Stat.MaxHealth] = 100;
            values[(int)Stat.BasicRate] = 1;
            values[(int)Stat.BasicRange] = 1.84f;
            values[(int)Stat.BasicDamage] = 1.5f;
            values[(int)Stat.PlayerSpeed] = 1;
            values[(int)Stat.PlayerRiskSpeed] = 1;
            values[(int)Stat.EnemySpeed] = 1;
            values[(int)Stat.EnemyHealth] = 1;
            values[(int)Stat.KillReward] = 1;
            values[(int)Stat.OrbitCount] = 2;
            values[(int)Stat.OrbitRadius] = 3.04f;
            values[(int)Stat.RicochetBounces] = 1;
            values[(int)Stat.RicochetRange] = 4.8f;
            values[(int)Stat.HoneyRadius] = 4.12f;
            values[(int)Stat.HoneySlow] = .45f;
            values[(int)Stat.CompanionCount] = companions;
            values[(int)Stat.ImpactRange] = 2.72f;
            values[(int)Stat.ImpactForce] = 8.8f;
        }
    }
    internal sealed class StatEffectSystem : ISimulationSystem
    {
        public void Update(GameSimulation g)
        {
            for (int i = 0; i < g.Wallet.ActiveItems.Length; i++)
            {
                float duration = g.Catalog.Items[i].Duration;
                if (g.Wallet.ActiveItems[i] > 0 && duration > 0 && g.WaveSeconds >= duration
                    && (g.Tick - 1) * GameSimulation.FixedDelta < duration)
                {
                    Recalculate(g);
                    SynchronizePlayerHealthLimit(g);
                    break;
                }
            }
        }
        internal static void Recalculate(GameSimulation g)
        {
            StatDefaults.Fill(g.Stats, g.Catalog.Characters[g.Run.CharacterIndex].InitialCompanions);
            for (int i = 0; i < g.Multipliers.Length; i++)
                g.Multipliers[i] = 1;
            for (int i = 0; i < g.Loadout.Levels.Length; i++)
                Apply(g, g.Catalog.CardEffects[i], g.Loadout.Levels[i]);
            for (int i = 0; i < g.Wallet.ActiveItems.Length; i++)
            {
                if (g.Catalog.Items[i].Duration <= 0 || g.WaveSeconds < g.Catalog.Items[i].Duration)
                    Apply(g, g.Catalog.ItemEffects[i], g.Wallet.ActiveItems[i]);
            }
            for (int i = 0; i < g.Stats.Length; i++)
                g.Stats[i] *= g.Multipliers[i];
            g.Stats[(int)Stat.MaxHealth] += g.Run.SupplyHealthBonus;
            g.Stats[(int)Stat.PlayerSpeed] *= g.Stats[(int)Stat.PlayerRiskSpeed];
            TemporaryContractSystem.Apply(g);
            g.Stats[(int)Stat.CompanionCount] = Math.Min(4, g.Stats[(int)Stat.CompanionCount]);
            g.Stats[(int)Stat.HoneySlow] = Math.Max(.25f, g.Stats[(int)Stat.HoneySlow]);
        }
        internal static void SynchronizePlayerHealthLimit(GameSimulation g)
        {
            if (!g.World.Alive(g.PlayerId)) return;
            ref var health = ref g.World.Health[g.PlayerId.Index];
            health.Maximum = g.GetStat(Stat.MaxHealth);
            health.Current = Math.Min(health.Current, health.Maximum);
        }
        internal static float ProjectStat(GameSimulation g, Stat stat, int extraCard, bool permanentOnly, int extraItem = -1)
        {
            float value = g.BaseStats[(int)stat], multiplier = 1;
            for (int i = 0; i < g.Loadout.Levels.Length; i++)
                ProjectEffects(g.Catalog.CardEffects[i], stat, g.Loadout.Levels[i] + (i == extraCard ? 1 : 0), ref value, ref multiplier);
            for (int i = 0; i < g.Wallet.OwnedItems.Length; i++)
                if (!permanentOnly || g.Catalog.Items[i].Lifetime == "run")
                    ProjectEffects(g.Catalog.ItemEffects[i], stat, g.Wallet.OwnedItems[i] + (i == extraItem ? 1 : 0), ref value, ref multiplier);
            return value * multiplier;
        }
        private static void ProjectEffects(CompiledEffect[] effects, Stat stat, int level, ref float value, ref float multiplier)
        {
            if (level == 0) return;
            foreach (var effect in effects)
            {
                if (effect.Stat != stat) continue;
                if (effect.Multiply) multiplier *= (float)Math.Pow(effect.Value, level);
                else value += effect.Value * level;
            }
        }
        private static void Apply(GameSimulation g, CompiledEffect[] effects, int level)
        {
            if (level == 0)
                return;
            for (int i = 0; i < effects.Length; i++)
            {
                var effect = effects[i];
                if (effect.Multiply)
                    g.Multipliers[(int)effect.Stat] *= (float)Math.Pow(effect.Value, level);
                else
                    g.Stats[(int)effect.Stat] += effect.Value * level;
            }
        }
    }
    internal static class StatValidation
    {
        // Conservative interval analysis over all allowed stack subsets. No runtime reflection serialization.
        internal static void Validate(Catalog catalog)
        {
            foreach (var character in catalog.Characters)
            {
                var low = new float[(int)Stat.Count];
                var high = new float[(int)Stat.Count];
                var lowMul = new float[(int)Stat.Count];
                var highMul = new float[(int)Stat.Count];
                catalog.Graph.VisitAssignments(reachable =>
                {
                    StatDefaults.Fill(low, character.InitialCompanions);
                    StatDefaults.Fill(high, character.InitialCompanions);
                    for (int i = 0; i < low.Length; i++) lowMul[i] = highMul[i] = 1;
                    for (int i = 0; i < catalog.Cards.Length; i++)
                        if (reachable[i]) Accumulate(catalog.CardEffects[i], catalog.Cards[i].Cap, low, high, lowMul, highMul);
                    for (int i = 0; i < catalog.Items.Length; i++)
                        Accumulate(catalog.ItemEffects[i], catalog.Items[i].Cap, low, high, lowMul, highMul);
                    for (int i = 0; i < low.Length; i++)
                    {
                        float min = low[i] * lowMul[i], max = high[i] * highMul[i];
                        Bounds((Stat)i, out float lower, out float upper);
                        if (float.IsNaN(min) || float.IsNaN(max) || float.IsInfinity(min) || float.IsInfinity(max)
                            || min < lower - .00001f || max > upper + .00001f)
                            throw new ArgumentException("Unsafe effective stat range: " + (Stat)i);
                    }
                });
            }
        }
        private static void Accumulate(CompiledEffect[] effects, int cap,
            float[] low, float[] high, float[] lowMul, float[] highMul)
        {
            foreach (var e in effects)
            {
                bool integral = e.Stat == Stat.KillReward || e.Stat == Stat.OrbitCount
                    || e.Stat == Stat.RicochetBounces || e.Stat == Stat.CompanionCount;
                if (integral && e.Value != (float)Math.Round(e.Value))
                    throw new ArgumentException("Fractional count effect");
                int i = (int)e.Stat;
                if (e.Multiply)
                {
                    float factor = (float)Math.Pow(e.Value, cap);
                    if (factor < 1)
                        lowMul[i] *= factor;
                    else
                        highMul[i] *= factor;
                }
                else if (e.Value < 0)
                    low[i] += e.Value * cap;
                else
                    high[i] += e.Value * cap;
            }
        }
        internal static bool InRange(Stat stat, float value)
        {
            Bounds(stat, out float low, out float high);
            return !float.IsNaN(value) && !float.IsInfinity(value) && value >= low - .00001f && value <= high + .00001f;
        }
        private static void Bounds(Stat stat, out float low, out float high)
        {
            low = 0;
            high = 100;
            // Typed domain dispatch, never a card/character ID dispatch.
            switch (stat)
            {
                case Stat.MaxHealth:
                    low = 1;
                    high = 10000;
                    break;
                case Stat.BasicRate:
                    low = .05f;
                    high = 20;
                    break;
                case Stat.BasicRange:
                case Stat.OrbitRadius:
                case Stat.RicochetRange:
                case Stat.HoneyRadius:
                case Stat.ImpactRange:
                    low = .1f;
                    high = 50;
                    break;
                case Stat.BasicDamage:
                    high = 1000;
                    break;
                case Stat.PlayerSpeed:
                case Stat.PlayerRiskSpeed:
                case Stat.EnemySpeed:
                    low = .05f;
                    high = 10;
                    break;
                case Stat.EnemyHealth:
                    low = .05f;
                    high = 100;
                    break;
                case Stat.KillReward:
                    low = 1;
                    high = 1000;
                    break;
                case Stat.OrbitCount:
                case Stat.RicochetBounces:
                case Stat.CompanionCount:
                    low = 1;
                    high = 4;
                    break;
                case Stat.HoneySlow:
                    low = .25f;
                    high = 1;
                    break;
                case Stat.ImpactForce:
                    high = 50;
                    break;
                case Stat.OrbitEnabled:
                case Stat.RicochetEnabled:
                case Stat.HoneyEnabled:
                case Stat.CompanionEnabled:
                case Stat.ImpactEnabled:
                    high = 10;
                    break;
            }
        }
    }
}
