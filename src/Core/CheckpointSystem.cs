using System;

namespace Hmi.Core
{
    internal static class CheckpointSystem
    {
        internal static bool ValidHash(string hash)
        {
            if (hash == null || hash.Length != 64) return false;
            foreach (char c in hash)
                if (!(c >= '0' && c <= '9') && !(c >= 'a' && c <= 'f') && !(c >= 'A' && c <= 'F')) return false;
            return true;
        }
        private static bool ValidIdentity(CheckpointIdentity identity) => identity != null && identity.RunId != Guid.Empty && identity.Sequence > 0;
        private static bool Fail(CheckpointError value, out CheckpointError error) { error = value; return false; }
        internal static bool TryCapture(GameSimulation g, CheckpointIdentity identity, out WaveCheckpointDto? dto, out CheckpointError error)
        {
            dto = null;
            if (g.Phase != RunPhase.Prepared) return Fail(CheckpointError.NotPrepared, out error);
            if (!g.Projectiles.EmptyForCheckpoint) return Fail(CheckpointError.InvalidTemporaryState, out error);
            if (g.CatalogHash == null) return Fail(CheckpointError.MissingCatalogIdentity, out error);
            if (!ValidIdentity(identity)) return Fail(CheckpointError.InvalidIdentity, out error);
            var cards = new CheckpointCount[g.Catalog.Cards.Length];
            for (int i = 0; i < cards.Length; i++) cards[i] = new CheckpointCount { Id = g.Catalog.Cards[i].Id, Count = g.Loadout.Levels[i] };
            var items = new CheckpointCount[g.Catalog.Items.Length];
            for (int i = 0; i < items.Length; i++) items[i] = new CheckpointCount { Id = g.Catalog.Items[i].Id, Count = g.Wallet.OwnedItems[i] };
            dto = new WaveCheckpointDto
            {
                CatalogHash = g.CatalogHash, RulesVersion = GameSimulation.CheckpointRulesVersion,
                RunId = identity.RunId, Sequence = identity.Sequence,
                EntityCapacity = g.Limits.EntityCapacity, EnemyCapacity = g.Limits.EnemyCapacity,
                CompanionCapacity = g.Limits.CompanionCapacity, EnemyHealthCap = g.Limits.EnemyHealthMultiplierCap,
                EnemySpeedCap = g.Limits.EnemySpeedMultiplierCap, CharacterId = g.CharacterId,
                Seed = g.Run.Seed, RandomState = g.Run.Random, Wave = g.Wave,
                FiniteCompleted = g.FiniteCompleted, IsEndless = g.IsEndless, EndlessWavesCompleted = g.EndlessWavesCompleted,
                CompanionsStationary = g.Stationary, EndlessOfferHistory = g.Run.EndlessOfferHistory,
                Coins = g.Coins, Earned = g.Earned, Spent = g.Spent, Kills = g.Kills,
                Cards = cards, Items = items, FinitePicks = g.Loadout.Picks.ToArray(),
                PendingContractId = g.PendingContractId, PendingLegacySupply = g.Run.PendingSupply
            };
            error = CheckpointError.None;
            return true;
        }
        internal static bool TryValidate(GameSimulation g, WaveCheckpointDto dto, CheckpointIdentity expected, out CheckpointError error)
            => TryBuild(g, dto, expected, false, out _, out error);
        internal static bool TryRestore(GameSimulation g, WaveCheckpointDto dto, CheckpointIdentity expected, out CheckpointError error)
        {
            if (g.Phase == RunPhase.Defeat || g.Phase == RunPhase.Finished)
                return Fail(CheckpointError.InvalidProgression, out error);
            if (!TryBuild(g, dto, expected, true, out GameSimulation? staged, out error)) return false;
            var ready = staged!;
            // All allocations, validation, arithmetic and derived-state calculation are complete.
            // These reference/value assignments cannot invoke caller code or expose a partial state on the single writer thread.
            ready.Projectiles.CopyGenerationBaseline(g.Projectiles);
            g.AdoptPreparedSystemStorage(ready);
            g.Projectiles = ready.Projectiles;
            g.World = ready.World;
            g.Run = ready.Run;
            g.Wallet = ready.Wallet;
            g.Loadout = ready.Loadout;
            g.BaseStats = ready.BaseStats;
            g.Stats = ready.Stats;
            g.Multipliers = ready.Multipliers;
            g.Stationary = ready.Stationary;
            g.MoveInput = default;
            g.MeleeTimer = g.RicochetTimer = g.HoneyTimer = g.ImpactTimer = g.SpawnTimer = 0;
            g.PowerTime = g.HasteTime = 0;
            g.SpawnCounter = 0;
            g.EventLength = 0;
            g.Observations.ResetScope();
            g.SpawnReservations.ResetScope();
            g.Commands.Clear();
            return true;
        }
        internal static bool TryConvertLegacy(GameSimulation g, WaveCheckpointDto source, CheckpointIdentity legacyIdentity,
            CheckpointIdentity targetIdentity, out WaveCheckpointDto? converted, out CheckpointError error)
        {
            // PROJECTILE-0001 P1: no implicit rule1/rule2 -> rule3 conversion. The explicit
            // preserved-slot consent adapter is a separate gate, not a constant-only migration.
            converted = null;
            return Fail(CheckpointError.RulesMismatch, out error);
        }
        private static bool TryBuild(GameSimulation g, WaveCheckpointDto source, CheckpointIdentity expected, bool restoring,
            out GameSimulation? staged, out CheckpointError error, string expectedRules = GameSimulation.CheckpointRulesVersion)
        {
            staged = null;
            if (g.CatalogHash == null) return Fail(CheckpointError.MissingCatalogIdentity, out error);
            if (source == null) return Fail(CheckpointError.InvalidData, out error);
            if (!ValidIdentity(expected) || source.RunId != expected.RunId || source.Sequence != expected.Sequence)
                return Fail(CheckpointError.InvalidIdentity, out error);
            if (source.SchemaVersion != 1) return Fail(CheckpointError.InvalidSchema, out error);
            if (!ValidHash(source.CatalogHash) || !string.Equals(source.CatalogHash, g.CatalogHash, StringComparison.OrdinalIgnoreCase))
                return Fail(CheckpointError.CatalogMismatch, out error);
            if (source.RulesVersion != expectedRules || source.EntityCapacity != g.Capacity
                || source.EnemyCapacity != g.Limits.EnemyCapacity || source.CompanionCapacity != g.Limits.CompanionCapacity
                || source.EnemyHealthCap != g.Limits.EnemyHealthMultiplierCap || source.EnemySpeedCap != g.Limits.EnemySpeedMultiplierCap)
                return Fail(CheckpointError.RulesMismatch, out error);
            if (restoring && (g.RunToken == int.MaxValue || !RunProgressionSystem.CanPrepare(g))) return Fail(CheckpointError.HandleLimit, out error);
            if (source.Cards == null || source.Cards.Length > g.Catalog.Cards.Length || source.Items == null
                || source.Items.Length > g.Catalog.Items.Length || source.FinitePicks == null || source.FinitePicks.Length > 5)
                return Fail(CheckpointError.InvalidData, out error);
            // Detach transport arrays and all scalar values before semantic validation; never retain caller data.
            WaveCheckpointDto d = Copy(source);
            if (d.Wave < 1 || (d.IsEndless
                ? d.Wave < 7 || !d.FiniteCompleted || d.EndlessWavesCompleted != d.Wave - 7
                : d.Wave > 6 || d.FiniteCompleted || d.EndlessWavesCompleted != 0))
                return Fail(CheckpointError.InvalidProgression, out error);
            if (d.RandomState == 0 || (d.Wave == 1 && d.RandomState != (d.Seed == 0 ? 1 : d.Seed)))
                return Fail(CheckpointError.InvalidRandom, out error);
            if ((!d.IsEndless && d.EndlessOfferHistory != 2166136261) || (d.Wave == 1 && d.CompanionsStationary))
                return Fail(CheckpointError.InvalidHistory, out error);
            if (d.CharacterId == null || !g.Catalog.CharacterIndex.ContainsKey(d.CharacterId)) return Fail(CheckpointError.InvalidCharacter, out error);
            if (d.Coins < 0 || d.Earned < 0 || d.Spent < 0 || d.Kills < 0 || (long)d.Earned - d.Spent != d.Coins || d.Kills > d.Earned)
                return Fail(CheckpointError.InvalidWallet, out error);
            if (d.Wave == 1 && (d.Coins != 0 || d.Earned != 0 || d.Spent != 0 || d.Kills != 0)) return Fail(CheckpointError.InvalidWallet, out error);
            var next = new GameSimulation(g.Catalog, g.Limits, g.CatalogHash);
            next.PrepareRun(d.CharacterId, d.Seed);
            next.Run.RunToken = restoring ? checked(g.RunToken + 1) : 1;
            next.Run.PreparationOrdinal = restoring ? checked(g.Run.PreparationOrdinal + 1) : 1;
            next.Run.Wave = d.Wave;
            next.Run.Random = d.RandomState;
            next.Run.FiniteCompleted = d.FiniteCompleted;
            next.Run.IsEndless = d.IsEndless;
            next.Run.EndlessWavesCompleted = d.EndlessWavesCompleted;
            next.Run.EndlessOfferHistory = d.EndlessOfferHistory;
            next.Stationary = d.CompanionsStationary;
            next.Wallet.Coins = d.Coins; next.Wallet.Earned = d.Earned; next.Wallet.Spent = d.Spent; next.Wallet.Kills = d.Kills;
            Array.Clear(next.Loadout.Levels, 0, next.Loadout.Levels.Length);
            var seenCards = new bool[next.Loadout.Levels.Length];
            long totalRanks = 0;
            foreach (var count in d.Cards)
            {
                if (count.Id == null || !g.Catalog.CardIndex.TryGetValue(count.Id, out int index) || seenCards[index]
                    || count.Count < 0 || count.Count > g.Catalog.Cards[index].Cap) return Fail(CheckpointError.InvalidCards, out error);
                seenCards[index] = true; next.Loadout.Levels[index] = count.Count; totalRanks += count.Count;
            }
            int root = g.Catalog.CardIndex[g.Catalog.Characters[next.Run.CharacterIndex].Root];
            if (next.Loadout.Levels[root] < 1 || totalRanks > d.Wave) return Fail(CheckpointError.InvalidCards, out error);
            for (int i = 0; i < next.Loadout.Levels.Length; i++)
            {
                if (next.Loadout.Levels[i] == 0) continue;
                foreach (var prerequisite in g.Catalog.Graph.All[i])
                    if (next.Loadout.Levels[prerequisite.Index] < prerequisite.Rank) return Fail(CheckpointError.InvalidCards, out error);
                if (g.Catalog.Graph.Any[i].Length > 0)
                {
                    bool found = false;
                    foreach (var prerequisite in g.Catalog.Graph.Any[i]) found |= next.Loadout.Levels[prerequisite.Index] >= prerequisite.Rank;
                    if (!found) return Fail(CheckpointError.InvalidCards, out error);
                }
                if (g.Catalog.Graph.Groups[i] >= 0)
                    for (int j = 0; j < i; j++)
                        if (g.Catalog.Graph.Groups[j] == g.Catalog.Graph.Groups[i] && next.Loadout.Levels[j] > 0)
                            return Fail(CheckpointError.InvalidCards, out error);
            }
            if (d.FinitePicks.Length != (d.IsEndless ? 5 : d.Wave - 1)) return Fail(CheckpointError.InvalidHistory, out error);
            var historyLevels = new int[next.Loadout.Levels.Length]; historyLevels[root] = 1;
            foreach (string pick in d.FinitePicks)
            {
                if (pick == null || !g.Catalog.CardIndex.TryGetValue(pick, out int index) || !g.Catalog.Graph.IsEligible(index, historyLevels))
                    return Fail(CheckpointError.InvalidHistory, out error);
                historyLevels[index]++;
                next.Loadout.Picks.Add(pick);
            }
            for (int i = 0; i < historyLevels.Length; i++)
                if (d.IsEndless ? historyLevels[i] > next.Loadout.Levels[i] : historyLevels[i] != next.Loadout.Levels[i])
                    return Fail(CheckpointError.InvalidHistory, out error);
            var seenItems = new bool[next.Wallet.OwnedItems.Length];
            long minimumSpent = 0;
            foreach (var count in d.Items)
            {
                if (count.Id == null || !g.Catalog.ItemIndex.TryGetValue(count.Id, out int index) || seenItems[index]
                    || count.Count < 0 || count.Count > g.Catalog.Items[index].Cap || count.Count > d.Wave - 1
                    || (g.Catalog.Items[index].Lifetime == "wave" && count.Count > 1)) return Fail(CheckpointError.InvalidItems, out error);
                long cost = (long)g.Catalog.Items[index].Price * count.Count;
                if (cost > d.Spent - minimumSpent) return Fail(CheckpointError.InvalidWallet, out error);
                minimumSpent += cost;
                seenItems[index] = true; next.Wallet.OwnedItems[index] = count.Count;
                next.Wallet.ActiveItems[index] = g.Catalog.Items[index].Lifetime == "run" ? count.Count : 0;
            }
            next.Loadout.CardPicked = d.Wave > 1;
            StatEffectSystem.Recalculate(next);
            if (d.PendingLegacySupply && d.PendingContractId != null) return Fail(CheckpointError.InvalidTemporaryState, out error);
            if ((d.PendingLegacySupply || d.PendingContractId != null) && d.Wave == 1) return Fail(CheckpointError.InvalidTemporaryState, out error);
            string? temporarySlot = null;
            if (d.PendingContractId != null)
            {
                int index = TemporaryContractSystem.Find(next, d.PendingContractId);
                if (index < 0 || !TemporaryContractSystem.IsEligible(next, index)) return Fail(CheckpointError.InvalidTemporaryState, out error);
                next.Run.PendingContract = index;
                temporarySlot = g.Catalog.Contracts[index].Slot;
            }
            else if (d.PendingLegacySupply)
            {
                if (g.Catalog.Contracts.Length != 0) return Fail(CheckpointError.InvalidTemporaryState, out error);
                next.Run.PendingSupply = true; temporarySlot = "trait";
            }
            if (temporarySlot != null)
                for (int i = 0; i < g.Catalog.Cards.Length; i++)
                    if (g.Catalog.Cards[i].Slot == temporarySlot && RunProgressionSystem.Availability(next, i) == CardAvailability.Available)
                        return Fail(CheckpointError.InvalidTemporaryState, out error);
            if (restoring) Array.Copy(g.World.Generation, next.World.Generation, g.Capacity);
            staged = next;
            error = CheckpointError.None;
            return true;
        }
        private static WaveCheckpointDto Copy(WaveCheckpointDto s)
        {
            var cards = new CheckpointCount[s.Cards.Length];
            for (int i = 0; i < cards.Length; i++) cards[i] = new CheckpointCount { Id = s.Cards[i]?.Id!, Count = s.Cards[i]?.Count ?? -1 };
            var items = new CheckpointCount[s.Items.Length];
            for (int i = 0; i < items.Length; i++) items[i] = new CheckpointCount { Id = s.Items[i]?.Id!, Count = s.Items[i]?.Count ?? -1 };
            return new WaveCheckpointDto
            {
                SchemaVersion = s.SchemaVersion, CatalogHash = s.CatalogHash, RulesVersion = s.RulesVersion,
                RunId = s.RunId, Sequence = s.Sequence, EntityCapacity = s.EntityCapacity, EnemyCapacity = s.EnemyCapacity,
                CompanionCapacity = s.CompanionCapacity, EnemyHealthCap = s.EnemyHealthCap, EnemySpeedCap = s.EnemySpeedCap,
                CharacterId = s.CharacterId, Seed = s.Seed, RandomState = s.RandomState, Wave = s.Wave,
                FiniteCompleted = s.FiniteCompleted, IsEndless = s.IsEndless, EndlessWavesCompleted = s.EndlessWavesCompleted,
                CompanionsStationary = s.CompanionsStationary, EndlessOfferHistory = s.EndlessOfferHistory,
                Coins = s.Coins, Earned = s.Earned, Spent = s.Spent, Kills = s.Kills,
                Cards = cards, Items = items, FinitePicks = (string[])s.FinitePicks.Clone(),
                PendingContractId = s.PendingContractId, PendingLegacySupply = s.PendingLegacySupply
            };
        }
    }
}
