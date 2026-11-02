using Hmi.Core;

namespace Hmi.Adapters.Persistence;

public enum PendingConsentKind { NotPending, Retryable, Selected, Blocked }
public enum PendingConsentStage { BeforeSlot, PendingCheckpoint, ReadyToSelect }
public sealed record PendingConsentSummary(string? CharacterId,int? Wave,int? Coins,RecoveryKind LegacyStatus);
public sealed record PendingConsentInspection(PendingConsentKind Kind,PendingConsent? Consent,MigrationInspection? Selected,string Detail);

/// <summary>Process-local display and retry receipt, not serialized authority or a playable checkpoint.</summary>
public sealed class PendingConsent
{
    public SlotInitializationKind Kind { get; }
    public PendingConsentStage Stage { get; }
    public PendingConsentSummary Summary { get; }
    internal RuleMigrationCoordinator Issuer { get; }
    internal string Operation { get; }
    internal string NewRun { get; }
    internal string Fingerprint { get; }
    internal string IntentDigest { get; }
    internal PendingConsent(RuleMigrationCoordinator issuer,MigrationIntent intent,string digest,PendingConsentStage stage,PendingConsentSummary summary)
    {
        Issuer=issuer;Kind=intent.Kind;Operation=intent.Operation;NewRun=intent.Run;
        Fingerprint=intent.SourceFingerprint;IntentDigest=digest;Stage=stage;Summary=summary;
    }
}

