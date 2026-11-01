using System;

namespace Hmi.Core
{
    // Ephemeral output schema v1. Never serialized in a checkpoint or consumed by rules.
    public enum FeedbackMechanism
    {
        Unknown = 0, BasicMelee = 1, Orbit = 2, Ricochet = 3, Honey = 4,
        CompanionShot = 5, Impact = 6, ImpactBlast = 7, EnemyContact = 8
    }
    public enum CombatObservationKind
    {
        SupportHealApplied = 0, SupportBuffApplied = 1, WallBounce = 2
    }
    public readonly struct CombatObservation
    {
        public readonly CombatObservationKind Kind;
        public readonly EntityId Source, Target;
        public readonly Vec2 From, To;
        // Actual HP delta (including zero), refreshed buff seconds, or existing bounce ordinal.
        public readonly float Value;
        public readonly long Tick;
        public readonly int RunToken, Wave;
        public CombatObservation(CombatObservationKind kind, EntityId source, EntityId target,
            Vec2 from, Vec2 to, float value, long tick, int runToken, int wave)
        {
            Kind = kind; Source = source; Target = target; From = from; To = to;
            Value = value; Tick = tick; RunToken = runToken; Wave = wave;
        }
    }
    public readonly struct EnemyMotionObservation
    {
        public readonly EntityId Id;
        public readonly float HoneyMultiplier;
        public readonly bool UsedKnockMovement;
        public readonly long Tick;
        public readonly int RunToken, Wave;
        public EnemyMotionObservation(EntityId id, float honeyMultiplier, bool usedKnockMovement,
            long tick, int runToken, int wave)
        {
            Id = id; HoneyMultiplier = honeyMultiplier; UsedKnockMovement = usedKnockMovement;
            Tick = tick; RunToken = runToken; Wave = wave;
        }
    }
    internal sealed class CombatObservationStorage
    {
        internal const int Capacity = 512;
        private readonly CombatObservation[] _events = new CombatObservation[Capacity];
        private readonly EnemyMotionObservation[] _motion;
        internal int Count;
        internal long Dropped;
        internal CombatObservationStorage(int entityCapacity) => _motion = new EnemyMotionObservation[entityCapacity];
        internal void BeginStep() => Count = 0;
        internal void ResetScope()
        {
            Count = 0; Dropped = 0;
            Array.Clear(_motion, 0, _motion.Length);
        }
        internal void Add(CombatObservation value)
        {
            if (Count < _events.Length) _events[Count++] = value;
            else if (Dropped < long.MaxValue) Dropped++;
        }
        internal CombatObservation Get(int index)
        {
            if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
            return _events[index];
        }
        internal void SetMotion(EnemyMotionObservation value) => _motion[value.Id.Index] = value;
        internal EnemyMotionObservation GetMotion(int index) => _motion[index];
    }
}
