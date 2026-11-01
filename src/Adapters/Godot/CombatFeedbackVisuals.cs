using Godot;
using Hmi.Core;
using System;
using System.Collections.Generic;

namespace Hmi.GodotAdapter;

/// <summary>Read-only, bounded combat cosmetics. Never changes actor transforms or authority.</summary>
public partial class CombatFeedbackVisuals : Control
{
    public const int PoolCapacity=48, SpawnBudget=12;
    private enum Cue { Strike, Ray, Impact, Blast, Hit, Death }
    private struct Effect { public bool Active,HasBody; public long BornFrame; public Cue Kind; public EntityId Target; public Vec2 From,To; public float Age,Duration; }
    private sealed class Actor
    {
        public EntityId Id; public Node3D Root; public EntityKind Kind; public Vec2 Position;
        public MeshInstance3D[] Meshes; public Material[] Previous;
        public StandardMaterial3D Tint; public float Remaining; public bool Applied; public long TintBornFrame;
    }
    private struct HitGate { public EntityId Id; public double Next; }
    private readonly Effect[] _pool=new Effect[PoolCapacity];
    private Actor[] _actors=Array.Empty<Actor>();
    private HitGate[] _hits=Array.Empty<HitGate>();
    private Camera3D _camera;
    private CombatSkillBodies _bodies;
    public int ActiveBodyCount=>_bodies?.ActiveCount??0;
    public int BodyNodeCount=>_bodies?.NodeCount??0;
    private int _run=-1,_wave=-1,_lastIndex=-1,_spawned;
    private long _lastTick=-1;
    private double _clock,_playerNext;
    private float _playerAge=1,_playerDuration=.32f;
    private Vec2 _playerPosition,_hurtFrom;
    private bool _paused,_combat,_disposed;
    // One epoch per host render; SetState/Consume catch-up calls never replenish it.
    private long _renderFrame,_playerBornFrame=-1;
    public int AcceptedEvents {get;private set;}
    public int RejectedEvents {get;private set;}
    public int DroppedEffects {get;private set;}
    public int CoalescedHits {get;private set;}
    public int PeakActive {get;private set;}
    public int ActiveCount {get {int n=0;foreach(var e in _pool)if(e.Active)n++;return n;}}
    public int SpawnedThisFrame=>_spawned;
    public bool PlayerCueActive=>_playerAge<_playerDuration;
    // The default is deliberately gentle: small local tint, <=2.86 pulses/s/entity,
    // no camera shake, fullscreen flash, hit stop or vibration.
    public void Initialize(int capacity,Camera3D camera,Node3D worldOwner=null)
    {
        if(capacity<=0 || _actors.Length>0)throw new InvalidOperationException("Feedback initializes once with positive capacity");
        _actors=new Actor[capacity];_hits=new HitGate[capacity];_camera=camera;
        MouseFilter=MouseFilterEnum.Ignore;
        if(worldOwner!=null)_bodies=new CombatSkillBodies(worldOwner,PoolCapacity);
    }
    public void BeginFrame(int run,int wave,bool paused,bool combat)
    {
        if(_disposed)return;
        _spawned=0;
        _renderFrame++;
        SetState(run,wave,paused,combat);
    }
    public void SetState(int run,int wave,bool paused,bool combat)
    {
        if(_disposed)return;
        if(run!=_run || wave!=_wave){Reset();_run=run;_wave=wave;}
        _paused=paused;_combat=combat;
        if(!combat)ClearCues();
    }
    public bool Consume(GameEvent e,int eventIndex,long currentTick,EntityId player)
    {
        if(_disposed || _paused || !_combat || e.RunToken!=_run || e.Wave!=_wave || e.Tick<0 || e.Tick>currentTick ||
            e.Tick<_lastTick || eventIndex<0 || eventIndex>=2048 || (e.Tick==_lastTick && eventIndex<=_lastIndex) ||
            !Finite(e.From) || !Finite(e.To) || !float.IsFinite(e.Value)) {RejectedEvents++;return false;}
        if(e.Tick!=_lastTick){_lastTick=e.Tick;_lastIndex=-1;}
        _lastIndex=eventIndex;
        if(e.Kind==EventKind.Damage && e.Target.Index>=0 && e.Target.Index<_actors.Length && _actors[e.Target.Index] is Actor target &&
            (target.Id.Generation>e.Target.Generation ||
             (target.Id==e.Target && (target.Kind==EntityKind.Companion || target.Kind==EntityKind.Pickup))))
        {RejectedEvents++;return false;}
        AcceptedEvents++;
        switch(e.Kind)
        {
            case EventKind.Melee: Spawn(Cue.Strike,e,.16f);break;
            case EventKind.Ray: Spawn(Cue.Ray,e,.13f);break;
            case EventKind.Impact: Spawn(Cue.Impact,e,.22f);break;
            case EventKind.Blast: Spawn(Cue.Blast,e,.24f);break;
            case EventKind.Damage:
                if(e.Value<=0)break;
                if(e.Target.Index>=0 && e.Target.Index<_actors.Length && _actors[e.Target.Index] is Actor bound && bound.Id==e.Target && (bound.Kind==EntityKind.Companion || bound.Kind==EntityKind.Pickup))break;
                if(e.Target==player)
                {
                    _playerPosition=e.To;_hurtFrom=e.From;
                    if(_clock>=_playerNext){_playerAge=0;_playerBornFrame=_renderFrame;_playerNext=_clock+.35;Tint(e.Target,true);}
                    else CoalescedHits++;
                    
                }
                else if(e.Target.Index>=0 && e.Target.Index<_hits.Length)
                {
                    ref var gate=ref _hits[e.Target.Index];
                    if(gate.Id==e.Target && _clock<gate.Next){CoalescedHits++;break;}
                    gate=new HitGate{Id=e.Target,Next=_clock+.35};
                    Tint(e.Target,false);Spawn(Cue.Hit,e,.18f);
                }
                break;
            case EventKind.Death:
                // The target is normally already destroyed: use its immutable position/full ID.
                // Replace this life's hit accent, never bind a recycled slot's new actor.
                for(int i=0;i<_pool.Length;i++)if(_pool[i].Active && _pool[i].Target==e.Target && _pool[i].Kind==Cue.Hit)_pool[i].Active=false;
                Spawn(Cue.Death,e,.30f);break;
        }
        PeakActive=Math.Max(PeakActive,ActiveCount);return true;
    }
    public void Bind(EntityView view,Node3D root)
    {
        int slot=view.Id.Index;if(slot<0 || slot>=_actors.Length)return;
        var old=_actors[slot];
        if(old!=null && old.Id==view.Id && old.Root==root){old.Position=view.Position;return;}
        Unbind(slot);
        var meshes=new List<MeshInstance3D>();
        if(view.Kind==EntityKind.Player || view.Kind==EntityKind.Enemy)Collect(root,meshes);
        var previous=new Material[meshes.Count];for(int i=0;i<meshes.Count;i++)previous[i]=meshes[i].MaterialOverlay;
        _actors[slot]=new Actor{Id=view.Id,Root=root,Kind=view.Kind,Position=view.Position,Meshes=meshes.ToArray(),Previous=previous};
    }
    public void Unbind(int slot)
    {
        if(slot<0 || slot>=_actors.Length)return;
        var a=_actors[slot];if(a==null)return;Restore(a);a.Tint?.Dispose();_actors[slot]=null;
    }
    public void Advance(float delta,bool paused,EntityView? player=null)
    {
        if(_disposed)return;
        if(player.HasValue)_playerPosition=player.Value.Position;
        if(paused || !_combat)return;
        if(!float.IsFinite(delta) || delta<=0)return;
        delta=Math.Min(.25f,delta);_clock+=delta;
        if(_playerBornFrame!=_renderFrame)_playerAge+=delta;
        for(int i=0;i<_pool.Length;i++)if(_pool[i].Active){if(_pool[i].BornFrame!=_renderFrame)_pool[i].Age+=delta;if(_pool[i].Age>=_pool[i].Duration){_pool[i].Active=false;_bodies?.Hide(i);}else if(_pool[i].HasBody)_bodies?.Phase(i,_pool[i].Age/_pool[i].Duration);}
        for(int i=0;i<_actors.Length;i++)
        {
            var a=_actors[i];if(a==null)continue;
            if(!GodotObject.IsInstanceValid(a.Root) || a.Root.IsQueuedForDeletion()){Unbind(i);continue;}
            if(a.Remaining<=0)continue;
            if(a.TintBornFrame!=_renderFrame)a.Remaining=Math.Max(0,a.Remaining-delta);
            if(a.Remaining<=0){Restore(a);continue;}
            float envelope=Mathf.Sin(Mathf.Pi*(1-a.Remaining/.22f));
            a.Tint.AlbedoColor=a.Kind==EntityKind.Player?new Color(.78f,.17f,.19f,.27f*envelope):new Color(1,.83f,.52f,.24f*envelope);
        }
        QueueRedraw();
    }
    public void Reset()
    {
        if(_disposed)return;
        ClearCues();for(int i=0;i<_actors.Length;i++)Unbind(i);Array.Clear(_hits);
        _lastTick=-1;_lastIndex=-1;_clock=0;_playerNext=0;
    }
    public override void _ExitTree(){Reset();_bodies?.Dispose();_disposed=true;}
    private void ClearCues(){_bodies?.Reset();Array.Clear(_pool);_playerAge=1;_playerBornFrame=-1;_spawned=0;for(int i=0;i<_actors.Length;i++)if(_actors[i]!=null){_actors[i].Remaining=0;_actors[i].TintBornFrame=-1;Restore(_actors[i]);}QueueRedraw();}
    private static bool Finite(Vec2 p)=>float.IsFinite(p.X)&&float.IsFinite(p.Y);
    private void Tint(EntityId id,bool player)
    {
        if(id.Index<0 || id.Index>=_actors.Length)return;var a=_actors[id.Index];
        if(a==null || a.Id!=id || (a.Kind==EntityKind.Companion || a.Kind==EntityKind.Pickup))return;
        if(a.Tint==null)a.Tint=new StandardMaterial3D {ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,Transparency=BaseMaterial3D.TransparencyEnum.Alpha,AlbedoColor=new Color(1,1,1,0),NoDepthTest=false};
        a.Remaining=.22f;a.Applied=true;a.TintBornFrame=_renderFrame;
        foreach(var m in a.Meshes)if(GodotObject.IsInstanceValid(m))m.MaterialOverlay=a.Tint;
    }
    private static void Restore(Actor a)
    {
        if(!a.Applied)return;
        for(int i=0;i<a.Meshes.Length;i++)if(GodotObject.IsInstanceValid(a.Meshes[i]))a.Meshes[i].MaterialOverlay=a.Previous[i];
        a.Applied=false;
    }
    private static void Collect(Node n,List<MeshInstance3D> result){if(n is MeshInstance3D m)result.Add(m);foreach(Node c in n.GetChildren())Collect(c,result);}
    private void Spawn(Cue kind,GameEvent e,float duration)
    {
        bool important=kind==Cue.Death;
        int slot=-1;
        // At budget saturation, a death may REPLACE a lower-priority accent, not
        // append a thirteenth cue. Nothing is queued for later playback.
        if(important && (_spawned>=SpawnBudget || ActiveCount==PoolCapacity))
            for(int i=0;i<_pool.Length;i++)if(_pool[i].Active && (_pool[i].Kind==Cue.Hit || _pool[i].Kind==Cue.Ray || _pool[i].Kind==Cue.Strike)){slot=i;break;}
        bool replacement=slot>=0;
        if(!replacement)
        {
            if(_spawned>=SpawnBudget){DroppedEffects++;return;}
            for(int i=0;i<_pool.Length;i++)if(!_pool[i].Active){slot=i;break;}
        }
        if(slot<0){DroppedEffects++;return;}
        if(!replacement)_spawned++;
        _bodies?.Hide(slot);
        var bodyKind=e.Kind==EventKind.Melee&&e.Mechanism==FeedbackMechanism.BasicMelee?CombatSkillBodies.BodyKind.Melee:
            e.Kind==EventKind.Ray&&e.Mechanism==FeedbackMechanism.Ricochet?CombatSkillBodies.BodyKind.Ricochet:CombatSkillBodies.BodyKind.None;
        bool body=bodyKind!=CombatSkillBodies.BodyKind.None&&_bodies!=null&&_bodies.Show(slot,bodyKind,e.From,e.To);
        _pool[slot]=new Effect{Active=true,HasBody=body,BornFrame=_renderFrame,Kind=kind,Target=e.Target,From=e.From,To=e.To,Duration=duration};
    }
    private bool Project(Vec2 world,float height,out Vector2 point)
    {
        point=default;if(!GodotObject.IsInstanceValid(_camera) || !_camera.IsInsideTree())return false;
        var p=new Vector3(world.X,height,world.Y);if(_camera.IsPositionBehind(p))return false;
        var screen=_camera.UnprojectPosition(p);if(!GetViewport().GetVisibleRect().HasPoint(screen))return false;
        point=GetGlobalTransformWithCanvas().AffineInverse()*screen;return float.IsFinite(point.X)&&float.IsFinite(point.Y);
    }
    public override void _Draw()
    {
        if(!_combat)return;
        foreach(var e in _pool)
        {
            if(!e.Active || e.HasBody || !Project(e.To,.55f,out var p))continue;
            float t=e.Age/e.Duration;float alpha=Math.Clamp((1-t)*1.6f,0,1)*.9f;
            Color cream=new(1,.86f,.49f,alpha);Color ink=new(.20f,.12f,.22f,alpha*.9f);
            if(e.Kind==Cue.Ray){if(Project(e.From,.55f,out var a)){DrawLine(a,p,ink,3.5f,true);DrawLine(a,p,cream,1.5f,true);}continue;}
            if(e.Kind==Cue.Strike || e.Kind==Cue.Impact)
            {
                if(!Project(e.From,.55f,out var a))continue;
                var d=p-a;float angle=d.Angle();var center=a+d*.55f;float radius=Math.Clamp(d.Length()*.38f,7,24);
                DrawArc(center,radius,angle-.8f,angle+.8f,12,ink,e.Kind==Cue.Impact?5:3.5f,true);
                DrawArc(center,radius,angle-.8f,angle+.8f,12,cream,e.Kind==Cue.Impact?3:1.5f,true);continue;
            }
            if(e.Kind==Cue.Death)
            {
                for(int j=0;j<5;j++){var direction=Vector2.FromAngle(j*Mathf.Tau/5);var start=p+direction*(4+12*t);var end=start+direction*(5*(1-t));DrawLine(start,end,ink,4,true);DrawLine(start,end,cream,2,true);}
                continue;
            }
            if(e.Kind==Cue.Blast){DrawArc(p,6+22*t,0,Mathf.Tau,24,ink,3.5f,true);DrawArc(p,6+22*t,0,Mathf.Tau,24,cream,1.5f,true);continue;}
            // Hit has only three short accents, not another attack tracer.
            for(int j=0;j<3;j++){var d=Vector2.FromAngle(j*Mathf.Tau/3-.4f);DrawLine(p+d*(3+4*t),p+d*(7+6*t),ink,3.5f,true);DrawLine(p+d*(3+4*t),p+d*(7+6*t),cream,1.5f,true);}
        }
        if(PlayerCueActive && Project(_playerPosition,.65f,out var player))
        {
            float t=_playerAge/_playerDuration;float a=.8f*(1-t);
            float angle=0;if(Project(_hurtFrom,.65f,out var source))angle=(source-player).Angle();
            DrawArc(player,20+5*t,angle-1.15f,angle+1.15f,18,new Color(.74f,.12f,.17f,a),4,true);
            DrawArc(player,23+5*t,angle-1.15f,angle+1.15f,18,new Color(1,.84f,.7f,a*.5f),1,true);
        }
    }
}
