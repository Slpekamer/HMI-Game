using System;
using System.Collections.Generic;

namespace Hmi.Core
{
    internal readonly struct CompiledContractEffect
    {
        internal readonly Stat Stat, RequiredEnabled;
        internal readonly float Fraction;
        internal CompiledContractEffect(Stat stat, float fraction, Stat enabled)
        { Stat = stat; Fraction = fraction; RequiredEnabled = enabled; }
    }
    internal sealed class CompiledContract
    {
        internal readonly string Id, Name, Slot, Description;
        internal readonly CompiledContractEffect[] Effects;
        internal CompiledContract(TemporaryContractDefinition source, CompiledContractEffect[] effects)
        { Id = source.Id; Name = source.Name; Slot = source.Slot; Description = source.Description; Effects = effects; }
        internal CardDefinition View() => new CardDefinition { Id = Id, Name = Name, Slot = Slot, Effect = Description };
    }
    internal static class TemporaryContractSystem
    {
        internal static CompiledContract[] Compile(GameContent content, Catalog catalog)
        {
            var input = content.TemporaryContracts;
            if (input == null || (content.SchemaVersion == 1 && input.Length > 0) || (input.Length != 0 && input.Length != 6))
                throw new ArgumentException("Temporary contracts require schema2 and exactly two per slot");
            var result = new CompiledContract[input.Length];
            var ids = new HashSet<string>(StringComparer.Ordinal);
            int trait = 0, basis = 0, risk = 0;
            for (int i = 0; i < input.Length; i++)
            {
                var c = input[i];
                if (c == null || string.IsNullOrWhiteSpace(c.Id) || string.IsNullOrWhiteSpace(c.Name) || string.IsNullOrWhiteSpace(c.Description) || !ids.Add(c.Id) || catalog.CardIndex.ContainsKey(c.Id)
                    || c.Id == EndlessProgressionSystem.SupplyId || c.Id.StartsWith("unavailable.", StringComparison.Ordinal)
                    || c.Effects == null || c.Effects.Length == 0 || c.Effects.Length > 8)
                    throw new ArgumentException("Invalid temporary contract");
                if (c.Slot == "trait") trait++; else if (c.Slot == "base") basis++; else if (c.Slot == "risk") risk++;
                else throw new ArgumentException("Invalid contract category");
                var effects = new CompiledContractEffect[c.Effects.Length];
                var used = new HashSet<Stat>();
                bool positive = false, negative = false;
                for (int e = 0; e < effects.Length; e++)
                {
                    var x = c.Effects[e];
                    if (x == null || !ParseStat(x.Stat, out Stat stat) || !Allowed(stat) || !used.Add(stat)
                        || float.IsNaN(x.PermanentFraction) || float.IsInfinity(x.PermanentFraction)
                        || x.PermanentFraction == 0 || Math.Abs(x.PermanentFraction) > .5f)
                        throw new ArgumentException("Invalid percent-of-permanent contract effect");
                    Stat enabled = Stat.Count;
                    if (x.RequiredEnabledStat != null)
                    {
                        if (!ParseStat(x.RequiredEnabledStat, out enabled)
                            || !((stat == Stat.RicochetRange && enabled == Stat.RicochetEnabled)
                                || (stat == Stat.HoneyRadius && enabled == Stat.HoneyEnabled)
                                || (stat == Stat.ImpactRange && enabled == Stat.ImpactEnabled)))
                            throw new ArgumentException("Invalid contract mechanism guard");
                    }
                    if ((stat == Stat.RicochetRange || stat == Stat.HoneyRadius || stat == Stat.ImpactRange) && enabled == Stat.Count)
                        throw new ArgumentException("Mechanism-specific contract range requires its enabled guard");
                    positive |= x.PermanentFraction > 0;
                    negative |= x.PermanentFraction < 0;
                    effects[e] = new CompiledContractEffect(stat, x.PermanentFraction, enabled);
                }
                if (!positive || (c.Slot == "risk" ? !negative : negative))
                    throw new ArgumentException("Contract needs a positive benefit and risk needs a real downside");
                result[i] = new CompiledContract(c, effects);
            }
            if (input.Length > 0 && (trait != 2 || basis != 2 || risk != 2)) throw new ArgumentException("Two contracts required per category");
            return result;
        }
        private static bool ParseStat(string text, out Stat stat) => Enum.TryParse(text, false, out stat)
            && stat != Stat.Count && Enum.IsDefined(typeof(Stat), stat) && Enum.GetName(typeof(Stat), stat) == text;
        private static bool Allowed(Stat stat) => stat == Stat.MaxHealth || stat == Stat.BasicRate || stat == Stat.BasicRange
            || stat == Stat.BasicDamage || stat == Stat.PlayerSpeed || stat == Stat.RicochetRange
            || stat == Stat.HoneyRadius || stat == Stat.ImpactRange;
        internal static float PermanentValue(GameSimulation g, Stat stat)
        {
            float value = StatEffectSystem.ProjectStat(g, stat, -1, true);
            return stat == Stat.PlayerSpeed ? value * StatEffectSystem.ProjectStat(g, Stat.PlayerRiskSpeed, -1, true) : value;
        }
        private static float NextWaveValue(GameSimulation g, Stat stat, int extraItem)
        {
            float value = StatEffectSystem.ProjectStat(g, stat, -1, false, extraItem);
            return stat == Stat.PlayerSpeed ? value * StatEffectSystem.ProjectStat(g, Stat.PlayerRiskSpeed, -1, false, extraItem) : value;
        }
        internal static bool IsEligible(GameSimulation g, int index, int extraItem = -1)
        {
            if (index < 0 || index >= g.Catalog.Contracts.Length) return false;
            bool benefit = false, downside = false;
            var contract = g.Catalog.Contracts[index];
            foreach (var effect in contract.Effects)
            {
                if (effect.RequiredEnabled != Stat.Count && StatEffectSystem.ProjectStat(g, effect.RequiredEnabled, -1, false, extraItem) <= 0) continue;
                float permanent = PermanentValue(g, effect.Stat);
                if (extraItem >= 0 && g.Catalog.Items[extraItem].Lifetime == "run")
                {
                    permanent = StatEffectSystem.ProjectStat(g, effect.Stat, -1, true, extraItem);
                    if (effect.Stat == Stat.PlayerSpeed) permanent *= StatEffectSystem.ProjectStat(g, Stat.PlayerRiskSpeed, -1, true, extraItem);
                }
                float delta = permanent * effect.Fraction;
                if (!StatValidation.InRange(effect.Stat, NextWaveValue(g, effect.Stat, extraItem) + delta)) return false;
                benefit |= delta > .00001f;
                downside |= delta < -.00001f;
            }
            return benefit && (contract.Slot != "risk" || downside);
        }
        internal static uint Mix(uint value)
        {
            value ^= value >> 16;
            value = unchecked(value * 0x7feb352d);
            value ^= value >> 15;
            value = unchecked(value * 0x846ca68b);
            return value ^ (value >> 16);
        }
        internal static int Find(GameSimulation g, string id)
        {
            for (int i = 0; i < g.Catalog.Contracts.Length; i++) if (g.Catalog.Contracts[i].Id == id) return i;
            return -1;
        }
        internal static void Apply(GameSimulation g)
        {
            if (g.Run.ActiveContract < 0) return;
            foreach (var effect in g.Catalog.Contracts[g.Run.ActiveContract].Effects)
            {
                if (effect.RequiredEnabled != Stat.Count && g.GetStat(effect.RequiredEnabled) <= 0) continue;
                g.Stats[(int)effect.Stat] += PermanentValue(g, effect.Stat) * effect.Fraction;
            }
        }
    }
}
