using System;
using System.IO;
using System.Text.Json;
using Hmi.Core;

internal static class ProductionBranchIndependentTests
{
    private static GameContent Load() => JsonSerializer.Deserialize<GameContent>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "gameplay.json")), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    private static void Rank(GameSimulation g, string id, int rank = 1) => g.Loadout.Levels[g.Catalog.CardIndex[id]] = rank;
    private static bool Available(GameSimulation g, string id) => g.GetCardAvailability(id) == CardAvailability.Available;
    private static void Near(Action<bool, string> check, float actual, float expected, string why) => check(Math.Abs(actual - expected) < .0001f, "independent C2: " + why + " actual=" + actual);
    public static void Run(Action<bool, string> check)
    {
        var data = Load();
        var g = new GameSimulation(data); g.StartRun("orbit", 1030);
        check(!Available(g, "fusion.sugaring"), "C2 fusion requires other root and geometry prerequisite");
        Rank(g, "honey.root");
        check(!Available(g, "fusion.sugaring"), "C2 fusion two roots alone do not satisfy ANY");
        Rank(g, "honey.wide");
        check(Available(g, "fusion.sugaring"), "C2 fusion honey geometry alternative unlocks");
        Rank(g, "honey.wide", 0); Rank(g, "orbit.wide");
        check(Available(g, "fusion.sugaring"), "C2 fusion orbit geometry alternative unlocks");
        Rank(g, "honey.root", 0);
        check(!Available(g, "fusion.sugaring"), "C2 fusion ANY does not bypass missing ALL root");
        Rank(g, "honey.root"); StatEffectSystem.Recalculate(g);
        float oldHoney = g.GetStat(Stat.HoneyRadius), oldOrbit = g.GetStat(Stat.OrbitRadius);
        Rank(g, "fusion.sugaring"); StatEffectSystem.Recalculate(g);
        Near(check, g.GetStat(Stat.HoneyRadius), oldHoney + .3f, "fusion honey gain");
        Near(check, g.GetStat(Stat.OrbitRadius), oldOrbit - .25f, "fusion orbit reach tradeoff");

        g = new GameSimulation(data); g.StartRun("crit", 1030); Rank(g, "bounce.root"); Rank(g, "crit.chain");
        check(!Available(g, "fusion.relaypush"), "C2 relay fusion rank one insufficient");
        Rank(g, "crit.chain", 2);
        check(Available(g, "fusion.relaypush"), "C2 relay fusion rank two sufficient");
        StatEffectSystem.Recalculate(g);
        float force = g.GetStat(Stat.ImpactForce), reach = g.GetStat(Stat.RicochetRange), count = g.GetStat(Stat.RicochetBounces);
        Rank(g, "fusion.relaypush"); StatEffectSystem.Recalculate(g);
        Near(check, g.GetStat(Stat.ImpactForce), force - .6f, "relay fusion force cost");
        Near(check, g.GetStat(Stat.RicochetRange), reach + .6f, "relay fusion reach gain");
        Near(check, g.GetStat(Stat.RicochetBounces), count, "relay fusion must not add hit count");

        // Each branch is evaluated on its minimal legal prerequisite build, not an all-max synthetic build.
        foreach (string id in new[] { "orbit.outer", "orbit.inner", "crit.longwatch", "crit.skirmish", "bounce.longpush", "bounce.heavy" })
        {
            var definition = Array.Find(data.Cards, x => x.Id == id)!;
            string hero = id.Substring(0, id.IndexOf('.'));
            g = new GameSimulation(data); g.StartRun(hero, 1030);
            foreach (var prerequisite in definition.RequiresAll!) Rank(g, prerequisite.Id, prerequisite.Rank);
            check(Available(g, id), "C2 minimal prerequisites unlock " + id);
            StatEffectSystem.Recalculate(g);
            var baseline = new float[(int)Stat.Count];
            for (int stat = 0; stat < baseline.Length; stat++) baseline[stat] = g.GetStat((Stat)stat);
            Rank(g, id); StatEffectSystem.Recalculate(g);
            foreach (var effect in definition.Effects)
                Near(check, g.GetStat(Enum.Parse<Stat>(effect.Stat)), baseline[(int)Enum.Parse<Stat>(effect.Stat)] + effect.Value,
                    id + " exact minimum-build effect " + effect.Stat);
            foreach (var sibling in data.Cards)
                if (sibling.Id != id && sibling.ExclusiveGroup == definition.ExclusiveGroup)
                    check(g.GetCardAvailability(sibling.Id) == CardAvailability.ExclusiveLocked, "C2 sibling lock " + sibling.Id);
            foreach (var legacy in data.Cards)
                if (legacy.ExclusiveGroup == null && legacy.Id.StartsWith(hero + ".", StringComparison.Ordinal))
                    check(g.GetCardAvailability(legacy.Id) != CardAvailability.ExclusiveLocked, "C2 old path not accidentally locked " + legacy.Id);
            Near(check, g.GetStat(Stat.BasicDamage), baseline[(int)Stat.BasicDamage], id + " common melee damage unchanged");
            Near(check, g.GetStat(Stat.BasicRate), baseline[(int)Stat.BasicRate], id + " common melee rate unchanged");
        }
    }
}
