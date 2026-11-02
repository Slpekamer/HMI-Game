using Hmi.Core;
using Hmi.Adapters.Persistence;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

static partial class Program
{
    static int count;
    static void Check(bool value, string label) { count++; if (!value) throw new Exception(label); }
    static readonly byte[] Content = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "gameplay.json"));
    static readonly string Hash = Convert.ToHexString(SHA256.HashData(Content)).ToLowerInvariant();
    static GameSimulation Sim() => new(JsonSerializer.Deserialize<GameContent>(Content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!, new SimulationLimits(32, 10, 4), Hash);
    static (GameSimulation G, CheckpointSession S, CheckpointCodec C, CheckpointStore Store) Make(ICheckpointFiles files)
    {
        var g = Sim(); var c = new CheckpointCodec(g);
        var store = new CheckpointStore(files, Hash, GameSimulation.CheckpointRulesVersion, c.Validate);
        return (g, new CheckpointSession(g, store, c), c, store);
    }
    static void Between(GameSimulation g)
    {
        g.Run.Tick = WaveRules.DurationTicks;
        g.World.Health[g.PlayerId.Index].Current = 100;
        g.Step();
        Check(g.Phase == RunPhase.Between, "controlled rule-duration completion (not survival)");
        var offer = g.GetOfferSlots().First(s => s.Enabled);
        g.Enqueue(GameCommand.Choose(offer.Card.Id)); g.Step();
        Check(g.CardPicked, "fixture choice");
    }
    static void Main()
    {
        var files = new MemoryFiles(); var (g,s,c,store) = Make(files);
        long old = s.IntentEpoch;
        Check(s.StartNew("honey", 1030, old), "start");
        Check(g.Phase == RunPhase.Combat && g.Tick == 0 && s.CurrentWaveSaved, "begin exactly once no tick");
        Check(!s.StartNew("orbit", 99, old) && g.CharacterId == "honey", "old start rejected");
        uint rng = g.Run.Random; int entities = g.EntityCount;
        Check(!s.Retry(old) && g.Run.Random == rng, "old retry rejected");
        var recovery = store.Recover();
        var id = new SnapshotIdentity(recovery.Control!.Run, recovery.Control.Sequence, Hash, GameSimulation.CheckpointRulesVersion);
        Check(c.TryDecode(recovery.Payload!, id, out var dto), "codec decode");
        string json = Encoding.UTF8.GetString(recovery.Payload!);
        Check(!c.Validate(Encoding.UTF8.GetBytes(json.Replace("\"SchemaVersion\":1", "\"SchemaVersion\":1,\"SchemaVersion\":1")), id), "duplicate property");
        Check(!c.Validate(Encoding.UTF8.GetBytes(json.Replace("\"SchemaVersion\":1,", "")), id), "missing field");
        Check(!c.Validate(Encoding.UTF8.GetBytes(json.Replace("\"SchemaVersion\":1", "\"SchemaVersion\":1,\"Unknown\":0")), id), "unknown property");
        Check(!c.Validate(recovery.Payload!, id with { Sequence = id.Sequence + 1 }), "descriptor seq mismatch");
        Between(g); int coins = g.Coins;
        Check(s.PrepareNext(s.IntentEpoch) && g.Wave == 2 && g.Coins == coins, "next persists without reward duplication");
        var saved = store.Recover(); uint waveRng = g.Run.Random;
        g.Step(); g.Step();
        Check(s.ExitSelection(s.IntentEpoch), "exit keeps safe point");
        Check(s.Resume(s.IntentEpoch) && g.Tick == 0 && g.Run.Random == waveRng && g.Coins == coins, "resume rollback RNG wallet");
        var fresh = Make(files);
        Check(fresh.S.Resume(fresh.S.IntentEpoch) && fresh.G.Wave == 2 && fresh.G.Run.Random == waveRng, "new process semantic resume");
        // Corrupt latest only; store retains previous same-run descriptor and codec uses its own sequence.
        files.Data[saved.Control!.Snapshots[0].Name] = new byte[] { 1 };
        var fallback = Make(files);
        Check(fallback.S.CurrentRecovery.Kind == RecoveryKind.RolledBack, "fallback listed");
        Check(fallback.S.Resume(fallback.S.IntentEpoch) && fallback.G.Wave == 1, "fallback descriptor sequence not control latest");
        Failures();
        BoundaryFailures();
        ContractRoundtrip();
        IndependentCases();
        string path = Path.Combine(Path.GetTempPath(), "hmi-session-" + Guid.NewGuid().ToString("N"));
        try
        {
            var physical = Make(new PhysicalCheckpointFiles(path));
            Check(physical.S.StartNew("orbit", 42, physical.S.IntentEpoch), "physical start");
            var resumed = Make(new PhysicalCheckpointFiles(path));
            Check(resumed.S.Resume(resumed.S.IntentEpoch) && resumed.G.CharacterId == "orbit", "physical restart resume");
            Check(resumed.S.Close(resumed.S.IntentEpoch), "physical explicit abandon");
            Check(Make(new PhysicalCheckpointFiles(path)).S.CurrentRecovery.Kind == RecoveryKind.Closed, "physical closed no resurrection");
        }
        finally { Directory.Delete(path, true); }
        Console.WriteLine("PASS CheckpointSession " + count);
    }
    static void ContractRoundtrip()
    {
        var files = new MemoryFiles(); var x = Make(files);
        Check(x.S.StartNew("honey", 77, x.S.IntentEpoch), "contract run start");
        bool found = false, itemSeen = false;
        for (int completed = 0; completed < 140 && !found; completed++)
        {
            for (int enemy = 0; enemy < 8; enemy++) x.G.SpawnEnemy();
            // Rule2 SpawnEnemy reserves a warning, not an immediately damageable enemy.
            // Advance the real preview deadline before this synthetic reward fixture kills them.
            for (int tick = 0; tick < WaveRules.SpawnPreviewTicks; tick++) x.G.Step();
            Check(x.G.Phase == RunPhase.Combat && Enumerable.Range(0, x.G.Capacity)
                .Any(slot => (x.G.World.Mask[slot] & Components.Enemy) != 0), "real preview activates fixture enemies before reward damage");
            for (int slot = 0; slot < x.G.Capacity; slot++)
                if ((x.G.World.Mask[slot] & Components.Enemy) != 0) x.G.Damage(slot, 10000, x.G.PlayerId);
            x.G.Run.Tick = WaveRules.DurationTicks; x.G.World.Health[x.G.PlayerId.Index].Current = 100; x.G.Step();
            if (x.G.Phase == RunPhase.Victory)
            {
                int before = x.G.Coins;
                x.G.Enqueue(GameCommand.ContinueEndless()); x.G.Step();
                Check(x.G.Phase == RunPhase.Between && before == x.G.Coins, "sixth reward not repeated by continue");
            }
            Check(x.G.Phase == RunPhase.Between, "synthetic full-wave boundary");
            if (x.G.CanBuy("shield")) { x.G.Enqueue(GameCommand.Buy("shield")); x.G.Step(); itemSeen = true; }
            var offers = x.G.GetOfferSlots();
            var chosen = offers.FirstOrDefault(o => o.Kind == OfferKind.TemporaryContract) ?? offers.First(o => o.Enabled);
            x.G.Enqueue(GameCommand.Choose(chosen.Card.Id)); x.G.Step();
            Check(x.G.CardPicked && x.S.PrepareNext(x.S.IntentEpoch), "commit chosen build and initialize");
            if (chosen.Kind != OfferKind.TemporaryContract) continue;
            found = true;
            string? contract = x.G.ActiveContractId;
            uint random = x.G.Run.Random; int coins = x.G.Coins, spent = x.G.Spent;
            float[] stats = Enum.GetValues<Stat>().Select(x.G.GetStat).ToArray();
            var replay = Make(files);
            Check(replay.S.Resume(replay.S.IntentEpoch), "contract snapshot resumed");
            Check(replay.G.ActiveContractId == contract && replay.G.PendingContractId == null
                && replay.G.Run.Random == random && replay.G.Coins == coins && replay.G.Spent == spent,
                "pending contract and items initialized once with RNG and ledger");
            Check(Enum.GetValues<Stat>().Select(replay.G.GetStat).SequenceEqual(stats), "restored stats equal once-applied contract and wave item");
            Check(replay.S.ExitSelection(replay.S.IntentEpoch) && replay.S.Resume(replay.S.IntentEpoch)
                && Enum.GetValues<Stat>().Select(replay.G.GetStat).SequenceEqual(stats), "repeated resume never compounds pending effects");
        }
        Check(found && itemSeen, "synthetic run reaches an actual contract with bought wave item found=" + found + " item=" + itemSeen);
    }

    static void BoundaryFailures()
    {
        var files = new MemoryFiles(); var x = Make(files);
        files.Deny = true;
        x.S.RefreshRecovery();
        Check(x.S.CurrentRecovery.Kind == RecoveryKind.Uncertain, "permission failure refresh becomes uncertain");
        files.Deny = false; x.S.RefreshRecovery();
        files.Deny = true;
        Check(!x.S.StartNew("orbit", 1, x.S.IntentEpoch) && !x.S.CanContinueUnsaved, "permission failure start gates core");
        files.Deny = false;
        Check(x.S.Retry(x.S.IntentEpoch), "permission retry start");
        Between(x.G);
        files.Deny = true;
        Check(!x.S.PrepareNext(x.S.IntentEpoch) && x.G.Phase == RunPhase.Prepared
            && !x.S.CanContinueUnsaved && !x.S.CurrentWaveSaved, "later save failure frozen not unsaved");
        files.Deny = false;
        Check(x.S.Retry(x.S.IntentEpoch) && x.G.Wave == 2, "later save retry");
        files.Deny = true;
        Check(!x.S.Close(x.S.IntentEpoch) && x.S.IsCloseFailure, "permission close gates actions");
        files.Deny = false;
        Check(x.S.Retry(x.S.IntentEpoch), "permission close retry");
        // Post-commit core preparation failure must not retry a new/different store request.
        var invalid = Make(new MemoryFiles());
        Check(!invalid.S.StartNew("not-a-hero", 1, invalid.S.IntentEpoch), "unknown hero core rejection");
        long seq = invalid.Store.Recover().Control!.Sequence;
        Check(!invalid.S.Retry(invalid.S.IntentEpoch) && invalid.Store.Recover().Control!.Sequence == seq
            && invalid.S.LastError.Contains("Unknown character"), "postcommit prepare retry no sequence/hash drift");
        var exhausted = Make(new MemoryFiles()); exhausted.G.Run.RunToken = int.MaxValue;
        Check(!exhausted.S.StartNew("orbit", 1, exhausted.S.IntentEpoch)
            && !exhausted.S.Retry(exhausted.S.IntentEpoch), "postcommit handle exhaustion fail closed");
        // Simulate an unauthorized host scope mutation while prepared: no second snapshot commit on retry.
        var scopeFiles = new MemoryFiles(); var scope = Make(scopeFiles); scopeFiles.FailBefore = 2;
        Check(!scope.S.StartNew("orbit", 1, scope.S.IntentEpoch), "scope fixture failed save");
        scope.G.PrepareRun("crit", 2);
        Check(!scope.S.Retry(scope.S.IntentEpoch), "scope mismatch after committed save rejected");
        long committed = scope.Store.Recover().Control!.Sequence;
        Check(!scope.S.Retry(scope.S.IntentEpoch) && scope.Store.Recover().Control!.Sequence == committed,
            "initialize rejection retry never rewrites acknowledged snapshot");
        // A second writer's terminal control invalidates a previously displayed Resume intent.
        var raceFiles = new MemoryFiles(); var first = Make(raceFiles);
        Check(first.S.StartNew("honey", 3, first.S.IntentEpoch), "race start");
        var stale = Make(raceFiles);
        Check(first.S.Close(first.S.IntentEpoch), "race close");
        Check(!stale.S.Resume(stale.S.IntentEpoch) && stale.G.Phase == RunPhase.Selecting
            && stale.S.CurrentRecovery.Kind == RecoveryKind.Closed, "resume rechecks committed authority");
        string parent = Path.Combine(Path.GetTempPath(), "hmi-file-parent-" + Guid.NewGuid().ToString("N"));
        File.WriteAllText(parent, "not a directory");
        try
        {
            var badDirectory = Make(new PhysicalCheckpointFiles(Path.Combine(parent, "saves")));
            Check(badDirectory.S.CurrentRecovery.Kind == RecoveryKind.Uncertain, "actual nondirectory parent fails recover safely");
        }
        finally { File.Delete(parent); }
    }

    static void Failures()
    {
        var files = new MemoryFiles(); var x = Make(files);
        files.FailBefore = 1;
        Check(!x.S.StartNew("crit", 1, x.S.IntentEpoch), "PendingStart failure");
        Check(x.G.Phase == RunPhase.Selecting && !x.S.CanContinueUnsaved, "no begin without PendingStart");
        Check(x.S.Retry(x.S.IntentEpoch) && x.G.CharacterId == "crit", "retry pending start");
        // Lost acknowledgement after snapshot control commit: exact operation and payload retry.
        var lost = new MemoryFiles(); var y = Make(lost); lost.FailAfter = 3;
        Check(!y.S.StartNew("honey", 7, y.S.IntentEpoch) && y.G.Phase == RunPhase.Prepared, "lost save ack frozen");
        long sequence = y.Store.Recover().Control!.Sequence;
        Check(!y.S.ContinueUnsaved(y.S.IntentEpoch), "lost ack cannot guess unsaved");
        Check(y.S.Retry(y.S.IntentEpoch) && y.Store.Recover().Control!.Sequence == sequence && y.G.Tick == 0, "retry idempotent seq single begin");
        int run = y.G.RunToken;
        Check(!y.S.Retry(y.S.IntentEpoch - 1) && y.G.RunToken == run, "stale retry scope");
        lost.FailAfter = lost.Writes + 1;
        Check(!y.S.Close(y.S.IntentEpoch) && y.S.IsCloseFailure, "close lost acknowledgement");
        long closedSeq = y.Store.Recover().Control!.Sequence;
        Check(y.S.Retry(y.S.IntentEpoch) && y.Store.Recover().Control!.Sequence == closedSeq, "close retry same operation sequence");
        var pendingAck = new MemoryFiles(); var p = Make(pendingAck); pendingAck.FailAfter = 1;
        Check(!p.S.StartNew("summon", 42, p.S.IntentEpoch) && p.G.Phase == RunPhase.Selecting,
            "PendingStart lost ack does not begin core");
        Check(p.S.Retry(p.S.IntentEpoch) && p.Store.Recover().Control!.Sequence == 2 && p.G.Tick == 0,
            "PendingStart lost ack replay then save once");
        // Snapshot failure after durable pending start permits explicit unsaved run, not old run.
        var unsavedFiles = new MemoryFiles(); var z = Make(unsavedFiles); unsavedFiles.FailBefore = 2;
        Check(!z.S.StartNew("bounce", 8, z.S.IntentEpoch) && z.S.CanContinueUnsaved, "first snapshot failure");
        Check(z.S.ContinueUnsaved(z.S.IntentEpoch) && !z.S.CurrentWaveSaved && z.G.CharacterId == "bounce", "explicit unsaved");
        Check(Make(unsavedFiles).S.CurrentRecovery.Kind == RecoveryKind.PendingStart, "old snapshot not revived");
        // Terminal close failure cannot be bypassed with StartNew or resume.
        z.G.World.Health[z.G.PlayerId.Index].Current = 0; z.G.Step();
        Check(z.G.Phase == RunPhase.Defeat, "defeat fixture");
        unsavedFiles.FailBefore = unsavedFiles.Writes + 1;
        Check(!z.S.Close(z.S.IntentEpoch) && z.S.IsCloseFailure, "close failure exposed");
        Check(!z.S.StartNew("orbit", 3, z.S.IntentEpoch) && !z.S.Resume(z.S.IntentEpoch), "close failure gates actions");
        Check(z.S.Retry(z.S.IntentEpoch) && z.Store.Recover().Kind == RecoveryKind.Closed, "close retry durable");
        var bad = new MemoryFiles(); bad.Data["control.json"] = new byte[] { 1 }; var b = Make(bad);
        Check(b.S.CurrentRecovery.Kind == RecoveryKind.Uncertain && !b.S.StartNew("orbit", 1, b.S.IntentEpoch), "uncertain no reset");
    }
}
sealed class MemoryFiles : ICheckpointFiles
{
    public bool Deny;
    public Dictionary<string, byte[]> Data = new(); public int Writes, FailBefore = -1, FailAfter = -1;
    sealed class Lease : IDisposable { public void Dispose() { } }
    public IDisposable AcquireWriter() => Deny ? throw new UnauthorizedAccessException("injected directory denied") : new Lease();
    public string[] Names() => Data.Keys.ToArray();
    public long Length(string name) => Data[name].Length;
    public byte[] Read(string name, int maximumBytes) => Data.TryGetValue(name, out var bytes) ? bytes.ToArray() : throw new IOException("missing");
    public void Delete(string name) => Data.Remove(name);
    public void AtomicWrite(string name, byte[] bytes, bool overwrite)
    {
        Writes++;
        if (Writes == FailBefore) throw new IOException("injected before durable write");
        if (!overwrite && Data.ContainsKey(name)) throw new IOException("exists");
        Data[name] = bytes.ToArray();
        if (Writes == FailAfter) throw new IOException("injected lost acknowledgement");
    }
}
