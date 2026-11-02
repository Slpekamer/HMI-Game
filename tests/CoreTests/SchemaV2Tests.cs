using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Hmi.Core;

internal static class SchemaV2Tests
{
    private static GameContent Load() => JsonSerializer.Deserialize<GameContent>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "legacy-gameplay.json")), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    internal static void Run(Action<bool, string> check)
    {
        // Historical rule1 0x668C7C237D35F542 and rule2 0x7D2B96E84569B575 remain recorded.
        // PROJECTILE-0001 changes authority timing; rule3 digest now includes projectile views/transitions/RNG.
        const ulong golden = 0xFB1A336A979BCC55;
        Console.WriteLine("RULE3_SCHEMA_PARITY_DIGEST " + golden.ToString("X16"));
        var data = Load();
        check(LegacyParity.Digest(data) == golden, "schema1 rule3 full projectile deterministic replay digest");
        check(LegacyParity.Digest(data) == golden, "same seed and commands repeat full rule3 actor/projectile trace");
        data.SchemaVersion = 2;
        check(LegacyParity.Digest(data) == golden, "schema2 legacy parent golden replay digest");
        foreach (var card in data.Cards)
        {
            if (!string.IsNullOrEmpty(card.Parent))
                card.RequiresAll = new[] { new RequirementDefinition { Id = card.Parent, Rank = card.ParentLevel } };
            card.Parent = null;
        }
        check(LegacyParity.Digest(data) == golden, "schema2 normalized graph golden replay digest");
        data = Load();
        foreach (var card in data.Cards) if (card.Parent == null) card.Parent = "";
        check(LegacyParity.Digest(data) == golden, "schema1 empty parent compatibility");

        data = Load();
        data.SchemaVersion = 2;
        data.Characters = new[] { data.Characters[0] };
        var root = new CardDefinition { Id = data.Characters[0].Root, Slot = "trait", Cap = 1 };
        var generous = new CardDefinition { Id = "pool.generous", Slot = "trait", Cap = 5, ExclusiveGroup = "pool" };
        var exhausted = new CardDefinition { Id = "pool.exhausted", Slot = "trait", Cap = 1, ExclusiveGroup = "pool" };
        data.Cards = new[] { root, generous, exhausted,
            new CardDefinition { Id = "pool.base", Slot = "base", Cap = 5 },
            new CardDefinition { Id = "pool.risk", Slot = "risk", Cap = 5 } };
        bool poolRejected = false;
        try { _ = new Catalog(data); } catch (ArgumentException) { poolRejected = true; }
        check(poolRejected, "one generous branch cannot hide another finite trait dead end");
        exhausted.Cap = 5;
        check(new Catalog(data).Graph.AssignmentCount == 3, "none group assignment is not falsely treated as exhausted");

        var nestedA = new CardDefinition { Id = "pool.nested.a", Slot = "trait", Cap = 5, ExclusiveGroup = "nested", RequiresAll = new[] { new RequirementDefinition { Id = generous.Id } } };
        var nestedB = new CardDefinition { Id = "pool.nested.b", Slot = "trait", Cap = 5, ExclusiveGroup = "nested", RequiresAll = new[] { new RequirementDefinition { Id = generous.Id } } };
        var nested = new CardDefinition[7];
        Array.Copy(data.Cards, nested, 5);
        nested[5] = nestedA;
        nested[6] = nestedB;
        data.Cards = nested;
        check(new Catalog(data).Graph.AssignmentCount == 9, "unavailable nested group none is legal terminal with sufficient other capacity");
        exhausted.Cap = 1;
        poolRejected = false;
        try { _ = new Catalog(data); } catch (ArgumentException) { poolRejected = true; }
        check(poolRejected, "unavailable nested group cannot rescue exhausted selected branch");

        data = Load();
        data.SchemaVersion = 2;
        int oldCount = data.Cards.Length;
        var envelopeCards = new CardDefinition[oldCount + 2];
        Array.Copy(data.Cards, envelopeCards, oldCount);
        for (int i = 0; i < 2; i++) envelopeCards[oldCount + i] = new CardDefinition
        {
            Id = "envelope." + i, Slot = "trait", Cap = 1, ExclusiveGroup = "envelope",
            Effects = new[] { new EffectDefinition { Stat = "BasicRate", Operation = "add", Value = 10 } }
        };
        data.Cards = envelopeCards;
        check(new Catalog(data).Graph.AssignmentCount == 3, "stat intervals never combine mutually exclusive positive effects");
        envelopeCards[oldCount].Effects[0].Value = -2;
        bool unsafeRejected = false;
        try { _ = new Catalog(data); } catch (ArgumentException) { unsafeRejected = true; }
        check(unsafeRejected, "reachable negative effective stat rejected inside exclusive group");

        // Eight 3-member groups have exactly 4^8 = 65536 assignments, including none.
        // Fill to the supported 256-card limit; this is synthetic load validation, not shipped content.
        data = Load();
        data.SchemaVersion = 2;
        int legacy = data.Cards.Length;
        var cards = new CardDefinition[256];
        Array.Copy(data.Cards, cards, legacy);
        for (int i = legacy; i < cards.Length; i++)
        {
            int offset = i - legacy;
            cards[i] = new CardDefinition
            {
                Id = "load." + offset, Name = "Synthetic", Slot = "trait", Cap = 1,
                ExclusiveGroup = offset < 24 ? "load.group." + offset / 3 : null,
                Effects = Array.Empty<EffectDefinition>()
            };
        }
        data.Cards = cards;
        long before = GC.GetAllocatedBytesForCurrentThread();
        var watch = Stopwatch.StartNew();
        var catalog = new Catalog(data);
        watch.Stop();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        check(catalog.Graph.AssignmentCount == 65536, "maximum supported assignment count loads");
        Console.WriteLine($"SCHEMA2_LOAD cards=256 assignments=65536 elapsed_ms={watch.Elapsed.TotalMilliseconds:F2} allocated_bytes={allocated}");
        var levels = new int[256];
        for (int i = 0; i < 1000; i++) _ = catalog.Graph.IsEligible(legacy, levels);
        before = GC.GetAllocatedBytesForCurrentThread();
        int available = 0;
        for (int i = 0; i < 10000; i++) if (catalog.Graph.IsEligible(legacy, levels)) available++;
        long queryBytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Console.WriteLine($"SCHEMA2_QUERY calls=10000 available={available} allocated_bytes={queryBytes}");
        check(queryBytes == 0 && available == 10000, "maximum catalog runtime eligibility zero allocation; bytes=" + queryBytes + "; available=" + available);
        Array.Resize(ref cards, 257);
        data.Cards = cards;
        bool rejected = false;
        try { _ = new Catalog(data); } catch (ArgumentException) { rejected = true; }
        check(rejected, "card budget rejected before incomplete data compilation");
    }
}
