using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Hmi.Core;

// Reconstructed regression suite. Counts/results must be generated afresh, never copied from lost run.
static class Program
{
    static int checks;
    static GameContent Load() => JsonSerializer.Deserialize<GameContent>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "legacy-gameplay.json")), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    static void Check(bool ok, string name)
    {
        checks++;
        if (!ok)
            throw new Exception("FAIL: " + name);
    }
    static void Near(float actual, float expected, string name, float tolerance = .0001f) => Check(Math.Abs(actual - expected) <= tolerance, name + " actual=" + actual + " expected=" + expected);
    static void Reject(GameContent content, string name)
    {
        bool rejected = false;
        try
        {
            _ = new GameSimulation(content);
        }
        catch (ArgumentException) { rejected = true; }
        Check(rejected, name);
    }
    static GameSimulation New(string hero = "orbit", int capacity = 512, uint seed = 1030)
    {
        var g = new GameSimulation(Load(), capacity);
        g.StartRun(hero, seed);
        return g;
    }
    static EffectDefinition Add(string stat, float value) => new EffectDefinition { Stat = stat, Operation = "add", Value = value };
    static GameSimulation Modified(string hero, params EffectDefinition[] extra)
    {
        var data = Load();
        foreach (var card in data.Cards)
        if (card.Id == hero + ".root")
        {
            var effects = new EffectDefinition[card.Effects.Length + extra.Length];
            card.Effects.CopyTo(effects, 0);
            extra.CopyTo(effects, card.Effects.Length);
            card.Effects = effects;
        }
        var g = new GameSimulation(data);
        g.StartRun(hero, 1030);
        return g;
    }
    static void Empty(GameSimulation g)
    {
        for (int i = 0; i < g.Capacity; i++)
        if (g.World.Has(i, Components.Enemy))
            g.World.Destroy(i);
        g.SpawnTimer = 999;
    }
    static EntityId Enemy(GameSimulation g, float x, float y, float hp = 100) => g.World.Create(Components.Enemy | Components.Health, new Vec2(x, y), .44f, "enemy_chaser", hp);
    static void Finish(GameSimulation g)
    {
        Empty(g);
        int guard = 2000;
        while (g.Phase == RunPhase.Combat && guard-- > 0)
        {
            g.World.Health[g.PlayerId.Index].Current = 10000;
            g.Step();
        }
        Check(guard > 0, "wave terminates within bounded ticks");
    }
    static void Pick(GameSimulation g, int slot = 0)
    {
        var id = g.GetOffers()[slot].Id;
        g.Enqueue(GameCommand.Choose(id));
        g.Step();
    }
    static void Next(GameSimulation g)
    {
        g.Enqueue(GameCommand.NextWave());
        g.Step();
    }
    static void Earn(GameSimulation g, int coins)
    {
        Empty(g);
        for (int i = 0; i < coins; i++)
        {
            var e = Enemy(g, 0, 0, 1);
            g.Damage(e.Index, 100, g.PlayerId);
            g.Step();
        }
    }
    static void Main()
    {
        CombatObservationTests.Run(Check);
        CombatObservationIndependentTests.Run(Check);
        SchemaV2IndependentTests.Run(Check);
        SchemaV2Tests.Run(Check);
        ProductionBranchTests.Run(Check);
        ProductionBranchIndependentTests.Run(Check);
        EndlessTests.Run(Check);
        EndlessIndependentTests.Run(Check);
        TemporaryContractTests.Run(Check);
        TemporaryContractIndependentTests.Run(Check);
        TimedHealthIndependentTests.Run(Check);
        PreparedCheckpointTests.Run(Check);
        PreparedCheckpointIndependentTests.Run(Check);
        Catalog();
        Lifecycle();
        RemovedDodge();
        Economy();
        Durations();
        Mechanics();
        Replay();
        Performance();
        Console.WriteLine("PASS " + checks + " freshly executed assertions; reconstructed core netstandard2.1; host net10.0.");
    }
    static void Catalog()
    {
        var data = Load();
        Check(data.Cards.Length == 21 && data.Items.Length == 4 && data.Characters.Length == 5, "documented content counts");
        var bad = Load();
        bad.SchemaVersion = 3;
        Reject(bad, "unsupported schema");
        bad = Load();
        bad.Cards[0].Effects[0].Stat = "GodotNode";
        Reject(bad, "unknown effect");
        bad = Load();
        bad.Cards[0].Effects[0].Stat = "1";
        Reject(bad, "numeric enum alias rejected");
        bad = Load();
        bad.Cards[0].Effects[0].Stat = "BasicRate, BasicDamage";
        Reject(bad, "combined enum alias rejected");
        bad = Load();
        bad.Cards[0].Parent = "missing";
        Reject(bad, "missing parent");
        bad = Load();
        bad.Cards[0].Parent = "orbit.more";
        Reject(bad, "cycle");
        bad = Load();
        bad.Cards[1].Id = bad.Cards[0].Id;
        Reject(bad, "duplicate id");
        bad = Load();
        bad.Cards[0].Effects[0].Value = float.NaN;
        Reject(bad, "NaN effect");
        bad = Load();
        foreach (var c in bad.Cards)
        if (c.Id == "crit.relay")
            c.ParentLevel = 3;
        Reject(bad, "impossible prerequisite");
        bad = Load();
        foreach (var c in bad.Cards)
        if (c.Slot == "base")
            c.Slot = "trait";
        Reject(bad, "missing base pool rejected before wave");
        bad = Load();
        foreach (var c in bad.Cards)
        if (c.Slot == "risk")
            c.Slot = "trait";
        Reject(bad, "missing risk pool");
        bad = Load();
        bad.Cards[0].Effects = new[] { Add("BasicRate", -2) };
        Reject(bad, "negative effective attack rate");
        bad = Load();
        bad.Cards[0].Effects = new[] { Add("OrbitCount", 30) };
        Reject(bad, "unsupported orbit count");
        bad = Load();
        bad.Cards[0].Effects = new[] { Add("RicochetBounces", 30) };
        Reject(bad, "unsupported bounce count");
        var isolated = new GameSimulation(data);
        data.Cards[0].Effects[0].Value = 100;
        isolated.StartRun("orbit", 1);
        Near(isolated.GetStat(Stat.OrbitEnabled), 1, "input DTO mutations isolated");
        foreach (var hero in Load().Characters)
        {
            var g = New(hero.Id);
            Near(g.GetStat(Stat.BasicDamage), 1.5f, "common melee damage " + hero.Id);
            Near(g.GetStat(Stat.BasicRate), 1, "common rate " + hero.Id);
            Near(g.GetStat(Stat.BasicRange), 1.84f, "common range " + hero.Id);
        }
        // 125 complete six-wave sequences, original seed/hero span preserved.
        for (uint seed = 1; seed <= 25; seed++)
        foreach (var hero in Load().Characters)
        {
            var g = New(hero.Id, 512, seed);
            for (int wave = 1; wave < 6; wave++)
            {
                Finish(g);
                Check(g.Phase == RunPhase.Between, "wave enters upgrade phase");
                var offers = g.GetOffers();
                Check(offers.Count == 3, "three slot offers");
                for (int slot = 0; slot < 3; slot++)
                {
                    var c = offers[slot];
                    Check(c.Slot == new[] { "trait", "base", "risk" }[slot], "category");
                    Check(g.GetLevel(c.Id) < c.Cap, "not capped");
                    Check(c.Parent == null || g.GetLevel(c.Parent) >= c.ParentLevel, "prerequisite");
                }
                Pick(g, (int)((seed + (uint)wave) % 3));
                Next(g);
                Check(g.Wave == wave + 1, "next wave advances once");
                Check(g.Coins == g.Earned - g.Spent && g.Coins >= 0, "ledger invariant throughout run");
            }
            Finish(g);
            Check(g.Phase == RunPhase.Victory, "six-wave victory");
            Check(g.GetOffers().Count == 0, "no postfinal rewards/shop");
        }
    }
    static void RemovedDodge()
    {
        var g = New("honey");
        Empty(g);
        Check(!Enum.IsDefined(typeof(CommandKind), 1), "removed command has no public enum name");
        Check(!Enum.IsDefined(typeof(EventKind), 8), "removed event has no public enum name");
        Check(!g.Enqueue(new GameCommand((CommandKind)1)), "legacy numeric command rejected");
        Check(!g.Enqueue(new GameCommand((CommandKind)(-1))), "negative command rejected");
        Check(!g.Enqueue(new GameCommand((CommandKind)99)), "unknown command rejected");
        g.Enqueue(GameCommand.Move(0, -1));
        var start = g.PlayerPosition;
        for (int i = 0; i < 30; i++)
        {
            Check(!g.Enqueue(new GameCommand((CommandKind)1)), "removed command cannot retrigger");
            g.Step();
            Check(!g.Paused, "reserved ID cannot alias pause");
            for (int e = 0; e < g.EventCount; e++)
                Check((int)g.GetEvent(e).Kind != 8, "no removed action event");
        }
        Near(g.PlayerPosition.Y, start.Y - 4.8f * GameSimulation.FixedDelta * 30, "normal speed only");
        g = New("honey");
        Empty(g);
        g.World.Health[g.PlayerId.Index].Current = 1;
        Enemy(g, 2, .3f);
        Check(!g.Enqueue(new GameCommand((CommandKind)1)), "removed action before contact rejected");
        g.Step();
        Check(g.Phase == RunPhase.Defeat, "removed action grants no contact immunity");
    }
    static void Lifecycle()
    {
        var g = New();
        var old = g.PlayerId;
        g.StartRun("honey", 4);
        Check(!g.IsAlive(old), "stale handle after reset");
        var w = new World(2);
        var a = w.Create(Components.Enemy, default, 1, "a");
        w.Destroy(a.Index);
        var b = w.Create(Components.Enemy, default, 1, "b");
        Check(a.Index == b.Index && a.Generation != b.Generation && !w.Alive(a), "generation checked reuse");
        w.Destroy(b.Index);
        w.Destroy(b.Index);
        Check(w.Count == 0, "destroy idempotent");
        long tick = g.Tick;
        g.Enqueue(GameCommand.Pause(true));
        g.Step();
        for (int i = 0; i < 50; i++)
            g.Step();
        Check(g.Tick == tick, "pause freezes tick");
        g.Enqueue(GameCommand.Move(1, 0));
        g.Step();
        g.Enqueue(GameCommand.Pause(false));
        g.Step();
        Near(g.PlayerPosition.X, 2, "paused move discarded");
        Check(g.Tick == tick + 1, "one tick on resume");
        g.Enqueue(new GameCommand(CommandKind.Move, x: 1, runToken: g.RunToken - 1, wave: g.Wave));
        g.Step();
        Near(g.PlayerPosition.X, 2, "old run intent rejected");
        g.Enqueue(GameCommand.Move(float.NaN, 1));
        g.Step();
        Check(!float.IsNaN(g.PlayerPosition.X), "nonfinite input rejected");
        g.Enqueue(GameCommand.Move(1, 0));
        g.Step();
        Empty(g);
        for (int i = 0; i < 400; i++)
            g.Step();
        Check(g.PlayerPosition.X <= 14 - .48f, "arena boundary");
        g = New();
        Empty(g);
        g.World.Health[g.PlayerId.Index].Current = 1;
        Enemy(g, 2, .3f);
        g.Step();
        Check(g.Phase == RunPhase.Defeat, "real defeat");
        tick = g.Tick;
        g.Step();
        Check(g.Tick == tick, "defeat stops simulation");
        g.StartRun("bounce", 1);
        Check(g.Phase == RunPhase.Combat && g.Coins == 0, "restart");
        g = New();
        Finish(g);
        string offered = g.GetOffers()[0].Id;
        g.GetOffers()[0].Id = "mutated";
        g.Enqueue(GameCommand.Choose("mutated"));
        g.Step();
        Check(!g.CardPicked, "mutated detached view cannot choose");
        g.Enqueue(GameCommand.Choose(offered));
        g.Step();
        Check(g.CardPicked, "authoritative offer survives view mutation");
        g = New();
        for (int i = 0; i < 256; i++)
            Check(g.Enqueue(GameCommand.Move(0, 0)), "command capacity accepts bounded input");
        Check(!g.Enqueue(GameCommand.Move(0, 0)), "queue overflow rejects");
        g.Step();
        Check(g.Enqueue(GameCommand.Move(0, 0)), "queue clears at consumption");
    }
    static void Economy()
    {
        var g = New();
        Empty(g);
        var e = Enemy(g, 0, 0, 1);
        g.Damage(e.Index, 100, g.PlayerId);
        g.Damage(e.Index, 100, g.PlayerId);
        Check(g.Coins == 1 && g.Kills == 1, "exactly once death reward");
        g.Step();
        Check(!g.IsAlive(e), "death cleanup");
        g.Enqueue(GameCommand.Buy("shield"));
        g.Step();
        Check(g.Coins == 1 && g.Spent == 0, "combat purchase rejected");
        Earn(g, 19);
        Check(g.Coins == 20, "earned wallet");
        Finish(g);
        g.Enqueue(GameCommand.NextWave());
        g.Step();
        Check(g.Wave == 1, "free pick required");
        int coins = g.Coins;
        g.Enqueue(GameCommand.Choose("crit.relay"));
        g.Enqueue(GameCommand.Buy("unknown"));
        g.Step();
        Check(!g.CardPicked && g.Coins == coins, "invalid menu atomic");
        string selected = g.GetOffers()[0].Id;
        int level = g.GetLevel(selected);
        g.Enqueue(GameCommand.Choose(selected));
        g.Enqueue(GameCommand.Choose(selected));
        foreach (string id in new[] { "shield", "shield", "tonic", "shoes", "gloves" })
            g.Enqueue(GameCommand.Buy(id));
        g.Step();
        Check(g.GetLevel(selected) == level + 1, "one pick only");
        Check(g.Coins == 5 && g.Spent == 15 && g.Earned == 20, "ledger2+3+5+5");
        Check(!g.CanBuy("shield") && g.GetItemStock("shield") == 0, "stock once");
        float hp = g.GetStat(Stat.MaxHealth);
        int run = g.RunToken;
        Next(g);
        Check(g.Wave == 2 && g.Coins == 5, "wallet carried");
        Near(g.GetStat(Stat.MaxHealth), hp * 1.25f, "shield next wave");
        Near(g.GetStat(Stat.BasicRate), 1.2f, "tonic next wave");
        g.Enqueue(new GameCommand(CommandKind.Buy, "shield", runToken: run, wave: 1));
        g.Step();
        Check(g.Coins == 5, "expired wave intent");
        Empty(g);
        while (g.Tick < 600)
            g.Step();
        Near(g.GetStat(Stat.BasicRate), 1, "tonic expires10s");
        Finish(g);
        Pick(g);
        g.Enqueue(GameCommand.Buy("shoes"));
        g.Step();
        Next(g);
        Near(g.GetStat(Stat.MaxHealth), hp, "shield expires across wave");
        Near(g.GetStat(Stat.PlayerSpeed), 1.16f, "two gear stacks");
        Finish(g);
        Check(!g.CanBuy("shoes"), "gear cap2");
        g.StartRun("orbit", 1030);
        g.Enqueue(new GameCommand(CommandKind.Buy, "shield", runToken: run, wave: 1));
        g.Step();
        Check(g.Coins == 0 && g.Earned == 0 && g.Spent == 0 && g.GetItemCount("shoes") == 0, "reset clears economy");
        Finish(g);
        g.Enqueue(GameCommand.Buy("shield"));
        g.Step();
        Check(g.Coins == 0 && g.Spent == 0 && g.GetItemStock("shield") == 1, "insufficient funds atomic");
        var altered = Modified("orbit", Add("BasicRate", .5f), Add("BasicRange", .5f));
        Near(altered.GetStat(Stat.OrbitRadius), 3.04f, "basic range separate domain");
        Near(altered.GetStat(Stat.RicochetRange), 4.8f, "ricochet separate domain");
        foreach (bool boosted in new[] { false, true })
        {
            var gear = boosted ? Modified("orbit", Add("BasicDamage", .5f)) : New("orbit");
            float baseline = boosted ? 2f : 1.5f;
            Near(gear.GetStat(Stat.BasicDamage), baseline, "gloves test baseline");
            for (int count = 1; count <= 2; count++)
            {
                Earn(gear, 5);
                Finish(gear);
                Pick(gear, 0); // Trait slot cannot alter the basic-damage domain.
                gear.Enqueue(GameCommand.Buy("gloves"));
                gear.Step();
                Near(gear.GetStat(Stat.BasicDamage), baseline + (count - 1) * .12f, "gloves wait until next wave");
                Next(gear);
                Near(gear.GetStat(Stat.BasicDamage), baseline + count * .12f, "gloves fixed baseline add not global multiplier");
            }
            Near(gear.GetStat(Stat.BasicDamage), boosted ? 2.24f : 1.74f, "two gloves are16percent original basic baseline");
            Finish(gear);
            Check(!gear.CanBuy("gloves"), "gloves cap2 enforced");
        }
    }
    static void Durations()
    {
        foreach (float duration in new[] { 1f, 15f })
        {
            var data = Load();
            foreach (var item in data.Items)
            if (item.Id == "tonic")
                item.Duration = duration;
            var g = new GameSimulation(data);
            g.StartRun("orbit", 1030);
            Earn(g, 3);
            Finish(g);
            g.Enqueue(GameCommand.Buy("tonic"));
            g.Step();
            Pick(g);
            Next(g);
            Empty(g);
            Near(g.GetStat(Stat.BasicRate), 1.2f, "arbitrary timed effect initial");
            while (g.Tick < (int)(duration * 60) - 1)
                g.Step();
            Near(g.GetStat(Stat.BasicRate), 1.2f, "active before duration");
            g.Step();
            Near(g.GetStat(Stat.BasicRate), 1, "expires at arbitrary duration " + duration);
        }
    }
    static void Mechanics()
    {
        var g = New("crit");
        Empty(g);
        var a = Enemy(g, 3, 0);
        var b = Enemy(g, 6, 0);
        var c = Enemy(g, 10, 0);
        g.Step();
        Near(g.World.Health[a.Index].Current, 95.5f, "common melee plus first shot");
        Near(g.World.Health[b.Index].Current, 98.5f, "one hop");
        Near(g.World.Health[c.Index].Current, 100, "finite count");
        g = New("crit");
        Empty(g);
        a = Enemy(g, 3, 0);
        b = Enemy(g, -3, 0);
        g.Step();
        Near(g.World.Health[b.Index].Current, 100, "finite hop range");
        // Use legal catalog max by moving existing chain/relay effects to the root would overcount validation; instead test repeated-target with current one bounce.
        g = New("crit");
        Empty(g);
        a = Enemy(g, 3, 0);
        g.Step();
        Near(g.World.Health[a.Index].Current, 95.5f, "visited target not hit twice");
        g = New("orbit");
        Empty(g);
        a = Enemy(g, 5.04f, 0);
        g.Step();
        Near(g.World.Health[a.Index].Current, 97.9f, "orbit contact");
        float hp = g.World.Health[a.Index].Current;
        g.Step();
        Near(g.World.Health[a.Index].Current, hp, "orbit cooldown shared");
        g = New("honey");
        Empty(g);
        a = Enemy(g, 5, 0);
        g.Step();
        Near(g.World.Health[a.Index].Current, 98.75f, "honey damage");
        Check(5 - g.World.Transform[a.Index].Position.X < .02f, "honey slow before movement");
        g = New("summon");
        Empty(g);
        int count = 0, shooter = -1, support = -1;
        for (int i = 0; i < g.Capacity; i++)
        if (g.World.Has(i, Components.Companion))
        {
            count++;
            if (g.World.Companion[i].Support)
                support = i;
            else
                shooter = i;
        }
        Check(count == 2, "initial independent companions");
        Vec2 anchor = g.World.Companion[shooter].Anchor;
        g.Enqueue(GameCommand.CompanionMode(true));
        g.Enqueue(GameCommand.Move(1, 0));
        for (int i = 0; i < 40; i++)
            g.Step();
        Near(g.World.Transform[shooter].Position.X, anchor.X, "stationary companion no hero attachment");
        a = Enemy(g, anchor.X, anchor.Y, 10000);
        for (int i = 0; i < 120; i++)
            g.Step();
        Check(g.World.Has(shooter, Components.Companion) && g.World.Has(support, Components.Companion), "companions never damaged");
        g.Enqueue(GameCommand.Recall());
        g.Step();
        Check((g.World.Companion[shooter].Anchor - anchor).Length > 1, "explicit redeploy");
        g = New("bounce");
        Empty(g);
        a = Enemy(g, 4, 0);
        b = Enemy(g, 2, 2.5f);
        g.Step();
        Check(g.World.Enemy[a.Index].KnockTime > 0, "impact knock");
        Near(g.World.Enemy[b.Index].KnockTime, 0, "narrow fan excludes perpendicular");
        g = New("bounce");
        Empty(g);
        a = Enemy(g, 4, 0);
        b = Enemy(g, 4.2f, .2f);
        g.Step();
        int blasts = 0;
        for (int i = 0; i < g.EventCount; i++)
        if (g.GetEvent(i).Kind == EventKind.Blast)
            blasts++;
        Check(blasts > 0 && blasts <= 2, "bounded collision blast");
        g.Step();
        blasts = 0;
        for (int i = 0; i < g.EventCount; i++)
        if (g.GetEvent(i).Kind == EventKind.Blast)
            blasts++;
        Check(blasts == 0, "no recursive/repeated collision blast");
        g = New();
        Empty(g);
        a = Enemy(g, 6, 0);
        g.World.Enemy[a.Index].KnockTime = .8f;
        g.World.Enemy[a.Index].KnockVelocity = new Vec2(10, 0);
        g.Step();
        Check(g.World.Enemy[a.Index].KnockVelocity.X < 0 && g.World.Enemy[a.Index].Bounces == 1, "wall reflection bounded");
        Vec2 moved = Geometry.Move(g, new Vec2(-8, 0), new Vec2(20, 0), .4f, out int hit);
        Check(moved.X < -7.4f && hit != 0, "substep wall no tunneling");
        Check(!Geometry.LineClear(g, new Vec2(-9, 0), new Vec2(-4, 0)), "LOS wall");
        Check(Geometry.LineClear(g, new Vec2(-9, 5), new Vec2(-4, 5)), "LOS passage");
        Empty(g);
        g.World.Transform[g.PlayerId.Index].Position = new Vec2(3, -1.4f);
        g.Step();
        Check(g.PowerSeconds > 7.9f, "pickup activates");
        int inactive = 0;
        for (int i = 0; i < g.Capacity; i++)
        if (g.TryGetEntityAtSlot(i, out var v) && v.Kind == EntityKind.Pickup && !v.Active)
            inactive++;
        Check(inactive == 1, "consumed pickup visual state");
        float power = g.PowerSeconds;
        g.Enqueue(GameCommand.Pause(true));
        g.Step();
        for (int i = 0; i < 50; i++)
            g.Step();
        Near(g.PowerSeconds, power, "pause buff expiry");
        g.Enqueue(GameCommand.Pause(false));
        g.Step();
        for (int i = 0; i < 500; i++)
            g.Step();
        Near(g.PowerSeconds, 0, "temporary buff expiry");
    }
    static void Replay()
    {
        var a = New("summon");
        var b = New("summon");
        for (int i = 0; i < 900; i++)
        {
            var cmd = GameCommand.Move((float)Math.Cos(i * .02), (float)Math.Sin(i * .02));
            a.Enqueue(cmd);
            b.Enqueue(cmd);
            if (i % 180 == 0)
            {
                Check(!a.Enqueue(new GameCommand((CommandKind)1)), "replay rejects removed command");
                Check(!b.Enqueue(new GameCommand((CommandKind)1)), "replay rejects removed command");
            }
            a.Step();
            b.Step();
            Check(a.Tick == b.Tick && a.Coins == b.Coins && a.EntityCount == b.EntityCount && a.Phase == b.Phase, "replay global");
            for (int j = 0; j < a.Capacity; j++)
            {
                bool aa = a.TryGetEntityAtSlot(j, out var av), bb = b.TryGetEntityAtSlot(j, out var bv);
                Check(aa == bb, "replay presence");
                if (aa)
                {
                    Check(av.Id == bv.Id, "replay identity");
                    Near(av.Position.X, bv.Position.X, "replay x", 0);
                    Near(av.Position.Y, bv.Position.Y, "replay y", 0);
                    Near(av.Health, bv.Health, "replay health", 0);
                }
            }
        }
        // Offers independently tested after bounded empty combat, so dead runs do not silently skip seed test.
        a = New("summon");
        b = New("summon");
        Finish(a);
        Finish(b);
        Check(a.GetOffers().Count == 3 && b.GetOffers().Count == 3, "seed offer sample present");
        for (int i = 0; i < 3; i++)
            Check(a.GetOffers()[i].Id == b.GetOffers()[i].Id, "seed offer identity");
    }
    static void Performance()
    {
        foreach (int population in new[] { 32, 128, 512 })
        {
            var g = New("honey", population + 32);
            Empty(g);
            for (int i = 0; i < population; i++)
                Enemy(g, -12 + i % 24, -6 + i / 24 % 12, 10000);
            Measure(g, population, "honey", false);
        }
        var mixed = Modified("bounce", Add("OrbitEnabled", 1), Add("RicochetEnabled", 1), Add("HoneyEnabled", 1), Add("CompanionEnabled", 1));
        Empty(mixed);
        for (int i = 0; i < 128; i++)
            Enemy(mixed, -5 + i % 20 * .5f, -3 + i / 20 % 12 * .5f, 10000);
        Measure(mixed, 128, "mixed_all_traits", true);
    }
    static void Measure(GameSimulation g, int population, string name, bool allowCosmeticDrops)
    {
        for (int i = 0; i < 60; i++)
        {
            g.World.Health[g.PlayerId.Index].Current = 1000000;
            g.Step();
        }
        var sw = new Stopwatch();
        long before = GC.GetAllocatedBytesForCurrentThread();
        sw.Start();
        for (int i = 0; i < 300; i++)
        {
            g.World.Health[g.PlayerId.Index].Current = 1000000;
            g.Step();
        }
        sw.Stop();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Console.WriteLine($"PERF {name} enemies={population} ticks=300 ms={sw.Elapsed.TotalMilliseconds:F2} allocated_bytes={allocated} dropped_cosmetic_events={g.DroppedEvents}");
        Check(allocated <= 128, "warm allocation budget " + name);
        Check(g.Phase == RunPhase.Combat && g.Tick == 360, "benchmark actually simulated all ticks");
        if (!allowCosmeticDrops)
            Check(g.DroppedEvents == 0, "representative event capacity");
    }
}
