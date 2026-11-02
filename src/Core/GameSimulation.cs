using System;
using System.Collections.Generic;

namespace Hmi.Core
{
    // Composition, schedule and read-only boundary. Rule mutation lives in explicit systems.
    public sealed class GameSimulation
    {
        public const float FixedDelta = 1f / WaveRules.TicksPerSecond;
        public const float WaveDuration = WaveRules.DurationSeconds;
        public const int TotalWaves = 6;
        public const float ArenaWidth = 28f, ArenaHeight = 16f;
        internal World World;
        internal readonly Catalog Catalog;
        internal RunProgressionResource Run = new RunProgressionResource();
        internal WalletResource Wallet;
        internal CardLoadoutResource Loadout;
        internal float[] BaseStats = new float[(int)Stat.Count];
        public SimulationLimits Limits { get; }
        public string? CatalogHash { get; }
        public const string CheckpointRulesVersion = WaveRules.Version;
        internal float[] Stats = new float[(int)Stat.Count];
        internal float[] Multipliers = new float[(int)Stat.Count];
        internal readonly Queue<GameCommand> Commands = new Queue<GameCommand>(256);
        internal Vec2 MoveInput;
        internal float MeleeTimer, RicochetTimer, HoneyTimer, ImpactTimer, SpawnTimer;
        internal float PowerTime, HasteTime;
        internal bool Stationary;
        internal int SpawnCounter;
        internal int EventLength;
        private readonly GameEvent[] _events = new GameEvent[2048];
        internal readonly CombatObservationStorage Observations;
        internal readonly SpawnReservationStorage SpawnReservations;
        public const int CombatObservationSchemaVersion = 1;
        private readonly RunProgressionSystem _progression = new RunProgressionSystem();
        private readonly StatEffectSystem _statEffects = new StatEffectSystem();
        private ISimulationSystem[] _systems;
        private readonly IReadOnlyList<Wall> _walls = Array.AsReadOnly(new[]
        {
            new Wall(-7.4f, -3.8f, 1.04f, 7.6f), new Wall(6.52f, -3.8f, 1.04f, 7.6f)
        });
        public RunPhase Phase => Run.Phase;
        public bool FiniteCompleted => Run.FiniteCompleted;
        public bool IsEndless => Run.IsEndless;
        public int EndlessWavesCompleted => Run.EndlessWavesCompleted;
        public RunTerminationReason TerminationReason => Run.TerminationReason;
        public bool PendingNextWaveSupply => Run.PendingSupply;
        public string? PendingContractId => Run.PendingContract < 0 ? null : Catalog.Contracts[Run.PendingContract].Id;
        public string? ActiveContractId => Run.ActiveContract < 0 ? null : Catalog.Contracts[Run.ActiveContract].Id;
        public float ActiveSupplyHealth => Run.SupplyHealthBonus;
        public const string NextWaveSupplyId = EndlessProgressionSystem.SupplyId;
        public float EffectiveEnemyHealthMultiplier => EndlessProgressionSystem.HealthFactor(this, Wave, GetStat(Stat.EnemyHealth));
        public float EffectiveEnemySpeedMultiplier => EndlessProgressionSystem.SpeedFactor(this, Wave, GetStat(Stat.EnemySpeed));
        public long Tick => Run.Tick;
        public int RunToken => Run.RunToken;
        public int Wave => Run.Wave;
        public float WaveSeconds => Run.WaveSeconds;
        public int Coins => Wallet.Coins;
        public int Earned => Wallet.Earned;
        public int Spent => Wallet.Spent;
        public int Kills => Wallet.Kills;
        public string CharacterId => Catalog.Characters[Run.CharacterIndex].Id;
        public EntityId PlayerId => Run.PlayerId;
        public bool Paused => Run.Paused;
        public bool CardPicked => Loadout.CardPicked;
        public float PowerSeconds => PowerTime;
        public float HasteSeconds => HasteTime;
        public bool CompanionsStationary => Stationary;
        public int Capacity => World.Capacity;
        public int EntityCount => World.Count;
        public int EventCount => EventLength;
        public int DroppedEvents
        {
            get; private set;
        }
        public IReadOnlyList<Wall> Walls => _walls;
        internal Vec2 PlayerPosition => World.Transform[PlayerId.Index].Position;
        public GameSimulation(GameContent content, int capacity = 512)
            : this(content, new SimulationLimits(capacity, capacity >= 32 && capacity <= 4096 ? Math.Min(256, capacity - 7) : 0)) { }
        public GameSimulation(GameContent content, SimulationLimits limits)
            : this(new Catalog(content), limits, null) { }
        public GameSimulation(GameContent content, SimulationLimits limits, string catalogHash)
            : this(new Catalog(content), limits, catalogHash) { }
        internal GameSimulation(Catalog catalog, SimulationLimits limits, string? catalogHash)
        {
            Limits = limits ?? throw new ArgumentNullException(nameof(limits));
            if (catalogHash != null && !CheckpointSystem.ValidHash(catalogHash))
                throw new ArgumentException("Catalog identity must be a SHA256 hex digest", nameof(catalogHash));
            CatalogHash = catalogHash?.ToLowerInvariant();
            Catalog = catalog;
            World = new World(limits.EntityCapacity);
            Observations = new CombatObservationStorage(limits.EntityCapacity);
            SpawnReservations = new SpawnReservationStorage(limits.EnemyCapacity);
            Wallet = new WalletResource(Catalog.Items.Length);
            Loadout = new CardLoadoutResource(Catalog.Cards.Length);
            _systems = new ISimulationSystem[]
            {
                new MovementSystem(), new CompanionSystem(), new BasicMeleeSystem(), new OrbitSystem(),
                new RicochetSystem(), new HoneySystem(), new ImpactSystem(), new ContactSystem(),
                new PickupSystem(), new CleanupSpawnSystem()
            };
        }
        public PreparedWaveToken PreparedToken => Phase == RunPhase.Prepared
            ? new PreparedWaveToken(RunToken, Wave, Run.PreparationOrdinal) : default;
        public void PrepareRun(string characterId, uint seed) => RunProgressionSystem.PrepareRun(this, characterId, seed);
        public bool PrepareNextWave() => RunProgressionSystem.PrepareNextWave(this);
        public bool BeginPreparedWave(PreparedWaveToken token) => RunProgressionSystem.BeginPreparedWave(this, token);
        public bool TryCapturePrepared(CheckpointIdentity identity, out WaveCheckpointDto? checkpoint, out CheckpointError error)
            => CheckpointSystem.TryCapture(this, identity, out checkpoint, out error);
        public bool TryConvertLegacyPreparedCheckpoint(WaveCheckpointDto legacy, CheckpointIdentity expectedLegacyIdentity,
            CheckpointIdentity targetIdentity, out WaveCheckpointDto? converted, out CheckpointError error)
            => CheckpointSystem.TryConvertLegacy(this, legacy, expectedLegacyIdentity, targetIdentity, out converted, out error);
        public bool TryValidateCheckpoint(WaveCheckpointDto checkpoint, CheckpointIdentity expectedIdentity, out CheckpointError error)
            => CheckpointSystem.TryValidate(this, checkpoint, expectedIdentity, out error);
        public bool TryRestorePrepared(WaveCheckpointDto checkpoint, CheckpointIdentity expectedIdentity, out CheckpointError error)
            => CheckpointSystem.TryRestore(this, checkpoint, expectedIdentity, out error);
        public void StartRun(string characterId, uint seed) => RunProgressionSystem.StartRun(this, characterId, seed);
        // Restore commits the already-built schedule storage too, so future stateful systems cannot inherit an old session cache.
        internal void AdoptPreparedSystemStorage(GameSimulation prepared) => _systems = prepared._systems;
        public bool Enqueue(GameCommand command)
        {
            int kind = (int)command.Kind;
            if (kind < 0 || kind == 1 || kind > (int)CommandKind.FinishRun || Commands.Count >= 256)
                return false;
            // Zero context is trusted-immediate convenience. Deferred UI callbacks must capture explicit context.
            Commands.Enqueue(new GameCommand(command.Kind, command.Id, command.Direction.X, command.Direction.Y,
                command.Flag, command.RunToken == 0 ? RunToken : command.RunToken, command.Wave == 0 ? Wave : command.Wave));
            return true;
        }
        public void Step()
        {
            EventLength = 0;
            Observations.BeginStep();
            SpawnReservations.BeginStep();
            _progression.Update(this);
            if (Phase != RunPhase.Combat || Paused)
                return;
            Run.Tick++;
            Run.WaveSeconds = Tick * FixedDelta;
            _statEffects.Update(this);
            PowerTime = Math.Max(0, PowerTime - FixedDelta);
            HasteTime = Math.Max(0, HasteTime - FixedDelta);
            for (int i = 0; i < _systems.Length; i++)
            {
                _systems[i].Update(this);
                if (Phase != RunPhase.Combat) break;
            }
            RunProgressionSystem.EndTick(this);
        }
        public bool CanBuy(string id) => RunProgressionSystem.CanBuy(this, id);
        public CardAvailability GetCardAvailability(string id) => id != null && Catalog.CardIndex.TryGetValue(id, out int index)
            ? RunProgressionSystem.Availability(this, index) : CardAvailability.Unknown;
        public IReadOnlyList<OfferSlotView> GetOfferSlots() => Phase == RunPhase.Between && !CardPicked
            ? Loadout.OfferSlots : Array.Empty<OfferSlotView>();
        public int GetLevel(string id) => id != null && Catalog.CardIndex.TryGetValue(id, out int i) ? Loadout.Levels[i] : 0;
        public int GetItemCount(string id) => id != null && Catalog.ItemIndex.TryGetValue(id, out int i) ? Wallet.OwnedItems[i] : 0;
        public int GetItemStock(string id) => id != null && Catalog.ItemIndex.TryGetValue(id, out int i) ? Wallet.Stock[i] : 0;
        public IReadOnlyList<CardDefinition> GetOffers() => Phase == RunPhase.Between && !CardPicked
            ? Loadout.OfferViews : Array.Empty<CardDefinition>();
        public float GetStat(Stat stat) => stat >= 0 && stat < Stat.Count ? Stats[(int)stat] : 0;
        public bool IsAlive(EntityId id) => World.Alive(id);
        public bool TryGetEntityAtSlot(int slot, out EntityView view)
        {
            if (slot >= 0 && slot < Capacity && World.Mask[slot] != Components.None)
            {
                view = View(slot);
                return true;
            }
            view = default;
            return false;
        }
        public bool TryGetEntity(EntityId id, out EntityView view)
        {
            if (World.Alive(id))
            {
                view = View(id.Index);
                return true;
            }
            view = default;
            return false;
        }
        public EntityView GetEntity(int ordinal)
        {
            for (int i = 0; i < Capacity; i++)
                if (World.Mask[i] != Components.None && ordinal-- == 0)
                    return View(i);
            throw new ArgumentOutOfRangeException(nameof(ordinal));
        }
        private EntityView View(int i)
        {
            EntityKind kind = World.Has(i, Components.Player) ? EntityKind.Player
                : World.Has(i, Components.Enemy) ? EntityKind.Enemy
                : World.Has(i, Components.Companion) ? EntityKind.Companion : EntityKind.Pickup;
            var t = World.Transform[i];
            return new EntityView(World.Id(i), kind, t.Position, t.Facing, World.Health[i].Current,
                World.Health[i].Maximum, World.Visual[i], kind != EntityKind.Pickup || World.Pickup[i].Cooldown <= 0);
        }
        public GameEvent GetEvent(int index)
        {
            if (index < 0 || index >= EventLength)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _events[index];
        }
        internal void Emit(EventKind kind, EntityId source, EntityId target, Vec2 from, Vec2 to, float value, FeedbackMechanism mechanism = FeedbackMechanism.Unknown)
        {
            // Cosmetic-only drop-newest policy. Rules never read this presentation event buffer.
            if (EventLength == _events.Length)
            {
                DroppedEvents++;
                return;
            }
            _events[EventLength++] = new GameEvent(kind, source, target, from, to, value, Tick, RunToken, Wave, mechanism);
        }
        internal void Damage(int target, float amount, EntityId source, FeedbackMechanism mechanism = FeedbackMechanism.Unknown)
            => DamageResolutionSystem.Apply(this, target, amount, source, mechanism);
        public int PendingSpawnCount => SpawnReservations.Count;
        public PendingSpawnView GetPendingSpawn(int index) => SpawnReservations.Get(index);
        public int SpawnTransitionCount => SpawnReservations.TransitionCount;
        public SpawnTransition GetSpawnTransition(int index) => SpawnReservations.GetTransition(index);
        public long DroppedSpawnTransitions => SpawnReservations.DroppedTransitions;
        public SpawnOutcome LastSpawnOutcome => SpawnReservations.LastOutcome;
        public int ObservationCount => Observations.Count;
        public long DroppedObservations => Observations.Dropped;
        public CombatObservation GetObservation(int index) => Observations.Get(index);
        internal void Observe(CombatObservationKind kind, EntityId source, EntityId target, Vec2 from, Vec2 to, float value)
            => Observations.Add(new CombatObservation(kind, source, target, from, to, value, Tick, RunToken, Wave));
        internal void ObserveMotion(int slot, float honeyMultiplier, bool usedKnockMovement)
            => Observations.SetMotion(new EnemyMotionObservation(World.Id(slot), honeyMultiplier, usedKnockMovement, Tick, RunToken, Wave));
        public bool TryGetEnemyMotionObservation(EntityId id, out EnemyMotionObservation value)
        {
            if (Phase == RunPhase.Combat && World.Alive(id) && World.Has(id.Index, Components.Enemy))
            {
                var sample = Observations.GetMotion(id.Index);
                if (sample.Id == id && sample.RunToken == RunToken && sample.Wave == Wave && sample.Tick == Tick)
                { value = sample; return true; }
            }
            value = default; return false;
        }
        internal void SpawnEnemy() => EntityLifecycleSystem.SpawnEnemy(this);
        internal void RecallCompanions() => EntityLifecycleSystem.RecallCompanions(this);
        internal uint Random()
        {
            uint x = Run.Random;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            Run.Random = x;
            return x;
        }
    }
}
