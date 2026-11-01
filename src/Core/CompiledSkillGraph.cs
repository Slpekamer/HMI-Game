using System;
using System.Collections.Generic;

namespace Hmi.Core
{
    internal readonly struct CompiledRequirement
    {
        internal readonly int Index, Rank;
        internal CompiledRequirement(int index, int rank) { Index = index; Rank = rank; }
    }

    /// <summary>Load-time graph compiler. Runtime queries inspect only compiled indices and ranks.</summary>
    internal sealed class CompiledSkillGraph
    {
        internal const int MaximumCards = 256;
        internal const int MaximumAssignments = 65536;
        private readonly CardDefinition[] _cards;
        internal readonly CompiledRequirement[][] All, Any;
        internal readonly int[] Groups;
        private readonly int[][] _members;
        private readonly int[] _topological;
        internal readonly int AssignmentCount;

        internal CompiledSkillGraph(Catalog catalog, int schema)
        {
            _cards = catalog.Cards;
            if (_cards.Length > MaximumCards) throw new ArgumentException("Skill graph exceeds 256-card compilation budget");
            All = new CompiledRequirement[_cards.Length][];
            Any = new CompiledRequirement[_cards.Length][];
            Groups = new int[_cards.Length];
            var names = new Dictionary<string, int>(StringComparer.Ordinal);
            var groups = new List<List<int>>();
            for (int i = 0; i < _cards.Length; i++)
            {
                var card = _cards[i];
                bool modern = card.RequiresAll != null || card.RequiresAny != null || card.ExclusiveGroup != null;
                if (schema == 1 && modern) throw new ArgumentException("Schema 1 does not accept graph-v2 fields");
                if (card.Parent != null && (card.RequiresAll != null || card.RequiresAny != null))
                    throw new ArgumentException("Mixed legacy and graph-v2 prerequisite syntax: " + card.Id);
                var seen = new HashSet<int>();
                All[i] = card.Parent == null
                    ? Compile(card.RequiresAll, i, catalog, seen)
                    : Compile(new[] { new RequirementDefinition { Id = card.Parent, Rank = card.ParentLevel } }, i, catalog, seen);
                Any[i] = Compile(card.RequiresAny, i, catalog, seen);
                if (All[i].Length + Any[i].Length > 16) throw new ArgumentException("Card exceeds 16-requirement budget");
                if (card.Slot != "trait" && (All[i].Length != 0 || Any[i].Length != 0 || card.ExclusiveGroup != null))
                    throw new ArgumentException("Only trait cards support prerequisites and exclusive groups");
                Groups[i] = -1;
                if (card.ExclusiveGroup == null) continue;
                if (!ValidGroupId(card.ExclusiveGroup)) throw new ArgumentException("Invalid exclusive group ID");
                if (!names.TryGetValue(card.ExclusiveGroup, out int group))
                {
                    group = groups.Count;
                    names.Add(card.ExclusiveGroup, group);
                    groups.Add(new List<int>());
                }
                Groups[i] = group;
                groups[group].Add(i);
            }
            _members = new int[groups.Count][];
            int assignments = 1;
            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i].Count < 2) throw new ArgumentException("Exclusive group requires at least two members");
                if (assignments > MaximumAssignments / (groups[i].Count + 1))
                    throw new ArgumentException("Skill graph exceeds 65536 exclusive-assignment validation budget");
                assignments *= groups[i].Count + 1;
                _members[i] = groups[i].ToArray();
            }
            AssignmentCount = assignments;
            var order = new List<int>(_cards.Length);
            var visit = new byte[_cards.Length];
            for (int i = 0; i < _cards.Length; i++) Visit(i, visit, order);
            _topological = order.ToArray();
            var everReachable = new bool[_cards.Length];
            VisitAssignments((reachable, selected) =>
            {
                ValidateTerminalPool(reachable, selected);
                for (int i = 0; i < reachable.Length; i++) everReachable[i] |= reachable[i];
            });
            for (int i = 0; i < everReachable.Length; i++)
                if (!everReachable[i]) throw new ArgumentException("Unreachable skill branch: " + _cards[i].Id);
        }

        // A maximal legal build must still contain the starting root plus five trait picks.
        // "None" is terminal only when no member of that unselected group can be acquired.
        private void ValidateTerminalPool(bool[] reachable, int[] selected)
        {
            for (int group = 0; group < selected.Length; group++)
            {
                if (selected[group] >= 0)
                {
                    if (!reachable[selected[group]]) return; // Not a legal acquired assignment.
                    continue;
                }
                foreach (int member in _members[group])
                {
                    bool allowed = true;
                    foreach (var requirement in All[member]) allowed &= reachable[requirement.Index];
                    if (Any[member].Length > 0)
                    {
                        bool alternative = false;
                        foreach (var requirement in Any[member]) alternative |= reachable[requirement.Index];
                        allowed &= alternative;
                    }
                    if (allowed) return; // This build can still select a group member.
                }
            }
            int capacity = 0;
            for (int i = 0; i < reachable.Length; i++)
                if (reachable[i] && _cards[i].Slot == "trait") capacity += _cards[i].Cap;
            if (capacity < 6) throw new ArgumentException("A terminal legal branch has insufficient five-wave trait capacity");
        }

        private static bool ValidGroupId(string id)
        {
            if (id.Length == 0 || id.Length > 64) return false;
            for (int i = 0; i < id.Length; i++)
            {
                char c = id[i];
                if (!(c >= 'a' && c <= 'z') && !(c >= 'A' && c <= 'Z')
                    && !(c >= '0' && c <= '9') && c != '.' && c != '_' && c != '-') return false;
            }
            return true;
        }
        private CompiledRequirement[] Compile(RequirementDefinition[]? input, int owner, Catalog catalog, HashSet<int> seen)
        {
            if (input == null) return Array.Empty<CompiledRequirement>();
            if (input.Length > 16) throw new ArgumentException("Card exceeds requirement compilation budget");
            var result = new CompiledRequirement[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                var requirement = input[i];
                if (requirement == null || requirement.Id == null
                    || !catalog.CardIndex.TryGetValue(requirement.Id, out int index) || index == owner
                    || requirement.Rank < 1 || requirement.Rank > _cards[index].Cap || !seen.Add(index)
                    || _cards[index].Slot != _cards[owner].Slot)
                    throw new ArgumentException("Unknown, repeated, self, cross-slot or invalid-rank prerequisite");
                result[i] = new CompiledRequirement(index, requirement.Rank);
            }
            return result;
        }
        private void Visit(int index, byte[] state, List<int> order)
        {
            if (state[index] == 2) return;
            if (state[index] == 1) throw new ArgumentException("Cyclic skill dependency, including alternative edges");
            state[index] = 1;
            foreach (var prerequisite in All[index]) Visit(prerequisite.Index, state, order);
            foreach (var prerequisite in Any[index]) Visit(prerequisite.Index, state, order);
            state[index] = 2;
            order.Add(index);
        }
        internal bool IsEligible(int index, int[] levels) => Availability(index, levels) == CardAvailability.Available;
        internal CardAvailability Availability(int index, int[] levels)
        {
            if (levels[index] >= _cards[index].Cap) return CardAvailability.Capped;
            int group = Groups[index];
            if (group >= 0)
                foreach (int sibling in _members[group])
                    if (sibling != index && levels[sibling] > 0) return CardAvailability.ExclusiveLocked;
            foreach (var requirement in All[index])
                if (levels[requirement.Index] < requirement.Rank) return CardAvailability.MissingPrerequisite;
            if (Any[index].Length > 0)
            {
                bool found = false;
                foreach (var requirement in Any[index]) found |= levels[requirement.Index] >= requirement.Rank;
                if (!found) return CardAvailability.MissingPrerequisite;
            }
            return CardAvailability.Available;
        }
        // Callback sees a reused mask. It must not retain or mutate it. Only used during catalog loading.
        internal void VisitAssignments(Action<bool[]> visitor) => VisitAssignments((reachable, selected) => visitor(reachable));
        private void VisitAssignments(Action<bool[], int[]> visitor)
        {
            var selected = new int[_members.Length];
            var reachable = new bool[_cards.Length];
            for (int assignment = 0; assignment < AssignmentCount; assignment++)
            {
                int code = assignment;
                for (int group = 0; group < _members.Length; group++)
                {
                    int option = code % (_members[group].Length + 1);
                    selected[group] = option == 0 ? -1 : _members[group][option - 1];
                    code /= _members[group].Length + 1;
                }
                Array.Clear(reachable, 0, reachable.Length);
                foreach (int index in _topological)
                {
                    int group = Groups[index];
                    if (group >= 0 && selected[group] != index) continue;
                    bool allowed = true;
                    foreach (var requirement in All[index]) allowed &= reachable[requirement.Index];
                    if (Any[index].Length > 0)
                    {
                        bool alternative = false;
                        foreach (var requirement in Any[index]) alternative |= reachable[requirement.Index];
                        allowed &= alternative;
                    }
                    reachable[index] = allowed;
                }
                visitor(reachable, selected);
            }
        }
    }
}
