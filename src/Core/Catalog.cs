using System;
using System.Collections.Generic;

namespace Hmi.Core
{
    internal sealed class Catalog
    {
        internal readonly CharacterDefinition[] Characters;
        internal readonly CardDefinition[] Cards;
        internal readonly ItemDefinition[] Items;
        internal readonly CompiledEffect[][] CardEffects, ItemEffects;
        internal readonly int[] Parents;
        internal readonly CompiledSkillGraph Graph;
        internal readonly CompiledContract[] Contracts;
        internal readonly Dictionary<string, int> CardIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        internal readonly Dictionary<string, int> ItemIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        internal readonly Dictionary<string, int> CharacterIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        internal Catalog(GameContent input)
        {
            if (input == null || (input.SchemaVersion != 1 && input.SchemaVersion != 2))
                throw new ArgumentException("Unsupported schema");
            if (input.Characters == null || input.Cards == null || input.Items == null
                || input.Characters.Length == 0 || input.Cards.Length == 0)
                throw new ArgumentException("Empty content");
            if (input.Cards.Length > CompiledSkillGraph.MaximumCards)
                throw new ArgumentException("Skill graph exceeds 256-card compilation budget");
            Characters = new CharacterDefinition[input.Characters.Length];
            Cards = new CardDefinition[input.Cards.Length];
            Items = new ItemDefinition[input.Items.Length];
            Parents = new int[Cards.Length];
            CardEffects = new CompiledEffect[Cards.Length][];
            ItemEffects = new CompiledEffect[Items.Length][];
            for (int i = 0; i < Cards.Length; i++)
            {
                var c = input.Cards[i];
                if (c == null || c.Id == EndlessProgressionSystem.SupplyId || c.Id == "unavailable.base" || c.Id == "unavailable.risk" || string.IsNullOrWhiteSpace(c.Id) || c.Cap < 1 || c.Cap > 99 || c.ParentLevel < 1
                    || !(c.Slot == "trait" || c.Slot == "base" || c.Slot == "risk") || CardIndex.ContainsKey(c.Id))
                    throw new ArgumentException("Invalid card");
                CardEffects[i] = Compile(c.Effects);
                Cards[i] = Copy(c);
                CardIndex.Add(c.Id, i);
            }
            Graph = new CompiledSkillGraph(this, input.SchemaVersion);
            int traits = 0, bases = 0, risks = 0;
            for (int i = 0; i < Cards.Length; i++)
            {
                Parents[i] = Cards[i].Parent == null ? -1 : CardIndex[Cards[i].Parent!];
                if (Cards[i].Slot == "trait") traits += Cards[i].Cap;
                else if (Cards[i].Slot == "base") bases += Cards[i].Cap;
                else risks += Cards[i].Cap;
            }
            if (traits < 6 || bases < 5 || risks < 5)
                throw new ArgumentException("Insufficient five-wave offer pools");
            for (int i = 0; i < Items.Length; i++)
            {
                var x = input.Items[i];
                if (x == null || string.IsNullOrWhiteSpace(x.Id) || x.Price < 0 || x.Cap < 1 || x.Cap > 99
                    || !Finite(x.Duration) || x.Duration < 0 || !(x.Lifetime == "wave" || x.Lifetime == "run")
                    || ItemIndex.ContainsKey(x.Id))
                    throw new ArgumentException("Invalid item");
                ItemEffects[i] = Compile(x.Effects);
                Items[i] = Copy(x);
                ItemIndex.Add(x.Id, i);
            }
            for (int i = 0; i < Characters.Length; i++)
            {
                var c = input.Characters[i];
                if (c == null || string.IsNullOrWhiteSpace(c.Id) || !CardIndex.TryGetValue(c.Root, out int root)
                    || Graph.All[root].Length != 0 || Graph.Any[root].Length != 0 || Graph.Groups[root] >= 0 || Cards[root].Slot != "trait" || c.InitialCompanions < 1
                    || c.InitialCompanions > 4 || CharacterIndex.ContainsKey(c.Id))
                    throw new ArgumentException("Invalid character");
                Characters[i] = new CharacterDefinition
                {
                    Id = c.Id,
                    Name = c.Name,
                    Root = c.Root,
                    VisualId = c.VisualId,
                    InitialCompanions = c.InitialCompanions
                };
                CharacterIndex.Add(c.Id, i);
            }
            StatValidation.Validate(this);
            Contracts = TemporaryContractSystem.Compile(input, this);
        }
        internal bool IsEligible(int index, int[] levels) => Graph.IsEligible(index, levels);
        private static bool Finite(float x) => !float.IsNaN(x) && !float.IsInfinity(x);
        private static CompiledEffect[] Compile(EffectDefinition[] effects)
        {
            if (effects == null)
                throw new ArgumentException("Missing effects");
            var result = new CompiledEffect[effects.Length];
            for (int i = 0; i < result.Length; i++)
            {
                var e = effects[i];
                if (e == null || !Enum.TryParse<Stat>(e.Stat, false, out var stat) || stat == Stat.Count
                    || !Enum.IsDefined(typeof(Stat), stat) || Enum.GetName(typeof(Stat), stat) != e.Stat
                    || !(e.Operation == "add" || e.Operation == "multiply")
                    || !Finite(e.Value) || Math.Abs(e.Value) > 1000 || (e.Operation == "multiply" && e.Value <= 0))
                    throw new ArgumentException("Invalid effect");
                result[i] = new CompiledEffect(stat, e.Operation == "multiply", e.Value);
            }
            return result;
        }
        internal static CardDefinition Copy(CardDefinition c) => new CardDefinition
        {
            Id = c.Id,
            Name = c.Name,
            Slot = c.Slot,
            Effect = c.Effect,
            Parent = string.IsNullOrEmpty(c.Parent) ? null : c.Parent,
            ParentLevel = c.ParentLevel,
            Cap = c.Cap,
            RequiresAll = CopyRequirements(c.RequiresAll),
            RequiresAny = CopyRequirements(c.RequiresAny),
            ExclusiveGroup = c.ExclusiveGroup,
            Effects = CopyEffects(c.Effects)
        };
        internal static ItemDefinition Copy(ItemDefinition x) => new ItemDefinition
        {
            Id = x.Id,
            Name = x.Name,
            Effect = x.Effect,
            Downside = x.Downside,
            Price = x.Price,
            Cap = x.Cap,
            Lifetime = x.Lifetime,
            Duration = x.Duration,
            Effects = CopyEffects(x.Effects)
        };
        private static RequirementDefinition[]? CopyRequirements(RequirementDefinition[]? source)
        {
            if (source == null) return null;
            var result = new RequirementDefinition[source.Length];
            for (int i = 0; i < result.Length; i++)
            {
                if (source[i] == null) throw new ArgumentException("Null requirement");
                result[i] = new RequirementDefinition { Id = source[i].Id, Rank = source[i].Rank };
            }
            return result;
        }
        private static EffectDefinition[] CopyEffects(EffectDefinition[] effects)
        {
            var result = new EffectDefinition[effects.Length];
            for (int i = 0; i < result.Length; i++)
                result[i] = new EffectDefinition
                {
                    Stat = effects[i].Stat,
                    Operation = effects[i].Operation,
                    Value = effects[i].Value
                };
            return result;
        }
    }
}
