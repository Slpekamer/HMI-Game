using Godot;
using Hmi.Core;
using System;
using System.Diagnostics;
namespace Hmi.GodotAdapter;
/// <summary>Bounded target-own rigid-surface reaction, not a reconstructed physics contact.</summary>
internal sealed class ProjectileImpactSurface
{
 internal const int CacheCapacity=8,TriangleBudget=4096,CandidateBudget=2;
 internal const float Epsilon=.002f;
 readonly Camera3D camera;readonly Mesh[] keys=new Mesh[CacheCapacity];readonly Vector3[][] faces=new Vector3[CacheCapacity][];int cacheCount;
 GameSimulation sim;Node3D[] roots;EntityId[] ids;Actor[] actors=Array.Empty<Actor>();int attempts;
 struct Actor{internal Node3D Root;internal MeshInstance3D Main;internal EntityId Id;internal int Cache;internal Rect2 Screen;internal Aabb Bounds;internal bool Ready;}
 internal sealed class Binding{internal MeshInstance3D Mesh;internal EntityId Target;internal Vector3 Local,Physical;internal float Limit;}
 internal readonly record struct Result(string Reason,Vector3 Physical,Vector3 Visual,EntityId Target,Vector3 Local,float Shift,float Limit,int Triangles,double Microseconds,bool Applied);
 internal int FrameTriangles{get;private set;}internal long TotalTriangles{get;private set;}internal long CacheMisses{get;private set;}internal long CacheRejects{get;private set;}internal long AmbiguityDrops{get;private set;}internal long BoundUpdates{get;private set;}internal double CacheMicroseconds{get;private set;}internal double RayMicroseconds{get;private set;}internal double MaxRayMicroseconds{get;private set;}
 internal ProjectileImpactSurface(Camera3D camera){this.camera=camera;}
 static bool Valid(GodotObject o)=>o!=null&&GodotObject.IsInstanceValid(o);
 static bool Finite(Vector3 p)=>float.IsFinite(p.X)&&float.IsFinite(p.Y)&&float.IsFinite(p.Z);
 static double Micros(long start)=>(Stopwatch.GetTimestamp()-start)*1000000.0/Stopwatch.Frequency;
 static MeshInstance3D FindMain(Node n){if(n is MeshInstance3D m&&m.Name.ToString()=="HeadMesh")return m;foreach(Node c in n.GetChildren()){var found=FindMain(c);if(found!=null)return found;}return null;}
 int Cache(Mesh mesh){for(int i=0;i<cacheCount;i++)if(ReferenceEquals(keys[i],mesh))return i;if(cacheCount==CacheCapacity||mesh is not ArrayMesh array){CacheRejects++;return -1;}int vertices=0;for(int i=0;i<array.GetSurfaceCount();i++){if(array.SurfaceGetPrimitiveType(i)!=Mesh.PrimitiveType.Triangles){CacheRejects++;return -1;}int n=array.SurfaceGetArrayIndexLen(i);vertices+=n>0?n:array.SurfaceGetArrayLen(i);}if(vertices<=0||vertices%3!=0||vertices/3>TriangleBudget){CacheRejects++;return -1;}long t=Stopwatch.GetTimestamp();Vector3[] data=mesh.GetFaces();CacheMicroseconds+=Micros(t);CacheMisses++;if(data.Length!=vertices){CacheRejects++;return -1;}int slot=cacheCount++;keys[slot]=mesh;faces[slot]=data;return slot;}
 internal void BindActors(GameSimulation simulation,Node3D[] actorRoots,EntityId[] actorIds)
 {
  sim=simulation;roots=actorRoots;ids=actorIds;if(actors.Length!=roots.Length)actors=new Actor[roots.Length];
  // Prewarm immutable triangles at actual actor binding, before any pending impact resolves.
  for(int i=0;i<actors.Length;i++){if(!sim.TryGetEntityAtSlot(i,out var e)||e.Kind!=EntityKind.Enemy||e.VisualId!="enemy_chaser"||!Valid(roots[i])||ids[i]!=e.Id){actors[i]=default;continue;}if(actors[i].Root==roots[i]&&actors[i].Id==e.Id)continue;var m=FindMain(roots[i]);int index=-1;if(Valid(m)&&m.Mesh!=null&&m.Skin==null&&m.GetBlendShapeCount()==0)index=Cache(m.Mesh);actors[i]=new Actor{Root=roots[i],Main=m,Id=e.Id,Cache=index,Ready=index>=0};}
 }
 static Vector3 Corner(Aabb a,int i)=>a.Position+new Vector3((i&1)!=0?a.Size.X:0,(i&2)!=0?a.Size.Y:0,(i&4)!=0?a.Size.Z:0);
 internal void ResetFrameCounters(){attempts=0;FrameTriangles=0;}
 internal void BeginRender(){ResetFrameCounters();if(camera==null)return;for(int i=0;i<actors.Length;i++){ref var a=ref actors[i];if(!a.Ready||!Valid(a.Main)||!a.Main.IsVisibleInTree())continue;var box=a.Main.GetAabb();var tr=a.Main.GlobalTransform;Vector3 min=tr*Corner(box,0),max=min;Vector2 q=camera.UnprojectPosition(min),lo=q,hi=q;for(int k=1;k<8;k++){var p=tr*Corner(box,k);min=min.Min(p);max=max.Max(p);q=camera.UnprojectPosition(p);lo=lo.Min(q);hi=hi.Max(q);}a.Bounds=new Aabb(min,max-min);a.Screen=new Rect2(lo,hi-lo);}}
 bool ActorFor(EntityId target,out Actor actor){actor=default;if(target.Index<0||target.Index>=actors.Length)return false;actor=actors[target.Index];return actor.Ready&&actor.Id==target&&Valid(actor.Main)&&Valid(actor.Root)&&actor.Main.IsVisibleInTree()&&sim!=null&&sim.TryGetEntity(target,out var e)&&e.Kind==EntityKind.Enemy&&e.Active;}
 bool Ambiguous(EntityId target,Vector3 point){Vector2 p=camera.UnprojectPosition(point);float radius=p.DistanceTo(camera.UnprojectPosition(point+camera.GlobalBasis.X.Normalized()*.22f))+1;var marker=new Rect2(p-new Vector2(radius,radius),new Vector2(radius*2,radius*2));for(int i=0;i<actors.Length;i++){var a=actors[i];if(a.Ready&&a.Id!=target&&a.Screen.HasArea()&&a.Screen.Intersects(marker)){AmbiguityDrops++;return true;}}return false;}
 internal static bool Triangle(Vector3 o,Vector3 d,Vector3 a,Vector3 b,Vector3 c,out float t){t=0;var e1=b-a;var e2=c-a;var p=d.Cross(e2);float determinant=e1.Dot(p);if(Mathf.Abs(determinant)<1e-8f)return false;float inv=1/determinant;var s=o-a;float u=s.Dot(p)*inv;if(u<0||u>1)return false;var q=s.Cross(e1);float v=d.Dot(q)*inv;if(v<0||u+v>1)return false;t=e2.Dot(q)*inv;return t>=0&&float.IsFinite(t);}
 static bool Slab(float p,float d,float min,float max,ref float enter,ref float exit){if(Mathf.Abs(d)<1e-8f)return p>=min&&p<=max;float a=(min-p)/d,b=(max-p)/d;if(a>b)(a,b)=(b,a);enter=Math.Max(enter,a);exit=Math.Min(exit,b);return enter<=exit;}
 internal static bool CrossesWall(Vec2 a,Vec2 b,Wall w){float enter=0,exit=1;return Slab(a.X,b.X-a.X,w.X,w.X+w.Width,ref enter,ref exit)&&Slab(a.Y,b.Y-a.Y,w.Y,w.Y+w.Height,ref enter,ref exit);}
 bool ClearDisplacement(Vector3 a,Vector3 b){if(!string.Equals(GameSimulation.CheckpointRulesVersion,"hmi-core-wave60-preview1-projectiles-3",StringComparison.Ordinal))return false;
 // Conservative presentation-only envelope pinned to this reviewed rule. Not a Core collision API.
 if(b.X<=-14||b.X>=14||b.Z<=-8||b.Z>=8)return false;foreach(var w in sim.Walls)if(CrossesWall(new Vec2(a.X,a.Z),new Vec2(b.X,b.Z),w))return false;return true;}
 internal bool TryCreate(ProjectileTransition e,out Binding binding,out Result result,out bool suppress)
 {
  binding=null;suppress=false;Vector3 physical=new(e.Position.X,ProjectilePresentation.DisplayY,e.Position.Y);result=new("physical-fallback",physical,physical,e.Target,default,0,0,0,0,false);
  if(attempts>=CandidateBudget){suppress=true;result=result with{Reason="candidate-budget"};return false;}attempts++;if(camera==null||!ActorFor(e.Target,out var actor)){result=result with{Reason="missing-exact-rigid-target"};return false;}
  var points=faces[actor.Cache];if(points.Length/3>TriangleBudget-FrameTriangles){result=result with{Reason="frame-triangle-budget"};return false;}
  var tr=actor.Main.GlobalTransform;float det=tr.Basis.Determinant();if(!float.IsFinite(det)||Mathf.Abs(det)<1e-8f){result=result with{Reason="invalid-matrix"};return false;}
  Vector3 toward=camera.GlobalBasis.Z.Normalized();Vector3 center=tr*actor.Main.GetAabb().GetCenter();Vector3 worldOrigin=center+toward*(actor.Bounds.Size.Length()+1),worldDir=-toward;var inverse=tr.AffineInverse();var origin=inverse*worldOrigin;var dir=inverse.Basis*worldDir;float nearest=float.PositiveInfinity;int visits=0;long time=Stopwatch.GetTimestamp();
  for(int i=0;i<points.Length;i+=3){visits++;if(Triangle(origin,dir,points[i],points[i+1],points[i+2],out float t)&&t<nearest)nearest=t;}
  double cost=Micros(time);FrameTriangles+=visits;TotalTriangles+=visits;RayMicroseconds+=cost;MaxRayMicroseconds=Math.Max(MaxRayMicroseconds,cost);result=result with{Triangles=visits,Microseconds=cost};if(!float.IsFinite(nearest)){result=result with{Reason="own-ray-miss"};return false;}
  Vector3 local=origin+dir*nearest;Vector3 surface=tr*local;Vector3 visual=surface+toward*Epsilon;float limit=Math.Min(1,.5f*actor.Bounds.Size.Length()+ProjectileRules.Radius);float shift=physical.DistanceTo(visual);result=result with{Visual=visual,Local=local,Shift=shift,Limit=limit};
  if(!Finite(visual)||shift>limit||!actor.Screen.HasPoint(camera.UnprojectPosition(surface))){result=result with{Reason="surface-envelope-guard"};return false;}if(!ClearDisplacement(physical,visual)){result=result with{Reason="displacement-wall-guard"};return false;}if(Ambiguous(e.Target,visual)){suppress=true;result=result with{Reason="neighbor-ambiguity"};return false;}
  binding=new Binding{Mesh=actor.Main,Target=e.Target,Local=local,Physical=physical,Limit=limit};result=result with{Reason="target-own-rigid-surface",Applied=true};return true;
 }
 internal bool Update(Binding b,out Vector3 point){point=default;BoundUpdates++;if(!ActorFor(b.Target,out var a)||a.Main!=b.Mesh)return false;point=b.Mesh.GlobalTransform*b.Local+camera.GlobalBasis.Z.Normalized()*Epsilon;return Finite(point)&&b.Physical.DistanceTo(point)<=b.Limit&&a.Screen.HasPoint(camera.UnprojectPosition(point))&&ClearDisplacement(b.Physical,point)&&!Ambiguous(b.Target,point);}
 internal void Clear(){sim=null;roots=null;ids=null;actors=Array.Empty<Actor>();Array.Clear(keys);Array.Clear(faces);cacheCount=0;}
}
