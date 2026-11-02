using Godot;
using Hmi.Core;
using System;
using System.Collections.Generic;
namespace Hmi.GodotAdapter;

/// <summary>Incoming player Damage only. Owns one overlay, never pose, gameplay or imported materials.</summary>
internal sealed class PlayerDamageMaterial : IDisposable
{
 internal const float Duration=.16f, PeakAlpha=.26f;
 readonly StandardMaterial3D tint=new(){Transparency=BaseMaterial3D.TransparencyEnum.Alpha,ShadingMode=BaseMaterial3D.ShadingModeEnum.PerPixel,NoDepthTest=false,Roughness=1,SpecularMode=BaseMaterial3D.SpecularModeEnum.Disabled,AlbedoColor=new Color(.98f,.35f,.20f,0)};
 GameSimulation simulation; int run=-1,wave=-1,lastIndex=-1;long highTick=-1,frameSerial,lastStopFrame=-1;
 Node3D root;EntityId boundId;MeshInstance3D[] meshes=Array.Empty<MeshInstance3D>();Material[] previous;
 readonly double[] onsetTimes=new double[2];int onsetCount;double clock;float age;bool active,pending,disposed,ended;GameEvent pendingEvent;
 internal int BindingsCreated{get;private set;}internal int Captured{get;private set;}internal int Rejected{get;private set;}internal int Merged{get;private set;}internal int GateDrops{get;private set;}internal int BindingDrops{get;private set;}internal int Started{get;private set;}internal int ObservedLossSteps{get;private set;}
 int lastDropped;bool dropBaseline;
 internal bool Active=>active;internal bool Pending=>pending;internal float Age=>age;internal float Alpha=>tint.AlbedoColor.A;internal double Clock=>clock;internal long HighTick=>highTick;internal int MeshCount=>meshes.Length;
 internal string StateSignature=>$"{clock:R}|{age:R}|{active}|{pending}|{onsetCount}";
 internal void BeginFrame(GameSimulation sim){if(disposed)return;Scope(sim);frameSerial++;ended=false;}
 void Scope(GameSimulation sim){if(sim==null)return;if(ReferenceEquals(simulation,sim)&&run==sim.RunToken&&wave==sim.Wave)return;ClearPresentation();simulation=sim;run=sim.RunToken;wave=sim.Wave;highTick=-1;lastIndex=-1;clock=0;onsetCount=0;Array.Clear(onsetTimes);lastStopFrame=-1;dropBaseline=false;}
 internal void CaptureStep(GameSimulation sim){if(disposed||sim==null)return;Scope(sim);if(dropBaseline&&sim.DroppedEvents!=lastDropped)ObservedLossSteps++;else if(!dropBaseline&&sim.DroppedEvents!=0)ObservedLossSteps++;lastDropped=sim.DroppedEvents;dropBaseline=true;
  bool validPlayer=sim.TryGetEntity(sim.PlayerId,out var player)&&player.Kind==EntityKind.Player;
  for(int i=0;i<sim.EventCount;i++){var e=sim.GetEvent(i);if(e.Kind!=EventKind.Damage)continue;Capture(e,i,sim.Tick,sim.PlayerId,validPlayer,sim.Paused||sim.Phase!=RunPhase.Combat);}
 }
 internal bool Capture(GameEvent e,int index,long currentTick,EntityId playerId,bool livePlayer,bool blocked){
  if(disposed||e.Kind!=EventKind.Damage||e.Target!=playerId||!livePlayer||e.RunToken!=run||e.Wave!=wave||e.Tick<0||e.Tick>currentTick||e.Tick<highTick||(e.Tick==highTick&&index<=lastIndex)||index<0||index>=2048||!float.IsFinite(e.Value)||e.Value<=0||!float.IsFinite(e.From.X)||!float.IsFinite(e.From.Y)||!float.IsFinite(e.To.X)||!float.IsFinite(e.To.Y)){Rejected++;return false;}
  highTick=e.Tick;lastIndex=index;if(blocked){Rejected++;return false;}Captured++;if(pending)Merged++;pendingEvent=e;pending=true;return true;
 }
 internal void SyncBinding(GameSimulation sim,Node3D[] roots,EntityId[] ids){if(disposed||sim==null)return;Scope(sim);if(sim.Phase!=RunPhase.Combat){ClearPresentation();return;}bool valid=sim.TryGetEntity(sim.PlayerId,out var p)&&p.Kind==EntityKind.Player&&p.Id.Index>=0&&p.Id.Index<roots.Length&&p.Id.Index<ids.Length&&ids[p.Id.Index]==p.Id;Node3D next=valid?roots[p.Id.Index]:null;valid=valid&&GodotObject.IsInstanceValid(next)&&!next.IsQueuedForDeletion();if(!valid){if(root!=null)ClearPresentation();return;}Bind(p.Id,next);}
 internal void BindForTest(EntityId id,Node3D node){if(!disposed)Bind(id,node);}
 void Bind(EntityId id,Node3D node){if(root==node&&boundId==id)return;if(root!=null)ClearPresentation();root=node;boundId=id;BindingsCreated++;var list=new List<MeshInstance3D>();void Walk(Node n){if(n is MeshInstance3D m&&m.Mesh!=null)list.Add(m);foreach(Node c in n.GetChildren())Walk(c);}Walk(node);meshes=list.ToArray();previous=new Material[meshes.Length];}
 internal void EndFrame(float delta,bool paused,bool combat){if(disposed||ended)return;ended=true;if(!combat){ClearPresentation();return;}if(paused){pending=false;return;}float dt=float.IsFinite(delta)?Math.Clamp(delta,0,.25f):0;clock+=dt;
  while(onsetCount>0&&clock-onsetTimes[0]>=1.0){if(onsetCount==2)onsetTimes[0]=onsetTimes[1];onsetCount--;}
  if(active){age+=dt;if(age>=Duration)Stop();}
  if(pending){pending=false;if(root==null||!GodotObject.IsInstanceValid(root)||root.IsQueuedForDeletion()||boundId!=pendingEvent.Target||meshes.Length==0){BindingDrops++;}
   else if(active||lastStopFrame==frameSerial||onsetCount>=2){GateDrops++;}
   else{for(int i=0;i<meshes.Length;i++){var m=meshes[i];if(!GodotObject.IsInstanceValid(m))continue;previous[i]=m.MaterialOverlay;m.MaterialOverlay=tint;}active=true;age=0;onsetTimes[onsetCount++]=clock;Started++;}}
  if(active)tint.AlbedoColor=new Color(.98f,.35f,.20f,PeakAlpha*(1-age/Duration));
 }
 void Stop(){if(active){if(previous!=null)for(int i=0;i<meshes.Length;i++){var m=meshes[i];if(GodotObject.IsInstanceValid(m)&&m.MaterialOverlay==tint)m.MaterialOverlay=previous[i];}lastStopFrame=frameSerial;}if(previous!=null)Array.Clear(previous);active=false;age=0;tint.AlbedoColor=new Color(.98f,.35f,.20f,0);}
 internal void ClearPresentation(){if(disposed)return;Stop();pending=false;root=null;meshes=Array.Empty<MeshInstance3D>();previous=null;}
 public void Dispose(){if(disposed)return;ClearPresentation();disposed=true;tint.Dispose();}
}