public sealed partial class RuleMigrationCoordinator
{
    /// <summary>Strict read-only recovery of a persisted initialization intent. Never repairs files,
    /// allocates a new operation, creates an absent slot, or begins gameplay. Writer-lock metadata
    /// uses the existing port convention; no checkpoint/backup/selector writes occur.</summary>
    public PendingConsentInspection ReadPendingConsent()
    {
        try
        {
            using var registry=new LockedPort(files.Registry);
            string[] names=Inventory();
            if(names.Contains(SelectorName))
                return new(PendingConsentKind.Selected,null,Selected(ReadSelector()),"An existing selector remains authoritative; use its current rule2 recovery");
            string[] slots=files.Slots();
            if(names.All(n=>n=="writer.lock")&&slots.Length==0)
                return new(PendingConsentKind.NotPending,null,null,"No persisted initialization intent; fresh explicit consent is required");
            if(!names.Contains(IntentName))throw new InvalidDataException("Artifacts without a validated intent cannot recover an operation");
            MigrationIntent intent=ReadIntent();
            byte[] intentBytes=files.Registry.Read(IntentName,CheckpointStore.ControlLimit);
            if(slots.Any(s=>s!=intent.Slot))throw new InvalidDataException("Unexpected staging slot identity");
            if(intent.Kind==SlotInitializationKind.PreserveLegacyNewGame&&!names.Contains(BackupName)&&slots.Length!=0)
                throw new InvalidDataException("Preserve-old staging proves backup should already be durable; missing backup is not an early crash");
            using var oldWriter=new LockedPort(files.Legacy);
            Recovery legacy=OldStore(oldWriter).Recover();
            WaveCheckpointDto? source=null;
            if(intent.Kind is SlotInitializationKind.Migration or SlotInitializationKind.PreserveLegacyNewGame)
            {
                if(legacy.Kind is not (RecoveryKind.Active or RecoveryKind.RolledBack))throw new InvalidDataException("Pending migration no longer has a valid legacy safe point");
                source=DecodeRecovered(legacy,true);
            }
            else
            {
                if(legacy.Kind is not (RecoveryKind.Empty or RecoveryKind.Closed or RecoveryKind.PendingStart))throw new InvalidDataException("Pending NewGame cannot bypass Active or corrupt legacy");
                if(legacy.Control?.Run==intent.Run)throw new InvalidDataException("Pending NewGame reused legacy UUID");
                if(names.Contains(BackupName))throw new InvalidDataException("NewGame cannot recover a migration backup");
            }
            var capture=CaptureLegacy();
            if(capture.Fingerprint!=intent.SourceFingerprint)throw new InvalidOperationException("Legacy changed since recorded consent; no new fingerprint is substituted");
            int missingBackupBytes=0;
            if(intent.Kind is SlotInitializationKind.Migration or SlotInitializationKind.PreserveLegacyNewGame)
            {
                if(source!.RunId==Guid.ParseExact(intent.Run,"N"))throw new InvalidDataException("Recorded initialization reused legacy UUID");
                if(intent.Kind==SlotInitializationKind.Migration)
                {
                    if(!simulation.TryConvertLegacyPreparedCheckpoint(source,new(source.RunId,source.Sequence),new(Guid.ParseExact(intent.Run,"N"),2),out var converted,out var error)||converted==null)
                        throw new InvalidDataException("Recorded migration conversion rejected: "+error);
                    if(Hash(codec.Encode(converted))!=intent.ConvertedHash)throw new InvalidDataException("Recorded conversion no longer matches");
                }
                if(Hash(capture.Backup)!=intent.BackupHash)throw new InvalidDataException("Recorded backup no longer matches");
                if(names.Contains(BackupName))VerifyBackup(intent);
                else missingBackupBytes=capture.Backup.Length; // Exact unchanged old group can reproduce the missing early backup only on explicit Retry.
            }
            ValidatePendingRegistryCapacity(intent,intentBytes,missingBackupBytes);
            var summary=new PendingConsentSummary(source?.CharacterId,source?.Wave,source?.Coins,legacy.Kind);
            PendingConsentStage stage=PendingConsentStage.BeforeSlot;
            if(slots.Length!=0)
            {
                // Do not ask the factory for a nonexistent slot: AcquireWriter creates directories.
                var port=files.Slot(intent.Slot);using var slotWriter=new LockedPort(port);
                ValidatePendingSlotCapacity(slotWriter);
                Recovery staged=NewStore(slotWriter).Recover();
                if(staged.Kind==RecoveryKind.Empty)stage=PendingConsentStage.BeforeSlot;
                else if(staged.Kind==RecoveryKind.PendingStart&&staged.Control!.Run==intent.Run&&staged.Control.Sequence==1&&staged.Control.Operation==intent.Operation)
                    stage=intent.Kind==SlotInitializationKind.Migration?PendingConsentStage.PendingCheckpoint:PendingConsentStage.ReadyToSelect;
                else if(intent.Kind==SlotInitializationKind.Migration&&staged.Kind==RecoveryKind.Active&&staged.Control!.Run==intent.Run&&staged.Control.Sequence==2&&staged.Control.Operation==SaveOperation(intent.Operation)&&staged.Payload!=null&&Hash(staged.Payload)==intent.ConvertedHash)
                { DecodeRecovered(staged,false);stage=PendingConsentStage.ReadyToSelect; }
                else throw new InvalidDataException("Staging is used, closed, advanced or damaged; no pending consent can revive it");
            }
            return new(PendingConsentKind.Retryable,new PendingConsent(this,intent,Hash(intentBytes),stage,summary),null,
                "Validated unfinished initialization only; explicit user Retry must recheck storage. No gameplay has begun.");
        }
        catch(Exception e)when(Expected(e)){return new(PendingConsentKind.Blocked,null,null,e.Message);}
    }
    /// <summary>Explicit UI retry only. The receipt is issuer-bound, not a serializable capability;
    /// both existing transactions reacquire locks and revalidate all source/staging identities.</summary>
    public MigrationInspection RetryPending(PendingConsent consent)
    {
        if(consent==null||!ReferenceEquals(consent.Issuer,this))throw new ArgumentException("Read a fresh consent from this coordinator instance");
        return consent.Kind switch
        {
            SlotInitializationKind.Migration=>ConfirmCore(consent.Operation,consent.NewRun,consent.Fingerprint,consent.IntentDigest),
            SlotInitializationKind.NewGame=>InitializeNewGameCore(consent.Operation,consent.NewRun,consent.Fingerprint,consent.IntentDigest),
            SlotInitializationKind.PreserveLegacyNewGame=>PreserveLegacyNewGameCore(consent.Operation,consent.NewRun,consent.Fingerprint,consent.IntentDigest),
            _=>throw new InvalidDataException("Unknown pending consent kind")
        };
    }
    private void ValidatePendingReceiptDigest(string? expected,string[] names)
    {
        if(expected==null)return; // Existing explicit fresh-consent APIs retain their original transaction behavior.
        if(!names.Contains(IntentName)||Hash(files.Registry.Read(IntentName,CheckpointStore.ControlLimit))!=expected)
            throw new InvalidOperationException("Recorded intent changed or disappeared after pending-consent inspection");
    }
    private void ValidatePendingRegistryCapacity(MigrationIntent intent,byte[] intentBytes,int missingBackupBytes)
    {
        long used=files.Registry.Names().Sum(n=>files.Registry.Length(n));
        var selector=new MigrationSelector(intent.Schema,intent.Kind,intent.Operation,intent.Run,intent.Slot,intent.SourceFingerprint,Hash(intentBytes),catalog,WaveRules.Version);
        if(used+missingBackupBytes+Encode(selector).Length>RegistryLimit)throw new IOException("Insufficient registry quota to complete the recorded operation");
    }
    private static void ValidatePendingSlotCapacity(ICheckpointFiles slot)
    {
        string[] names=slot.Names();
        if(names.Length>1024)throw new IOException("Staging slot entry quota exceeded");
        long total=0;
        foreach(string name in names)
        {
            if(name!="writer.lock"&&name!="control.json"&&!System.Text.RegularExpressions.Regex.IsMatch(name,@"^(snapshot-[0-9a-f]{32}-[1-9][0-9]*-[0-9a-f]{32}\.json|temp-[0-9a-f]{32})$"))throw new InvalidDataException("Unknown staging file");
            long length=slot.Length(name);
            if(length<0||length>CheckpointStore.StoreLimit||(total+=length)>CheckpointStore.StoreLimit)throw new IOException("Staging slot quota exceeded");
        }
    }
}
