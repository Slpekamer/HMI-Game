using System;
using System.Collections;
using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Hmi.Core;

internal static class PreparedCheckpointTests
{
    static readonly CheckpointIdentity Identity = new CheckpointIdentity(Guid.Parse("6d761d19-b0e1-4b03-a8a3-876a7297c041"), 2);
    static GameContent Load() => JsonSerializer.Deserialize<GameContent>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "gameplay.json")), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    static string Hash => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "gameplay.json")))).ToLowerInvariant();
    static GameSimulation New() => new GameSimulation(Load(), new SimulationLimits(32, 10, 4), Hash);
    static WaveCheckpointDto Copy(WaveCheckpointDto dto) => JsonSerializer.Deserialize<WaveCheckpointDto>(JsonSerializer.Serialize(dto))!;
    static void Dump(StringBuilder b, object? value, bool handles)
    {
        if (value == null) { b.Append("null;"); return; }
        var type = value.GetType();
        if (value is float f) { b.Append(BitConverter.SingleToInt32Bits(f)).Append(';'); return; }
        if (type.IsPrimitive || type.IsEnum || value is string || value is Guid) { b.Append(value).Append(';'); return; }
        if (value is IEnumerable sequence) { foreach (var item in sequence) Dump(b, item, handles); b.Append("end;"); return; }
        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).OrderBy(x => x.Name, StringComparer.Ordinal))
        {
            if (!handles && (field.Name == "Generation" || field.Name == "RunToken" || field.Name == "PreparationOrdinal" || (field.Name == "Run" && field.FieldType == typeof(int)))) continue;
            b.Append(field.Name).Append(':'); Dump(b, field.GetValue(value), handles);
        }
    }
    // Semantic comparisons exclude session handles and inactive component storage; exact rejection comparisons include handles.
    internal static string Digest(GameSimulation g, bool handles)
    {
        var b = new StringBuilder();
        Dump(b, g.Run, handles); Dump(b, g.Wallet, handles); Dump(b, g.Loadout, handles);
        Dump(b, g.Stats, handles); Dump(b, g.BaseStats, handles); Dump(b, g.Multipliers, handles);
        Dump(b, new object[] { g.MoveInput, g.MeleeTimer, g.RicochetTimer, g.HoneyTimer, g.ImpactTimer, g.SpawnTimer, g.PowerTime, g.HasteTime, g.Stationary, g.SpawnCounter }, handles);
        foreach (var command in g.Commands) Dump(b, command, handles);
        b.Append(g.EntityCount).Append(';');
        for (int i = 0; i < g.Capacity; i++)
        {
            if (handles) b.Append(g.World.Generation[i]).Append(';');
            if (g.World.Mask[i] == Components.None) continue;
            b.Append(i).Append(';'); Dump(b, g.World.Mask[i], handles); Dump(b, g.World.Transform[i], handles);
            Dump(b, g.World.Health[i], handles); Dump(b, g.World.Enemy[i], handles); Dump(b, g.World.Companion[i], handles);
            Dump(b, g.World.Pickup[i], handles); Dump(b, g.World.Visual[i], handles); Dump(b, g.World.PendingDeath[i], handles);
        }
        if (handles)
        {
            Dump(b, typeof(GameSimulation).GetField("_systems", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(g), true);
            b.Append(g.EventCount).Append(';').Append(g.DroppedEvents).Append(';');
            for (int i = 0; i < g.EventCount; i++) Dump(b, g.GetEvent(i), true);
        }
        return b.ToString();
    }
    static WaveCheckpointDto Capture(GameSimulation g)
    {
        if (!g.TryCapturePrepared(Identity, out var dto, out var error)) throw new Exception("Capture fixture failed: " + error);
        return dto!;
    }
    static void Set(WaveCheckpointDto dto, string id, int level)
    { foreach (var card in dto.Cards) if (card.Id == id) { card.Count = level; return; } throw new Exception(id); }
    static void Reject(Action<bool, string> check, GameSimulation g, WaveCheckpointDto dto, string label)
    {
        string before = Digest(g, true);
        object? systems = typeof(GameSimulation).GetField("_systems", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(g);
        check(!g.TryRestorePrepared(dto, Identity, out var error) && error != CheckpointError.None, "checkpoint reject " + label);
        check(Digest(g, true) == before && ReferenceEquals(systems, typeof(GameSimulation).GetField("_systems", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(g)), "failed checkpoint leaves exact authority and system storage unchanged " + label);
    }
    static GameSimulation Late(string contract)
    {
        var g = New(); g.StartRun("orbit", 1030);
        for (int wave = 1; wave < 100; wave++)
        {
            RunProgressionSystem.FinishWave(g);
            if (g.Phase == RunPhase.Victory) { g.Enqueue(GameCommand.ContinueEndless()); g.Step(); }
            var slots = g.GetOfferSlots(); int index = wave % 3;
            while (!slots[index].Enabled) index = (index + 1) % 3;
            g.Enqueue(GameCommand.Choose(slots[index].Card.Id)); g.Step();
            g.Enqueue(GameCommand.NextWave()); g.Step();
        }
        RunProgressionSystem.FinishWave(g);
        g.Wallet.Coins = g.Wallet.Earned = 100; g.Wallet.Spent = 0; g.Wallet.Kills = 0;
        g.Enqueue(GameCommand.Buy("shield")); g.Enqueue(GameCommand.Buy("tonic")); g.Enqueue(GameCommand.Buy("shoes")); g.Step();
        int ci = TemporaryContractSystem.Find(g, contract);
        string category = g.Catalog.Contracts[ci].Slot;
        g.Loadout.Offers[category == "trait" ? 0 : category == "base" ? 1 : 2] = -3 - ci;
        g.Enqueue(GameCommand.Choose(contract)); g.Step();
        if (!g.CardPicked || !g.PrepareNextWave()) throw new Exception("Late prepared fixture failed");
        return g;
    }
    internal static void Run(Action<bool, string> check)
    {
        var g = New(); g.PrepareRun("orbit", 0);
        uint random = g.Run.Random; var token = g.PreparedToken;
        check(g.Phase == RunPhase.Prepared && g.EntityCount == 0 && g.Tick == 0 && random == 1, "initial prepare has no world/tick/RNG initialization");
        var dto = Capture(g); string serialized = JsonSerializer.Serialize(dto);
        for (int i = 0; i < 3; i++) check(JsonSerializer.Serialize(Capture(g)) == serialized && g.Run.Random == random, "capture idempotent and RNG unchanged");
        var detached = Capture(g); detached.Cards[0].Count = 99;
        check(JsonSerializer.Serialize(Capture(g)) == serialized, "capture has detached arrays");
        g.Enqueue(GameCommand.Move(1, 0)); g.Enqueue(GameCommand.Pause(true)); g.Enqueue(GameCommand.Buy("shield")); g.Enqueue(GameCommand.Choose("honey.root")); g.Step();
        check(g.Phase == RunPhase.Prepared && !g.Paused && g.Tick == 0 && g.Run.Random == random && g.GetLevel("honey.root") == 0 && g.GetItemCount("shield") == 0, "prepared ignores gameplay commands");
        check(g.TryValidateCheckpoint(dto, Identity, out _), "initial checkpoint validates");
        check(g.BeginPreparedWave(token) && g.Tick == 0 && g.EntityCount > 0, "begin initializes without implicit tick");
        string started = Digest(g, true);
        check(!g.BeginPreparedWave(token) && Digest(g, true) == started, "duplicate begin no mutation");
        check(!g.TryCapturePrepared(Identity, out _, out var captureError) && captureError == CheckpointError.NotPrepared, "combat cannot be captured as canonical wave start");
        check(g.TryValidateCheckpoint(dto, Identity, out _) && Digest(g, true) == started, "pure validation works after begin without authority mutation");
        int oldRun = g.RunToken; var oldEntity = g.PlayerId;
        object? oldSystems = typeof(GameSimulation).GetField("_systems", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(g);
        check(g.TryRestorePrepared(dto, Identity, out _) && g.RunToken == oldRun + 1 && g.EntityCount == 0 && g.Phase == RunPhase.Prepared && !ReferenceEquals(oldSystems, typeof(GameSimulation).GetField("_systems", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(g)), "restore atomically replaces old session into prepared");
        check(!g.BeginPreparedWave(token), "pre-restore begin token rejected");
        check(g.BeginPreparedWave(g.PreparedToken) && !g.IsAlive(oldEntity), "old entity handle does not alias restored actor");
        Vec2 position = g.World.Transform[g.PlayerId.Index].Position;
        g.Enqueue(new GameCommand(CommandKind.Move, x: 1, runToken: oldRun, wave: g.Wave)); g.Step();
        check(g.World.Transform[g.PlayerId.Index].Position.X == position.X, "old UI command run scope rejected after restore");
        var unbound = new GameSimulation(Load()); unbound.PrepareRun("orbit", 1);
        check(!unbound.TryCapturePrepared(Identity, out _, out var missing) && missing == CheckpointError.MissingCatalogIdentity, "legacy constructor cannot invent checkpoint catalog identity");

        Action<WaveCheckpointDto>[] corrupt = {
            x => x.SchemaVersion = 99, x => x.CatalogHash = new string('0',64), x => x.RulesVersion = "wrong",
            x => x.RunId = Guid.Parse("a1111111-2222-4333-8444-555555555555"), x => x.Sequence++, x => x.EntityCapacity++, x => x.EnemyHealthCap = float.NaN,
            x => x.Wave = 0, x => x.Wave = 7, x => x.FiniteCompleted = true, x => x.RandomState = 0,
            x => x.CharacterId = "missing", x => x.Cards[0].Count = -1, x => x.Cards[0].Id = "missing",
            x => x.Cards[1].Id = x.Cards[0].Id, x => x.Cards = null!, x => x.Items = null!,
            x => x.Coins = -1, x => { x.Earned = 5; x.Coins = 5; }, x => x.Spent = int.MaxValue,
            x => x.Kills = 1, x => x.FinitePicks = new[] { "orbit.root" }, x => x.PendingContractId = "temp.base.rate",
            x => x.PendingLegacySupply = true, x => x.EndlessOfferHistory++, x => x.CompanionsStationary = true,
            x => x.Items[0].Count = 1, x => x.Cards[0] = null!, x => x.Items[0] = null!
        };
        for (int i = 0; i < corrupt.Length; i++) { var bad = Copy(dto); corrupt[i](bad); Reject(check, g, bad, "field-" + i); }
        string unchanged = Digest(g, true);
        check(!g.TryRestorePrepared(dto, new CheckpointIdentity(Identity.RunId, Identity.Sequence + 1), out _) && Digest(g, true) == unchanged, "descriptor identity binds snapshot actual sequence");
        var ordered = Copy(dto); ordered.Wave = 3; Set(ordered, "orbit.wide", 1); Set(ordered, "orbit.outer", 1);
        ordered.FinitePicks = new[] { "orbit.outer", "orbit.wide" }; Reject(check, g, ordered, "child before parent history");
        ordered.FinitePicks = new[] { "orbit.wide", "orbit.outer" };
        check(g.TryValidateCheckpoint(ordered, Identity, out _), "legal prerequisite history order accepted");
        var freeItems = Copy(ordered); freeItems.Items.First(x => x.Id == "shoes").Count = 1;
        Reject(check, g, freeItems, "owned permanent item exceeds spent lower bound");
        var exclusive = Copy(ordered); exclusive.Wave = 4; Set(exclusive, "orbit.inner", 1); exclusive.FinitePicks = new[] { "orbit.wide", "orbit.outer", "orbit.inner" };
        Reject(check, g, exclusive, "exclusive siblings coexist");
        var limited = New(); limited.Run.RunToken = int.MaxValue;
        check(limited.TryValidateCheckpoint(dto, Identity, out _), "pure DTO validation independent of live token exhaustion");
        Reject(check, limited, dto, "session token exhausted");
        limited = New(); limited.World.Generation[0] = int.MaxValue;
        Reject(check, limited, dto, "entity generation exhausted");
        limited = New(); limited.Run.PreparationOrdinal = long.MaxValue;
        Reject(check, limited, dto, "preparation ordinal exhausted");
        g.Run.Phase = RunPhase.Defeat; Reject(check, g, dto, "terminal live run cannot be revived through restore");

        foreach (var definition in Load().TemporaryContracts)
        {
            var a = Late(definition.Id); var saved = Capture(a); uint preRandom = a.Run.Random;
            check(a.PendingContractId == definition.Id && a.GetItemCount("shield") == 1 && a.ActiveContractId == null, "prepared contains unapplied contract and wave shop item");
            check(a.TryValidateCheckpoint(saved, Identity, out var validationError), "late complete DTO validates " + definition.Id + "/" + validationError);
            var b = New(); check(b.TryRestorePrepared(saved, Identity, out _), "restore each pending contract");
            check(a.Run.Random == preRandom && b.Run.Random == preRandom && b.GetItemCount("shield") == 1, "capture/restore never consumes pending item or RNG");
            check(a.BeginPreparedWave(a.PreparedToken) && b.BeginPreparedWave(b.PreparedToken), "same canonical initializer for uninterrupted/restored wave");
            check(Digest(a, false) == Digest(b, false), "normalized initial authority matches including wallet, stats and RNG");
            for (int tick = 0; tick < 120; tick++)
            {
                float x = (float)Math.Sin(tick * .1), y = (float)Math.Cos(tick * .1);
                a.Enqueue(GameCommand.Move(x, y)); b.Enqueue(GameCommand.Move(x, y)); a.Step(); b.Step();
                if (tick % 30 == 0) check(Digest(a, false) == Digest(b, false), "normalized command replay after restore");
            }
            check(b.TryRestorePrepared(saved, Identity, out _) && b.BeginPreparedWave(b.PreparedToken), "same checkpoint may rebuild canonical state once per fresh session");
            var c = New(); check(c.TryRestorePrepared(saved, Identity, out _) && c.BeginPreparedWave(c.PreparedToken) && Digest(b, false) == Digest(c, false), "repeated restores do not stack pending effects");
        }
        var retired = new World(2); retired.Generation[0] = int.MaxValue;
        var remaining = retired.Create(Components.Enemy | Components.Health, default, .4f, "fixture", 1);
        check(remaining.Index == 1 && retired.Generation[0] == int.MaxValue, "retired generation slot is skipped without wrap");
        retired.Destroy(1); retired.Generation[1] = int.MaxValue;
        check(retired.Create(Components.Enemy, default, .4f, "fixture") == EntityId.None && !retired.HasFreeSlot, "all retired slots fail closed");
        var retiredGame = New(); retiredGame.Run.Random = 123;
        for (int i = 0; i < retiredGame.Capacity; i++) retiredGame.World.Generation[i] = int.MaxValue;
        retiredGame.SpawnEnemy();
        check(retiredGame.Run.Random == 123 && retiredGame.EntityCount == 0, "retired-slot spawn rejection does not consume RNG");
        var finalOrdinal = New(); finalOrdinal.Run.PreparationOrdinal = long.MaxValue - 1; finalOrdinal.PrepareRun("orbit", 1);
        check(finalOrdinal.PreparedToken.Ordinal == long.MaxValue && finalOrdinal.BeginPreparedWave(finalOrdinal.PreparedToken), "last representable prepared ordinal initializes once without increment overflow");
        var perf = new GameSimulation(Load(), new SimulationLimits(), Hash); perf.PrepareRun("orbit", 1); var perfDto = Capture(perf);
        for (int i = 0; i < 5; i++) perf.TryValidateCheckpoint(perfDto, Identity, out _);
        var watch = new Stopwatch(); long bytes = GC.GetAllocatedBytesForCurrentThread(); int validations = 0;
        watch.Start(); for (int i = 0; i < 50; i++) if (perf.TryValidateCheckpoint(perfDto, Identity, out _)) validations++; watch.Stop();
        bytes = GC.GetAllocatedBytesForCurrentThread() - bytes;
        check(validations == 50 && perf.EntityCount == 0 && perf.Tick == 0 && perf.Run.Random == 1, "validation benchmark is pure and does not initialize");
        Console.WriteLine($"E_VALIDATE capacity=512 count=50 elapsed_ms={watch.Elapsed.TotalMilliseconds:F2} allocated_bytes={bytes}; deliberate staging allocation, not a tick hot path");
        Console.WriteLine("E_CORE prepared/capture/validate/restore tests exercised; semantic digests exclude runtime handles and inactive storage; no file crash guarantee claimed");
    }
}
