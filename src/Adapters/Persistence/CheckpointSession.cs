using System.Security.Cryptography;
using Hmi.Core;

namespace Hmi.Adapters.Persistence;

/// <summary>Synchronous single-writer persistence orchestration; no gameplay or UI authority.</summary>
public sealed class CheckpointSession
{
    private enum Pending { None, BeginRun, Prepare, Save, Initialize, Close }
    private readonly GameSimulation simulation;
    private readonly CheckpointStore store;
    private readonly CheckpointCodec codec;
    private Pending pending;
    private string run = "", operation = "", hero = "";
    private uint seed;
    private long sequence;
    private byte[]? payload;
    private PreparedWaveToken prepared;
    private bool unsaved, startCommitted, firstSnapshot;

    public Recovery CurrentRecovery { get; private set; } = new(RecoveryKind.Empty, null, null, "Not scanned");
    public bool HasPendingFailure => pending != Pending.None;
    public bool CanContinueUnsaved => pending == Pending.Save && startCommitted && firstSnapshot && simulation.Phase == RunPhase.Prepared;
    public bool IsCloseFailure => pending == Pending.Close;
    public bool CurrentWaveSaved { get; private set; }
    public string LastError { get; private set; } = "";
    public long IntentEpoch { get; private set; } = 1;

    public CheckpointSession(GameSimulation simulation, CheckpointStore store, CheckpointCodec codec)
    {
        this.simulation = simulation;
        this.store = store;
        this.codec = codec;
        RefreshRecovery();
    }

    private bool Accept(long expected) => expected == IntentEpoch && IntentEpoch < long.MaxValue;
    private void Advance() { IntentEpoch++; LastError = ""; }
    public void RefreshRecovery()
    {
        if (HasPendingFailure) return;
        try { CurrentRecovery = store.Recover(); }
        catch (Exception ex) when (Expected(ex))
        { CurrentRecovery = new(RecoveryKind.Uncertain, null, null, ex.Message); }
        LastError = CurrentRecovery.Kind == RecoveryKind.Uncertain ? CurrentRecovery.Detail : "";
        if (IntentEpoch < long.MaxValue) IntentEpoch++;
    }

    public bool StartNew(string characterId, uint initialSeed, long expectedEpoch)
    {
        if (!Accept(expectedEpoch) || HasPendingFailure || CurrentRecovery.Kind == RecoveryKind.Uncertain) return false;
        Advance();
        hero = characterId;
        seed = initialSeed;
        run = Guid.NewGuid().ToString("N");
        sequence = CurrentRecovery.Control?.Sequence ?? 0;
        operation = Guid.NewGuid().ToString("N");
        payload = null;
        unsaved = false;
        startCommitted = false;
        firstSnapshot = true;
        CurrentWaveSaved = false;
        pending = Pending.BeginRun;
        return ExecutePending();
    }

    public bool PrepareNext(long expectedEpoch)
    {
        if (!Accept(expectedEpoch) || HasPendingFailure || run.Length == 0 || simulation.Phase != RunPhase.Between) return false;
        if (!simulation.PrepareNextWave()) return false;
        CurrentWaveSaved = false;
        Advance();
        if (unsaved) return simulation.BeginPreparedWave(simulation.PreparedToken);
        return CaptureAndSave();
    }

    private bool CaptureAndSave()
    {
        pending = Pending.Save;
        operation = Guid.NewGuid().ToString("N");
        prepared = simulation.PreparedToken;
        try
        {
            if (!simulation.TryCapturePrepared(new CheckpointIdentity(Guid.ParseExact(run, "N"), checked(sequence + 1)), out var dto, out var error))
                throw new InvalidDataException("Capture rejected: " + error);
            payload = codec.Encode(dto!);
            return ExecutePending();
        }
        catch (Exception ex) when (Expected(ex)) { LastError = ex.Message; return false; }
    }

    public bool Retry(long expectedEpoch)
    {
        if (!Accept(expectedEpoch)) return false;
        Advance();
        if (!HasPendingFailure) { RefreshRecovery(); return CurrentRecovery.Kind != RecoveryKind.Uncertain; }
        return ExecutePending();
    }

