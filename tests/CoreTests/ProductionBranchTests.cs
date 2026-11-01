using System;
using System.IO;
using System.Text.Json;
using Hmi.Core;

internal static class ProductionBranchTests
{
    private static GameContent Load(string file) => JsonSerializer.Deserialize<GameContent>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, file)), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    internal static void Run(Action<bool, string> check)
    {
        var legacy = Load("legacy-gameplay.json");
        var production = Load("gameplay.json");
        check(production.SchemaVersion == 2 && production.Cards.Length == 29, "production has schema2 and eight added nodes");
        check(JsonSerializer.Serialize(production.Characters) == JsonSerializer.Serialize(legacy.Characters), "production five heroes unchanged");
        check(JsonSerializer.Serialize(production.Items) == JsonSerializer.Serialize(legacy.Items), "production four items unchanged");
        for (int i = 0; i < legacy.Cards.Length; i++)
            check(JsonSerializer.Serialize(production.Cards[i]) == JsonSerializer.Serialize(legacy.Cards[i]), "production legacy definition unchanged: " + legacy.Cards[i].Id);
        var g = new GameSimulation(production);
        g.StartRun("orbit", 1030);
        var catalog = g.Catalog;
        // All legacy cards remain reachable without selecting any newly exclusive member.
        var levels = new int[production.Cards.Length];
        for (int pass = 0; pass < legacy.Cards.Length; pass++)
            for (int i = 0; i < legacy.Cards.Length; i++)
                if (catalog.Graph.IsEligible(i, levels)) levels[i]++;
        for (int i = 0; i < legacy.Cards.Length; i++) check(levels[i] == legacy.Cards[i].Cap, "legacy cap remains reachable: " + legacy.Cards[i].Id);
        // Every new branch can be reached with the original prerequisites, with siblings unselected.
        for (int i = legacy.Cards.Length; i < production.Cards.Length; i++)
            check(catalog.Graph.IsEligible(i, levels), "new node reachable through old path: " + production.Cards[i].Id);
        for (int i = 0; i < legacy.Cards.Length; i++) g.Loadout.Levels[i] = levels[i];
        StatEffectSystem.Recalculate(g);
        float basicRate = g.GetStat(Stat.BasicRate), basicRange = g.GetStat(Stat.BasicRange), basicDamage = g.GetStat(Stat.BasicDamage);
        float[] baseline = new float[(int)Stat.Count];
        for (int i = 0; i < baseline.Length; i++) baseline[i] = g.GetStat((Stat)i);
        for (int i = legacy.Cards.Length; i < production.Cards.Length; i++)
        {
            g.Loadout.Levels[i] = 1;
            StatEffectSystem.Recalculate(g);
            foreach (var effect in production.Cards[i].Effects)
            {
                var stat = Enum.Parse<Stat>(effect.Stat);
                float delta = effect.Value * (stat == Stat.PlayerSpeed ? g.GetStat(Stat.PlayerRiskSpeed) : 1);
                check(Math.Abs(g.GetStat(stat) - (baseline[(int)stat] + delta)) < .0001f,
                    "registered effect exact: " + production.Cards[i].Id + "/" + stat);
            }
            check(g.GetStat(Stat.BasicRate) == basicRate && g.GetStat(Stat.BasicRange) == basicRange && g.GetStat(Stat.BasicDamage) == basicDamage,
                "deep trait leaves shared melee domains unchanged");
            g.Loadout.Levels[i] = 0;
        }
        // Synthetic progression isolates selection/cap/command behavior, not simulated combat survival.
        int runs = 0, deepOffers = 0;
        var distribution = new int[production.Cards.Length];
        foreach (var hero in production.Characters)
        for (uint seed = 1; seed <= 50; seed++)
        {
            g = new GameSimulation(production);
            g.StartRun(hero.Id, seed);
            for (int wave = 1; wave <= 6; wave++)
            {
                RunProgressionSystem.FinishWave(g);
                if (wave == 6) { check(g.Phase == RunPhase.Victory, "production finite sixth wave victory"); break; }
                var offers = g.GetOffers();
                check(offers.Count == 3, "production always three finite slots");
                for (int slot = 0; slot < 3; slot++)
                {
                    check(g.GetCardAvailability(offers[slot].Id) == CardAvailability.Available, "production offer eligible");
                    int offerIndex = catalog.CardIndex[offers[slot].Id];
                    if (offerIndex >= legacy.Cards.Length) { deepOffers++; distribution[offerIndex]++; }
                }
                int selected = seed % 2 == 0 ? 0 : (int)((seed + wave) % 3);
                var old = g.GetLevel(offers[selected].Id);
                g.Enqueue(new GameCommand(CommandKind.Choose, offers[selected].Id, runToken: g.RunToken, wave: g.Wave));
                g.Step();
                check(g.CardPicked && g.GetLevel(offers[selected].Id) == old + 1, "production scoped selection applied once");
                g.Enqueue(GameCommand.NextWave());
                g.Step();
                check(g.Wave == wave + 1 && g.Phase == RunPhase.Combat, "production next wave progresses");
            }
            runs++;
        }
        check(deepOffers > 0, "production deeper nodes actually appear in finite mixed pools");
        for (int i = legacy.Cards.Length; i < production.Cards.Length; i++)
            Console.WriteLine($"C2_DEEP_OFFER id={production.Cards[i].Id} count={distribution[i]}");
        Console.WriteLine($"C2_SYNTHETIC finite_runs={runs} heroes=5 seeds_per_hero=50 deep_offer_count={deepOffers}; combat survival not exercised by this probe");
    }
}
