using Hmi.Core;

namespace Hmi.Adapters.Persistence;

public sealed partial class RuleMigrationCoordinator
{
    /// <summary>Explicit consent to preserve the full valid old Active/RolledBack group and
    /// choose an independent new-rule NewGame slot. No gameplay checkpoint/hero/seed or Begin
    /// is created. Cold retry must return to character selection, not guess a previous choice.</summary>
    public MigrationInspection PreserveLegacyAndInitializeNewGame(string operation,string newRun,string expectedFingerprint)
        =>PreserveLegacyNewGameCore(operation,newRun,expectedFingerprint,null);
    private MigrationInspection PreserveLegacyNewGameCore(string operation,string newRun,string expectedFingerprint,string? requiredIntentDigest)
    {
        if(!Uuid(operation)||!Uuid(newRun)||!Digest(expectedFingerprint))throw new ArgumentException("Invalid preserve-old NewGame identity");
        using var registry=new LockedPort(files.Registry);
        var names=Inventory();ValidatePendingReceiptDigest(requiredIntentDigest,names);
        if(names.Contains(SelectorName))
        {
            var selected=ReadSelector();
            if(selected.Kind!=SlotInitializationKind.PreserveLegacyNewGame||selected.Operation!=operation||selected.Run!=newRun||selected.SourceFingerprint!=expectedFingerprint)
                throw new InvalidOperationException("Selected authority rejects different preserve-old operation; use normal selected-slot Session");
            return Selected(selected); // Acknowledgement loss never re-reads changed legacy or revives it.
        }
        MigrationIntent? prior=names.Contains(IntentName)?ReadIntent():null;
        if(prior!=null&&(prior.Kind!=SlotInitializationKind.PreserveLegacyNewGame||prior.Operation!=operation||prior.Run!=newRun||prior.SourceFingerprint!=expectedFingerprint))
            throw new InvalidOperationException("Only the original explicitly confirmed preserve-old operation may resume");
        if(prior==null&&(names.Any(n=>n!="writer.lock")||files.Slots().Length!=0))throw new InvalidDataException("Unattributed preserve-old artifacts");
        if(files.Slots().Any(s=>s!=newRun))throw new InvalidDataException("Unexpected preserve-old slot identity");
        // Slot creation is ordered after durable backup. Its presence proves this is
        // not the legitimate intent-only crash stage; never reconstruct a deleted backup.
        if(!names.Contains(BackupName)&&files.Slots().Length!=0)throw new InvalidDataException("Preserve-old staging exists without its previously durable backup");
        using var oldWriter=new LockedPort(files.Legacy);
        var legacy=OldStore(oldWriter).Recover();
        if(legacy.Kind is not (RecoveryKind.Active or RecoveryKind.RolledBack))throw new InvalidDataException("Preserve-old NewGame requires a valid resumable legacy run");
        var dto=DecodeRecovered(legacy,true);
        if(dto.RunId==Guid.ParseExact(newRun,"N"))throw new InvalidDataException("Preserve-old NewGame requires a distinct UUID");
        var capture=CaptureLegacy();
        if(capture.Fingerprint!=expectedFingerprint)throw new InvalidOperationException("Legacy changed after preserve-old consent");
        var intent=new MigrationIntent(3,SlotInitializationKind.PreserveLegacyNewGame,operation,newRun,newRun,expectedFingerprint,Hash(capture.Backup),"",catalog,WaveRules.Version);
        byte[] intentBytes=Encode(intent);
        var selector=new MigrationSelector(3,intent.Kind,operation,newRun,newRun,expectedFingerprint,Hash(intentBytes),catalog,WaveRules.Version);
        byte[] selectorBytes=Encode(selector);
        // Preflight remaining registry bytes before the first write, never delete backups to fit.
        long used=files.Registry.Names().Sum(n=>files.Registry.Length(n));
        long needed=(prior==null?intentBytes.Length:0)+(names.Contains(BackupName)?0:capture.Backup.Length)+selectorBytes.Length;
        if(used+needed>RegistryLimit)throw new IOException("Insufficient preserve-old registry quota");
        if(prior==null)Write(IntentName,intentBytes,CheckpointStore.ControlLimit);
        else if(prior!=intent)throw new InvalidDataException("Preserve-old retry differs from immutable intent");
        if(!names.Contains(BackupName))Write(BackupName,capture.Backup,BackupLimit);
        VerifyBackup(intent);
        var port=files.Slot(newRun);using var slotWriter=new LockedPort(port);
        ValidatePendingSlotCapacity(slotWriter);
        var store=NewStore(slotWriter);var staged=store.Recover();
        if(staged.Kind==RecoveryKind.Empty)store.BeginRun(newRun,operation,0);
        staged=store.Recover();
        if(staged.Kind!=RecoveryKind.PendingStart||staged.Control!.Run!=newRun||staged.Control.Sequence!=1||staged.Control.Operation!=operation)
            throw new InvalidDataException("Preserve-old staging is not exact unused PendingStart");
        if(CaptureLegacy().Fingerprint!=expectedFingerprint)throw new InvalidOperationException("Legacy changed before preserve-old selector commit");
        Write(SelectorName,selectorBytes,CheckpointStore.ControlLimit);
        var committed=store.Recover();
        if(committed.Kind!=RecoveryKind.PendingStart||committed.Control!.Run!=newRun||committed.Control.Sequence!=1||committed.Control.Operation!=operation)
            throw new InvalidDataException("Preserve-old selection lost pending authority");
        return new(MigrationKind.Selected,expectedFingerprint,newRun,committed,null,"Old run backed up and preserved; independent rule2 PendingStart selected. Explicit new character choice required before Begin.");
    }
}
