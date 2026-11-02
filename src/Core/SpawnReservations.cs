using System;

namespace Hmi.Core
{
    public readonly struct SpawnReservationId : IEquatable<SpawnReservationId>
    {
        public readonly int RunToken, Wave;
        public readonly long Serial;
        public SpawnReservationId(int runToken, int wave, long serial) { RunToken=runToken; Wave=wave; Serial=serial; }
        public bool Equals(SpawnReservationId other) => RunToken==other.RunToken && Wave==other.Wave && Serial==other.Serial;
        public override bool Equals(object? obj) => obj is SpawnReservationId other && Equals(other);
        public override int GetHashCode() => RunToken ^ Wave ^ Serial.GetHashCode();
    }
    public readonly struct PendingSpawnView
    {
        public readonly SpawnReservationId Id;
        public readonly Vec2 Position;
        public readonly long ScheduledTick, ActivateTick;
        public float Radius => .44f;
        public string VisualId => "enemy_chaser";
        internal PendingSpawnView(SpawnReservationId id, Vec2 position, long scheduledTick)
        { Id=id; Position=position; ScheduledTick=scheduledTick; ActivateTick=scheduledTick+WaveRules.SpawnPreviewTicks; }
    }
    public enum SpawnTransitionKind { Scheduled, Activated, Cancelled, Rejected }
    public enum SpawnOutcome { None, Capacity, SoftCapacity, NoSafePosition, Deadline, ScopeEnded, Defeat, ActivationCapacity, HandleLimit }
    public readonly struct SpawnTransition
    {
        public readonly SpawnTransitionKind Kind;
        public readonly SpawnOutcome Outcome;
        public readonly PendingSpawnView Reservation;
        public readonly EntityId Entity;
        public readonly long Tick;
        internal SpawnTransition(SpawnTransitionKind kind, SpawnOutcome outcome, PendingSpawnView reservation, EntityId entity, long tick)
        { Kind=kind; Outcome=outcome; Reservation=reservation; Entity=entity; Tick=tick; }
    }
    // Separate bounded authority storage; observations are optional, never the marker source of truth.
    internal sealed class SpawnReservationStorage
    {
        private readonly PendingSpawnView[] _pending;
        private readonly SpawnTransition[] _transitions;
        private long _serial;
        internal int Count { get; private set; }
        internal int TransitionCount { get; private set; }
        internal long DroppedTransitions { get; private set; }
        internal SpawnOutcome LastOutcome { get; private set; }
        internal SpawnReservationStorage(int capacity) { _pending=new PendingSpawnView[capacity]; _transitions=new SpawnTransition[capacity*2+8]; }
        internal PendingSpawnView Get(int index) => index>=0 && index<Count ? _pending[index] : throw new ArgumentOutOfRangeException(nameof(index));
        internal SpawnTransition GetTransition(int index) => index>=0 && index<TransitionCount ? _transitions[index] : throw new ArgumentOutOfRangeException(nameof(index));
        internal void BeginStep() { TransitionCount=0; }
        internal void ResetScope() { Count=0; TransitionCount=0; DroppedTransitions=0; _serial=0; LastOutcome=SpawnOutcome.None; }
        private void Emit(SpawnTransitionKind kind, SpawnOutcome outcome, PendingSpawnView view, EntityId entity, long tick)
        {
            LastOutcome=outcome;
            if(TransitionCount<_transitions.Length) _transitions[TransitionCount++]=new SpawnTransition(kind,outcome,view,entity,tick);
            else if(DroppedTransitions<long.MaxValue) DroppedTransitions++;
        }
        private void Reject(GameSimulation g, SpawnOutcome outcome) => Emit(SpawnTransitionKind.Rejected,outcome,new PendingSpawnView(new SpawnReservationId(g.RunToken,g.Wave,0),default,g.Tick),EntityId.None,g.Tick);
        internal void RejectSoftCapacity(GameSimulation g) => Reject(g,SpawnOutcome.SoftCapacity);
        internal void Cancel(GameSimulation g, SpawnOutcome outcome)
        {
            for(int i=0;i<Count;i++) Emit(SpawnTransitionKind.Cancelled,outcome,_pending[i],EntityId.None,g.Tick);
            Count=0;
        }
        internal void Schedule(GameSimulation g)
        {
            if(g.Phase!=RunPhase.Combat || g.Tick+WaveRules.SpawnPreviewTicks>=WaveRules.DurationTicks) { Reject(g,SpawnOutcome.Deadline); return; }
            int enemies=0, free=0;
            for(int i=0;i<g.Capacity;i++)
            {
                if(g.World.Has(i,Components.Enemy)) enemies++;
                if(g.World.Mask[i]==Components.None && g.World.Generation[i]>=0 && g.World.Generation[i]<int.MaxValue) free++;
            }
            if(Count>=_pending.Length || enemies+Count>=g.Limits.EnemyCapacity || free<=Count) { Reject(g,SpawnOutcome.Capacity); return; }
            if(_serial==long.MaxValue) { Reject(g,SpawnOutcome.HandleLimit); return; }
            uint random=g.Random();
            int edge=g.SpawnCounter++%4;
            float fraction=random%10000/10000f;
            float origin=edge<2 ? -6.5f+fraction*13 : -12.5f+fraction*25;
            float limit=edge<2 ? 6.5f : 12.5f;
            // Eight deterministic positions, one RNG draw. Out-of-edge positions are rejected, not clamped/teleported.
            for(int candidate=0;candidate<8;candidate++)
            {
                float offset=candidate==0 ? 0 : (candidate%2==1 ? (candidate+1)/2 : -candidate/2);
                float along=origin+offset;
                if(along < -limit || along > limit) continue;
                var position=edge<2 ? new Vec2(edge==0?-12.8f:12.8f,along) : new Vec2(along,edge==2?-6.8f:6.8f);
                if(Geometry.Blocked(g,position,.44f) || (g.World.Alive(g.PlayerId) && (position-g.PlayerPosition).LengthSquared<2.25f)) continue;
                bool occupied=false;
                for(int i=0;i<Count;i++) if((_pending[i].Position-position).LengthSquared<1f) { occupied=true; break; }
                if(occupied) continue;
                var reservation=new PendingSpawnView(new SpawnReservationId(g.RunToken,g.Wave,++_serial),position,g.Tick);
                _pending[Count++]=reservation;
                Emit(SpawnTransitionKind.Scheduled,SpawnOutcome.None,reservation,EntityId.None,g.Tick);
                return;
            }
            Reject(g,SpawnOutcome.NoSafePosition);
        }
        internal void ActivateDue(GameSimulation g)
        {
            if(g.Tick>=WaveRules.DurationTicks || g.Phase!=RunPhase.Combat) { Cancel(g,SpawnOutcome.ScopeEnded); return; }
            if(g.World.Alive(g.PlayerId) && g.World.Health[g.PlayerId.Index].Current<=0) { Cancel(g,SpawnOutcome.Defeat); return; }
            int retained=0;
            for(int i=0;i<Count;i++)
            {
                var pending=_pending[i];
                if(pending.ActivateTick>g.Tick) { _pending[retained++]=pending; continue; }
                int enemies=0;
                for(int slot=0;slot<g.Capacity;slot++)if(g.World.Has(slot,Components.Enemy))enemies++;
                var entity=enemies<g.Limits.EnemyCapacity ? g.World.Create(Components.Enemy|Components.Health,pending.Position,.44f,"enemy_chaser",8*g.EffectiveEnemyHealthMultiplier) : EntityId.None;
                bool alive=g.World.Alive(entity);
                Emit(alive?SpawnTransitionKind.Activated:SpawnTransitionKind.Cancelled,alive?SpawnOutcome.None:SpawnOutcome.ActivationCapacity,pending,entity,g.Tick);
            }
            Count=retained;
        }
    }
}
