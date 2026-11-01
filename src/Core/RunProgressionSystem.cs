using System;
using System.Globalization;

namespace Hmi.Core
{
    internal sealed class RunProgressionSystem : ISimulationSystem
    {
        public void Update(GameSimulation g)
        {
            int count = g.Commands.Count;
            for (int i = 0; i < count && g.Commands.Count > 0; i++)
            {
                var command = g.Commands.Dequeue();
                if (command.RunToken == g.RunToken && command.Wave == g.Wave)
                    Consume(g, command);
            }
        }
        internal static void StartRun(GameSimulation g, string characterId, uint seed)
        {
            PrepareRun(g, characterId, seed);
            if (!BeginPreparedWave(g, g.PreparedToken)) throw new InvalidOperationException("Prepared initialization rejected");
        }
        internal static void PrepareRun(GameSimulation g, string characterId, uint seed)
        {
            if (!g.Catalog.CharacterIndex.TryGetValue(characterId, out int character))
                throw new ArgumentException("Unknown character", nameof(characterId));
            if (g.Run.RunToken == int.MaxValue || !CanPrepare(g))
                throw new InvalidOperationException("Session or entity handles exhausted; create a new simulation");
            g.Run.CharacterIndex = character;
            StatDefaults.Fill(g.BaseStats, g.Catalog.Characters[character].InitialCompanions);
            g.Run.FiniteCompleted = g.Run.IsEndless = g.Run.PendingSupply = false;
            g.Run.EndlessWavesCompleted = 0;
            g.Run.EndlessOfferHistory = 2166136261;
            g.Run.SupplyHealthBonus = 0;
            g.Run.PendingContract = g.Run.ActiveContract = -1;
            g.Run.TerminationReason = RunTerminationReason.None;
            g.Run.Seed = seed;
            g.Run.Random = seed == 0 ? 1 : seed;
            g.Run.RunToken++;
            g.Run.Wave = 1;
            g.Wallet.Coins = g.Wallet.Earned = g.Wallet.Spent = g.Wallet.Kills = 0;
            g.Stationary = false;
            g.Commands.Clear();
            g.Loadout.Picks.Clear();
            Array.Clear(g.Loadout.Levels, 0, g.Loadout.Levels.Length);
            Array.Clear(g.Wallet.OwnedItems, 0, g.Wallet.OwnedItems.Length);
            Array.Clear(g.Wallet.ActiveItems, 0, g.Wallet.ActiveItems.Length);
            g.Loadout.Levels[g.Catalog.CardIndex[g.Catalog.Characters[character].Root]] = 1;
            EnterPrepared(g);
        }
        private static void Consume(GameSimulation g, GameCommand command)
        {
            switch (command.Kind)
            {
                case CommandKind.Pause:
                    if (g.Phase == RunPhase.Combat)
                    {
                        g.Run.Paused = command.Flag;
                        g.MoveInput = default;
                    }
                    break;
                case CommandKind.Move:
                    if (g.Phase == RunPhase.Combat && !g.Paused && Finite(command.Direction.X) && Finite(command.Direction.Y))
                        g.MoveInput = command.Direction.LengthSquared > 1 ? command.Direction.Normalized : command.Direction;
                    break;
                case CommandKind.Choose:
                    Choose(g, command.Id);
                    break;
                case CommandKind.Buy:
                    Buy(g, command.Id);
                    break;
                case CommandKind.NextWave:
                    if (PrepareNextWave(g)) BeginPreparedWave(g, g.PreparedToken);
                    break;
                case CommandKind.ContinueEndless:
                    if (g.Phase == RunPhase.Victory && g.FiniteCompleted && !g.IsEndless)
                    {
                        g.Run.IsEndless = true;
                        g.Run.Phase = RunPhase.Between;
                        g.Loadout.CardPicked = false;
                        foreach (string pick in g.Loadout.Picks)
                            for (int i = 0; i < pick.Length; i++) g.Run.EndlessOfferHistory = unchecked((g.Run.EndlessOfferHistory ^ pick[i]) * 16777619);
                        BuildOffers(g);
                    }
                    break;
                case CommandKind.FinishRun:
                    if (g.Phase == RunPhase.Prepared || g.Phase == RunPhase.Victory || (g.IsEndless && g.Phase == RunPhase.Between))
                        EndlessProgressionSystem.Terminate(g, RunTerminationReason.FinishedByPlayer);
                    break;
                case CommandKind.CompanionMode:
                    if (g.Phase == RunPhase.Combat)
                        g.Stationary = command.Flag;
                    break;
                case CommandKind.Recall:
                    if (g.Phase == RunPhase.Combat && !g.Paused)
                        g.RecallCompanions();
                    break;
            }
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        internal static bool CanPrepare(GameSimulation g)
        {
            return g.Run.PreparationOrdinal < long.MaxValue && CanInitialize(g);
        }
        internal static bool CanInitialize(GameSimulation g)
        {
            for (int i = 0; i < g.Capacity; i++)
                if (g.World.Generation[i] < 0 || g.World.Generation[i] == int.MaxValue) return false;
            return true;
        }
        internal static bool PrepareNextWave(GameSimulation g)
        {
            if (g.Phase != RunPhase.Between || !g.CardPicked) return false;
            if (g.Run.PendingContract >= 0 && !TemporaryContractSystem.IsEligible(g, g.Run.PendingContract)) return false;
            if (g.Wave == int.MaxValue)
            {
                EndlessProgressionSystem.Terminate(g, RunTerminationReason.WaveLimit);
                return false;
            }
            if (!CanPrepare(g))
            {
                EndlessProgressionSystem.Terminate(g, RunTerminationReason.HandleLimit);
                return false;
            }
            g.Run.Wave = checked(g.Wave + 1);
            EnterPrepared(g);
            return true;
        }
        internal static void EnterPrepared(GameSimulation g)
        {
            g.World.Clear();
            g.Run.PlayerId = EntityId.None;
            g.Run.Phase = RunPhase.Prepared;
            g.Run.PreparationOrdinal++;
            g.Run.Tick = 0;
            g.Run.WaveSeconds = 0;
            g.Run.Paused = false;
            g.Run.ActiveContract = -1;
            g.Run.SupplyHealthBonus = 0;
            g.Commands.Clear();
            g.EventLength = 0;
            g.MoveInput = default;
            g.PowerTime = g.HasteTime = 0;
            g.MeleeTimer = g.RicochetTimer = g.HoneyTimer = g.ImpactTimer = g.SpawnTimer = 0;
            g.SpawnCounter = 0;
            g.Loadout.OfferViews = Array.Empty<CardDefinition>();
            g.Loadout.OfferSlots = Array.Empty<OfferSlotView>();
            for (int i = 0; i < g.Loadout.Offers.Length; i++) g.Loadout.Offers[i] = -1;
            for (int i = 0; i < g.Wallet.ActiveItems.Length; i++)
                g.Wallet.ActiveItems[i] = g.Catalog.Items[i].Lifetime == "run" ? g.Wallet.OwnedItems[i] : 0;
            StatEffectSystem.Recalculate(g);
        }
        internal static bool BeginPreparedWave(GameSimulation g, PreparedWaveToken token)
        {
            if (g.Phase != RunPhase.Prepared || !g.PreparedToken.Equals(token) || !CanInitialize(g)) return false;
            if (g.Run.PendingContract >= 0 && !TemporaryContractSystem.IsEligible(g, g.Run.PendingContract)) return false;
            for (int i = 0; i < g.Wallet.OwnedItems.Length; i++)
            {
                g.Wallet.ActiveItems[i] = g.Wallet.OwnedItems[i];
                if (g.Catalog.Items[i].Lifetime == "wave") g.Wallet.OwnedItems[i] = 0;
            }
            BeginWave(g);
            return true;
        }
        private static void BeginWave(GameSimulation g)
        {
            g.World.Clear();
            g.EventLength = 0;
            g.Run.Phase = RunPhase.Combat;
            g.Run.Paused = false;
            g.Run.Tick = 0;
            g.Run.WaveSeconds = 0;
            g.Loadout.CardPicked = false;
            g.MoveInput = default;
            g.PowerTime = g.HasteTime = 0;
            g.MeleeTimer = g.RicochetTimer = g.HoneyTimer = g.ImpactTimer = g.SpawnTimer = 0;
            g.SpawnCounter = 0;
            for (int i = 0; i < g.Wallet.Stock.Length; i++)
                g.Wallet.Stock[i] = 1;
            g.Run.ActiveContract = g.Run.PendingContract;
            g.Run.PendingContract = -1;
            g.Run.SupplyHealthBonus = 0;
            if (g.Run.PendingSupply)
            {
                g.Run.SupplyHealthBonus = StatEffectSystem.ProjectStat(g, Stat.MaxHealth, -1, true) * .1f;
                g.Run.PendingSupply = false;
            }
            StatEffectSystem.Recalculate(g);
            g.Run.PlayerId = g.World.Create(Components.Player | Components.Health, new Vec2(2, 0), .48f,
                g.Catalog.Characters[g.Run.CharacterIndex].VisualId, g.GetStat(Stat.MaxHealth));
            int companions = g.GetStat(Stat.CompanionEnabled) > 0 ? Math.Min(g.Limits.CompanionCapacity, (int)g.GetStat(Stat.CompanionCount)) : 0;
            for (int i = 0; i < companions; i++)
            {
                var position = new Vec2(i % 2 == 0 ? -.9f : 2.7f, 1.8f + i / 2 * 1.6f);
                var id = g.World.Create(Components.Companion, position, .35f,
                    i % 2 == 0 ? "companion_shooter" : "companion_support");
                g.World.Companion[id.Index] = new CompanionComponent
                {
                    Anchor = position,
                    Cooldown = .15f + i * .25f,
                    Support = i % 2 == 1
                };
            }
            var power = g.World.Create(Components.Pickup, new Vec2(3, -1.4f), .3f, "pickup_power");
            g.World.Pickup[power.Index].Type = 0;
            var haste = g.World.Create(Components.Pickup, new Vec2(-2.2f, 1.1f), .3f, "pickup_haste");
            g.World.Pickup[haste.Index].Type = 1;
            for (int i = 0; i < Math.Min(g.Limits.EnemyCapacity, 8 + Math.Min(g.Wave, 1024) * 2); i++)
                g.SpawnEnemy();
        }
        internal static void EndTick(GameSimulation g)
        {
            if (g.Phase != RunPhase.Combat) return;
            if (g.World.Alive(g.PlayerId) && g.World.Health[g.PlayerId.Index].Current <= 0)
            {
                EndlessProgressionSystem.ExpireSupply(g);
                g.Run.Phase = RunPhase.Defeat;
                g.MoveInput = default;
            }
            else if (g.Tick >= 1440)
                FinishWave(g);
        }
        internal static void FinishWave(GameSimulation g)
        {
            if (g.Phase != RunPhase.Combat)
                return;
            EndlessProgressionSystem.ExpireSupply(g);
            if (g.IsEndless)
            {
                if (g.Run.EndlessWavesCompleted == int.MaxValue)
                {
                    EndlessProgressionSystem.Terminate(g, RunTerminationReason.WaveLimit);
                    return;
                }
                g.Run.EndlessWavesCompleted = checked(g.Run.EndlessWavesCompleted + 1);
            }
            else if (g.Wave >= GameSimulation.TotalWaves) g.Run.FiniteCompleted = true;
            g.Run.Phase = !g.IsEndless && g.Wave >= GameSimulation.TotalWaves ? RunPhase.Victory : RunPhase.Between;
            g.MoveInput = default;
            g.Emit(EventKind.WaveEnded, g.PlayerId, EntityId.None, g.PlayerPosition, g.PlayerPosition, g.Wave);
            if (g.Phase == RunPhase.Between)
                BuildOffers(g);
        }
        internal static CardAvailability Availability(GameSimulation g, int index)
        {
            var result = g.Catalog.Graph.Availability(index, g.Loadout.Levels);
            return result == CardAvailability.Available && !EndlessProgressionSystem.RiskFits(g, index)
                ? CardAvailability.RiskLimit : result;
        }
        private static bool Eligible(GameSimulation g, int index) => Availability(g, index) == CardAvailability.Available;
        private static void BuildOffers(GameSimulation g)
        {
            string[] slots = { "trait", "base", "risk" };
            var views = new CardDefinition[3];
            var slotViews = new OfferSlotView[3];
            for (int slot = 0; slot < slots.Length; slot++)
            {
                int count = 0;
                for (int i = 0; i < g.Catalog.Cards.Length; i++)
                    if (g.Catalog.Cards[i].Slot == slots[slot] && Eligible(g, i))
                        count++;
                if (count == 0)
                {
                    if (g.Catalog.Contracts.Length > 0)
                    {
                        int valid = 0;
                        for (int i = 0; i < g.Catalog.Contracts.Length; i++)
                            if (g.Catalog.Contracts[i].Slot == slots[slot] && TemporaryContractSystem.IsEligible(g, i)) valid++;
                        if (valid > 0)
                        {
                            uint rotation = TemporaryContractSystem.Mix(unchecked(g.Run.EndlessOfferHistory ^ g.Run.Seed ^ ((uint)g.Wave * 0x9e3779b9) ^ ((uint)slot << 24)));
                            int chosen = (int)(rotation % (uint)valid);
                            for (int i = 0; i < g.Catalog.Contracts.Length; i++)
                            {
                                if (g.Catalog.Contracts[i].Slot != slots[slot] || !TemporaryContractSystem.IsEligible(g, i) || chosen-- != 0) continue;
                                g.Loadout.Offers[slot] = -3 - i;
                                views[slot] = g.Catalog.Contracts[i].View();
                                slotViews[slot] = new OfferSlotView(slots[slot], OfferKind.TemporaryContract, views[slot]);
                                break;
                            }
                            continue;
                        }
                        // Custom catalogs fail closed instead of displaying a zero-effect buff.
                        g.Loadout.Offers[slot] = -1;
                        views[slot] = new CardDefinition { Id = "unavailable." + slots[slot], Slot = slots[slot], Name = "临时契约不可用", Effect = "当前构筑已达效果安全边界，无法提供有效临时增益。" };
                        slotViews[slot] = new OfferSlotView(slots[slot], OfferKind.Unavailable, views[slot], OfferUnavailableReason.ContractLimit);
                        continue;
                    }
                    bool supply = slot == 0;
                    bool riskLimit = false;
                    for (int i = 0; i < g.Catalog.Cards.Length; i++)
                        if (g.Catalog.Cards[i].Slot == slots[slot] && Availability(g, i) == CardAvailability.RiskLimit) riskLimit = true;
                    views[slot] = new CardDefinition
                    {
                        Id = supply ? EndlessProgressionSystem.SupplyId : "unavailable." + slots[slot], Slot = slots[slot],
                        Name = supply ? "下波临时护体" : "该类选项已达上限",
                        Effect = supply ? "下一波初始化时，按永久最大生命的10%增加最大与当前生命一次；波末撤销并钳制当前生命，不永久叠加。"
                            : riskLimit ? "敌方强化已达上限，无法提供此风险选项。" : "该类永久升级已全部封顶。"
                    };
                    g.Loadout.Offers[slot] = supply ? -2 : -1;
                    slotViews[slot] = new OfferSlotView(slots[slot], supply ? OfferKind.NextWaveSupply : OfferKind.Unavailable,
                        views[slot], supply ? OfferUnavailableReason.None : riskLimit ? OfferUnavailableReason.RiskLimit : OfferUnavailableReason.Capped);
                    continue;
                }
                string key = g.Run.Seed.ToString(CultureInfo.InvariantCulture) + "|" + g.CharacterId + "|"
                    + string.Join(",", g.Loadout.Picks) + "|" + slots[slot];
                uint hash = 2166136261;
                for (int i = 0; i < key.Length; i++)
                    hash = unchecked((hash ^ key[i]) * 16777619);
                if (g.IsEndless)
                {
                    hash = unchecked((g.Run.EndlessOfferHistory ^ (uint)g.Wave) * 16777619);
                    hash = unchecked((hash ^ (uint)slot) * 16777619);
                }
                int choice = (int)(hash % (uint)count);
                for (int i = 0; i < g.Catalog.Cards.Length; i++)
                {
                    if (g.Catalog.Cards[i].Slot != slots[slot] || !Eligible(g, i) || choice-- != 0)
                        continue;
                    g.Loadout.Offers[slot] = i;
                    views[slot] = Catalog.Copy(g.Catalog.Cards[i]);
                    slotViews[slot] = new OfferSlotView(slots[slot], OfferKind.PermanentCard, views[slot]);
                    break;
                }
            }
            g.Loadout.OfferViews = Array.AsReadOnly(views);
            g.Loadout.OfferSlots = Array.AsReadOnly(slotViews);
        }
        private static void Choose(GameSimulation g, string id)
        {
            if (g.Phase != RunPhase.Between || g.CardPicked || id == null) return;
            int contract = TemporaryContractSystem.Find(g, id);
            if (contract >= 0)
            {
                int code = -3 - contract;
                if ((g.Loadout.Offers[0] != code && g.Loadout.Offers[1] != code && g.Loadout.Offers[2] != code)
                    || !TemporaryContractSystem.IsEligible(g, contract)) return;
                g.Run.PendingSupply = false;
                g.Run.PendingContract = contract;
                g.Loadout.CardPicked = true;
                RecordPick(g, id);
                return;
            }
            if (id == EndlessProgressionSystem.SupplyId && g.Loadout.Offers[0] == -2)
            {
                g.Run.PendingSupply = true;
                g.Loadout.CardPicked = true;
                RecordPick(g, id);
                return;
            }
            if (!g.Catalog.CardIndex.TryGetValue(id, out int index) || !Eligible(g, index)) return;
            if (g.Loadout.Offers[0] != index && g.Loadout.Offers[1] != index && g.Loadout.Offers[2] != index)
                return;
            g.Loadout.Levels[index]++;
            RecordPick(g, id);
            g.Loadout.CardPicked = true;
            StatEffectSystem.Recalculate(g);
        }
        private static void RecordPick(GameSimulation g, string id)
        {
            if (!g.IsEndless) g.Loadout.Picks.Add(id);
            else for (int i = 0; i < id.Length; i++) g.Run.EndlessOfferHistory = unchecked((g.Run.EndlessOfferHistory ^ id[i]) * 16777619);
        }
        internal static bool CanBuy(GameSimulation g, string id)
        {
            if (g.Phase != RunPhase.Between || id == null || !g.Catalog.ItemIndex.TryGetValue(id, out int index)
                || g.Wallet.Stock[index] <= 0 || g.Coins < g.Catalog.Items[index].Price || g.Wallet.OwnedItems[index] >= g.Catalog.Items[index].Cap) return false;
            return g.Run.PendingContract < 0 || TemporaryContractSystem.IsEligible(g, g.Run.PendingContract, index);
        }
        private static void Buy(GameSimulation g, string id)
        {
            if (!CanBuy(g, id))
                return;
            int index = g.Catalog.ItemIndex[id];
            if (g.Wallet.Spent > int.MaxValue - g.Catalog.Items[index].Price)
            {
                EndlessProgressionSystem.Terminate(g, RunTerminationReason.LedgerLimit);
                return;
            }
            g.Wallet.Coins -= g.Catalog.Items[index].Price;
            g.Wallet.Spent += g.Catalog.Items[index].Price;
            g.Wallet.Stock[index]--;
            g.Wallet.OwnedItems[index]++;
            if (!g.CardPicked && g.Catalog.Contracts.Length > 0) BuildOffers(g);
        }
        internal static void RewardDeath(GameSimulation g, int target, EntityId source)
        {
            int reward = (int)g.GetStat(Stat.KillReward);
            if (g.Wallet.Coins > int.MaxValue - reward || g.Wallet.Earned > int.MaxValue - reward || g.Wallet.Kills == int.MaxValue)
            {
                EndlessProgressionSystem.Terminate(g, RunTerminationReason.LedgerLimit);
                return;
            }
            g.Wallet.Coins = checked(g.Wallet.Coins + reward);
            g.Wallet.Earned = checked(g.Wallet.Earned + reward);
            g.Wallet.Kills = checked(g.Wallet.Kills + 1);
            Vec2 position = g.World.Transform[target].Position;
            g.Emit(EventKind.Death, source, g.World.Id(target), position, position, reward);
        }
    }
}
