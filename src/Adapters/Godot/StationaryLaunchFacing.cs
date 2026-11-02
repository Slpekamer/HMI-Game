using System;
using Hmi.Core;
namespace Hmi.GodotAdapter;
/// <summary>Cosmetic Crit launch direction; never queries a target or changes simulation/input.</summary>
internal sealed class StationaryLaunchFacing
{
 internal const float Rate=240*MathF.PI/180;
 object simulation;int run,wave;EntityId player=EntityId.None;long tick=-1;int index=-1;long drops;bool dropBaseline,eligible,moving=true;object owner;
 internal bool InputKnown{get;private set;}internal long UnknownInputDrops{get;private set;}
 internal bool Held{get;private set;}internal bool Sampled{get;private set;}internal float Yaw{get;private set;}
 internal long Accepted{get;private set;}internal long MovingDrops{get;private set;}internal long DirectionDrops{get;private set;}internal long LossDrops{get;private set;}
 internal void Bind(object identity,int r,int w,EntityId p){if(!ReferenceEquals(simulation,identity)||run!=r||wave!=w||player!=p){simulation=identity;run=r;wave=w;player=p;tick=-1;index=-1;dropBaseline=false;drops=0;owner=null;moving=true;InputKnown=false;Held=false;}}
 internal void Begin(GameSimulation sim,bool visible){Bind(sim,sim.RunToken,sim.Wave,sim.PlayerId);Sampled=false;if(sim.Paused)InputKnown=false;eligible=visible&&sim.Phase==RunPhase.Combat&&sim.TryGetEntity(player,out var p)&&p.Active&&p.VisualId=="hero_crit";if(!eligible){Held=false;InputKnown=false;}}
 internal void Input(float x,float y){Sampled=true;InputKnown=true;moving=!float.IsFinite(x)||!float.IsFinite(y)||x*x+y*y>1e-8f;if(moving)Held=false;}
 internal void ClearPresentation(){Held=false;InputKnown=false;}
 internal void Attach(object current){if(owner!=null&&!ReferenceEquals(owner,current))Held=false;owner=current;}
 internal void CaptureStep(GameSimulation sim)
 {
  bool loss=dropBaseline?sim.DroppedProjectileTransitions!=drops:sim.DroppedProjectileTransitions!=0;drops=sim.DroppedProjectileTransitions;dropBaseline=true;
  for(int i=0;i<sim.ProjectileTransitionCount;i++){
   var e=sim.GetProjectileTransition(i);if(e.Tick<0||e.Tick!=sim.Tick||e.RunToken!=run||e.Wave!=wave)continue;if(e.Tick<tick||(e.Tick==tick&&i<=index))continue;tick=e.Tick;index=i;
   if(e.Kind!=ProjectileTransitionKind.Launched||e.ProjectileKind!=ProjectileKind.Ricochet||e.Owner!=player)continue;
   if(loss){LossDrops++;continue;}if(!eligible||sim.Paused)continue;if(!InputKnown||!Sampled){UnknownInputDrops++;continue;}if(moving){MovingDrops++;continue;}
   Vec2 direction=default;bool found=false;
   if(sim.TryGetProjectile(e.Id,e.RunToken,e.Wave,out var p)&&p.Owner==e.Owner&&p.Kind==e.ProjectileKind&&p.BirthTick==e.Tick&&p.HitsCompleted==0){direction=p.Direction;found=true;}
   else for(int j=i+1;j<sim.ProjectileTransitionCount;j++){var hit=sim.GetProjectileTransition(j);if(hit.Kind==ProjectileTransitionKind.Impact&&hit.Id==e.Id&&hit.Owner==e.Owner&&hit.ProjectileKind==e.ProjectileKind&&hit.RunToken==e.RunToken&&hit.Wave==e.Wave&&hit.Tick==e.Tick){direction=hit.Position-hit.From;found=true;break;}}
   if(!found||!AcceptDirection(direction.X,direction.Y))DirectionDrops++;
  }
 }
 internal bool AcceptDirection(float x,float y){if(!float.IsFinite(x)||!float.IsFinite(y)||x*x+y*y<=1e-10f)return false;Yaw=MathF.Atan2(-x,-y);Held=true;Accepted++;return true;}
 internal static float Turn(float from,float to,float delta){if(!float.IsFinite(from)||!float.IsFinite(to)||!float.IsFinite(delta)||delta<=0)return from;float difference=(to-from)%(2*MathF.PI);if(difference<=-MathF.PI)difference+=2*MathF.PI;else if(difference>MathF.PI)difference-=2*MathF.PI;float limit=Rate*Math.Min(delta,.25f);return from+Math.Clamp(difference,-limit,limit);}
}
