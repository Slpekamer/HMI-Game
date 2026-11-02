using System;
using System.Numerics;
namespace Hmi.PresentationStudy;

// Adapter DTOs only: full identity, never a slot-only lookup or a Core entity owner.
public readonly record struct EntityKey(int Slot, int Generation) {
    public bool Valid => Slot >= 0 && Generation > 0;
}
public readonly record struct Scope(int Run, int Wave);
public readonly record struct Observation(EntityKey Id, Vector2 Position,
    bool LiveEnemy, bool ActorIdentityMatches, bool InViewport, bool WallLineClear) {
    public bool Eligible => Id.Valid && LiveEnemy && ActorIdentityMatches && InViewport && WallLineClear && ReadyAttention.Finite(Position);
}
public enum HeadingReason { Retained, Movement, Attention, ActualMelee, AuthoritativeMeleeAim }
public readonly record struct Heading(Vector2 Direction, HeadingReason Reason, EntityKey? AttentionTarget);

/// <summary>Pure cosmetic selection/latch. No engine, RNG, world writes, attack scheduling, or pose transforms.</summary>
public sealed class ReadyAttention {
    public const float EnterDistance=4f, ExitDistance=5f, SwitchAdvantage=.6f;
    // One micrometre tolerance only for a float-coordinate inclusive distance threshold.
    const float DistanceTolerance=1e-6f;
    public const double MinimumHold=.18;
    readonly int capacity;
    Scope scope; EntityKey source; bool bound, paused, visible;
    long tick=-1;
    EntityKey? target; Vector2 direction=Vector2.UnitY, meleeDirection;
    double held; bool gestureActive, gestureHasDirection, gestureCompleted; long gestureSequence=-1;
    public Heading Current { get; private set; } = new(Vector2.UnitY,HeadingReason.Retained,null);
    public double HoldSeconds => held;
    public bool GestureActive => gestureActive;
    public long GestureSequence => gestureSequence;
    public ReadyAttention(int maximumObservations=256) {
        if(maximumObservations<1)throw new ArgumentOutOfRangeException(nameof(maximumObservations));
        capacity=maximumObservations;
    }
    public static bool Finite(Vector2 p)=>float.IsFinite(p.X)&&float.IsFinite(p.Y);
    static Vector2 Unit(Vector2 v) { float n=v.LengthSquared();return Finite(v)&&float.IsFinite(n)&&n>1e-10f?v/MathF.Sqrt(n):Vector2.Zero; }
    public void Bind(Scope value,EntityKey owner,Vector2 initialHeading) {
        if(!owner.Valid||value.Run<0||value.Wave<1)throw new ArgumentException("Invalid binding");
        Reset();scope=value;source=owner;bound=true;
        var u=Unit(initialHeading);direction=u==Vector2.Zero?Vector2.UnitY:u;
        Current=new(direction,HeadingReason.Retained,null);
    }
    public void Reset() {
        bound=false;paused=false;visible=false;tick=-1;target=null;held=0;gestureActive=gestureHasDirection=gestureCompleted=false;gestureSequence=-1;
        direction=Vector2.UnitY;meleeDirection=Vector2.Zero;Current=new(direction,HeadingReason.Retained,null);
    }
    // One call per render update. Same-tick snapshots may refresh visibility but time advances exactly once per call.
    // Adapter owns honest delta; this class does not synthesize simulation ticks or replay missing observations.
    public bool Update(Scope value,EntityKey owner,long snapshotTick,Vector2 position,Vector2 movement,
        ReadOnlySpan<Observation> observations,double elapsed,bool isPaused,bool combatVisible=true) {
        if(!bound||value!=scope||owner!=source||snapshotTick<0||snapshotTick<tick||!Finite(position)||
           !Finite(movement)||!double.IsFinite(elapsed)||elapsed<0||observations.Length>capacity)return false;
        for(int i=0;i<observations.Length;i++)for(int j=0;j<i;j++)if(observations[i].Id==observations[j].Id)return false;
        tick=snapshotTick;paused=isPaused;visible=combatVisible;
        if(!combatVisible) { target=null;held=0;gestureActive=gestureHasDirection=gestureCompleted=false;gestureSequence=-1;Current=new(direction,HeadingReason.Retained,null);return true; }
        if(!paused) { held+=elapsed; }
        int current=-1;
        if(target is EntityKey key)for(int i=0;i<observations.Length;i++)
            if(observations[i].Id==key&&EligibleAt(observations[i],position,ExitDistance)){current=i;break;}
        if(current<0){target=null;held=0;}
        if(!paused) {
            int nearest=-1;
            for(int i=0;i<observations.Length;i++)if(EligibleAt(observations[i],position,EnterDistance)&&
                (nearest<0||Before(observations[i],observations[nearest],position)))nearest=i;
            bool select=nearest>=0&&(current<0||(held+1e-12>=MinimumHold&&observations[nearest].Id!=target&&
                Vector2.Distance(position,observations[current].Position)-Vector2.Distance(position,observations[nearest].Position)>=SwitchAdvantage-DistanceTolerance));
            if(select){current=nearest;target=observations[nearest].Id;held=0;}
        }
        if(paused) { Current=new(direction,gestureActive?HeadingReason.ActualMelee:target!=null?Current.Reason:HeadingReason.Retained,target);return true; }
        Vector2 wanted=gestureActive?meleeDirection:current>=0?Unit(observations[current].Position-position):Unit(movement);
        HeadingReason reason=gestureActive?HeadingReason.ActualMelee:current>=0?HeadingReason.Attention:wanted!=Vector2.Zero?HeadingReason.Movement:HeadingReason.Retained;
        if(wanted!=Vector2.Zero)direction=wanted;
        Current=new(direction,reason,target);return true;
    }
    // Authentic target adoption reuses existing attention hold; no attack duration is added.
    public bool AdoptActualMeleeTarget(Scope value,EntityKey owner,EntityKey actualTarget,Vector2 position,ReadOnlySpan<Observation> observations)
    {
        if(!bound||paused||!visible||value!=scope||owner!=source||!Finite(position)||!actualTarget.Valid)return false;
        for(int i=0;i<observations.Length;i++)if(observations[i].Id==actualTarget&&EligibleAt(observations[i],position,ExitDistance))
        {target=actualTarget;held=0;return true;}
        return false;
    }
    bool EligibleAt(Observation o,Vector2 from,float radius) {
        float d=Vector2.DistanceSquared(from,o.Position);
        return o.Id!=source&&o.Eligible&&float.IsFinite(d)&&d>1e-10f&&d<=radius*radius;
    }
    static bool Before(Observation a,Observation b,Vector2 p) {
        float da=Vector2.DistanceSquared(p,a.Position),db=Vector2.DistanceSquared(p,b.Position);
        return da<db||(da==db&&(a.Id.Slot<b.Id.Slot||(a.Id.Slot==b.Id.Slot&&a.Id.Generation<b.Id.Generation)));
    }
    // End state is supplied by the actual native owner, never estimated from a second duration.
    public bool SyncAcceptedGesture(Scope value,EntityKey owner,long sequence,bool hasMeleeDirection,bool active,Vector2 actualDirection)
    {
        if(!bound||!visible||value!=scope||owner!=source||sequence<0||sequence<gestureSequence)return false;
        Vector2 u=Unit(actualDirection);
        if(hasMeleeDirection&&(u==Vector2.Zero||sequence==0))return false;
        bool newActive=hasMeleeDirection&&active;
        if(sequence==gestureSequence && (hasMeleeDirection!=gestureHasDirection ||
            (hasMeleeDirection&&u!=meleeDirection) || (gestureCompleted&&newActive)))return false;
        if(paused && (newActive||gestureActive) && (sequence!=gestureSequence || newActive!=gestureActive))return false;
        gestureSequence=sequence;gestureHasDirection=hasMeleeDirection;gestureActive=newActive;gestureCompleted=!newActive;
        if(hasMeleeDirection)meleeDirection=u;
        if(gestureActive) { direction=meleeDirection;Current=new(direction,HeadingReason.ActualMelee,target); }
        return true;
    }
}
