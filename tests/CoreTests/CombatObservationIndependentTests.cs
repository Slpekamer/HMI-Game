using System;
using System.IO;
using System.Text.Json;
using Hmi.Core;
internal static class CombatObservationIndependentTests
{
 public static void Run(Action<bool,string> check)
 {
  int count=0;void C(bool ok,string why){count++;check(ok,"independent observation: "+why);}
  var content=JsonSerializer.Deserialize<GameContent>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"gameplay.json")),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
  var g=new GameSimulation(content,new SimulationLimits(512,256),new string('a',64));g.StartRun("honey",1030);
  for(int i=0;i<g.Capacity;i++)if(i!=g.PlayerId.Index&&g.World.Mask[i]!=Components.None)g.World.Destroy(i);
  g.World.Transform[g.PlayerId.Index].Position=default;g.SpawnTimer=999;
  EntityId Enemy()=>g.World.Create(Components.Enemy|Components.Health,new Vec2(1,0),.44f,"enemy_chaser",100);
  var e=Enemy();g.Stats[(int)Stat.HoneySlow]=.65f;
  new MovementSystem().Update(g);C(g.TryGetEnemyMotionObservation(e,out var old),"actual movement sample exists");
  g.Damage(e.Index,10000,g.PlayerId,FeedbackMechanism.Honey);
  C(!g.TryGetEnemyMotionObservation(e,out _),"same tick pending death invalidates sample before destroy");
  g.World.Destroy(e.Index);var replacement=Enemy();C(replacement.Index==e.Index&&replacement.Generation!=e.Generation,"controlled reused slot has new generation");
  C(!g.TryGetEnemyMotionObservation(replacement,out _),"new occupant cannot inherit old sample");
  C(!g.TryGetEnemyMotionObservation(EntityId.None,out _),"None rejected without indexing");
  C(!g.TryGetEnemyMotionObservation(new EntityId(int.MaxValue,1),out _),"out of range ID safely rejected");
  new MovementSystem().Update(g);C(g.TryGetEnemyMotionObservation(replacement,out _),"new occupant gets its own pass sample");
  g.World.Transform[replacement.Index].Position=new Vec2(10,0);
  C(g.TryGetEnemyMotionObservation(replacement,out var historical)&&historical.HoneyMultiplier==.65f,"post-pass position does not recalculate slow");
  g.Observations.ResetScope();g.EventLength=0;uint rng=g.Run.Random;float hp=g.World.Health[replacement.Index].Current;
  for(int i=0;i<513;i++)g.Observe(CombatObservationKind.WallBounce,replacement,replacement,default,default,i);
  C(g.GetObservation(511).Value==511&&g.ObservationCount==512&&g.DroppedObservations==1,"overflow preserves earliest512 and drops newest");
  C(g.Run.Random==rng&&g.World.Health[replacement.Index].Current==hp,"notification overflow leaves RNG/HP untouched");
  g.Damage(replacement.Index,1,g.PlayerId,FeedbackMechanism.Orbit);
  C(g.EventCount==1&&g.GetEvent(0).Mechanism==FeedbackMechanism.Orbit&&g.World.Health[replacement.Index].Current==hp-1,"full optional pool cannot suppress actual damage/event");
  g.Observations.BeginStep();C(g.ObservationCount==0&&g.DroppedObservations==1,"step clears buffer but preserves scope diagnostic");
  bool thrown=false;try{g.GetObservation(0);}catch(ArgumentOutOfRangeException){thrown=true;}C(thrown,"old buffer index rejected after BeginStep");
  g.Observations.ResetScope();C(g.DroppedObservations==0,"scope clears diagnostic");
  g.PrepareRun("honey",1030);var id=new CheckpointIdentity(Guid.NewGuid(),1);
  C(g.TryCapturePrepared(id,out var dto,out _)&&dto!=null,"prepare capture valid");
  g.Observe(CombatObservationKind.SupportHealApplied,g.PlayerId,g.PlayerId,default,default,0);
  int token=g.RunToken;dto!.CatalogHash=new string('b',64);
  C(!g.TryRestorePrepared(dto,id,out _),"wrong catalog restore rejected");
  C(g.RunToken==token&&g.ObservationCount==1&&g.GetObservation(0).Value==0,"failed restore does not clear existing transient output");
  dto.CatalogHash=new string('a',64);C(g.TryRestorePrepared(dto,id,out _),"valid restore accepted");
  C(g.ObservationCount==0&&g.DroppedObservations==0,"successful restore has no notification replay");
  var prepared=g.PreparedToken;C(g.BeginPreparedWave(prepared)&&!g.BeginPreparedWave(prepared),"observation addition preserves single Begin token");
  C(g.ObservationCount==0,"Begin creates no fake support notifications");
  Console.WriteLine("INDEPENDENT_OBSERVATION_PASS "+count);
 }
}