    private bool ExecutePending()
    {
        try
        {
            if (pending == Pending.BeginRun)
            {
                sequence = store.BeginRun(run, operation, sequence).Sequence;
                startCommitted = true;
                pending = Pending.Prepare;
            }
            if (pending == Pending.Prepare)
            {
                simulation.PrepareRun(hero, seed);
                return CaptureAndSave();
            }
            if (pending == Pending.Save)
            {
                if (payload == null) throw new InvalidDataException("No valid prepared payload; cannot retry write");
                sequence = store.Save(run, operation, sequence, payload).Sequence;
                pending = Pending.Initialize;
            }
            if (pending == Pending.Initialize)
            {
                if (!simulation.BeginPreparedWave(prepared)) throw new InvalidOperationException("Prepared scope changed before durable acknowledgement");
                pending = Pending.None;
                firstSnapshot = false;
                CurrentWaveSaved = true;
                payload = null;
                RefreshRecovery();
                return true;
            }
            if (pending == Pending.Close)
            {
                sequence = store.Close(run, operation, sequence).Sequence;
                run = "";
                CurrentWaveSaved = false;
                pending = Pending.None;
                RefreshRecovery();
                return true;
            }
            return false;
        }
        catch (Exception ex) when (Expected(ex)) { LastError = ex.Message; return false; }
    }

    public bool Resume(long expectedEpoch)
    {
        if (!Accept(expectedEpoch) || HasPendingFailure || CurrentRecovery.Payload == null
            || CurrentRecovery.Kind is not (RecoveryKind.Active or RecoveryKind.RolledBack)) return false;
        var offered = CurrentRecovery.Control!;
        Recovery latest;
        try { latest = store.Recover(); }
        catch (Exception ex) when (Expected(ex)) { LastError = ex.Message; return false; }
        if (latest.Control?.Run != offered.Run || latest.Control.Sequence != offered.Sequence
            || latest.Payload == null || latest.Kind is not (RecoveryKind.Active or RecoveryKind.RolledBack))
        {
            CurrentRecovery = latest;
            Advance();
            LastError = "Recovery authority changed; refresh the selection";
            return false;
        }
        CurrentRecovery = latest;
        var control = latest.Control;
        var bytes = latest.Payload;
        string hash = Convert.ToHexString(SHA256.HashData(bytes));
        var descriptor = control.Snapshots.FirstOrDefault(s => s.Hash == hash && s.Length == bytes.Length);
        if (descriptor == null) return false;
        var identity = new SnapshotIdentity(descriptor.Run, descriptor.Sequence, control.Catalog, control.Rules);
        if (!codec.TryDecode(bytes, identity, out var dto)) return false;
        if (!simulation.TryRestorePrepared(dto!, new CheckpointIdentity(Guid.ParseExact(descriptor.Run, "N"), descriptor.Sequence), out var error))
        { LastError = "Restore rejected: " + error; return false; }
        Advance();
        run = control.Run;
        sequence = control.Sequence;
        startCommitted = true;
        firstSnapshot = false;
        CurrentWaveSaved = true;
        unsaved = false;
        return simulation.BeginPreparedWave(simulation.PreparedToken);
    }

    public bool ContinueUnsaved(long expectedEpoch)
    {
        if (!Accept(expectedEpoch) || !CanContinueUnsaved) return false;
        Recovery observed;
        try { observed = store.Recover(); }
        catch (Exception ex) when (Expected(ex)) { LastError = ex.Message; return false; }
        // An uncertain/lost acknowledgement must be retried, never guessed unsaved.
        if (observed.Kind != RecoveryKind.PendingStart || observed.Control?.Run != run
            || observed.Control.Sequence != sequence) return false;
        Advance();
        if (!simulation.BeginPreparedWave(prepared)) return false;
        pending = Pending.None;
        payload = null;
        unsaved = true;
        CurrentWaveSaved = false;
        RefreshRecovery();
        return true;
    }

    public bool Close(long expectedEpoch)
    {
        if (!Accept(expectedEpoch) || HasPendingFailure || run.Length == 0
            || simulation.Phase is not (RunPhase.Defeat or RunPhase.Finished or RunPhase.Combat or RunPhase.Between or RunPhase.Victory)) return false;
        Advance();
        operation = Guid.NewGuid().ToString("N");
        pending = Pending.Close;
        return ExecutePending();
    }

    public bool ExitSelection(long expectedEpoch)
    {
        if (!Accept(expectedEpoch) || HasPendingFailure) return false;
        Advance();
        RefreshRecovery();
        return true;
    }

    private static bool Expected(Exception ex) => ex is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException
        or InvalidOperationException or OverflowException or System.Text.Json.JsonException;
}
