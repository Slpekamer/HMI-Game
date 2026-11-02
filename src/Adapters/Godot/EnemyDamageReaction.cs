using Godot;
using Hmi.Core;
using System;
using System.Collections.Generic;
namespace Hmi.GodotAdapter;

/// <summary>Study-only plain owner. Consumes authoritative enemy Damage; never writes simulation.</summary>
internal sealed class EnemyDamageReaction:IDisposable
{
 public const int Capacity=12,PerFrame=4;public const float Duration=.16f,Cooldown=.4f;
 sealed class Binding {public EntityId Id;public Node3D Root;public MeshInstance3D[] Meshes;public Aabb Bounds;}
 sealed class Cue {public MeshInstance3D Node;public StandardMaterial3D Material,Tint;public bool Active;public EntityId Id;public Vec2 Position;public float Age;public Binding Actor;public Material[] Previous;public long BirthTick;public int EventIndex;}
 struct Pending {public GameEvent Event;public int Index;public bool Lethal;}
 struct Gate {public EntityId Id;public double Next,LastStop;public long LastStopFrame;}
 internal readonly record struct Onset(EntityId Target,double Clock,double DarkSeconds);
 readonly Onset[] onsets=new Onset[PerFrame];long frameSerial;
 internal Onset OnsetAt(int i)=>i>=0&&i<StartsThisFrame?onsets[i]:throw new ArgumentOutOfRangeException(nameof(i));
 internal string StateSignature {get {var s=new System.Text.StringBuilder(clock.ToString("R",System.Globalization.CultureInfo.InvariantCulture));foreach(var c in cues)if(c.Active)s.Append("|").Append(c.Id).Append(":").Append(c.Age.ToString("R",System.Globalization.CultureInfo.InvariantCulture));return s.ToString();}}
 readonly Binding[] bindings;readonly Gate[] gates;readonly Pending[] pending=new Pending[Capacity];readonly Cue[] cues=new Cue[Capacity];
 readonly Node3D container;readonly Camera3D camera;readonly ArrayMesh mesh;int pendingCount,run=-1,wave=-1,lastIndex=-1;long lastTick=-1;double clock;float tokens=4;bool disposed,ended;
 public int Captured{get;private set;}public int Rejected{get;private set;}public int Merged{get;private set;}public int BudgetDrops{get;private set;}public int GateDrops{get;private set;}public int Started{get;private set;}public int Peak{get;private set;}public int StartsThisFrame{get;private set;}
 public int ActiveCount{get{int n=0;foreach(var c in cues)if(c.Active)n++;return n;}}
 internal readonly record struct CueSnapshot(string Target,long BirthTick,int EventIndex,float Age,float TintAlpha,float ShapeAlpha,bool LiveBound,string WorldPosition,bool NormalDepth);
 internal CueSnapshot[] Snapshot(){var list=new List<CueSnapshot>();foreach(var c in cues)if(c.Active)list.Add(new(c.Id.ToString(),c.BirthTick,c.EventIndex,c.Age,c.Tint.AlbedoColor.A,c.Material.AlbedoColor.A,c.Actor!=null,c.Node.GlobalPosition.ToString(),!c.Material.NoDepthTest&&!c.Tint.NoDepthTest));return list.ToArray();}
 public int PendingCount=>pendingCount;public long HighTick=>lastTick;public double Clock=>clock;
 public EnemyDamageReaction(Node3D parent,Camera3D camera,int entityCapacity)
 {
  this.camera=camera;bindings=new Binding[entityCapacity];gates=new Gate[entityCapacity];container=new Node3D{Name="EnemyDamageReactionPool"};parent.AddChild(container);mesh=CreateFracture();
  for(int i=0;i<Capacity;i++){
   var material=new StandardMaterial3D{ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,VertexColorUseAsAlbedo=true,Transparency=BaseMaterial3D.TransparencyEnum.Alpha,NoDepthTest=false,CullMode=BaseMaterial3D.CullModeEnum.Disabled};
   var tint=new StandardMaterial3D{ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,Transparency=BaseMaterial3D.TransparencyEnum.Alpha,NoDepthTest=false,AlbedoColor=new Color(.98f,.35f,.20f,0)};
   var node=new MeshInstance3D{Name="EnemyDamageCue"+i,Mesh=mesh,MaterialOverride=material,Visible=false,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};container.AddChild(node);cues[i]=new Cue{Node=node,Material=material,Tint=tint};
  }
 }
 static ArrayMesh CreateFracture()
 {
  var vertices=new List<Vector3>();var colors=new List<Color>();
  void Tri(Vector3 a,Vector3 b,Vector3 c,Color color){vertices.Add(a);vertices.Add(b);vertices.Add(c);colors.Add(color);colors.Add(color);colors.Add(color);}
  for(int j=0;j<3;j++){
   float a=j*Mathf.Tau/3+.25f;Vector3 d=new(Mathf.Cos(a),Mathf.Sin(a),0),n=new(-d.Y,d.X,0);
   Vector3 p=d*.035f-n*.032f,q=d*.14f,r=d*.035f+n*.032f;var front=new Vector3(0,0,.008f);var back=new Vector3(0,0,-.012f);
   Tri(p+front,q+front,r+front,new Color("ef6846"));Tri(p+back,r+back,q+back,new Color("61364e"));
   foreach(var pair in new[]{(p,q),(q,r),(r,p)}){Tri(pair.Item1+front,pair.Item1+back,pair.Item2+front,new Color("61364e"));Tri(pair.Item2+front,pair.Item1+back,pair.Item2+back,new Color("61364e"));}
   Tri(p+front+Vector3.Back*.001f,d*.085f+front+Vector3.Back*.001f,d*.038f+front+n*.010f+Vector3.Back*.001f,new Color("ffe5b1"));
  }
  using var arrays=new Godot.Collections.Array();arrays.Resize((int)Mesh.ArrayType.Max);arrays[(int)Mesh.ArrayType.Vertex]=vertices.ToArray();arrays[(int)Mesh.ArrayType.Color]=colors.ToArray();var result=new ArrayMesh();result.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,arrays);return result;
 }
 public void BeginFrame(int currentRun,int currentWave){if(disposed)return;ended=false;frameSerial++;StartsThisFrame=0;SetScope(currentRun,currentWave);}
 void SetScope(int r,int w){if(r==run&&w==wave)return;ClearPresentation();run=r;wave=w;lastTick=-1;lastIndex=-1;clock=0;tokens=4;Array.Clear(gates);}
 public void CaptureStep(GameSimulation sim)
 {
  if(disposed||sim==null)return;SetScope(sim.RunToken,sim.Wave);
  for(int i=0;i<sim.EventCount;i++){
   var e=sim.GetEvent(i);if(e.Kind!=EventKind.Damage)continue;
   EntityKind? kind=sim.TryGetEntity(e.Target,out var target)?target.Kind:null;bool lethal=false;
   if(!kind.HasValue)for(int j=i+1;j<sim.EventCount;j++){var death=sim.GetEvent(j);if(death.Kind==EventKind.Death&&death.Target==e.Target&&death.Tick==e.Tick&&death.RunToken==e.RunToken&&death.Wave==e.Wave){lethal=true;break;}}
   Capture(e,i,sim.Tick,kind,lethal,sim.Paused||sim.Phase!=RunPhase.Combat);
  }
 }
 // kind/death evidence is supplied directly by CaptureStep, never inferred from source availability or damage amount.
 internal bool Capture(GameEvent e,int index,long tick,EntityKind? liveKind,bool sameStepEnemyDeath,bool paused)
 {
  if(disposed||e.Kind!=EventKind.Damage||e.RunToken!=run||e.Wave!=wave||e.Tick<0||e.Tick>tick||index<0||index>=2048||e.Tick<lastTick||(e.Tick==lastTick&&index<=lastIndex)||
   e.Target.Index<0||e.Target.Index>=bindings.Length||e.Target.Generation<0||!float.IsFinite(e.Value)||e.Value<=0||!float.IsFinite(e.From.X)||!float.IsFinite(e.From.Y)||!float.IsFinite(e.To.X)||!float.IsFinite(e.To.Y)||
   (liveKind.HasValue?liveKind!=EntityKind.Enemy:!sameStepEnemyDeath)){Rejected++;return false;}
  lastTick=e.Tick;lastIndex=index;if(paused){Rejected++;return false;}Captured++;
  for(int i=0;i<pendingCount;i++)if(pending[i].Event.Target==e.Target){pending[i]=new Pending{Event=e,Index=index,Lethal=sameStepEnemyDeath};Merged++;return true;}
  if(pendingCount==Capacity){BudgetDrops++;return true;}pending[pendingCount++]=new Pending{Event=e,Index=index,Lethal=sameStepEnemyDeath};return true;
 }
 public void SyncBindings(GameSimulation sim,Node3D[] roots,EntityId[] ids)
 {
  if(disposed)return;
  for(int i=0;i<bindings.Length;i++){
   bool valid=sim.TryGetEntityAtSlot(i,out var e)&&e.Kind==EntityKind.Enemy&&roots[i]!=null&&ids[i]==e.Id&&GodotObject.IsInstanceValid(roots[i])&&!roots[i].IsQueuedForDeletion();
   var old=bindings[i];if(old!=null&&(!valid||old.Id!=e.Id||old.Root!=roots[i]))Unbind(i);
   if(!valid||bindings[i]!=null)continue;bindings[i]=Bind(e.Id,roots[i]);
  }
 }
 internal void BindForTest(EntityId id,Node3D root){if(bindings[id.Index]!=null)Unbind(id.Index);bindings[id.Index]=Bind(id,root);}
 static Binding Bind(EntityId id,Node3D root)
 {
  var meshes=new List<MeshInstance3D>();void Walk(Node n){if(n is MeshInstance3D m&&m.Mesh!=null)meshes.Add(m);foreach(Node c in n.GetChildren())Walk(c);}Walk(root);
  Vector3 min=new(float.PositiveInfinity,float.PositiveInfinity,float.PositiveInfinity),max=-min;var inverse=root.GlobalTransform.AffineInverse();
  foreach(var m in meshes){Aabb a=m.GetAabb();var t=inverse*m.GlobalTransform;for(int k=0;k<8;k++){var v=t*(a.Position+new Vector3((k&1)==0?0:a.Size.X,(k&2)==0?0:a.Size.Y,(k&4)==0?0:a.Size.Z));min=min.Min(v);max=max.Max(v);}}
  if(meshes.Count==0){min=new(-.15f,.1f,-.15f);max=new(.15f,.7f,.15f);}
  return new Binding{Id=id,Root=root,Meshes=meshes.ToArray(),Bounds=new Aabb(min,max-min)};
 }
 void Unbind(int index){var b=bindings[index];if(b==null)return;foreach(var c in cues)if(c.Actor==b){Restore(c);c.Actor=null;}bindings[index]=null;}
 public void EndFrame(float delta,bool paused,bool combat)
 {
  if(disposed||ended)return;ended=true;
  if(!combat){ClearPresentation();return;}
  if(paused){pendingCount=0;return;}
  float dt=float.IsFinite(delta)?Math.Clamp(delta,0,.25f):0;clock+=dt;tokens=Math.Min(4,tokens+16*dt);
  foreach(var c in cues)if(c.Active){c.Age+=dt;if(c.Age>=Duration)Stop(c);}
  for(int i=0;i<pendingCount;i++){
   var p=pending[i];ref var gate=ref gates[p.Event.Target.Index];if(gate.Id==p.Event.Target&&(clock<gate.Next||gate.LastStopFrame==frameSerial)){GateDrops++;continue;}
   if(StartsThisFrame>=PerFrame||tokens<1){BudgetDrops++;continue;}
   Cue slot=null;foreach(var c in cues)if(!c.Active){slot=c;break;}if(slot==null){BudgetDrops++;continue;}
   var actor=bindings[p.Event.Target.Index];if(actor!=null&&actor.Id!=p.Event.Target)actor=null;
   // If a nonlethal captured target was destroyed before render, position-only cue remains truthful to the captured hit.
   slot.Active=true;slot.Age=0;slot.BirthTick=p.Event.Tick;slot.EventIndex=p.Index;slot.Id=p.Event.Target;slot.Position=p.Event.To;slot.Actor=actor;
   if(actor!=null){slot.Previous=new Material[actor.Meshes.Length];for(int m=0;m<actor.Meshes.Length;m++){slot.Previous[m]=actor.Meshes[m].MaterialOverlay;actor.Meshes[m].MaterialOverlay=slot.Tint;}}
   double dark=gate.Id==p.Event.Target&&gate.LastStopFrame>0?clock-gate.LastStop:-1;
   onsets[StartsThisFrame]=new Onset(p.Event.Target,clock,dark);
   gate=new Gate{Id=p.Event.Target,Next=clock+Cooldown,LastStop=gate.Id==p.Event.Target?gate.LastStop:0,LastStopFrame=gate.Id==p.Event.Target?gate.LastStopFrame:0};tokens--;StartsThisFrame++;Started++;
  }
  pendingCount=0;foreach(var c in cues)if(c.Active)UpdateVisual(c);Peak=Math.Max(Peak,ActiveCount);
 }
 void UpdateVisual(Cue c)
 {
  float t=c.Age/Duration,alpha=1-t;c.Material.AlbedoColor=new Color(1,1,1,.95f*alpha);c.Tint.AlbedoColor=new Color(.98f,.35f,.20f,.38f*alpha);
  Vector3 position=new(c.Position.X,.55f,c.Position.Y);
  if(c.Actor!=null&&GodotObject.IsInstanceValid(c.Actor.Root)&&!c.Actor.Root.IsQueuedForDeletion()){
   var root=c.Actor.Root;var a=c.Actor.Bounds;Vector3 center=root.GlobalTransform*a.GetCenter();Vector3 d=(camera.GlobalPosition-center).Normalized();Vector3 localRay=root.GlobalBasis.Inverse()*d;float extent=float.PositiveInfinity;
   for(int axis=0;axis<3;axis++)if(Math.Abs(localRay[axis])>.000001f)extent=Math.Min(extent,a.Size[axis]*.5f/Math.Abs(localRay[axis]));
   position=center+d*(extent+.012f);
  }
  Vector3 normal=(camera.GlobalPosition-position).Normalized(),right=camera.GlobalBasis.X.Normalized(),up=normal.Cross(right).Normalized();right=up.Cross(normal).Normalized();
  c.Node.GlobalTransform=new Transform3D(new Basis(right,up,normal).Scaled(Vector3.One*(1-.12f*t)),position);c.Node.Visible=true;
 }
 static void Restore(Cue c)
 {
  if(c.Actor!=null&&c.Previous!=null)for(int i=0;i<c.Actor.Meshes.Length;i++){var m=c.Actor.Meshes[i];if(GodotObject.IsInstanceValid(m)&&m.MaterialOverlay==c.Tint)m.MaterialOverlay=c.Previous[i];}
  c.Previous=null;
 }
 void Stop(Cue c){if(c.Active&&c.Id.Index>=0&&c.Id.Index<gates.Length&&gates[c.Id.Index].Id==c.Id){gates[c.Id.Index].LastStop=clock;gates[c.Id.Index].LastStopFrame=frameSerial;}Restore(c);c.Actor=null;c.Active=false;c.Age=0;if(GodotObject.IsInstanceValid(c.Node))c.Node.Visible=false;}
 public void ClearPresentation(){if(disposed)return;pendingCount=0;foreach(var c in cues)Stop(c);Array.Clear(bindings);}
 public void Dispose(){if(disposed)return;ClearPresentation();disposed=true;foreach(var c in cues){if(GodotObject.IsInstanceValid(c.Node)){c.Node.Mesh=null;c.Node.MaterialOverride=null;}c.Material.Dispose();c.Tint.Dispose();}mesh.Dispose();if(GodotObject.IsInstanceValid(container))container.QueueFree();}
}
