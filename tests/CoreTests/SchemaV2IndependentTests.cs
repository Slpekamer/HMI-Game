using System;
using System.IO;
using System.Text.Json;
using Hmi.Core;

// Independent adversarial graph tests. Synthetic cards are not shipped content.
internal static class SchemaV2IndependentTests
{
    private static GameContent Load()
    {
        var value = JsonSerializer.Deserialize<GameContent>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "legacy-gameplay.json")), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        value.SchemaVersion = 2;
        return value;
    }
    private static RequirementDefinition R(string id, int rank = 1) => new RequirementDefinition { Id = id, Rank = rank };
    private static CardDefinition Card(string id, string? group = null) => new CardDefinition
    {
        Id = id, Name = id, Slot = "trait", Cap = 2, ExclusiveGroup = group,
        RequiresAll = new[] { R("orbit.root") }, Effects = Array.Empty<EffectDefinition>()
    };
    private static GameContent Append(params CardDefinition[] extra)
    {
        var data = Load();
        var cards = new CardDefinition[data.Cards.Length + extra.Length];
        Array.Copy(data.Cards, cards, data.Cards.Length);
        Array.Copy(extra, 0, cards, data.Cards.Length, extra.Length);
        data.Cards = cards;
        return data;
    }
    private static void Reject(Action<bool, string> check, GameContent data, string label)
    {
        bool rejected = false;
        try { _ = new GameSimulation(data); }
        catch (ArgumentException) { rejected = true; }
        check(rejected, "independent schema2: " + label);
    }
    public static void Run(Action<bool, string> check)
    {
        // Legacy accepted an empty parent on non-character-root cards as no prerequisite.
        var emptyParentLegacy = Load();
        emptyParentLegacy.SchemaVersion = 1;
        CardDefinition? legacyLeaf = null;
        foreach (var candidate in emptyParentLegacy.Cards)
            if (candidate.Parent != null) { legacyLeaf = candidate; break; }
        if (legacyLeaf == null) throw new Exception("Legacy fixture requires an existing child card");
        legacyLeaf.Parent = "";
        bool emptyParentAccepted = true;
        try { _ = new GameSimulation(emptyParentLegacy); }
        catch (ArgumentException) { emptyParentAccepted = false; }
        check(emptyParentAccepted, "independent schema1: existing empty-parent acceptance preserved");

        // Each branch exists on its own, but they cannot jointly satisfy ALL.
        var left = Card("qa.left", "qa.branch");
        var right = Card("qa.right", "qa.branch");
        var impossible = Card("qa.impossible");
        impossible.RequiresAll = new[] { R(left.Id), R(right.Id) };
        Reject(check, Append(left, right, impossible), "exclusive siblings jointly required rejected");

        // An OR with an independently reachable escape must not be over-rejected.
        var escape = Card("qa.escape");
        escape.RequiresAll = new[] { R(left.Id, 2) };
        escape.RequiresAny = new[] { R(right.Id), R("crit.root") };
        var data = Append(left, right, escape);
        var g = new GameSimulation(data);
        var catalog = g.Catalog;
        var levels = new int[data.Cards.Length];
        int li = catalog.CardIndex[left.Id], ri = catalog.CardIndex[right.Id], ei = catalog.CardIndex[escape.Id];
        levels[catalog.CardIndex["orbit.root"]] = 1;
        levels[catalog.CardIndex["crit.root"]] = 1;
        levels[li] = 1;
        check(!catalog.Graph.IsEligible(ei, levels), "independent schema2: prerequisite rank one is insufficient for rank two");
        levels[li] = 2;
        check(catalog.Graph.IsEligible(ei, levels), "independent schema2: feasible OR escape unlocks with ALL satisfied");
        check(!catalog.Graph.IsEligible(ri, levels), "independent schema2: acquired sibling locks alternative");
        levels[li] = 1;
        check(catalog.Graph.IsEligible(li, levels), "independent schema2: selected exclusive member can rank up");
        levels[li] = 2;
        check(!catalog.Graph.IsEligible(li, levels), "independent schema2: selected member cap still enforced");
        levels[ei] = 2;
        check(!catalog.Graph.IsEligible(ei, levels), "independent schema2: complex node cap enforced");
        levels[ei] = 0;

        // Requirements must be detached from caller-owned arrays and objects.
        escape.RequiresAll![0].Id = "missing.after.compile";
        escape.RequiresAll[0].Rank = 99;
        escape.RequiresAny![1].Id = "missing.or.after.compile";
        left.ExclusiveGroup = "mutated.group";
        check(catalog.Graph.IsEligible(ei, levels), "independent schema2: caller mutation cannot alter compiled prerequisites");
        check(!catalog.Graph.IsEligible(ri, levels), "independent schema2: caller mutation cannot unlock exclusive sibling");

        // A dependency path can hide the exclusive conflict from direct-edge checks.
        left = Card("qa.left", "qa.branch"); right = Card("qa.right", "qa.branch");
        var descendant = Card("qa.descendant");
        descendant.RequiresAll = new[] { R(left.Id) };
        impossible = Card("qa.hiddenConflict");
        impossible.RequiresAll = new[] { R(descendant.Id), R(right.Id) };
        Reject(check, Append(left, right, descendant, impossible), "transitive exclusive contradiction rejected");

        // Non-empty OR cycles are conservatively rejected even if an escape exists.
        var a = Card("qa.a"); var b = Card("qa.b");
        a.RequiresAny = new[] { R(b.Id), R("crit.root") };
        b.RequiresAll = new[] { R(a.Id) };
        Reject(check, Append(a, b), "OR cycle with escape rejected by declared conservative contract");
        a = Card("qa.a"); a.RequiresAll = new[] { R("orbit.root", 0) };
        Reject(check, Append(a), "zero prerequisite rank rejected");
        a = Card("qa.a"); a.RequiresAny = new[] { R("orbit.root", int.MaxValue) };
        Reject(check, Append(a), "extreme prerequisite rank rejected without overflow");
        a = Card("qa.a"); a.RequiresAll = new[] { R("orbit.root"), R("orbit.root") };
        Reject(check, Append(a), "duplicate ALL entries rejected");
        a = Card("qa.a"); a.RequiresAny = new[] { R("crit.root"), R("crit.root") };
        Reject(check, Append(a), "duplicate ANY entries rejected");
        a = Card("qa.a"); a.Parent = "orbit.root";
        Reject(check, Append(a), "legacy and new prerequisite syntax rejected together");
        a = Card("qa.a"); data = Append(a); data.SchemaVersion = 1;
        Reject(check, data, "schema1 cannot silently accept schema2 requirements");

        // Every alternative is individually reachable, but each conflicts with a mandatory ancestor.
        var aa = Card("qa.aa", "qa.g1"); var ab = Card("qa.ab", "qa.g1");
        var ba = Card("qa.ba", "qa.g2"); var bb = Card("qa.bb", "qa.g2");
        var cross = Card("qa.cross");
        cross.RequiresAll = new[] { R(aa.Id), R(ba.Id) };
        cross.RequiresAny = new[] { R(ab.Id), R(bb.Id) };
        Reject(check, Append(aa, ab, ba, bb, cross), "joint ALL plus OR cannot union incompatible assignments");

        // 11 two-member groups have 3^11 assignments including none, exceeding 65536.
        var manyGroups = new CardDefinition[22];
        for (int i = 0; i < 11; i++)
        {
            manyGroups[i * 2] = Card("qa.budget.left." + i, "qa.budget." + i);
            manyGroups[i * 2 + 1] = Card("qa.budget.right." + i, "qa.budget." + i);
        }
        Reject(check, Append(manyGroups), "exclusive assignment budget explicitly rejects oversized catalog");

        // Runtime reset must clear the exclusive selection; it is not persistent catalog state.
        var resetData = Append(Card("qa.reset.left", "qa.reset"), Card("qa.reset.right", "qa.reset"));
        var resetGame = new GameSimulation(resetData);
        resetGame.StartRun("orbit", 1030);
        int resetLeft = resetGame.Catalog.CardIndex["qa.reset.left"];
        resetGame.Loadout.Levels[resetLeft] = 1; // test-only fixture, not a public gameplay port.
        check(resetGame.GetCardAvailability("qa.reset.right") == CardAvailability.ExclusiveLocked,
            "independent schema2: runtime availability reports sibling lock");
        check(resetGame.GetCardAvailability("qa.reset.left") == CardAvailability.Available,
            "independent schema2: runtime selected member remains rankable");
        resetGame.StartRun("orbit", 1030);
        check(resetGame.GetCardAvailability("qa.reset.right") == CardAvailability.Available,
            "independent schema2: new run clears group selection");

        // Authoritative command consumption rechecks requirements, not merely old offer membership.
        RunProgressionSystem.FinishWave(resetGame);
        int resetRight = resetGame.Catalog.CardIndex["qa.reset.right"];
        resetGame.Loadout.Levels[resetLeft] = 1;
        resetGame.Loadout.Offers[0] = resetRight; // synthetic stale view, core must still reject.
        resetGame.Enqueue(GameCommand.Choose("qa.reset.right")); resetGame.Step();
        check(resetGame.GetLevel("qa.reset.right") == 0 && !resetGame.CardPicked,
            "independent schema2: stale offered exclusive sibling rejected at consumption");
        resetGame.Loadout.Offers[0] = resetLeft;
        resetGame.Enqueue(GameCommand.Choose("qa.reset.left"));
        resetGame.Enqueue(GameCommand.Choose("qa.reset.left")); resetGame.Step();
        check(resetGame.GetLevel("qa.reset.left") == 2 && resetGame.CardPicked,
            "independent schema2: ranked member selected only once for duplicate intents");
        int oldRun = resetGame.RunToken;
        resetGame.StartRun("orbit", 1030); RunProgressionSystem.FinishWave(resetGame);
        resetGame.Loadout.Offers[0] = resetRight;
        resetGame.Enqueue(new GameCommand(CommandKind.Choose, "qa.reset.right", runToken: oldRun, wave: 1));
        resetGame.Step();
        check(resetGame.GetLevel("qa.reset.right") == 0 && !resetGame.CardPicked,
            "independent schema2: prior-run callback cannot select new graph card");

        a = Card("qa.a"); a.RequiresAll = new[] { R("missing") };
        Reject(check, Append(a), "unknown requirement rejected");
        a = Card("qa.a"); a.RequiresAll = new[] { R("qa.a") };
        Reject(check, Append(a), "self dependency rejected");
        a = Card("qa.a"); a.RequiresAll = new RequirementDefinition[] { null! };
        Reject(check, Append(a), "null requirement rejected with catalog error");
        a = Card("qa.a"); a.RequiresAll = new[] { R("orbit.root") }; a.RequiresAny = new[] { R("orbit.root") };
        Reject(check, Append(a), "same requirement across ALL and ANY rejected");
        a = Card("qa.a"); a.Slot = "base";
        Reject(check, Append(a), "cross-slot requirement rejected");
        a = Card("qa.a", "qa.single");
        Reject(check, Append(a), "single-member exclusive group rejected");
        a = Card("qa.a", "bad group"); b = Card("qa.b", "bad group");
        Reject(check, Append(a, b), "malformed exclusive group ID rejected");
        data = Load(); data.Cards[0].RequiresAll = Array.Empty<RequirementDefinition>();
        data.Cards[0].Parent = "orbit.root";
        Reject(check, data, "explicit empty modern array still conflicts with legacy parent");
        data = Load(); data.SchemaVersion = 1; data.Cards[0].RequiresAny = Array.Empty<RequirementDefinition>();
        Reject(check, data, "schema1 rejects even empty explicitly modern fields");

        // Eligibility is a compiled hot-path query, not a per-call graph search allocation.
        for (int i = 0; i < 1000; i++) _ = catalog.Graph.IsEligible(ei, levels);
        long before = GC.GetAllocatedBytesForCurrentThread();
        int eligible = 0;
        for (int i = 0; i < 10000; i++) if (catalog.Graph.IsEligible(ei, levels)) eligible++;
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        check(eligible == 10000, "independent schema2: repeated query deterministic");
        check(allocated == 0, "independent schema2: warm eligibility zero allocated bytes, actual=" + allocated);
    }
}
