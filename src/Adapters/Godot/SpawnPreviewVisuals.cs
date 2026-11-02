using System;
using Godot;
using Hmi.Core;

namespace Hmi.GodotAdapter;

/// <summary>Read-only ground warnings. Pending snapshots, never lossy events, are authoritative.</summary>
internal sealed class SpawnPreviewVisuals : IDisposable
{
    private readonly MultiMeshInstance3D _node;
    private readonly MultiMesh _instances;
    private readonly PendingSpawnView[] _views;
    private readonly float[] _progress;
    private bool _disposed;
    public int Count { get; private set; }
    public int Capacity => _views.Length;
    public int InstanceCount => Count * 12;
    public SpawnPreviewVisuals(Node3D owner, int capacity)
    {
        if(capacity<=0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _views=new PendingSpawnView[capacity]; _progress=new float[capacity];
        using var mesh=new BoxMesh { Size=Vector3.One };
        using var material=new StandardMaterial3D {
            ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,
            VertexColorUseAsAlbedo=true, AlbedoColor=Colors.White,
            SpecularMode=BaseMaterial3D.SpecularModeEnum.Disabled
        };
        mesh.Material=material;
        _instances=new MultiMesh { TransformFormat=MultiMesh.TransformFormatEnum.Transform3D,
            UseColors=true,Mesh=mesh,InstanceCount=checked(capacity*12),VisibleInstanceCount=0 };
        _node=new MultiMeshInstance3D { Name="SpawnGroundWarnings",Multimesh=_instances,
            CastShadow=GeometryInstance3D.ShadowCastingSetting.Off };
        owner.AddChild(_node);
    }
    public void Update(GameSimulation simulation)
    {
        if(_disposed) return;
        Count=0;
        if(simulation.Phase!=RunPhase.Combat) { _instances.VisibleInstanceCount=0;return; }
        if(simulation.PendingSpawnCount>Capacity) throw new InvalidOperationException("Pending spawn capacity exceeds marker capacity; warnings must not be silently omitted.");
        for(int i=0;i<simulation.PendingSpawnCount;i++)
        {
            var view=simulation.GetPendingSpawn(i);
            if(view.Id.RunToken!=simulation.RunToken || view.Id.Wave!=simulation.Wave || view.Id.Serial<=0)
                throw new InvalidOperationException("Foreign spawn snapshot scope.");
            float progress=Math.Clamp((float)(simulation.Tick-view.ScheduledTick)/WaveRules.SpawnPreviewTicks,0,1);
            _views[Count]=view; _progress[Count]=progress;
            int filled=Math.Clamp((int)MathF.Floor(progress*6),0,5);
            for(int segment=0;segment<6;segment++)
            {
                float angle=segment*Mathf.Tau/6;
                var position=new Vector3(view.Position.X+Mathf.Sin(angle)*.55f,.045f,view.Position.Y+Mathf.Cos(angle)*.55f);
                var rotation=new Basis(Vector3.Up,angle);
                int offset=Count*12+segment*2;
                _instances.SetInstanceTransform(offset,new Transform3D(rotation.ScaledLocal(new Vector3(.42f,.012f,.095f)),position));
                _instances.SetInstanceColor(offset,new Color("51344e"));
                _instances.SetInstanceTransform(offset+1,new Transform3D(rotation.ScaledLocal(new Vector3(.35f,.008f,.047f)),position+Vector3.Up*.012f));
                // Monotonic fill, not a repeated flash. At activation the whole marker disappears.
                _instances.SetInstanceColor(offset+1,segment<=filled?new Color("ec765c"):new Color("e4cfab"));
            }
            Count++;
        }
        _instances.VisibleInstanceCount=Count*12;
    }
    public bool TryGet(int index,out PendingSpawnView view,out float progress)
    {
        view=default;progress=0;
        if(_disposed || index<0 || index>=Count)return false;
        view=_views[index];progress=_progress[index];return true;
    }
    public void Clear()
    {
        if(_disposed)return;
        Count=0;_instances.VisibleInstanceCount=0;
    }
    public void Dispose()
    {
        if(_disposed)return;
        Clear();_disposed=true;
        if(GodotObject.IsInstanceValid(_node))_node.QueueFree();
        _instances.Dispose();
    }
}
