using System;

namespace Hmi.Core
{
    internal sealed class ProjectileStorage
    {
        internal readonly ProjectileComponent[] Data = new ProjectileComponent[ProjectileRules.Capacity];
        internal readonly int[] Generation = new int[ProjectileRules.Capacity];
        internal readonly bool[] Active = new bool[ProjectileRules.Capacity], Remove = new bool[ProjectileRules.Capacity];
        internal readonly EntityId[] Visited = new EntityId[ProjectileRules.Capacity * 5];
        internal readonly ProjectileLaunchIntent[] Requests = new ProjectileLaunchIntent[ProjectileRules.SourceLanes];
        internal readonly Vec2[] EnemyStart, EnemyEnd;
        internal readonly EntityId[] EnemyId;
        private readonly float[] _contactTimes;
        private readonly ProjectileTransition[] _transitions = new ProjectileTransition[256];
        internal int Count, TransitionCount, NextLane;
        internal long DroppedTransitions;
        internal ProjectileStorage(int actors)
        {
            EnemyStart = new Vec2[actors]; EnemyEnd = new Vec2[actors]; EnemyId = new EntityId[actors];
            _contactTimes = new float[actors];
        }
        internal bool EmptyForCheckpoint
        {
            get
            {
                if (Count != 0) return false;
                for (int i = 0; i < Active.Length; i++) if (Active[i]) return false;
                for (int i = 0; i < Requests.Length; i++) if (Requests[i].Present) return false;
                return true;
            }
        }
        internal void BeginStep() { TransitionCount = 0; Array.Clear(Requests, 0, Requests.Length); }
        internal void ResetScope()
        {
            Array.Clear(Active, 0, Active.Length); Array.Clear(Remove, 0, Remove.Length);
            Array.Clear(Requests, 0, Requests.Length); Array.Clear(EnemyId, 0, EnemyId.Length);
            Array.Clear(Visited, 0, Visited.Length); Count = TransitionCount = NextLane = 0; DroppedTransitions = 0;
        }
        internal void CopyGenerationBaseline(ProjectileStorage old)
        {
            Array.Copy(old.Generation, Generation, Generation.Length);
        }
        internal ProjectileId Id(int slot) => new ProjectileId(slot, Generation[slot]);
        internal bool TryGet(int slot, long tick, out ProjectileView view)
        {
            if (slot >= 0 && slot < Data.Length && Active[slot] && !Remove[slot])
            { view = new ProjectileView(Id(slot), Data[slot], tick); return true; }
            view = default; return false;
        }
        internal ProjectileTransition GetTransition(int index) => index >= 0 && index < TransitionCount
            ? _transitions[index] : throw new ArgumentOutOfRangeException(nameof(index));
        private void Emit(ProjectileTransitionKind kind, ProjectileEndReason reason, ProjectileId id,
            ProjectileComponent p, EntityId target, Vec2 from, Vec2 position, long tick, float applied = 0)
        {
            if (TransitionCount < _transitions.Length)
                _transitions[TransitionCount++] = new ProjectileTransition(kind, reason, id, p, target, from, position, tick, applied);
            else if (DroppedTransitions < long.MaxValue) DroppedTransitions++;
        }
        internal void CaptureStart(GameSimulation g)
        {
            for (int i = 0; i < g.Capacity; i++)
            {
                EnemyId[i] = g.World.Has(i, Components.Enemy | Components.Health) ? g.World.Id(i) : EntityId.None;
                EnemyStart[i] = g.World.Transform[i].Position;
            }
        }
        internal void CaptureEnd(GameSimulation g)
        {
            for (int i = 0; i < g.Capacity; i++) EnemyEnd[i] = g.World.Transform[i].Position;
        }
        internal void Request(GameSimulation g, int lane, ProjectileKind kind, EntityId owner, EntityId target,
            Vec2 origin, float damage, int bounces = 0, float bounceRange = 0)
        {
            if (lane < 0 || lane >= Requests.Length || Requests[lane].Present || !g.World.Alive(target)) return;
            Vec2 direction = (g.World.Transform[target.Index].Position - origin).Normalized;
            if (direction.LengthSquared < .5f) direction = new Vec2(0, 1); // t=0 overlap remains a real collision.
            Requests[lane] = new ProjectileLaunchIntent
            {
                Present = true, Lane = lane, Kind = kind, Owner = owner, Target = target, Origin = origin,
                Direction = direction, Damage = damage, Bounces = bounces, BounceRange = bounceRange,
                Run = g.RunToken, Wave = g.Wave
            };
        }
        private static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);
        private static ProjectileEndReason Validate(GameSimulation g, ProjectileLaunchIntent request)
        {
            if (g.Phase != RunPhase.Combat || request.Run != g.RunToken || request.Wave != g.Wave
                || !g.World.Alive(request.Owner)) return ProjectileEndReason.InvalidSource;
            if (request.Kind == ProjectileKind.Ricochet)
            {
                if (request.Lane != 0 || request.Owner != g.PlayerId || g.GetStat(Stat.RicochetEnabled) <= 0)
                    return ProjectileEndReason.InvalidSource;
            }
            else if (request.Kind == ProjectileKind.CompanionShot)
            {
                if (!g.World.Has(request.Owner.Index, Components.Companion)
                    || g.World.Companion[request.Owner.Index].Support
                    || request.Lane != g.World.Companion[request.Owner.Index].SpawnOrdinal + 1)
                    return ProjectileEndReason.InvalidSource;
            }
            else return ProjectileEndReason.InvalidRequest;
            if (!Finite(request.Origin.X) || !Finite(request.Origin.Y) || !Finite(request.Direction.X)
                || !Finite(request.Direction.Y) || Math.Abs(request.Direction.LengthSquared - 1) > .001f
                || !Finite(request.Damage) || request.Damage <= 0 || request.Damage > 1000
                || request.Bounces < 0 || request.Bounces > 4 || !Finite(request.BounceRange)
                || (request.Kind == ProjectileKind.Ricochet && (request.BounceRange < .1f || request.BounceRange > 50))
                || (request.Kind == ProjectileKind.CompanionShot && (request.Bounces != 0 || request.BounceRange != 0)))
                return ProjectileEndReason.InvalidRequest;
            return ProjectileGeometry.OriginBlocked(g, request.Origin) ? ProjectileEndReason.BlockedOrigin : ProjectileEndReason.None;
        }
        internal void Commit(GameSimulation g)
        {
            int start = NextLane;
            for (int offset = 0; offset < Requests.Length; offset++)
            {
                int lane = (start + offset) % Requests.Length;
                var request = Requests[lane]; Requests[lane] = default;
                if (!request.Present) continue;
                var p = new ProjectileComponent
                {
                    Owner = request.Owner, AimTarget = request.Target, Run = request.Run, Wave = request.Wave,
                    Kind = request.Kind, Previous = request.Origin, Position = request.Origin, Direction = request.Direction,
                    Damage = request.Damage, RemainingBounces = request.Bounces, BounceRange = request.BounceRange,
                    BirthTick = g.Tick, RemainingTicks = ProjectileRules.LifetimeTicks,
                    LegRemaining = request.Kind == ProjectileKind.Ricochet ? ProjectileRules.RicochetInitialRange : ProjectileRules.CompanionRange
                };
                p.TotalRemaining = p.LegRemaining + request.Bounces * request.BounceRange;
                var reason = Validate(g, request);
                int free = -1;
                if (reason == ProjectileEndReason.None)
                {
                    for (int i = 0; i < Data.Length; i++)
                        if (!Active[i] && Generation[i] >= 0 && Generation[i] < int.MaxValue) { free = i; break; }
                    if (free < 0) reason = ProjectileEndReason.Capacity;
                }
                if (reason != ProjectileEndReason.None)
                {
                    Emit(ProjectileTransitionKind.Rejected, reason, ProjectileId.None, p, request.Target,
                        p.Position, p.Position, g.Tick); continue;
                }
                Generation[free]++; Active[free] = true; Remove[free] = false; Data[free] = p; Count++;
                Array.Clear(Visited, free * 5, 5); NextLane = (lane + 1) % Requests.Length;
                if (request.Kind == ProjectileKind.Ricochet) g.RicochetTimer = .85f;
                else g.World.Companion[request.Owner.Index].Cooldown = .95f;
                Emit(ProjectileTransitionKind.Launched, ProjectileEndReason.None, Id(free), p, request.Target,
                    p.Position, p.Position, g.Tick);
            }
        }
        private bool WasVisited(int slot, EntityId enemy)
        {
            for (int i = 0; i < Data[slot].VisitedCount; i++) if (Visited[slot * 5 + i] == enemy) return true;
            return false;
        }
        private int NextTarget(GameSimulation g, int slot, Vec2 center, float range)
        {
            int best = -1; float distance = range * range;
            for (int i = 0; i < g.Capacity; i++)
            {
                if (!g.World.Has(i, Components.Enemy | Components.Health) || WasVisited(slot, g.World.Id(i))) continue;
                Vec2 point = g.World.Transform[i].Position;
                float next = (point - center).LengthSquared;
                if (next > distance || (next == distance && best >= 0) || !Geometry.LineClear(g, center, point)) continue;
                best = i; distance = next;
            }
            return best;
        }
        private void Terminate(GameSimulation g, int slot, ProjectileEndReason reason)
        {
            if (Remove[slot]) return;
            Remove[slot] = true;
            var p = Data[slot];
            Emit(ProjectileTransitionKind.Terminated, reason, Id(slot), p, EntityId.None, p.Previous, p.Position, g.Tick);
        }
        internal void Flight(GameSimulation g)
        {
            for (int slot = 0; slot < Data.Length; slot++)
            {
                if (!Active[slot] || Remove[slot]) continue;
                if (g.Phase != RunPhase.Combat) break;
                ref var p = ref Data[slot];
                if (p.Run != g.RunToken || p.Wave != g.Wave) { Terminate(g, slot, ProjectileEndReason.ScopeEnded); continue; }
                if (p.ContinueTick > g.Tick) continue;
                if (p.RemainingTicks <= 0) { Terminate(g, slot, ProjectileEndReason.Lifetime); continue; }
                p.State = ProjectileState.Flying; p.Previous = p.Position;
                float distance = Math.Min(ProjectileRules.Speed * GameSimulation.FixedDelta, Math.Min(p.LegRemaining, p.TotalRemaining));
                float fraction = distance / (ProjectileRules.Speed * GameSimulation.FixedDelta);
                Vec2 end = p.Position + p.Direction * distance;
                ProjectileGeometry.WallTime(g, p.Position, end, out float wallTime);
                float first = wallTime;
                for (int enemy = 0; enemy < g.Capacity; enemy++)
                {
                    _contactTimes[enemy] = float.PositiveInfinity;
                    if (!g.World.Has(enemy, Components.Enemy | Components.Health) || WasVisited(slot, g.World.Id(enemy))) continue;
                    // A newborn's firing stage is after Movement: never sweep enemies from before its birth.
                    bool existing = p.BirthTick < g.Tick && EnemyId[enemy] == g.World.Id(enemy);
                    Vec2 startEnemy = existing ? EnemyStart[enemy] : g.World.Transform[enemy].Position;
                    Vec2 endEnemy = existing ? startEnemy + (EnemyEnd[enemy] - startEnemy) * fraction : startEnemy;
                    if (ProjectileGeometry.Circle(p.Position, end, startEnemy, endEnemy,
                        ProjectileRules.Radius + g.World.Transform[enemy].Radius, out float time))
                    { _contactTimes[enemy] = time; first = Math.Min(first, time); }
                }
                int hit = -1;
                bool wall = !float.IsPositiveInfinity(wallTime) && wallTime <= first + ProjectileRules.TimeTieTolerance;
                float contact = wall ? wallTime : first;
                if (!wall && !float.IsPositiveInfinity(first))
                    for (int enemy = 0; enemy < g.Capacity; enemy++)
                        if (_contactTimes[enemy] <= first + ProjectileRules.TimeTieTolerance) { hit = enemy; contact = _contactTimes[enemy]; break; }
                float travel = float.IsPositiveInfinity(contact) ? distance : distance * contact;
                p.Position += p.Direction * travel;
                p.LegRemaining = Math.Max(0, p.LegRemaining - travel); p.TotalRemaining = Math.Max(0, p.TotalRemaining - travel);
                p.RemainingTicks--;
                if (wall) { Terminate(g, slot, ProjectileEndReason.Wall); continue; }
                if (hit >= 0)
                {
                    EntityId victim = g.World.Id(hit); Vec2 center = g.World.Transform[hit].Position;
                    Visited[slot * 5 + p.VisitedCount++] = victim;
                    float applied = p.Damage * (g.PowerTime > 0 ? 1.25f : 1);
                    Emit(ProjectileTransitionKind.Impact, ProjectileEndReason.None, Id(slot), p, victim,
                        p.Previous, p.Position, g.Tick, applied);
                    g.Damage(hit, p.Damage, p.Owner, p.Kind == ProjectileKind.Ricochet ? FeedbackMechanism.Ricochet : FeedbackMechanism.CompanionShot);
                    if (g.Phase != RunPhase.Combat) { Terminate(g, slot, ProjectileEndReason.ScopeEnded); continue; }
                    if (p.Kind == ProjectileKind.CompanionShot || p.RemainingBounces == 0)
                    { Terminate(g, slot, ProjectileEndReason.EnemyHit); continue; }
                    if (p.RemainingTicks == 0) { Terminate(g, slot, ProjectileEndReason.Lifetime); continue; }
                    if (p.TotalRemaining <= 0) { Terminate(g, slot, ProjectileEndReason.Range); continue; }
                    int next = NextTarget(g, slot, center, p.BounceRange);
                    if (next < 0) { Terminate(g, slot, ProjectileEndReason.NoNextTarget); continue; }
                    p.AimTarget = g.World.Id(next); p.Direction = (g.World.Transform[next].Position - p.Position).Normalized;
                    if (p.Direction.LengthSquared < .5f) p.Direction = new Vec2(0, 1);
                    p.RemainingBounces--; p.Damage *= .5f; p.LegRemaining = Math.Min(p.BounceRange, p.TotalRemaining);
                    p.ContinueTick = g.Tick + 1; p.State = ProjectileState.ContinuationPending; p.Previous = p.Position;
                    Emit(ProjectileTransitionKind.BounceArmed, ProjectileEndReason.None, Id(slot), p, p.AimTarget,
                        p.Position, p.Position, g.Tick);
                }
                else if (p.LegRemaining <= 0 || p.TotalRemaining <= 0) Terminate(g, slot, ProjectileEndReason.Range);
                else if (p.RemainingTicks == 0) Terminate(g, slot, ProjectileEndReason.Lifetime);
            }
            Cleanup();
        }
        internal void Cleanup()
        {
            for (int i = 0; i < Data.Length; i++) if (Active[i] && Remove[i]) { Active[i] = false; Remove[i] = false; Count--; }
        }
        internal void Cancel(GameSimulation g)
        {
            Array.Clear(Requests, 0, Requests.Length);
            for (int i = 0; i < Data.Length; i++) if (Active[i]) Terminate(g, i, ProjectileEndReason.ScopeEnded);
            Cleanup();
        }
    }
    internal sealed class ProjectileMotionStartSystem : ISimulationSystem { public void Update(GameSimulation g) => g.Projectiles.CaptureStart(g); }
    internal sealed class ProjectileMotionEndSystem : ISimulationSystem { public void Update(GameSimulation g) => g.Projectiles.CaptureEnd(g); }
    internal sealed class ProjectileLaunchSystem : ISimulationSystem { public void Update(GameSimulation g) => g.Projectiles.Commit(g); }
    internal sealed class ProjectileFlightSystem : ISimulationSystem { public void Update(GameSimulation g) => g.Projectiles.Flight(g); }
}
