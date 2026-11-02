using System;
using System.IO;
using System.Text.Json;
using Hmi.Core;

internal static class Wave60TimingTests
{
    static GameSimulation New()
    {
        var content=JsonSerializer.Deserialize<GameContent>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"gameplay.json")),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
        return new GameSimulation(content,new SimulationLimits(),new string('a',64));
    }
    static void Empty(GameSimulation g)
    {
        for(int i=0;i<g.Capacity;i++)if(g.World.Has(i,Components.Enemy))g.World.Destroy(i);
        g.SpawnTimer=999; g.SpawnReservations.ResetScope(); /* Controlled empty-arena fixture removes rule2 previews. */
    }
    internal static void Run(Action<bool,string> check)
    {
        check(WaveRules.DurationTicks==3600&&WaveRules.SpawnPreviewTicks==60&&GameSimulation.WaveDuration==60,"single versioned 60s/1s authority policy");
        var g=New();g.PrepareRun("crit",1030);uint random=g.Run.Random;
        for(int i=0;i<4000;i++)g.Step();
        check(g.Tick==0&&g.Phase==RunPhase.Prepared&&g.Run.Random==random,"Prepared waiting consumes no combat time/RNG");
        check(g.BeginPreparedWave(g.PreparedToken),"begin actual prepared wave");Empty(g);
        for(int i=0;i<WaveRules.DurationTicks-1;i++)g.Step();
        check(g.Tick==3599&&g.Phase==RunPhase.Combat,"wave still combat at3599, not old1440 boundary");
        long tick=g.Tick;float seconds=g.WaveSeconds;g.Enqueue(GameCommand.Pause(true));g.Step();
        for(int i=0;i<600;i++)g.Step();
        check(g.Paused&&g.Tick==tick&&g.WaveSeconds==seconds,"pause excludes ten seconds worth of attempted steps");
        g.Enqueue(GameCommand.Pause(false));g.Step();
        check(g.Tick==3600&&g.Phase==RunPhase.Between&&!g.Paused,"one resumed tick reaches60s and between");
        for(int i=0;i<600;i++)g.Step();
        check(g.Tick==3600&&g.Phase==RunPhase.Between,"cards/shop waiting consumes no combat time");
        // Controlled legal phase fixtures exercise endpoints, not natural six-wave survival.
        foreach(int wave in new[]{6,7})
        {
            g=New();g.StartRun("crit",1030);Empty(g);g.Run.Wave=wave;g.Run.IsEndless=wave==7;g.Run.FiniteCompleted=wave==7;g.Run.Tick=3598;
            g.Step();check(g.Phase==RunPhase.Combat&&g.Tick==3599,"finite/endless combat before deadline "+wave);
            g.Step();check(g.Tick==3600&&g.Phase==(wave==6?RunPhase.Victory:RunPhase.Between),"finite/endless same60s endpoint "+wave);
            if(wave==7)check(g.EndlessWavesCompleted==1,"endless completion counted once");
        }
        g=New();g.StartRun("crit",1030);Empty(g);g.Run.Tick=3599;g.World.Health[g.PlayerId.Index].Current=0;g.Step();
        check(g.Tick==3600&&g.Phase==RunPhase.Defeat&&!g.FiniteCompleted,"death retains precedence at final tick");
        g=New();g.PrepareRun("crit",1030);var identity=new CheckpointIdentity(Guid.NewGuid(),1);
        check(g.TryCapturePrepared(identity,out var dto,out _)&&dto!.RulesVersion==WaveRules.Version&&dto.SchemaVersion==1,"new semantics version, unchanged DTO shape");
        dto!.RulesVersion=WaveRules.LegacyVersion;
        check(!g.TryValidateCheckpoint(dto,identity,out var error)&&error==CheckpointError.RulesMismatch,"ordinary restore cannot silently reinterpret legacy24 rules");
    }
}
