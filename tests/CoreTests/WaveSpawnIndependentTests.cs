using System;
using System.IO;
using System.Text.Json;
using System.Text;
using Hmi.Core;
internal static class WaveSpawnIndependentTests
{
 static GameSimulation New(int cap=8) => new GameSimulation(JsonSerializer.Deserialize<GameContent>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"gameplay.json")),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!,new SimulationLimits(64,cap),new string('a',64));
 static void Quiet(GameSimulation g){g.SpawnTimer=g.MeleeTimer=g.HoneyTimer=g.ImpactTimer=g.RicochetTimer=999;}
 static string State(GameSimulation g){var s=new StringBuilder();s.Append($"{g.RunToken}|{g.Tick}|{g.Phase}|{g.Run.Random}|{g.SpawnCounter}|{g.EntityCount}|{g.PendingSpawnCount}|{g.SpawnTransitionCount}|{g.DroppedSpawnTransitions}|{g.Coins}");for(int i=0;i<g.PendingSpawnCount;i++){var p=g.GetPendingSpawn(i);s.Append($"/{p.Id.RunToken},{p.Id.Wave},{p.Id.Serial},{p.Position.X},{p.Position.Y},{p.ActivateTick}");}return s.ToString();}
 internal static void Run(Action<bool,string> check)
 {
  var donor=New();donor.PrepareRun("crit",1030);var legacy=new CheckpointIdentity(Guid.NewGuid(),1);check(donor.TryCapturePrepared(legacy,out var dto,out _),"independent captures prepared migration input");dto!.RulesVersion=WaveRules.LegacyVersion;
  var live=New();live.StartRun("crit",1030);Quiet(live);live.Step();string before=State(live);string transport=JsonSerializer.Serialize(dto);var target=new CheckpointIdentity(Guid.NewGuid(),1);
  check(live.TryConvertLegacyPreparedCheckpoint(dto,legacy,target,out var converted,out _),"independent valid conversion while live");
  check(State(live)==before&&JsonSerializer.Serialize(dto)==transport,"independent successful conversion leaves live pending, notifications, RNG and source untouched");
  check(!live.TryConvertLegacyPreparedCheckpoint(dto,legacy,new CheckpointIdentity(legacy.RunId,99),out _,out var e)&&e==CheckpointError.InvalidIdentity,"independent same UUID higher sequence rejected");
  check(State(live)==before,"independent rejected identity conversion does not reset live markers");
  string convertedText=JsonSerializer.Serialize(converted);dto.Cards[0].Count=0;
  check(JsonSerializer.Serialize(converted)==convertedText,"independent converted nested arrays do not alias caller");dto.Cards[0].Count=1;
  var old=live.GetPendingSpawn(0).Id;
  check(live.TryRestorePrepared(converted!,target,out _)&&live.Phase==RunPhase.Prepared&&live.PendingSpawnCount==0,"independent restore cancels old pending and remains prepared");
  var token=live.PreparedToken;check(live.BeginPreparedWave(token),"independent restored begin once");var fresh=live.GetPendingSpawn(0).Id;int count=live.PendingSpawnCount;
  check(!fresh.Equals(old)&&fresh.RunToken!=old.RunToken,"independent restored marker scope cannot alias old reservation");
  check(!live.BeginPreparedWave(token)&&live.PendingSpawnCount==count,"independent duplicate begin cannot reschedule or double warnings");
  // Fill all authority slots after reservation, then release one only after its fixed deadline.
  var g=New(1);g.StartRun("crit",1030);Quiet(g);var scheduled=g.GetPendingSpawn(0);EntityId extra=EntityId.None;
  while(g.World.Count<g.Capacity){var id=g.World.Create(Components.Pickup,new Vec2(10,5),.1f,"pickup_power",1);if(!g.World.Alive(id))break;extra=id;}
  while(g.Tick<60)g.Step();
  check(g.PendingSpawnCount==0&&g.LastSpawnOutcome==SpawnOutcome.ActivationCapacity,"independent occupied live slots cancel due marker");
  check(g.SpawnTransitionCount==1&&g.GetSpawnTransition(0).Reservation.Id.Equals(scheduled.Id),"independent cancellation retains exact reservation identity");
  g.World.Destroy(extra.Index);g.Step();check(g.PendingSpawnCount==0&&!g.World.Has(extra.Index,Components.Enemy),"independent released capacity cannot resurrect expired cancelled marker");
  // Exact last-tick defeat wins over completion, without late activation.
  g=New();g.StartRun("crit",1030);Quiet(g);g.Run.Tick=3599;g.World.Health[g.PlayerId.Index].Current=0;g.Step();
  check(g.Tick==3600&&g.Phase==RunPhase.Defeat&&!g.FiniteCompleted&&g.PendingSpawnCount==0,"independent final tick death cancels and wins over wave completion");
  // Notification saturation must not consume old combat-event capacity or affect authority.
  g=New(1);g.StartRun("crit",1030);Quiet(g);var r=g.Run.Random;int ev=g.EventLength;for(int i=0;i<1000;i++)g.SpawnEnemy();
  check(g.DroppedSpawnTransitions>0&&g.EventLength==ev&&g.Run.Random==r&&g.PendingSpawnCount==1,"independent saturated spawn channel never spills into combat events or RNG");
  check(g.GetPendingSpawn(0).ActivateTick==60,"independent repeated rejected schedules do not extend warning");
  Console.WriteLine("WAVE_INDEPENDENT lifecycle, migration purity, occupied-slot cancellation and channel isolation checked");
 }
}
