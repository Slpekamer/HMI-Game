using System;

namespace Hmi.Core
{
    public static class ProjectileRules
    {
        public const int SchemaVersion = 1, Capacity = 32, LifetimeTicks = 720, SourceLanes = 5;
        public const float Speed = 18, Radius = .08f, RicochetInitialRange = 10.4f, CompanionRange = 5;
        public const float TimeTieTolerance = .000001f;
    }
    public readonly struct ProjectileId : IEquatable<ProjectileId>
    {
        public readonly int Index, Generation;
        public ProjectileId(int index, int generation) { Index = index; Generation = generation; }
        public bool Equals(ProjectileId other) => Index == other.Index && Generation == other.Generation;
        public override bool Equals(object? obj) => obj is ProjectileId other && Equals(other);
        public override int GetHashCode() => unchecked(Index * 397 ^ Generation);
        public static bool operator ==(ProjectileId a, ProjectileId b) => a.Equals(b);
        public static bool operator !=(ProjectileId a, ProjectileId b) => !a.Equals(b);
        public static ProjectileId None => new ProjectileId(-1, 0);
    }
    public enum ProjectileKind { Ricochet, CompanionShot }
    public enum ProjectileState { Flying, ContinuationPending }
    public enum ProjectileTransitionKind { Launched, Impact, BounceArmed, Terminated, Rejected }
    public enum ProjectileEndReason { None, EnemyHit, Wall, Range, Lifetime, NoNextTarget, ScopeEnded, Capacity, InvalidSource, BlockedOrigin, InvalidRequest }
    public readonly struct ProjectileView
    {
        public readonly ProjectileId Id;
        public readonly EntityId Owner;
        public readonly int RunToken, Wave, HitsCompleted, RemainingTicks;
        public readonly long BirthTick, Tick;
        public readonly ProjectileKind Kind;
        public readonly ProjectileState State;
        public readonly Vec2 PreviousPosition, Position, Direction;
        public readonly float Radius, RawDamage, RemainingLegDistance, RemainingTotalDistance;
        internal ProjectileView(ProjectileId id, ProjectileComponent p, long tick)
        {
            Id = id; Owner = p.Owner; RunToken = p.Run; Wave = p.Wave; HitsCompleted = p.VisitedCount;
            BirthTick = p.BirthTick; Tick = tick; Kind = p.Kind; State = p.State;
            PreviousPosition = p.Previous; Position = p.Position; Direction = p.Direction;
            Radius = ProjectileRules.Radius; RawDamage = p.Damage; RemainingLegDistance = p.LegRemaining;
            RemainingTotalDistance = p.TotalRemaining; RemainingTicks = p.RemainingTicks;
        }
    }
    public readonly struct ProjectileTransition
    {
        public readonly ProjectileTransitionKind Kind;
        public readonly ProjectileEndReason Reason;
        public readonly ProjectileKind ProjectileKind;
        public readonly ProjectileId Id;
        public readonly EntityId Owner, Target;
        public readonly int RunToken, Wave, HitsCompleted;
        public readonly long Tick;
        public readonly Vec2 From, Position;
        public readonly float RawDamage, AppliedDamageAmount;
        internal ProjectileTransition(ProjectileTransitionKind kind, ProjectileEndReason reason, ProjectileId id,
            ProjectileComponent p, EntityId target, Vec2 from, Vec2 position, long tick, float applied = 0)
        {
            Kind = kind; Reason = reason; Id = id; ProjectileKind = p.Kind; Owner = p.Owner; Target = target;
            RunToken = p.Run; Wave = p.Wave; Tick = tick; HitsCompleted = p.VisitedCount;
            From = from; Position = position; RawDamage = p.Damage; AppliedDamageAmount = applied;
        }
    }
    internal struct ProjectileComponent
    {
        internal EntityId Owner, AimTarget;
        internal int Run, Wave, RemainingTicks, RemainingBounces, VisitedCount;
        internal long BirthTick, ContinueTick;
        internal ProjectileKind Kind;
        internal ProjectileState State;
        internal Vec2 Previous, Position, Direction;
        internal float Damage, LegRemaining, TotalRemaining, BounceRange;
    }
    internal struct ProjectileLaunchIntent
    {
        internal bool Present;
        internal int Lane, Run, Wave, Bounces;
        internal ProjectileKind Kind;
        internal EntityId Owner, Target;
        internal Vec2 Origin, Direction;
        internal float Damage, BounceRange;
    }
}
