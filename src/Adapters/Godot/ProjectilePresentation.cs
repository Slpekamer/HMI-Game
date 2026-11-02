using Godot;
using Hmi.Core;
using System;
using System.Collections.Generic;
namespace Hmi.GodotAdapter;
/// <summary>Read-only authoritative flight. No prediction, collision, damage or terminal replay.</summary>
internal sealed class ProjectilePresentation : IDisposable
{
 internal const float DisplayY=.55f,MaxTail=.18f;
 readonly Node3D root;readonly MeshInstance3D[] bodies=new MeshInstance3D[ProjectileRules.Capacity],tails=new MeshInstance3D[ProjectileRules.Capacity];
 readonly Vector3[] lastDraw=new Vector3[ProjectileRules.Capacity];
 readonly ProjectileId[] ids=new ProjectileId[ProjectileRules.Capacity];readonly int[] hops=new int[ProjectileRules.Capacity];readonly bool[] bound=new bool[ProjectileRules.Capacity];
 readonly StandardMaterial3D[] mats=new StandardMaterial3D[2],tailMats=new StandardMaterial3D[2];readonly ArrayMesh[] meshes=new ArrayMesh[2];readonly ArrayMesh tailMesh;
 GameSimulation simulation;int run=-1,wave=-1;bool disposed;float heldAlpha;
 readonly ProjectileEventLedger ledger;readonly ProjectileImpactPresentation impacts;
 internal long Captured=>ledger.Captured;internal long Launches=>ledger.Launches;internal long Impacts=>ledger.Impacts;internal long TransitionLoss=>ledger.TransitionLoss;internal long DuplicateTransitions=>ledger.Duplicates;
 internal ProjectileImpactPresentation ImpactPresentation=>impacts;
 internal int ActiveCount{get;private set;}internal int TailCount{get;private set;}internal float Interpolation=>heldAlpha;
 internal readonly record struct Snapshot(ProjectileId Id,int Hits,Vector3 Body,Vector3 TailStart,Vector3 TailEnd,bool TailVisible);
 internal Snapshot[] Inspect(){var a=new List<Snapshot>();for(int i=0;i<bodies.Length;i++)if(bound[i]&&bodies[i].Visible){Vector3 half=tails[i].GlobalBasis.Z*.5f;a.Add(new(ids[i],hops[i],bodies[i].Position,tails[i].Position-half,tails[i].Position+half,tails[i].Visible));}return a.ToArray();}
 internal ProjectilePresentation(Node3D owner,Camera3D camera=null,ProjectileEventLedger eventLedger=null){ledger=eventLedger??new ProjectileEventLedger();impacts=new ProjectileImpactPresentation(owner,camera,ledger,System.Environment.GetEnvironmentVariable("PROJECTILE_IMPACT_OFF")!="1");root=new Node3D{Name="ProjectileFlightPool"};owner.AddChild(root);tailMesh=CreateTail();
  for(int k=0;k<2;k++){meshes[k]=CreateBody(k==0);mats[k]=new StandardMaterial3D{ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,VertexColorUseAsAlbedo=true,CullMode=BaseMaterial3D.CullModeEnum.Disabled,NoDepthTest=false};tailMats[k]=new StandardMaterial3D{ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,Transparency=BaseMaterial3D.TransparencyEnum.Alpha,AlbedoColor=new Color(k==0?"23b7c5":"e87925"){A=.30f},CullMode=BaseMaterial3D.CullModeEnum.Disabled,NoDepthTest=false};}
  for(int i=0;i<bodies.Length;i++){bodies[i]=new MeshInstance3D{Name="Bullet"+i,Visible=false,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off,CustomAabb=new Aabb(new Vector3(-.14f,-.14f,-.16f),new Vector3(.28f,.28f,.32f))};tails[i]=new MeshInstance3D{Name="Tail"+i,Mesh=tailMesh,Visible=false,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off,CustomAabb=new Aabb(new Vector3(-.5f,-.5f,-.5f),Vector3.One)};root.AddChild(bodies[i]);root.AddChild(tails[i]);}
 }
 static ArrayMesh CreateBody(bool crystal)
 {
  var v=new List<Vector3>();var c=new List<Color>();
  Color main=new(crystal?"23b7c5":"e87925"),dark=new(crystal?"155d77":"8c3f27"),light=new(crystal?"88ddd9":"ffc16b");
  // Broad central 60%, tapered end 20% each. Eight perimeter facets; one narrow light facet.
  Vector2[] cross={new(.14f,0),new(.10f,.10f),new(.025f,.14f),new(-.025f,.14f),new(-.10f,.10f),new(-.14f,0),new(-.10f,-.10f),new(.10f,-.10f)};
  void Tri(Vector3 a,Vector3 b,Vector3 d,Color color){if((b-a).Cross(d-a).Dot((a+b+d)/3)<0)(b,d)=(d,b);v.Add(a);v.Add(b);v.Add(d);c.Add(color);c.Add(color);c.Add(color);}
  for(int j=0;j<cross.Length;j++){var q=cross[j];var r=cross[(j+1)%cross.Length];Vector3 a=new(q.X,q.Y,-.096f),b=new(r.X,r.Y,-.096f),d=new(q.X,q.Y,.096f),e=new(r.X,r.Y,.096f);Color col=j==2?light:j>=4&&j<=6?dark:main;Tri(new(0,0,-.16f),b,a,col);Tri(a,b,e,col);Tri(a,e,d,col);Tri(new(0,0,.16f),d,e,col);}
  return MeshOf(v,c);
 }
 static ArrayMesh CreateTail(){return MeshOf(new List<Vector3>{new(-.5f,0,.5f),new(.5f,0,.5f),new(0,0,-.5f)},new List<Color>{Colors.White,Colors.White,Colors.White});}
 static ArrayMesh MeshOf(List<Vector3> vertices,List<Color> colors){using var arrays=new Godot.Collections.Array();arrays.Resize((int)Mesh.ArrayType.Max);arrays[(int)Mesh.ArrayType.Vertex]=vertices.ToArray();arrays[(int)Mesh.ArrayType.Color]=colors.ToArray();var mesh=new ArrayMesh();mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,arrays);return mesh;}
 internal void Bind(GameSimulation sim){if(disposed)return;if(!ReferenceEquals(simulation,sim)||run!=sim.RunToken||wave!=sim.Wave){ClearPresentation(false);ledger.Bind(sim);simulation=sim;run=sim.RunToken;wave=sim.Wave;heldAlpha=0;}}
 internal void SyncImpactBindings(GameSimulation sim,Node3D[] actors,EntityId[] ids){if(!disposed)impacts.BindActors(sim,actors,ids);}
 internal void CaptureStep(GameSimulation sim,Action<ProjectileTransition> launch){if(disposed)return;Bind(sim);ledger.Capture(sim,launch,impacts.Enabled);}
 internal void Reconcile(GameSimulation sim,float alpha,bool paused,bool visible,double renderDelta=0){if(disposed)return;Bind(sim);if(!visible||sim.Phase!=RunPhase.Combat){ClearPresentation();return;}if(!paused)heldAlpha=float.IsFinite(alpha)?Math.Clamp(alpha,0,1):0;ActiveCount=TailCount=0;impacts.Render(renderDelta,paused,visible);
  for(int i=0;i<bodies.Length;i++){if(!sim.TryGetProjectileAtSlot(i,out var p)||p.RunToken!=run||p.Wave!=wave||!Finite(p.PreviousPosition)||!Finite(p.Position)||!Finite(p.Direction)){bodies[i].Visible=tails[i].Visible=bound[i]=false;continue;}
   bool same=bound[i]&&ids[i]==p.Id&&hops[i]==p.HitsCompleted;ids[i]=p.Id;hops[i]=p.HitsCompleted;bound[i]=true;int kind=p.Kind==ProjectileKind.Ricochet?0:1;var start=Point(p.PreviousPosition);var end=Point(p.Position);var draw=start.Lerp(end,heldAlpha);var body=bodies[i];body.Mesh=meshes[kind];body.MaterialOverride=mats[kind];body.Position=draw;Vector3 dir=new(p.Direction.X,0,p.Direction.Y);if(dir.LengthSquared()>1e-10f)body.Basis=Basis.LookingAt(dir.Normalized(),Vector3.Up);body.Visible=true;ActiveCount++;
   // Retain only an actually drawn point of the same leg; never bridge bounce or replay catchup.
   if(paused&&same){if(tails[i].Visible)TailCount++;continue;}
   var trailStart=same?lastDraw[i]:start;lastDraw[i]=draw;
   var delta=draw-trailStart;float length=Math.Min(MaxTail,delta.Length());var tail=tails[i];tail.Visible=length>1e-5f&&p.State==ProjectileState.Flying;if(tail.Visible){Vector3 a=draw-delta.Normalized()*length;tail.Position=(a+draw)*.5f;tail.Basis=Basis.LookingAt(-delta.Normalized(),Vector3.Up)*Basis.FromScale(new Vector3(.08f,.08f,length));tail.MaterialOverride=tailMats[kind];TailCount++;}
  }
 }
 static bool Finite(Vec2 p)=>float.IsFinite(p.X)&&float.IsFinite(p.Y);
 static Vector3 Point(Vec2 p)=>new(p.X,DisplayY,p.Y);
 internal void ClearPresentation(bool cancelLedger=true){if(disposed)return;impacts.Clear(cancelLedger);for(int i=0;i<bodies.Length;i++){bodies[i].Visible=tails[i].Visible=bound[i]=false;}ActiveCount=TailCount=0;}
 public void Dispose(){if(disposed)return;ClearPresentation();disposed=true;impacts.Dispose();foreach(var b in bodies){b.Mesh=null;b.MaterialOverride=null;}foreach(var t in tails){t.Mesh=null;t.MaterialOverride=null;}root.QueueFree();foreach(var m in meshes)m.Dispose();foreach(var m in mats)m.Dispose();foreach(var m in tailMats)m.Dispose();tailMesh.Dispose();}
}
