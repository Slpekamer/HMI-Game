using System;
using System.IO;
using System.Text.Json;
using Hmi.Core;

internal static class SpawnPreviewTests
{
    static GameSimulation New(int enemies=256)
    {
        var content=JsonSerializer.Deserialize<GameContent>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"gameplay.json")),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
        return new GameSimulation(content,new SimulationLimits(512,enemies),new string('a',64));
    }
    static int Enemies(GameSimulation g) { int n=0;for(int i=0;i<g.Capacity;i++)if(g.World.Has(i,Components.Enemy))n++;return n; }
    static void Quiet(GameSimulation g) { g.SpawnTimer=999;g.MeleeTimer=g.HoneyTimer=g.ImpactTimer=g.RicochetTimer=999; }
    static void ToTick(GameSimulation g,long tick) { while(g.Tick<tick&&g.Phase==RunPhase.Combat)g.Step(); }
    internal static void Run(Action<bool,string> check)
    {
        var g=New();g.StartRun("crit",1030);Quiet(g);
        check(g.PendingSpawnCount==10&&Enemies(g)==0&&g.EntityCount==3,"initial ten markers have no world entities");
        var first=g.GetPendingSpawn(0);uint rng=g.Run.Random;
        check(first.ScheduledTick==0&&first.ActivateTick==60&&first.Id.RunToken==g.RunToken&&first.Id.Wave==1&&first.Id.Serial>0,"tick-zero snapshot carries full scope identity and fixed deadline");
        for(int i=0;i<g.PendingSpawnCount;i++)
        {
            var p=g.GetPendingSpawn(i);
            check(!Geometry.Blocked(g,p.Position,p.Radius)&&(p.Position-g.PlayerPosition).LengthSquared>=2.25f,"request location outside wall/player safety radius");
            for(int j=0;j<i;j++)check((p.Position-g.GetPendingSpawn(j).Position).LengthSquared>=1,"pending disks reserve separated positions");
        }
        ToTick(g,59);check(Enemies(g)==0&&g.PendingSpawnCount==10&&g.Coins==0,"no target/collision/reward enemy during warning");
        g.Enqueue(GameCommand.Pause(true));g.Step();for(int i=0;i<120;i++)g.Step();
        check(g.Tick==59&&g.PendingSpawnCount==10&&g.GetPendingSpawn(0).ActivateTick==60,"pause freezes warning time and positions");
        g.Enqueue(GameCommand.Pause(false));g.Step();
        check(g.Tick==60&&Enemies(g)==10&&g.PendingSpawnCount==0&&g.Run.Random==rng,"all initial enemies activate exactly sixty ticks without RNG");
        check(g.SpawnTransitionCount==10&&g.GetSpawnTransition(0).Kind==SpawnTransitionKind.Activated&&g.GetSpawnTransition(0).Reservation.Id.Equals(first.Id),"activation records correlate reservation and full entity ID");
        var entity=g.GetSpawnTransition(0).Entity;
        check(g.World.Alive(entity)&&(g.World.Transform[entity.Index].Position-first.Position).LengthSquared==0,"activation position immutable");
        g=New();g.StartRun("crit",1030);Quiet(g);first=g.GetPendingSpawn(0);
        g.World.Transform[g.PlayerId.Index].Position=first.Position;float hp=g.World.Health[g.PlayerId.Index].Current;
        ToTick(g,60);check(g.World.Health[g.PlayerId.Index].Current==hp&&Enemies(g)==10,"standing on marker does not delay/teleport or damage before activation");
        g.Step();check(g.World.Health[g.PlayerId.Index].Current<hp,"standing on activated enemy takes ordinary next-tick contact, no spawn immunity");
        g=New(1);g.StartRun("crit",1030);Quiet(g);rng=g.Run.Random;int counter=g.SpawnCounter;
        for(int i=0;i<1000;i++)g.SpawnEnemy();
        check(g.PendingSpawnCount==1&&g.LastSpawnOutcome==SpawnOutcome.Capacity&&g.Run.Random==rng&&g.SpawnCounter==counter,"reservation capacity rejection consumes no RNG");
        check(g.DroppedSpawnTransitions>0&&g.PendingSpawnCount==1,"observation overflow never drops authoritative marker");
        ToTick(g,60);check(Enemies(g)==1,"observation saturation cannot suppress activation");
        g=New();g.StartRun("crit",1030);Quiet(g);g.SpawnReservations.ResetScope();
        g.Run.Tick=3539;g.SpawnEnemy();first=g.GetPendingSpawn(0);check(first.ActivateTick==3599,"last eligible warning activates before end");
        g.Run.Tick=3540;rng=g.Run.Random;g.SpawnEnemy();check(g.PendingSpawnCount==1&&g.LastSpawnOutcome==SpawnOutcome.Deadline&&g.Run.Random==rng,"final second rejects before RNG");
        g.Run.Tick=3598;g.Step();check(Enemies(g)==1&&g.PendingSpawnCount==0,"last legal reservation activates at3599");
        g.Step();check(g.Phase==RunPhase.Between&&g.PendingSpawnCount==0,"wave ends at3600 without pending leak");
        g=New();g.StartRun("crit",1030);g.World.Health[g.PlayerId.Index].Current=0;g.Step();
        check(g.Phase==RunPhase.Defeat&&g.PendingSpawnCount==0&&Enemies(g)==0,"death cancels pending before activation");
        g=New();g.StartRun("crit",1030);first=g.GetPendingSpawn(0);g.StartRun("crit",1030);
        check(!g.GetPendingSpawn(0).Id.Equals(first.Id)&&g.PendingSpawnCount==10,"new run invalidates old reservation identities");
        RunProgressionSystem.FinishWave(g);check(g.PendingSpawnCount==0,"forced legal finish cancels pending");
        g=New();g.StartRun("crit",1030);EndlessProgressionSystem.Terminate(g,RunTerminationReason.FinishedByPlayer);
        check(g.PendingSpawnCount==0,"explicit termination cancels pending");
        g=New();g.StartRun("crit",1030);Quiet(g);first=g.GetPendingSpawn(0);
        for(int i=0;i<g.Capacity;i++)if(g.World.Mask[i]==Components.None)g.World.Generation[i]=int.MaxValue;
        ToTick(g,60);check(g.PendingSpawnCount==0&&Enemies(g)==0&&g.LastSpawnOutcome==SpawnOutcome.ActivationCapacity,"external retired-slot violation cancels explicitly without invalid entity");
        var a=New();var b=New();a.StartRun("crit",1030);b.StartRun("crit",1030);
        for(int tick=0;tick<3600&&a.Phase==RunPhase.Combat;tick++)
        {
            a.World.Health[a.PlayerId.Index].Current=b.World.Health[b.PlayerId.Index].Current=100000;
            a.Step();b.Step();check(a.Tick==b.Tick&&a.Run.Random==b.Run.Random&&a.PendingSpawnCount==b.PendingSpawnCount&&a.EntityCount==b.EntityCount,"same-seed schedule/activation deterministic");
            for(int i=0;i<a.PendingSpawnCount;i++)check(a.GetPendingSpawn(i).Position.X==b.GetPendingSpawn(i).Position.X&&a.GetPendingSpawn(i).Position.Y==b.GetPendingSpawn(i).Position.Y,"same-seed pending positions");
        }
        g=New();g.StartRun("crit",1030);Quiet(g);for(int i=0;i<10;i++)g.GetPendingSpawn(0);
        long bytes=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<10000;i++){var p=g.GetPendingSpawn(i%g.PendingSpawnCount);if(p.Id.Serial<0)throw new Exception();}
        check(GC.GetAllocatedBytesForCurrentThread()==bytes,"pending snapshot reads allocate zero bytes");
        Pressure(check);
        Migration(check);
    }
    static void Pressure(Action<bool,string> check)
    {
        foreach(int wave in new[]{1,6,8,30,1000})
        {
            var g=New();g.PrepareRun("crit",1030);g.Run.Wave=wave;g.Run.IsEndless=wave>6;g.Run.FiniteCompleted=wave>6;g.Run.EndlessWavesCompleted=Math.Max(0,wave-7);
            g.BeginPreparedWave(g.PreparedToken);
            int scheduled=0,activated=0,rejected=0,spatial=0,peakPending=0,peakLive=0;
            void Record()
            {
                peakPending=Math.Max(peakPending,g.PendingSpawnCount);peakLive=Math.Max(peakLive,Enemies(g));
                for(int i=0;i<g.SpawnTransitionCount;i++)
                {
                    var e=g.GetSpawnTransition(i);
                    if(e.Kind==SpawnTransitionKind.Scheduled)scheduled++;
                    if(e.Kind==SpawnTransitionKind.Activated)activated++;
                    if(e.Kind==SpawnTransitionKind.Rejected)rejected++;
                    if(e.Outcome==SpawnOutcome.NoSafePosition)spatial++;
                }
            }
            Record();int initial=g.PendingSpawnCount;
            // Synthetic pressure only: stationary player health replenished, original attacks/cadence/rewards run normally.
            while(g.Phase==RunPhase.Combat)
            {
                g.World.Health[g.PlayerId.Index].Current=100000;
                g.Step();Record();
                check(g.PendingSpawnCount+Enemies(g)<=g.Limits.EnemyCapacity,"live plus pending never exceeds hard capacity");
            }
            check(g.Tick==3600&&g.PendingSpawnCount==0,"pressure run exactly60s and drained warnings");
            Console.WriteLine($"WAVE60_PRESSURE wave={wave} initial_accepted={initial} scheduled={scheduled} activated={activated} rejected={rejected} spatial_rejected={spatial} peak_pending={peakPending} peak_live={peakLive} kills={g.Kills} earned={g.Earned}; synthetic stationary/refilledHP, not survival/balance");
        }
    }
    static void Migration(Action<bool,string> check)
    {
        var g=New();g.PrepareRun("crit",1030);var oldId=new CheckpointIdentity(Guid.NewGuid(),8);var target=new CheckpointIdentity(Guid.NewGuid(),1);
        g.TryCapturePrepared(oldId,out var dto,out _);dto!.RulesVersion=WaveRules.LegacyVersion;
        check(!g.TryRestorePrepared(dto,oldId,out var error)&&error==CheckpointError.RulesMismatch,"ordinary restore rejects legacy rules");
        check(!g.TryConvertLegacyPreparedCheckpoint(dto,oldId,new CheckpointIdentity(oldId.RunId,9),out _,out _),"legacy conversion rejects in-place same UUID even newer sequence");
        check(g.TryConvertLegacyPreparedCheckpoint(dto,oldId,target,out var converted,out error)&&error==CheckpointError.None,"explicit fully validated legacy fork conversion");
        check(converted!.RunId==target.RunId&&converted.Sequence==1&&converted.RulesVersion==WaveRules.Version&&dto.RulesVersion==WaveRules.LegacyVersion&&dto.Sequence==8,"new slot identity permitted, legacy transport unchanged");
        check(converted.RandomState==dto.RandomState&&converted.Coins==dto.Coins&&converted.Wave==dto.Wave&&converted.Seed==dto.Seed&&g.Phase==RunPhase.Prepared&&g.EntityCount==0,"conversion preserves authority and does not Begin");
        check(!ReferenceEquals(converted.Cards,dto.Cards)&&!ReferenceEquals(converted.Cards[0],dto.Cards[0]),"migration arrays and members detached");
        check(g.TryRestorePrepared(converted,target,out _)&&g.PendingSpawnCount==0&&g.BeginPreparedWave(g.PreparedToken)&&g.PendingSpawnCount==10,"converted prepared data starts only via explicit Begin");
        var first=g.GetPendingSpawn(0);long tick=g.Tick;uint rng=g.Run.Random;
        dto.Coins=-1;check(!g.TryConvertLegacyPreparedCheckpoint(dto,oldId,target,out converted,out _)&&converted==null,"tampered legacy wallet rejected");
        check(!g.TryRestorePrepared(dto,oldId,out _)&&g.PendingSpawnCount==10&&g.GetPendingSpawn(0).Id.Equals(first.Id)&&g.Tick==tick&&g.Run.Random==rng,"failed restore/conversion preserve live reservation authority");
        dto.Coins=0;dto.RulesVersion=WaveRules.Version;check(!g.TryConvertLegacyPreparedCheckpoint(dto,oldId,target,out _,out _),"migration cannot replay already-current rule as legacy");
        dto.RulesVersion=WaveRules.LegacyVersion;check(!g.TryConvertLegacyPreparedCheckpoint(dto,oldId,new CheckpointIdentity(Guid.Empty,1),out _,out _),"invalid target migration identity rejected");
        check(g.TryConvertLegacyPreparedCheckpoint(dto,oldId,target,out var again,out _)&&g.TryConvertLegacyPreparedCheckpoint(dto,oldId,target,out var retry,out _)&&again!.RandomState==retry!.RandomState&&again.RunId==retry.RunId,"repeated conversion pure and deterministic, no RNG or Begin");
    }
}
