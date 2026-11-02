using System;
using System.IO;
using System.Diagnostics;
using System.Text.Json;
using Hmi.Core;

internal static class EndlessTests
{
    private static GameContent Load() => JsonSerializer.Deserialize<GameContent>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "legacy-gameplay.json")), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    private static GameSimulation New(string hero = "orbit", uint seed = 1, SimulationLimits? limits = null)
    {
        var g = new GameSimulation(Load(), limits ?? new SimulationLimits());
        g.StartRun(hero, seed);
        return g;
    }
    private static void ToEndless(GameSimulation g)
    {
        g.Run.Wave = 6; // synthetic boundary fixture, not six played waves.
        RunProgressionSystem.FinishWave(g);
        g.Enqueue(GameCommand.ContinueEndless());
        g.Step();
    }
    private static void Saturate(GameSimulation g)
    {
        for (int pass = 0; pass < g.Catalog.Cards.Length * 5; pass++)
            for (int i = 0; i < g.Catalog.Cards.Length; i++)
                if (g.Catalog.Graph.IsEligible(i, g.Loadout.Levels)) g.Loadout.Levels[i]++;
        StatEffectSystem.Recalculate(g);
    }
    internal static void Run(Action<bool, string> check)
    {
        var g = New();
        g.Wallet.Coins = g.Wallet.Earned = 37;
        g.Run.Wave = 6;
        RunProgressionSystem.FinishWave(g);
        check(g.Phase == RunPhase.Victory && g.FiniteCompleted && !g.IsEndless, "finite completion distinct from endless");
        uint random = g.Run.Random;
        g.Enqueue(GameCommand.ContinueEndless()); g.Enqueue(GameCommand.ContinueEndless()); g.Step();
        check(g.IsEndless && g.Phase == RunPhase.Between && g.Wave == 6 && g.Coins == 37 && g.Run.Random == random,
            "continue duplicate neither pays rewards nor initializes wave");
        check(g.GetOfferSlots().Count == 3, "continue enters ordinary three-slot menu");
        g.Enqueue(GameCommand.FinishRun()); g.Step();
        check(g.Phase == RunPhase.Finished && g.TerminationReason == RunTerminationReason.FinishedByPlayer, "explicit finish terminal");
        g.Enqueue(GameCommand.ContinueEndless()); g.Step();
        check(g.Phase == RunPhase.Finished, "finished cannot continue");
        g.StartRun("orbit", 2);
        check(!g.IsEndless && !g.FiniteCompleted && g.TerminationReason == RunTerminationReason.None, "new run resets results and scopes");

        g = New(); Saturate(g); ToEndless(g);
        var slots = g.GetOfferSlots();
        check(slots[0].Kind == OfferKind.NextWaveSupply && slots[0].Enabled && slots[1].Kind == OfferKind.Unavailable && slots[2].Kind == OfferKind.Unavailable,
            "saturated slots preserve categories and one valid supply");
        float permanent = StatEffectSystem.ProjectStat(g, Stat.MaxHealth, -1, true);
        g.Wallet.Coins = g.Wallet.Earned = 10;
        g.Enqueue(GameCommand.Buy("shield")); g.Step();
        g.Enqueue(GameCommand.Choose(GameSimulation.NextWaveSupplyId)); g.Enqueue(GameCommand.Choose(GameSimulation.NextWaveSupplyId)); g.Step();
        check(g.PendingNextWaveSupply && g.CardPicked, "duplicate supply choice only commits once");
        g.Enqueue(GameCommand.NextWave()); g.Enqueue(GameCommand.NextWave()); g.Step();
        check(g.Wave == 7 && !g.PendingNextWaveSupply && Math.Abs(g.ActiveSupplyHealth - permanent * .1f) < .0001f,
            "supply uses permanent max excluding wave shield and initializes once");
        check(g.TryGetEntity(g.PlayerId, out var player) && Math.Abs(player.Health - (permanent * 1.25f + permanent * .1f)) < .0001f,
            "supply actually adds both maximum and current health on initialized wave");
        RunProgressionSystem.FinishWave(g);
        check(g.ActiveSupplyHealth == 0 && g.TryGetEntity(g.PlayerId, out player) && Math.Abs(player.MaxHealth - permanent) < .0001f && player.Health <= permanent,
            "wave end expires supply and wave item then clamps without heal");
        g.Enqueue(GameCommand.Choose(GameSimulation.NextWaveSupplyId)); g.Step(); g.Enqueue(GameCommand.NextWave()); g.Step();
        check(Math.Abs(g.ActiveSupplyHealth - permanent * .1f) < .0001f && Math.Abs(g.GetStat(Stat.MaxHealth) - permanent * 1.1f) < .0001f,
            "consecutive supply does not compound previous shield or supply");

        var limits = new SimulationLimits(32, 10, 1);
        g = New("summon", 1, limits); ToEndless(g);
        g.Run.Wave = int.MaxValue;
        g.Loadout.CardPicked = true;
        g.Enqueue(GameCommand.NextWave()); g.Step();
        check(g.Phase == RunPhase.Finished && g.TerminationReason == RunTerminationReason.WaveLimit && g.Wave == int.MaxValue,
            "wave boundary stops before integer overflow");
        g = New(limits: limits);
        random = g.Run.Random;
        int count = g.EntityCount;
        g.SpawnEnemy();
        check(g.Run.Random == random && g.EntityCount == count, "enemy cap rejection consumes neither RNG nor entity slot");
        g.Run.IsEndless = true; g.Run.Wave = int.MaxValue;
        check(g.EffectiveEnemyHealthMultiplier <= 12 && g.EffectiveEnemySpeedMultiplier <= 1.5f, "difficulty clamps before huge wave arithmetic");
        check(g.GetCardAvailability("risk.bounty") == CardAvailability.RiskLimit && g.GetCardAvailability("risk.fleet") == CardAvailability.RiskLimit,
            "bounded difficulty cannot offer free risk bonuses");
        g.Run.IsEndless = false;
        check(g.GetCardAvailability("risk.bounty") == CardAvailability.Available, "finite risk semantics unchanged");

        g = New();
        for(int i=0;i<WaveRules.SpawnPreviewTicks;i++)g.Step(); // Rule2: wait for actual activation before kill-ledger test.
        g.Wallet.Coins = g.Wallet.Earned = int.MaxValue;
        int enemy = -1;
        for (int i = 0; i < g.Capacity; i++) if (g.World.Has(i, Components.Enemy)) { enemy = i; break; }
        DamageResolutionSystem.Apply(g, enemy, 10000, g.PlayerId);
        check(g.Phase == RunPhase.Finished && g.TerminationReason == RunTerminationReason.LedgerLimit
            && g.Coins == int.MaxValue && g.Earned == int.MaxValue && g.Kills == 0, "kill ledger limit terminates atomically without wrap");

        g = New("honey"); Saturate(g); ToEndless(g);
        g.Run.Wave = 999;
        g.Loadout.CardPicked = true;
        g.Enqueue(GameCommand.NextWave()); g.Step();
        // Explicit controlled capacity stress, not natural marker admission: separated perimeter cannot hold256 simultaneous markers.
        g.SpawnReservations.ResetScope();
        for(int i=0;i<g.Limits.EnemyCapacity;i++)g.World.Create(Components.Enemy|Components.Health,new Vec2(-12+(i%25),-6+(i/25)),.44f,"enemy_chaser",100000);
        int enemyCount = 0, companionCount = 0;
        for (int i = 0; i < g.Capacity; i++)
        {
            if (g.World.Has(i, Components.Enemy)) { enemyCount++; g.World.Health[i].Current = g.World.Health[i].Maximum = 100000; }
            if (g.World.Has(i, Components.Companion)) companionCount++;
        }
        check(enemyCount == 256 && companionCount <= 4 && g.EntityCount <= 512, "controlled live256 capacity stress population");
        g.World.Health[g.PlayerId.Index].Current = g.World.Health[g.PlayerId.Index].Maximum = 100000;
        for (int i = 0; i < 60; i++) g.Step();
        var watch = new Stopwatch();
        long bytes = GC.GetAllocatedBytesForCurrentThread(), startTick = g.Tick;
        watch.Start();
        for (int i = 0; i < 300; i++) g.Step();
        watch.Stop();
        bytes = GC.GetAllocatedBytesForCurrentThread() - bytes;
        check(bytes == 0 && g.Tick - startTick == 300 && g.Phase == RunPhase.Combat, "default endless 256-enemy all-trait warm ticks zero allocation");
        Console.WriteLine($"D_PERF enemies={enemyCount} ticks=300 elapsed_ms={watch.Elapsed.TotalMilliseconds:F2} allocated_bytes={bytes} dropped_cosmetic_events={g.DroppedEvents}");

        int runs = 0, menus = 0, singletonMenus = 0, supplyPicks = 0;
        foreach (var hero in Load().Characters)
        for (uint seed = 1; seed <= 3; seed++)
        {
            g = New(hero.Id, seed, limits); ToEndless(g);
            for (int wave = 6; wave < 1006; wave++)
            {
                slots = g.GetOfferSlots();
                int valid = 0;
                for (int i = 0; i < slots.Count; i++) if (slots[i].Enabled) valid++;
                check(slots.Count == 3 && valid >= 1, "synthetic endless never has empty menu");
                menus++; if (valid == 1) singletonMenus++;
                int choice = (int)((seed + wave) % 3);
                while (!slots[choice].Enabled) choice = (choice + 1) % 3;
                if (slots[choice].Kind == OfferKind.NextWaveSupply) supplyPicks++;
                g.Enqueue(GameCommand.Choose(slots[choice].Card.Id)); g.Step();
                check(g.CardPicked && g.Coins == g.Earned - g.Spent, "synthetic endless pick and ledger invariant");
                g.Enqueue(GameCommand.NextWave()); g.Step();
                check(g.Wave == wave + 1 && g.Phase == RunPhase.Combat && g.EntityCount <= 32, "synthetic endless next-wave bounded init");
                RunProgressionSystem.FinishWave(g);
            }
            check(g.EndlessWavesCompleted == 1000 && g.Loadout.Picks.Count <= 5, "1000 synthetic completions and bounded history");
            runs++;
        }
        Console.WriteLine($"D_SYNTHETIC runs={runs} waves_per_run=1000 menus={menus} singleton_menus={singletonMenus} supply_picks={supplyPicks}; selection/init simulation only, not combat survival");
    }
}
