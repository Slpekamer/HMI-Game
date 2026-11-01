using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using Hmi.Core;

internal static class PreparedCheckpointIndependentTests
{
    static GameContent Load() => JsonSerializer.Deserialize<GameContent>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"gameplay.json")),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
    static string Hash => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"gameplay.json")))).ToLowerInvariant();
    static GameSimulation New() => new GameSimulation(Load(),new SimulationLimits(32,10,4),Hash);
    static readonly CheckpointIdentity Id=new CheckpointIdentity(Guid.Parse("c7926772-20dc-4ba5-9355-041c3c5a5ad2"),7);
    static WaveCheckpointDto Capture(GameSimulation g) { if(!g.TryCapturePrepared(Id,out var d,out var e))throw new Exception("Independent capture: "+e);return d!; }
    static string Wire(WaveCheckpointDto d)=>JsonSerializer.Serialize(d);
    static void Reject(Action<bool,string> c,GameSimulation g,WaveCheckpointDto d,string label)
    { var before=PreparedCheckpointTests.Digest(g,true);c(!g.TryRestorePrepared(d,Id,out _),"independent E rejects "+label);c(before==PreparedCheckpointTests.Digest(g,true),"independent E exact rejection atomicity "+label); }
    public static void Run(Action<bool,string> check)
    {
        foreach(var hero in new[]{"summon","orbit","honey","crit","bounce"})
        {
            var a=New();a.PrepareRun(hero,uint.MaxValue);var d=Capture(a);var initial=Wire(d);var token=a.PreparedToken;
            for(int i=0;i<8;i++)a.Step();
            check(a.Tick==0 && Wire(Capture(a))==initial,"independent E prepared stepping cannot consume RNG "+hero);
            // Wire order is not authority: use stable content IDs rather than array positions.
            Array.Reverse(d.Cards);Array.Reverse(d.Items);
            var b=New();check(b.TryRestorePrepared(d,Id,out _),"independent E reordered stable IDs restore "+hero);
            d.Cards[0].Count=999;d.Items[0].Count=999;d.CharacterId="missing";
            check(b.CharacterId==hero && b.TryValidateCheckpoint(Capture(b),Id,out _),"independent E caller DTO mutation detached "+hero);
            check(a.BeginPreparedWave(token)&&b.BeginPreparedWave(b.PreparedToken),"independent E both begin "+hero);
            for(int tick=0;tick<90;tick++)
            { a.Enqueue(GameCommand.Move(tick%2==0?.3f:-.3f,.2f));b.Enqueue(GameCommand.Move(tick%2==0?.3f:-.3f,.2f));a.Step();b.Step(); }
            check(PreparedCheckpointTests.Digest(a,false)==PreparedCheckpointTests.Digest(b,false),"independent E seed max replay equivalent "+hero);
            var begun=PreparedCheckpointTests.Digest(b,true);
            check(!b.BeginPreparedWave(b.PreparedToken)&&begun==PreparedCheckpointTests.Digest(b,true),"independent E duplicate begin leaves live combat exact "+hero);
        }
        var g=New();g.PrepareRun("honey",1030);var saved=Capture(g);
        var before=PreparedCheckpointTests.Digest(g,true);
        check(!g.TryCapturePrepared(new CheckpointIdentity(Guid.Empty,1),out _,out _) && !g.TryCapturePrepared(new CheckpointIdentity(Id.RunId,0),out _,out _) && before==PreparedCheckpointTests.Digest(g,true),"independent E invalid capture identity has no mutation");
        g.BeginPreparedWave(g.PreparedToken);g.Enqueue(GameCommand.Move(1,1));g.Step();
        var bad=JsonSerializer.Deserialize<WaveCheckpointDto>(Wire(saved))!;bad.Items=Array.Empty<CheckpointCount>();bad.Cards=Array.Empty<CheckpointCount>();Reject(check,g,bad,"missing mandatory hero root");
        bad=JsonSerializer.Deserialize<WaveCheckpointDto>(Wire(saved))!;bad.EnemySpeedCap=float.PositiveInfinity;Reject(check,g,bad,"infinite rule cap");
        bad=JsonSerializer.Deserialize<WaveCheckpointDto>(Wire(saved))!;bad.EndlessWavesCompleted=int.MaxValue;Reject(check,g,bad,"overflow progression");
        var negative=New();negative.World.Generation[3]=-1;Reject(check,negative,saved,"negative inactive generation");
        // Real menu preparation must not be available before a free pick, nor run twice.
        RunProgressionSystem.FinishWave(g);
        before=PreparedCheckpointTests.Digest(g,true);
        check(!g.PrepareNextWave()&&before==PreparedCheckpointTests.Digest(g,true),"independent E no preparation before card choice");
        string choice=g.GetOfferSlots().First(x=>x.Enabled).Card.Id;g.Enqueue(GameCommand.Choose(choice));g.Step();
        check(g.PrepareNextWave(),"independent E ordinary selected menu prepares");
        before=PreparedCheckpointTests.Digest(g,true);var nextToken=g.PreparedToken;var next=Capture(g);
        check(!g.PrepareNextWave()&&before==PreparedCheckpointTests.Digest(g,true),"independent E repeated preparation cannot advance twice");
        var target=New();target.StartRun("crit",77);var old=target.PlayerId;int oldRun=target.RunToken;
        target.Enqueue(new GameCommand(CommandKind.Move,x:1,runToken:oldRun,wave:1));
        check(target.TryRestorePrepared(next,Id,out _)&&target.Commands.Count==0,"independent E restore clears queued old intents");
        check(!target.BeginPreparedWave(nextToken),"independent E foreign source token rejected");
        check(target.BeginPreparedWave(target.PreparedToken)&&!target.IsAlive(old),"independent E old actor generation cannot alias");
        check(target.Wave==2&&target.GetLevel(choice)==g.GetLevel(choice),"independent E restore preserves paid selection without replay");
    }
}
