using System;
using Godot;
using Hmi.Core;
namespace Hmi.GodotAdapter;

/// <summary>Optional, bounded assembly cues from real Activated notifications only. Never writes Core.</summary>
internal sealed class MechanicalArrivalVisuals : IDisposable
{
    public const int MaximumCues=16, InstancesPerCue=15, DurationTicks=16;
    private struct Cue { public bool Used; public EntityId Entity; public SpawnReservationId Reservation; public Vec2 Position; public long Birth,FirstRender; }
    private readonly Cue[] _cues=new Cue[MaximumCues];
    private readonly MultiMeshInstance3D _node;
    private readonly MultiMesh _instances;
    private int _run=-1,_wave=-1;private long _observed=-1;
    private bool _disposed;
    public int Count {get;private set;}
    public int Accepted {get;private set;}
    public int SpatiallySuppressed {get;private set;}
    public int BudgetDropped {get;private set;}
    public int Replaced {get;private set;}
    public int InstanceCount=>Count*InstancesPerCue;
    public MechanicalArrivalVisuals(Node3D owner)
    {
        using var material=new StandardMaterial3D { ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,
            VertexColorUseAsAlbedo=true,AlbedoColor=Colors.White,Transparency=BaseMaterial3D.TransparencyEnum.Alpha,
            SpecularMode=BaseMaterial3D.SpecularModeEnum.Disabled };
        using var mesh=new BoxMesh { Size=Vector3.One,Material=material };
        _instances=new MultiMesh {TransformFormat=MultiMesh.TransformFormatEnum.Transform3D,UseColors=true,
            Mesh=mesh,CustomAabb=new Aabb(new Vector3(-GameSimulation.ArenaWidth/2-1,-.1f,-GameSimulation.ArenaHeight/2-1),
                new Vector3(GameSimulation.ArenaWidth+2,.5f,GameSimulation.ArenaHeight+2)),
            InstanceCount=MaximumCues*InstancesPerCue,VisibleInstanceCount=0};
        _node=new MultiMeshInstance3D {Name="MechanicalArrivalCues",Multimesh=_instances,
            CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};owner.AddChild(_node);
    }
    private bool BindScope(GameSimulation g)
    {
        if(_disposed)return false;
        if(g.RunToken!=_run||g.Wave!=_wave) {Clear();_observed=-1;_run=g.RunToken;_wave=g.Wave;}
        if(g.Phase!=RunPhase.Combat){Clear();return false;}
        return true;
    }
    public void Observe(GameSimulation g)
    {
        if(!BindScope(g)||g.Tick<=_observed)return;
        _observed=g.Tick;
        if(!g.TryGetEntity(g.PlayerId,out var player)){Clear();return;}
        Prune(g,false);
        for(int i=0;i<g.SpawnTransitionCount;i++)
        {
            var e=g.GetSpawnTransition(i);
            if(e.Kind!=SpawnTransitionKind.Activated||e.Tick!=g.Tick||e.Tick<0||
                e.Reservation.Id.RunToken!=g.RunToken||e.Reservation.Id.Wave!=g.Wave||e.Reservation.Id.Serial<=0||
                !float.IsFinite(e.Reservation.Position.X)||!float.IsFinite(e.Reservation.Position.Y)||
                !g.TryGetEntity(e.Entity,out var entity)||entity.Kind!=EntityKind.Enemy)continue;
            int free=-1,furthest=-1;float farDistance=-1;bool suppress=false;
            for(int j=0;j<_cues.Length;j++)
            {
                if(!_cues[j].Used){if(free<0)free=j;continue;}
                if(_cues[j].Entity==e.Entity||_cues[j].Reservation.Equals(e.Reservation.Id)){suppress=true;break;}
                if(Cell(_cues[j].Position.X)==Cell(e.Reservation.Position.X)&&Cell(_cues[j].Position.Y)==Cell(e.Reservation.Position.Y))
                {SpatiallySuppressed++;suppress=true;break;}
                float distance=DistanceSquared(_cues[j].Position,player.Position);
                if(distance>farDistance){farDistance=distance;furthest=j;}
            }
            if(suppress)continue;
            int slot=free;
            if(slot<0)
            {
                if(furthest<0||DistanceSquared(e.Reservation.Position,player.Position)>=farDistance){BudgetDropped++;continue;}
                slot=furthest;Replaced++;
            }
            _cues[slot]=new Cue {Used=true,Entity=e.Entity,Reservation=e.Reservation.Id,Position=e.Reservation.Position,Birth=e.Tick,FirstRender=-1};Accepted++;
        }
    }
    private static int Cell(float v)=>(int)MathF.Floor(v/2);
    private static float DistanceSquared(Vec2 a,Vec2 b){float x=a.X-b.X,y=a.Y-b.Y;return x*x+y*y;}
    private void Prune(GameSimulation g,bool rendering)
    {
        for(int i=0;i<_cues.Length;i++)if(_cues[i].Used)
        {
            ref var c=ref _cues[i];
            if(!g.TryGetEntity(c.Entity,out var e)||e.Kind!=EntityKind.Enemy||c.Reservation.RunToken!=g.RunToken||c.Reservation.Wave!=g.Wave||g.Tick<c.Birth)
            {c=default;continue;}
            // Host catch-up is at most8 ticks, less than this16-tick life. New cues therefore
            // survive their first render without reviving old, never-rendered notifications.
            if(g.Tick-c.Birth>=DurationTicks){c=default;continue;}
            if(rendering&&c.FirstRender<0)c.FirstRender=g.Tick;
        }
    }
    public void Update(GameSimulation g)
    {
        if(!BindScope(g))return;
        Prune(g,true);Count=0;
        for(int i=0;i<_cues.Length;i++)if(_cues[i].Used)
        {
            var c=_cues[i];long age=g.Tick-c.Birth;
            float t=Math.Clamp((float)age/DurationTicks,0,1);
            float eased=t*t*(3-2*t),alpha=t<.35f?1:Math.Clamp((1-t)/.65f,0,1);
            float radius=Mathf.Lerp(.55f,.32f,eased);
            int offset=Count*InstancesPerCue;
            for(int segment=0;segment<6;segment++)
            {
                float angle=segment*Mathf.Tau/6;var basis=new Basis(Vector3.Up,angle);
                var p=new Vector3(c.Position.X+Mathf.Sin(angle)*radius,.045f,c.Position.Y+Mathf.Cos(angle)*radius);
                // Initial twelve instances exactly match the completed pending ground marker.
                Set(offset+segment*2,basis,new Vector3(.42f*(1-.30f*t),.012f,.095f),p,new Color("51344e"),alpha);
                Set(offset+segment*2+1,basis,new Vector3(.35f*(1-.30f*t),.008f,.047f),p+Vector3.Up*.012f,new Color("ec765c"),alpha);
            }
            for(int piece=0;piece<3;piece++)
            {
                float angle=piece*Mathf.Tau/3+Mathf.Pi/6;float r=Mathf.Lerp(.62f,.28f,eased);
                var p=new Vector3(c.Position.X+Mathf.Sin(angle)*r,Mathf.Lerp(.18f,.055f,eased),c.Position.Y+Mathf.Cos(angle)*r);
                Set(offset+12+piece,new Basis(Vector3.Up,angle),new Vector3(.14f,.035f,.075f),p,new Color("aebdca"),alpha);
            }
            Count++;
        }
        _instances.VisibleInstanceCount=InstanceCount;
    }
    private void Set(int i,Basis basis,Vector3 scale,Vector3 position,Color color,float alpha)
    {color.A=alpha;_instances.SetInstanceTransform(i,new Transform3D(basis.ScaledLocal(scale),position));_instances.SetInstanceColor(i,color);}
    internal bool TryGet(int index,out EntityId entity,out Vec2 origin,out long birth,out long firstRender)
    {
        entity=default;origin=default;birth=firstRender=0;if(_disposed||index<0)return false;
        for(int i=0,n=0;i<_cues.Length;i++)if(_cues[i].Used){if(n++!=index)continue;entity=_cues[i].Entity;origin=_cues[i].Position;birth=_cues[i].Birth;firstRender=_cues[i].FirstRender;return true;}return false;
    }
    public void Clear()
    {if(_disposed)return;Array.Clear(_cues);Count=Accepted=SpatiallySuppressed=BudgetDropped=Replaced=0;_instances.VisibleInstanceCount=0;}
    public void Dispose()
    {if(_disposed)return;Clear();_disposed=true;if(GodotObject.IsInstanceValid(_node))_node.QueueFree();_instances.Dispose();}
}
