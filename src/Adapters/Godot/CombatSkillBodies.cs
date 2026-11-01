using Godot;
using Hmi.Core;
using System;
using System.Collections.Generic;

namespace Hmi.GodotAdapter;

/// <summary>Adapter-only, preallocated geometry. Uses exactly the caller's effect-slot indices.</summary>
internal sealed class CombatSkillBodies : IDisposable
{
    internal enum BodyKind { None, Melee, Ricochet }
    private sealed class Slot
    {
        internal MeshInstance3D Main;
        internal MultiMeshInstance3D Crystals;
        internal MultiMesh Instances;
        internal StandardMaterial3D Material;
        internal BodyKind Kind;
    }
    private readonly Slot[] _slots;
    private ArrayMesh _arc,_link,_crystal;
    private bool _disposed;
    internal int Capacity=>_slots.Length;
    internal int NodeCount=>_disposed?0:_slots.Length*2;
    internal int ActiveCount {get {int n=0;foreach(var s in _slots)if(s!=null&&s.Kind!=BodyKind.None)n++;return n;}}
    internal CombatSkillBodies(Node3D world,int capacity)
    {
        if(world==null||capacity<=0)throw new ArgumentException("World owner and positive shared slot count required");
        _slots=new Slot[capacity];
        try
        {
            _arc=Geometry.Arc();_link=Geometry.Link();_crystal=Geometry.Crystal();
            for(int i=0;i<capacity;i++)
            {
                var s=new Slot();_slots[i]=s;
                s.Material=new StandardMaterial3D{VertexColorUseAsAlbedo=true,ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,Transparency=BaseMaterial3D.TransparencyEnum.Alpha,CullMode=BaseMaterial3D.CullModeEnum.Disabled};
                s.Instances=new MultiMesh{TransformFormat=MultiMesh.TransformFormatEnum.Transform3D,UseColors=true,Mesh=_crystal,InstanceCount=3};
                s.Main=new MeshInstance3D{Name=$"SkillBodyA_{i:D2}",Mesh=_link,MaterialOverride=s.Material,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off,Visible=false};
                s.Crystals=new MultiMeshInstance3D{Name=$"SkillCrystals_{i:D2}",Multimesh=s.Instances,MaterialOverride=s.Material,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off,Visible=false};
                world.AddChild(s.Main);world.AddChild(s.Crystals);
            }
        }
        catch{Dispose();throw;}
    }
    // No node/mesh/material/list allocation here. All geometry is adapter-owned, never GD.Load.
    internal bool Show(int slot,BodyKind kind,Vec2 from,Vec2 to)
    {
        if(_disposed||slot<0||slot>=_slots.Length)return false;
        Hide(slot);
        float dx=to.X-from.X,dz=to.Y-from.Y,length=MathF.Sqrt(dx*dx+dz*dz);
        if(!float.IsFinite(length)||length<.001f||length>40||kind==BodyKind.None)return false;
        var s=_slots[slot];if(!GodotObject.IsInstanceValid(s.Main)||!GodotObject.IsInstanceValid(s.Crystals))return false;s.Kind=kind;
        var origin=new Vector3(from.X,.55f,from.Y);var rotation=new Vector3(0,Mathf.Atan2(dx,dz),0);
        s.Material.AlbedoColor=Colors.White;
        s.Main.Position=origin;s.Main.Rotation=rotation;s.Main.Visible=true;
        s.Crystals.Position=origin;s.Crystals.Rotation=rotation;s.Crystals.Scale=Vector3.One;
        if(kind==BodyKind.Melee)
        {
            // Single-target arc, not a filled damage cone. Radius follows the actual event endpoint.
            s.Main.Mesh=_arc;s.Main.Scale=new Vector3(length/1.84f,1,length/1.84f);
            s.Material.CullMode=BaseMaterial3D.CullModeEnum.Back;
        }
        else
        {
            s.Main.Mesh=_link;s.Main.Scale=new Vector3(1,1,length);
            s.Material.CullMode=BaseMaterial3D.CullModeEnum.Disabled;
            float cap=Math.Min(1,length/.6f);
            Place(s,0,0,new Vector3(.10f,.075f,.11f)*cap,new Color(1,.83f,.73f));
            Place(s,1,length*.5f,new Vector3(.18f,.12f,.25f)*cap,Colors.White);
            Place(s,2,length,new Vector3(.15f,.105f,.18f)*cap,Colors.White);
            s.Crystals.Visible=true;
        }
        return true;
    }
    private static void Place(Slot s,int instance,float z,Vector3 size,Color color)
    {
        s.Instances.SetInstanceTransform(instance,new Transform3D(Basis.Identity.Scaled(size),new Vector3(0,.13f,z)));
        s.Instances.SetInstanceColor(instance,color);
    }
    internal void Phase(int slot,float age,bool reducedMotion=false)
    {
        if(_disposed||slot<0||slot>=_slots.Length)return;
        var s=_slots[slot];if(s.Kind==BodyKind.None)return;
        if(!GodotObject.IsInstanceValid(s.Main)){Hide(slot);return;}
        float fade=Math.Clamp((age-.35f)/.65f,0,1);
        if(!float.IsFinite(fade))fade=1;
        s.Material.AlbedoColor=new Color(1,1,1,1-fade);
        // Keep every endpoint present; only fade. No travelling head or delayed damage implication.
        // Shape contraction intentionally omitted here: pooled length and crystal dimensions stay exact.
    }
    internal void Hide(int slot)
    {
        if(_disposed||slot<0||slot>=_slots.Length)return;
        var s=_slots[slot];if(s==null)return;
        s.Kind=BodyKind.None;if(GodotObject.IsInstanceValid(s.Main))s.Main.Visible=false;if(GodotObject.IsInstanceValid(s.Crystals))s.Crystals.Visible=false;
    }
    internal void Reset(){if(_disposed)return;for(int i=0;i<_slots.Length;i++)Hide(i);}
    public void Dispose()
    {
        if(_disposed)return;_disposed=true;
        foreach(var s in _slots)
        {
            if(s==null)continue;
            // Detach only resources created by this owner; never Dispose property getters/shared assets.
            if(GodotObject.IsInstanceValid(s.Main)){s.Main.Visible=false;s.Main.MaterialOverride=null;s.Main.Mesh=null;s.Main.QueueFree();}
            if(GodotObject.IsInstanceValid(s.Crystals)){s.Crystals.Visible=false;s.Crystals.MaterialOverride=null;s.Crystals.Multimesh=null;s.Crystals.QueueFree();}
            if(s.Instances!=null){s.Instances.Mesh=null;s.Instances.Dispose();}
            s.Material?.Dispose();s.Kind=BodyKind.None;
        }
        _arc?.Dispose();_link?.Dispose();_crystal?.Dispose();
    }
    private sealed class Geometry
    {
        private readonly List<Vector3> _vertices=new(),_normals=new();private readonly List<Color> _colors=new();
        private static readonly Color Cream=new("fff1be"),Edge=new("815148"),Warm=new("f3ad74"),Purple=new("ae91db");
        private void Tri(Vector3 a,Vector3 b,Vector3 c,Color color)
        {var n=-(b-a).Cross(c-a).Normalized();_vertices.Add(a);_vertices.Add(b);_vertices.Add(c);for(int i=0;i<3;i++){_normals.Add(n);_colors.Add(color);}}
        private void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color color){Tri(a,b,c,color);Tri(a,c,d,color);}
        private ArrayMesh Finish()
        {
            using var arrays=new Godot.Collections.Array();arrays.Resize((int)Mesh.ArrayType.Max);
            arrays[(int)Mesh.ArrayType.Vertex]=_vertices.ToArray();arrays[(int)Mesh.ArrayType.Normal]=_normals.ToArray();arrays[(int)Mesh.ArrayType.Color]=_colors.ToArray();
            var mesh=new ArrayMesh();mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,arrays);return mesh;
        }
        internal static ArrayMesh Arc()
        {
            var g=new Geometry();const float radius=1.84f,thickness=.28f,low=.025f,high=.09f;const int sections=16;
            Vector3 P(float r,float a,float y)=>new(Mathf.Sin(a)*r,y,Mathf.Cos(a)*r);
            for(int i=0;i<sections;i++)
            {
                float a=-.68f+.72f*i/sections,b=-.68f+.72f*(i+1)/sections;
                float wa=.22f+.78f*Mathf.Sin(Mathf.Pi*i/sections),wb=.22f+.78f*Mathf.Sin(Mathf.Pi*(i+1)/sections);
                var il=P(radius-thickness*wa,a,low);var ih=P(radius-thickness*wa,a,high);var ol=P(radius,a,low);var oh=P(radius,a,high);
                var jl=P(radius-thickness*wb,b,low);var jh=P(radius-thickness*wb,b,high);var pl=P(radius,b,low);var ph=P(radius,b,high);
                g.Quad(ih,jh,ph,oh,Cream);g.Quad(il,ol,pl,jl,Edge);g.Quad(ol,oh,ph,pl,Warm);g.Quad(il,jl,jh,ih,Edge);
                if(i==0)g.Quad(il,ih,oh,ol,Warm);if(i==sections-1)g.Quad(jl,pl,ph,jh,Warm);
            }
            return g.Finish();
        }
        internal static ArrayMesh Link()
        {var g=new Geometry();g.Quad(new(-.018f,.13f,0),new(.018f,.13f,0),new(.018f,.13f,1),new(-.018f,.13f,1),new Color("b198bc"));return g.Finish();}
        internal static ArrayMesh Crystal()
        {
            var g=new Geometry();Vector3 P(int side){float a=side*Mathf.Tau/8;return new(Mathf.Cos(a),Mathf.Sin(a),0);}
            for(int side=0;side<8;side++)
            {Color face=side is 1 or 2?Cream:side is 5 or 6?Edge:Purple;g.Tri(new(0,0,-1),P(side+1),P(side),face);g.Tri(new(0,0,1),P(side),P(side+1),side%2==0?face:Purple);}
            return g.Finish();
        }
    }
}
