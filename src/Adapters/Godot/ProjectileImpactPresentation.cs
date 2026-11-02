using Godot;
using Hmi.Core;
using System;
using System.Collections.Generic;
namespace Hmi.GodotAdapter;
/// <summary>Real enemy-impact contact symbol only. Bounded, optional and independent of flight bodies.</summary>
internal sealed class ProjectileImpactPresentation : IDisposable
{
 internal const int Capacity=8;internal const float Lifetime=.10f;
 readonly Node3D root;readonly Camera3D camera;readonly ProjectileEventLedger ledger;
 readonly MeshInstance3D[] nodes=new MeshInstance3D[Capacity];readonly StandardMaterial3D[] materials=new StandardMaterial3D[Capacity];
 readonly float[] age=new float[Capacity];readonly bool[] active=new bool[Capacity];readonly int[] victimSlots=new int[Capacity];readonly ProjectileTransition[] events=new ProjectileTransition[Capacity];
 readonly ArrayMesh mesh;bool disposed;internal readonly bool Enabled;internal readonly bool SurfaceEnabled;
 readonly ProjectileImpactSurface surface;readonly ProjectileImpactSurface.Binding[] bindings=new ProjectileImpactSurface.Binding[Capacity];readonly ProjectileImpactSurface.Result[] anchors=new ProjectileImpactSurface.Result[Capacity];internal long SurfaceSuppressed{get;private set;}
 internal object SurfaceStatistics=>new{surface.FrameTriangles,surface.TotalTriangles,surface.CacheMisses,surface.CacheRejects,surface.CacheMicroseconds,surface.RayMicroseconds,surface.MaxRayMicroseconds,surface.AmbiguityDrops,surface.BoundUpdates,SurfaceSuppressed};
 internal void BindActors(GameSimulation sim,Node3D[] actors,EntityId[] ids)=>surface.BindActors(sim,actors,ids);
 internal int ActiveCount{get;private set;}
 internal readonly record struct Snapshot(ProjectileId Id,long Tick,int Hits,EntityId Target,float Age,Vector3 Position,bool SurfaceBound,ProjectileImpactSurface.Result Anchor);
 internal Snapshot[] Inspect(){var a=new List<Snapshot>();for(int i=0;i<Capacity;i++)if(active[i])a.Add(new(events[i].Id,events[i].Tick,events[i].HitsCompleted,events[i].Target,age[i],nodes[i].Position,bindings[i]!=null,anchors[i]));return a.ToArray();}
 internal ProjectileImpactPresentation(Node3D owner,Camera3D camera,ProjectileEventLedger ledger,bool enabled){this.camera=camera;this.ledger=ledger;Enabled=enabled;SurfaceEnabled=System.Environment.GetEnvironmentVariable("PROJECTILE_SURFACE_OFF")!="1";surface=new ProjectileImpactSurface(camera);root=new Node3D{Name="ProjectileActualImpactPool"};owner.AddChild(root);mesh=CreateMesh();for(int i=0;i<Capacity;i++){materials[i]=new StandardMaterial3D{ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,VertexColorUseAsAlbedo=true,Transparency=BaseMaterial3D.TransparencyEnum.Alpha,CullMode=BaseMaterial3D.CullModeEnum.Disabled,NoDepthTest=false};nodes[i]=new MeshInstance3D{Name="Impact"+i,Mesh=mesh,MaterialOverride=materials[i],Visible=false,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off,CustomAabb=new Aabb(new Vector3(-.22f,-.22f,-.01f),new Vector3(.44f,.44f,.02f))};root.AddChild(nodes[i]);}}
 static ArrayMesh CreateMesh(){var v=new List<Vector3>();var colors=new List<Color>();void Tri(Vector3 a,Vector3 b,Vector3 c,Color color){v.Add(a);v.Add(b);v.Add(c);colors.Add(color);colors.Add(color);colors.Add(color);}for(int i=0;i<3;i++){float angle=i*Mathf.Tau/3+.15f;Vector3 d=new(Mathf.Cos(angle),Mathf.Sin(angle),0),side=new(-d.Y,d.X,0);float length=i==0?.20f:i==1?.17f:.15f;var inner=d*.035f;var left=d*.095f+side*.048f;var right=d*.095f-side*.048f;var tip=d*length;Tri(inner,left,tip,new Color("e77f62"));Tri(inner,tip,right,new Color("9b4951"));}using var arrays=new Godot.Collections.Array();arrays.Resize((int)Mesh.ArrayType.Max);arrays[(int)Mesh.ArrayType.Vertex]=v.ToArray();arrays[(int)Mesh.ArrayType.Color]=colors.ToArray();var m=new ArrayMesh();m.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,arrays);return m;}
 internal void Render(double delta,bool paused,bool visible){if(disposed)return;surface.ResetFrameCounters();if(!visible){Clear();return;}if(paused){ledger.CancelPending();return;}ledger.Advance(delta);surface.BeginRender();
  for(int i=0;i<Capacity;i++)if(active[i]){age[i]+=(float)Math.Clamp(delta,0,.25);if(age[i]>=Lifetime){active[i]=false;nodes[i].Visible=false;bindings[i]=null;ledger.EndActive(victimSlots[i]);}else {if(bindings[i]!=null){if(surface.Update(bindings[i],out var position))nodes[i].Position=position;else{active[i]=false;nodes[i].Visible=false;bindings[i]=null;ledger.EndActive(victimSlots[i]);SurfaceSuppressed++;continue;}}materials[i].AlbedoColor=new Color(1,1,1,1-age[i]/Lifetime);}}
  int spawned=0;for(int p=0;p<ledger.PendingCount;p++){var e=ledger.Pending[p];int free=Array.FindIndex(active,x=>!x);if(!Enabled)continue;
   ProjectileImpactSurface.Binding binding=null;ProjectileImpactSurface.Result result=new("physical-mode",new Vector3(e.Position.X,ProjectilePresentation.DisplayY,e.Position.Y),new Vector3(e.Position.X,ProjectilePresentation.DisplayY,e.Position.Y),e.Target,default,0,0,0,0,false);
   if(SurfaceEnabled){surface.TryCreate(e,out binding,out result,out bool suppress);if(suppress){SurfaceSuppressed++;continue;}}
   if(!ledger.TryOnset(e.Target,spawned,free>=0,out int victim))continue;active[free]=true;age[free]=0;victimSlots[free]=victim;events[free]=e;bindings[free]=binding;anchors[free]=result;nodes[free].Position=binding!=null?result.Visual:new Vector3(e.Position.X,ProjectilePresentation.DisplayY,e.Position.Y);nodes[free].Basis=camera!=null?camera.GlobalBasis:Basis.Identity;materials[free].AlbedoColor=Colors.White;nodes[free].Visible=true;spawned++;}
  ledger.EndEligibleRender();ActiveCount=0;foreach(bool a in active)if(a)ActiveCount++;
 }
 internal void Clear(bool cancelLedger=true){if(disposed)return;for(int i=0;i<Capacity;i++){nodes[i].Visible=false;active[i]=false;}ActiveCount=0;Array.Clear(bindings);if(cancelLedger)ledger.ClearPresentation();}
 public void Dispose(){if(disposed)return;Clear();disposed=true;surface.Clear();foreach(var n in nodes){n.Mesh=null;n.MaterialOverride=null;}root.QueueFree();foreach(var m in materials)m.Dispose();mesh.Dispose();}
}
