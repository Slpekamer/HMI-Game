using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Hmi.Core;

internal static class TemporaryContractTests
{
    static GameContent Load() => JsonSerializer.Deserialize<GameContent>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "gameplay.json")), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    static GameSimulation New(uint seed = 1, string hero = "orbit", GameContent? content = null)
    {
        var g = new GameSimulation(content ?? Load(), new SimulationLimits(32, 10, 4));
        g.StartRun(hero, seed);
        return g;
    }
    static void Saturate(GameSimulation g)
    {
        for (int pass = 0; pass < g.Catalog.Cards.Length * 5; pass++)
            for (int i = 0; i < g.Catalog.Cards.Length; i++)
                if (g.Catalog.Graph.IsEligible(i, g.Loadout.Levels)) g.Loadout.Levels[i]++;
        StatEffectSystem.Recalculate(g);
    }
    static void Menu(GameSimulation g)
    {
        g.Run.Wave = 6; RunProgressionSystem.FinishWave(g);
        g.Enqueue(GameCommand.ContinueEndless()); g.Step();
    }
    static void Offer(GameSimulation g, string id)
    {
        int index = TemporaryContractSystem.Find(g, id);
        string slot = g.Catalog.Contracts[index].Slot;
        g.Loadout.Offers[slot == "trait" ? 0 : slot == "base" ? 1 : 2] = -3 - index;
    }
    static void Begin(GameSimulation g, string id)
    {
        Offer(g, id); // Focused offered-choice fixture; actual rotation is tested below.
        g.Enqueue(GameCommand.Choose(id)); g.Enqueue(GameCommand.Choose(id)); g.Step();
        g.Enqueue(GameCommand.NextWave()); g.Enqueue(GameCommand.NextWave()); g.Step();
    }
    internal static void Run(Action<bool, string> check)
    {
        var data = Load();
        check(data.TemporaryContracts.Length == 6, "recovered D2 six data definitions");
        foreach (var definition in data.TemporaryContracts)
        {
            var g = New(); Saturate(g); Menu(g);
            var permanent = new float[(int)Stat.Count];
            for (int i = 0; i < permanent.Length; i++) permanent[i] = TemporaryContractSystem.PermanentValue(g, (Stat)i);
            check(TemporaryContractSystem.IsEligible(g, TemporaryContractSystem.Find(g, definition.Id)), "saturated contract effective");
            Begin(g, definition.Id);
            check(g.ActiveContractId == definition.Id && g.PendingContractId == null && !g.PendingNextWaveSupply && g.ActiveSupplyHealth == 0, "one contract with no residual old supply");
            foreach (var e in definition.Effects)
            {
                var stat = Enum.Parse<Stat>(e.Stat);
                check(Math.Abs(g.GetStat(stat) - permanent[(int)stat] * (1 + e.PermanentFraction)) < .0001f, "exact permanent fraction " + definition.Id + "/" + stat);
            }
            g.World.Health[g.PlayerId.Index].Current = 20;
            RunProgressionSystem.FinishWave(g);
            check(g.ActiveContractId == null && Math.Abs(g.GetStat(Stat.MaxHealth) - permanent[(int)Stat.MaxHealth]) < .0001f && g.World.Health[g.PlayerId.Index].Current == 20, "expiry restores max without healing");
            Begin(g, definition.Id);
            foreach (var e in definition.Effects)
            {
                var stat = Enum.Parse<Stat>(e.Stat);
                check(Math.Abs(g.GetStat(stat) - permanent[(int)stat] * (1 + e.PermanentFraction)) < .0001f, "repeat cannot compound");
            }
            g.StartRun("orbit", 9);
            check(g.ActiveContractId == null && g.PendingContractId == null, "restart clears contract");
        }
        var game = New(); Menu(game);
        int scout = TemporaryContractSystem.Find(game, "temp.trait.scouting");
        check(!TemporaryContractSystem.IsEligible(game, scout), "orbit only has no scouting benefit");
        game.Loadout.Levels[game.Catalog.CardIndex["crit.root"]] = 1; StatEffectSystem.Recalculate(game);
        check(TemporaryContractSystem.IsEligible(game, scout), "one enabled mechanism yields real benefit");
        Begin(game, "temp.trait.scouting");
        check(Math.Abs(game.GetStat(Stat.RicochetRange) - 4.8f * 1.15f) < .0001f && game.GetStat(Stat.HoneyRadius) == 4.12f && game.GetStat(Stat.ImpactRange) == 2.72f, "only enabled ranges changed");
        game = New(); Saturate(game); Menu(game);
        float hp = TemporaryContractSystem.PermanentValue(game, Stat.MaxHealth);
        game.Wallet.Coins = game.Wallet.Earned = 100; game.Enqueue(GameCommand.Buy("shield")); game.Step(); Begin(game, "temp.trait.protection");
        check(Math.Abs(game.GetStat(Stat.MaxHealth) - hp * 1.35f) < .0001f, "shield and protection noncompounding");
        game = New(); Saturate(game); Menu(game);
        float rate = TemporaryContractSystem.PermanentValue(game, Stat.BasicRate);
        game.Wallet.Coins = game.Wallet.Earned = 100; game.Enqueue(GameCommand.Buy("tonic")); game.Step(); Begin(game, "temp.base.rate");
        check(Math.Abs(game.GetStat(Stat.BasicRate) - rate * 1.3f) < .0001f, "tonic plus permanent contract delta");
        game.Run.Tick = 599; game.Step();
        check(Math.Abs(game.GetStat(Stat.BasicRate) - rate * 1.1f) < .0001f, "tonic expiry retains one contract delta");
        data = Load(); var original = data.Cards[0].Effects;
        var effects = new EffectDefinition[original.Length + 1]; Array.Copy(original, effects, original.Length);
        effects[original.Length] = new EffectDefinition { Stat = "BasicRate", Operation = "add", Value = 15 }; data.Cards[0].Effects = effects;
        game = New(content: data); Saturate(game); Menu(game); game.Wallet.Coins = game.Wallet.Earned = 100;
        Offer(game, "temp.base.rate"); game.Enqueue(GameCommand.Choose("temp.base.rate")); game.Step();
        check(!game.CanBuy("tonic"), "selected contract prevents unsafe purchase");
        int coins = game.Coins; game.Enqueue(GameCommand.Buy("tonic")); game.Step();
        check(game.Coins == coins && game.GetItemCount("tonic") == 0, "unsafe purchase atomic rejection");
        game = New(content: data); Saturate(game); Menu(game); game.Wallet.Coins = game.Wallet.Earned = 100;
        game.Enqueue(GameCommand.Buy("tonic")); game.Step();
        check(game.GetOfferSlots()[1].Card.Id == "temp.base.reach" && game.GetOfferSlots()[1].Enabled, "pre-choice purchase refreshes safe candidate");

        int menus = 0, singleton = 0; var chosen = new int[6];
        for (uint seed = 1; seed <= 15; seed++)
        {
            game = New(seed, Load().Characters[(seed - 1) % 5].Id); Saturate(game); Menu(game);
            GameSimulation? twin = seed == 1 ? New(seed) : null;
            if (twin != null) { Saturate(twin); Menu(twin); }
            for (int wave = 0; wave < 1000; wave++)
            {
                var slots = game.GetOfferSlots(); int enabled = 0;
                for (int i = 0; i < slots.Count; i++) if (slots[i].Enabled) enabled++;
                check(slots.Count == 3 && enabled == 3, "all production saturated categories effective");
                menus++; if (enabled == 1) singleton++;
                string id = slots[(int)((seed + wave) % 3)].Card.Id;
                if (twin != null)
                {
                    var other = twin.GetOfferSlots();
                    for (int i = 0; i < 3; i++) check(other[i].Card.Id == slots[i].Card.Id, "deterministic twin three-slot replay");
                    twin.Enqueue(GameCommand.Choose(id)); twin.Step(); twin.Enqueue(GameCommand.NextWave()); twin.Step(); RunProgressionSystem.FinishWave(twin);
                }
                chosen[TemporaryContractSystem.Find(game, id)]++;
                game.Enqueue(GameCommand.Choose(id)); game.Step(); game.Enqueue(GameCommand.NextWave()); game.Step();
                check(game.ActiveContractId == id && game.Phase == RunPhase.Combat && game.Coins == game.Earned - game.Spent, "contract init and wallet conservation");
                RunProgressionSystem.FinishWave(game);
            }
        }
        for (int i = 0; i < chosen.Length; i++)
        {
            check(chosen[i] > 1000 && chosen[i] < 4000, "rotation not pathologically collapsed");
            Console.WriteLine($"D2_CONTRACT id={Load().TemporaryContracts[i].Id} selections={chosen[i]}");
        }
        var stress = new GameSimulation(Load()); stress.StartRun("honey", 1); Saturate(stress); Menu(stress); stress.Run.Wave = 999; Begin(stress, "temp.base.rate");
        int enemies = 0;
        for (int i = 0; i < stress.Capacity; i++) if (stress.World.Has(i, Components.Enemy)) { enemies++; stress.World.Health[i].Current = stress.World.Health[i].Maximum = 100000; }
        stress.World.Health[stress.PlayerId.Index].Current = stress.World.Health[stress.PlayerId.Index].Maximum = 100000;
        for (int i = 0; i < 60; i++) stress.Step();
        var watch = new Stopwatch(); long bytes = GC.GetAllocatedBytesForCurrentThread(), tick = stress.Tick;
        watch.Start(); for (int i = 0; i < 300; i++) stress.Step(); watch.Stop(); bytes = GC.GetAllocatedBytesForCurrentThread() - bytes;
        check(enemies == 256 && stress.Tick - tick == 300 && bytes == 0 && stress.ActiveContractId == "temp.base.rate", "peak active-contract ticks zero allocation");
        Console.WriteLine($"D2_PERF enemies={enemies} ticks=300 elapsed_ms={watch.Elapsed.TotalMilliseconds:F2} allocated_bytes={bytes}");
        Console.WriteLine($"D2_SYNTHETIC runs=15 waves_per_run=1000 menus={menus} singleton_menus={singleton}; menu/init only, not survival");
    }
}
