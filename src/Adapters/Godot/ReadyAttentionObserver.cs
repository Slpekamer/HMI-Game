using System;
using Godot;
using Hmi.Core;
using Hmi.PresentationStudy;
using NVec2=System.Numerics.Vector2;
namespace Hmi.GodotAdapter;

/// <summary>Read-only observer prototype. Does NOT apply yaw, affect inputs, or decide whether attacks occur.</summary>
internal sealed class ReadyAttentionObserver
{
    readonly ReadyAttention state;
    readonly bool realMeleePriority;
    readonly AuthoritativeMeleeAim realAim=new();
    Heading desired;CharacterPresentation observedNativeOwner;long observedNativeEpoch=-1;
    public AuthoritativeMeleeAim AimDiagnostics=>realAim;
    public long CapturedStepCalls {get;private set;}
    public long SeenCoreEvents {get;private set;}
    readonly Observation[] observations;
    Scope scope;EntityKey owner;bool bound; NVec2 previousPosition,motion;long previousTick=-1;
    public bool OutputValid { get; private set; }
    public Heading Desired => OutputValid?desired:default;
    public ReadyAttentionObserver(int capacity,bool priority=false){observations=new Observation[capacity];state=new ReadyAttention(capacity);realMeleePriority=priority;}
    public void Reset(){ResetObservation();realAim.ClearPresentation();}
    // Host scope visual initialization happens AFTER same-step capture. Do not discard that new scope's mailbox.
    public void ResetObservation(){observedNativeOwner=null;observedNativeEpoch=-1;state.Reset();desired=default;bound=false;OutputValid=false;previousTick=-1;motion=previousPosition=default;}
    // Called once after every actual Core Step, before renderer/event animation coalescing.
    public void CaptureCoreStep(GameSimulation sim)
    {
        if(sim==null||sim.Phase!=RunPhase.Combat||!sim.TryGetEntity(sim.PlayerId,out var player)){realAim.ClearPresentation();return;}
        var stamp=new Scope(sim.RunToken,sim.Wave);var key=new EntityKey(player.Id.Index,player.Id.Generation);
        realAim.Bind(stamp,key);CapturedStepCalls++;
        for(int i=0;i<sim.EventCount;i++)
        {
            var e=sim.GetEvent(i);SeenCoreEvents++;
            if(e.Kind!=EventKind.Melee||e.Source!=player.Id)continue;
            realAim.Capture(new(new(e.RunToken,e.Wave),new(e.Source.Index,e.Source.Generation),new(e.Target.Index,e.Target.Generation),e.Tick,i,new(e.To.X-e.From.X,e.To.Y-e.From.Y)),sim.Tick,sim.Paused);
        }
    }
    public void SuspendAim(){realAim.ClearPresentation();}
    public bool Observe(GameSimulation sim,Camera3D camera,Node3D[] actors,EntityId[] ids,
        CharacterPresentation presenter,float elapsed)
    {
        OutputValid=false;
        if(sim==null||camera==null||!GodotObject.IsInstanceValid(camera)||!camera.IsInsideTree()||
            actors==null||ids==null||actors.Length!=ids.Length||actors.Length<sim.Capacity||
            presenter==null||presenter.IsDisposed||sim.Phase!=RunPhase.Combat||
            !sim.TryGetEntity(sim.PlayerId,out var player)){Reset();return false;}
        if(!ReferenceEquals(observedNativeOwner,presenter)||observedNativeEpoch!=presenter.GestureOwnerEpoch)
        {ResetObservation();observedNativeOwner=presenter;observedNativeEpoch=presenter.GestureOwnerEpoch;}
        var stamp=new Scope(sim.RunToken,sim.Wave);var key=new EntityKey(player.Id.Index,player.Id.Generation);
        if(realMeleePriority)realAim.Bind(stamp,key);
        if(!bound||scope!=stamp||owner!=key){scope=stamp;owner=key;state.Bind(scope,owner,new(player.Facing.X,player.Facing.Y));bound=true;previousTick=sim.Tick;previousPosition=new(player.Position.X,player.Position.Y);motion=default;}
        int count=0;
        var viewport=camera.GetViewport();Rect2 visible=viewport.GetVisibleRect();
        for(int slot=0;slot<sim.Capacity;slot++)
        {
            if(!sim.TryGetEntityAtSlot(slot,out var enemy)||enemy.Kind!=EntityKind.Enemy||!enemy.Active)continue;
            var actor=actors[slot];
            bool actorMatches=ids[slot]==enemy.Id&&actor!=null&&GodotObject.IsInstanceValid(actor)&&!actor.IsQueuedForDeletion()&&actor.IsInsideTree()&&actor.IsVisibleInTree()&&actor.GetViewport()==viewport;
            // A fixed visual torso sample, not a measured mesh center or GPU/HUD visibility claim.
            Vector3 point=actorMatches?actor.GlobalPosition+Vector3.Up*.55f:Vector3.Zero;
            bool inView=actorMatches&&!camera.IsPositionBehind(point)&&visible.HasPoint(camera.UnprojectPosition(point));
            bool lineClear=sim.HasUnobstructedSegment(player.Position,enemy.Position);
            if(count>=observations.Length){Reset();return false;}
            observations[count++]=new(new(slot,enemy.Id.Generation),new(enemy.Position.X,enemy.Position.Y),true,actorMatches,inView,lineClear);
        }
        NVec2 position=new(player.Position.X,player.Position.Y);
        if(sim.Tick>previousTick){motion=position-previousPosition;previousPosition=position;previousTick=sim.Tick;}
        NVec2 movement=motion;
        var snapshot=presenter.AcceptedGesture;
        if(snapshot.Source!=player.Id||snapshot.RunToken!=sim.RunToken||snapshot.Wave!=sim.Wave||
            !state.Update(scope,owner,sim.Tick,position,movement,observations.AsSpan(0,count),elapsed,sim.Paused)){Reset();return false;}
        bool melee=snapshot.Event is GameEvent e&&e.Kind==EventKind.Melee&&e.Source==player.Id&&e.RunToken==sim.RunToken&&e.Wave==sim.Wave;
        NVec2 direction=default;
        if(melee){var acceptedEvent=snapshot.Event.Value;direction=new(acceptedEvent.To.X-acceptedEvent.From.X,acceptedEvent.To.Y-acceptedEvent.From.Y); }
        if(!state.SyncAcceptedGesture(scope,owner,snapshot.Sequence,melee,snapshot.Active,direction)||
            !state.Update(scope,owner,sim.Tick,position,movement,observations.AsSpan(0,count),0,sim.Paused)){Reset();return false;}
        desired=state.Current;
        if(realMeleePriority&&realAim.Resolve(presenter,presenter.GestureOwnerEpoch,snapshot.Sequence,snapshot.Active,sim.Paused,out var actual,out bool fresh))
        {
            if(fresh)state.AdoptActualMeleeTarget(scope,owner,actual.Target,position,observations.AsSpan(0,count));
            if(!state.Update(scope,owner,sim.Tick,position,movement,observations.AsSpan(0,count),0,sim.Paused)){Reset();return false;}
            desired=new(actual.Direction,HeadingReason.AuthoritativeMeleeAim,state.Current.AttentionTarget);
        }
        if(!realMeleePriority)realAim.ClearPresentation();
        OutputValid=true;return true;
    }
}
