using Hmi.Core;
using System.Text.Json;

namespace Hmi.Adapters.Persistence;

public enum RuleUpgradeKind { CurrentSelected, PriorAvailable, PriorNotResumable, LegacyPreserveOnly, Empty, Incomplete, Blocked }
public sealed record RuleUpgradeInspection(RuleUpgradeKind Kind, string Fingerprint, string? Slot,
    Recovery? Recovery, WaveCheckpointDto? Checkpoint, RuleUpgradeReceipt? Pending, string Detail);
public sealed class RuleUpgradeReceipt
{
    public bool StartNew { get; }
    internal RuleMigrationCoordinator Issuer { get; }
    internal UpgradeIntent Intent { get; }
    internal string IntentHash { get; }
    internal RuleUpgradeReceipt(RuleMigrationCoordinator issuer, UpgradeIntent intent, string hash)
    { Issuer=issuer; Intent=intent; IntentHash=hash; StartNew=intent.StartNew; }
}
internal sealed record UpgradeIntent(int Schema, string Operation, string Run, string SourceSlot, string SourceRules,
    string SourceFingerprint, string BackupHash, string PayloadHash, string Catalog, string Rules, bool StartNew);
internal sealed record UpgradeSelector(int Schema, string Operation, string Run, string Slot, string SourceSlot,
    string IntentHash, string Catalog, string Rules);
internal sealed record UpgradeFile(string Scope, string Name, string Hash, byte[] Bytes);
internal sealed record UpgradeBackup(int Schema, string SourceSlot, string SourceRules, UpgradeFile[] Files);

public sealed partial class RuleMigrationCoordinator
{
    private const string UpgradeIntentName="upgrade-intent.json", UpgradeBackupName="upgrade-backup.json";
    private sealed record UpgradeSource(string Slot, string Rules, Recovery Recovery, WaveCheckpointDto? Checkpoint,
        string Fingerprint, byte[] Backup);

    private static T UpgradeDecode<T>(byte[] raw)
    {
        RejectDuplicateProperties(raw);
        RequireProtocolShape(raw,typeof(Envelope));
        var envelope=JsonSerializer.Deserialize<Envelope>(raw,Json)??throw new InvalidDataException("Missing envelope");
        byte[] body=Convert.FromBase64String(envelope.Payload);
        if(Hash(body)!=envelope.Hash)throw new InvalidDataException("Upgrade checksum mismatch");
        RejectDuplicateProperties(body);
        using var doc=JsonDocument.Parse(body);
        Exact(doc.RootElement,typeof(T));
        if(typeof(T)==typeof(UpgradeBackup))
        {
            var entries=doc.RootElement.GetProperty("Files");
            if(entries.ValueKind!=JsonValueKind.Array||entries.GetArrayLength()>EntryLimit)throw new InvalidDataException("Upgrade backup entries");
            foreach(var entry in entries.EnumerateArray())Exact(entry,typeof(UpgradeFile));
        }
        return JsonSerializer.Deserialize<T>(body,Json)??throw new InvalidDataException("Missing upgrade data");
        static void Exact(JsonElement root,Type type)
        {
            if(root.ValueKind!=JsonValueKind.Object)throw new InvalidDataException("Upgrade object expected");
            var required=type.GetProperties().Select(p=>p.Name).ToHashSet(StringComparer.Ordinal);
            var actual=root.EnumerateObject().Select(p=>p.Name).ToArray();
            if(actual.Length!=required.Count||!required.SetEquals(actual))throw new InvalidDataException("Upgrade fields mismatch");
        }
    }
    private string[] UpgradeInventory()
    {
        string[] names=files.Registry.Names();
        if(names.Length>EntryLimit)throw new IOException("Registry entry quota");
        long bytes=0;
        foreach(string name in names)
        {
            if(name!="writer.lock"&&name!=IntentName&&name!=BackupName&&name!=SelectorName
                &&name!=UpgradeIntentName&&name!=UpgradeBackupName)
                throw new InvalidDataException("Unknown registry artifact");
            long size=files.Registry.Length(name);
            if(size<0||size>BackupLimit||(bytes+=size)>RegistryLimit)throw new IOException("Registry byte quota");
        }
        var slots=files.Slots();
        if(slots.Length>2||slots.Any(s=>!Uuid(s))||slots.Distinct(StringComparer.Ordinal).Count()!=slots.Length)
            throw new InvalidDataException("Invalid slot inventory");
        return names;
    }
    private UpgradeIntent ReadUpgradeIntent()
    {
        var i=UpgradeDecode<UpgradeIntent>(files.Registry.Read(UpgradeIntentName,CheckpointStore.ControlLimit));
        if(i.Schema!=4||!Uuid(i.Operation)||!Uuid(i.Run)||i.SourceSlot!=""&&!Uuid(i.SourceSlot)
            ||i.Run==i.SourceSlot||i.SourceRules!=(i.SourceSlot==""?WaveRules.LegacyVersion:WaveRules.PriorVersion)
            ||!Digest(i.SourceFingerprint)||i.BackupHash!=i.SourceFingerprint||i.Catalog!=catalog||i.Rules!=WaveRules.Version
            ||(i.StartNew?i.PayloadHash!="":!Digest(i.PayloadHash))||(!i.StartNew&&i.SourceSlot==""))
            throw new InvalidDataException("Invalid upgrade intent");
        return i;
    }
    private static void ExactSlots(string[] actual,string source,string target,bool targetRequired)
    {
        var allowed=new HashSet<string>(StringComparer.Ordinal){target};
        if(source!="")allowed.Add(source);
        if(actual.Any(s=>!allowed.Contains(s))||(source!=""&&!actual.Contains(source))||(targetRequired&&!actual.Contains(target)))
            throw new InvalidDataException("Upgrade slots missing or ambiguous");
    }
    private Recovery RecoverArchived(LockedPort port,string rules)
        =>new CheckpointStore(port,catalog,rules,(b,id)=>codec.TryDecodeArchived(b,id,out _)).Recover();
    private WaveCheckpointDto? DecodeArchivedRecovery(Recovery recovery,string rules)
    {
        if(recovery.Kind is not (RecoveryKind.Active or RecoveryKind.RolledBack))return null;
        if(recovery.Control==null||recovery.Payload==null)throw new InvalidDataException("Missing archived checkpoint");
        var descriptor=recovery.Control.Snapshots.FirstOrDefault(d=>d.Hash==Hash(recovery.Payload)&&d.Length==recovery.Payload.Length);
        if(descriptor==null||!codec.TryDecodeArchived(recovery.Payload,new(descriptor.Run,descriptor.Sequence,catalog,rules),out var dto))
            throw new InvalidDataException("Archived descriptor mismatch");
        return dto;
    }
    // Caller holds registry and source writer. No absent source slot is opened.
    private UpgradeSource CaptureUpgradeSource(string sourceSlot,LockedPort source)
    {
        var entries=new List<UpgradeFile>();
        string rules=sourceSlot==""?WaveRules.LegacyVersion:WaveRules.PriorVersion;
        if(sourceSlot!="")
        {
            var selected=ReadSelector(WaveRules.PriorVersion,true);
            if(selected.Slot!=sourceSlot)throw new InvalidDataException("Prior selector changed");
            foreach(string name in new[]{IntentName,BackupName,SelectorName})
                if(files.Registry.Names().Contains(name))Add("registry",name,files.Registry);
        }
        else if(files.Registry.Names().Any(n=>n==IntentName||n==BackupName||n==SelectorName))
            throw new InvalidDataException("Unselected legacy initialization remains incomplete; no chain upgrade");
        ValidatePendingSlotCapacity(source);
        if(source.Names().Any(n=>n.StartsWith("temp-",StringComparison.Ordinal)))
            throw new InvalidDataException("Unattributed source temporary artifact; preserved without repair");
        var recovery=RecoverArchived(source,rules);
        if(recovery.Kind==RecoveryKind.Uncertain)throw new InvalidDataException(recovery.Detail);
        if(sourceSlot!=""&&recovery.Kind==RecoveryKind.Empty)throw new InvalidDataException("Selected prior control missing");
        if(recovery.Kind is RecoveryKind.Active or RecoveryKind.RolledBack)
        {
            var referenced=recovery.Control!.Snapshots.Select(d=>d.Name).ToHashSet(StringComparer.Ordinal);
            if(source.Names().Any(n=>n.StartsWith("snapshot-",StringComparison.Ordinal)&&!referenced.Contains(n)))
                throw new InvalidDataException("Unreferenced Active source snapshot; preserved without authority repair");
        }
        else
        {
            // Nonresumable authority can preserve only independently validated same-run
            // archival bytes. A prior run left by BeginRun has no provenance link here.
            foreach(string name in source.Names().Where(n=>n.StartsWith("snapshot-",StringComparison.Ordinal)))
                ValidateNonresumableSnapshotOwnership(source,name,recovery,rules);
        }
        var dto=DecodeArchivedRecovery(recovery,rules);
        foreach(string name in source.Names().Where(n=>n=="control.json"||n.StartsWith("snapshot-",StringComparison.Ordinal)).OrderBy(n=>n,StringComparer.Ordinal))
            Add("source",name,source);
        var backup=Encode(new UpgradeBackup(4,sourceSlot,rules,entries.ToArray()));
        if(backup.Length>BackupLimit)throw new IOException("Upgrade encoded backup quota; originals retained");
        return new(sourceSlot,rules,recovery,dto,Hash(backup),backup);
        void Add(string scope,string name,ICheckpointFiles port)
        {
            if(entries.Count>=EntryLimit)throw new IOException("Upgrade backup entry quota");
            byte[] data=port.Read(name,BackupLimit);
            entries.Add(new(scope,name,Hash(data),data));
        }
    }
    private void ValidateNonresumableSnapshotOwnership(ICheckpointFiles source,string name,Recovery recovery,string rules)
    {
        var control=recovery.Control;
        var match=System.Text.RegularExpressions.Regex.Match(name,@"^snapshot-([0-9a-f]{32})-([1-9][0-9]*)-[0-9a-f]{32}\.json$");
        if(control==null||!match.Success||match.Groups[1].Value!=control.Run
            ||!long.TryParse(match.Groups[2].Value,out long sequence))
            throw new InvalidDataException("Unattributed nonresumable source snapshot");
        bool owned=recovery.Kind==RecoveryKind.Closed?sequence<control.Sequence
            :recovery.Kind==RecoveryKind.PendingStart&&control.Sequence<long.MaxValue&&sequence==control.Sequence+1;
        if(!owned||!codec.TryDecodeArchived(source.Read(name,CheckpointStore.SnapshotLimit),
            new(control.Run,sequence,catalog,rules),out _))
            throw new InvalidDataException("Nonresumable snapshot ownership validation failed; no payload is resumed");
    }
    private string SourceSlotBeforeUpgrade(string[] names)
    {
        if(!names.Contains(SelectorName))return "";
        var selector=UpgradeDecode<MigrationSelector>(files.Registry.Read(SelectorName,CheckpointStore.ControlLimit));
        if(selector.Rules!=WaveRules.PriorVersion)throw new InvalidDataException("Unsupported selected rules");
        return ReadSelector(WaveRules.PriorVersion,true).Slot;
    }
    private ICheckpointFiles SourcePort(string slot)
    {
        if(slot=="")return files.Legacy;
        if(!files.Slots().Contains(slot))throw new InvalidDataException("Source slot missing");
        return files.Slot(slot);
    }
    private byte[] ConvertUpgrade(UpgradeSource source,UpgradeIntent intent)
    {
        if(intent.Run==source.Recovery.Control?.Run)throw new InvalidDataException("Target reuses source run UUID");
        if(intent.StartNew)return Array.Empty<byte>();
        var dto=source.Checkpoint;
        if(dto==null||!simulation.TryConvertPriorPreparedCheckpoint(dto,new(dto.RunId,dto.Sequence),
            new(Guid.ParseExact(intent.Run,"N"),2),out var converted,out var error)||converted==null)
            throw new InvalidDataException("Prior safe point cannot convert");
        byte[] payload=codec.Encode(converted);
        if(payload.Length==0||payload.Length>CheckpointStore.SnapshotLimit)throw new IOException("Converted snapshot exceeds unchanged quota");
        return payload;
    }
    private UpgradeBackup VerifyUpgradeBackup(UpgradeIntent intent)
    {
        byte[] raw=files.Registry.Read(UpgradeBackupName,BackupLimit);
        if(Hash(raw)!=intent.BackupHash)throw new InvalidDataException("Upgrade backup hash mismatch");
        var b=UpgradeDecode<UpgradeBackup>(raw);
        if(b.Schema!=4||b.SourceSlot!=intent.SourceSlot||b.SourceRules!=intent.SourceRules||b.Files==null)
            throw new InvalidDataException("Upgrade backup provenance");
        var keys=new HashSet<string>(StringComparer.Ordinal);
        foreach(var f in b.Files)
        {
            if(f==null||f.Bytes==null||!keys.Add(f.Scope+":"+f.Name)||Hash(f.Bytes)!=f.Hash)
                throw new InvalidDataException("Invalid upgrade backup entry");
            bool valid=f.Scope=="registry"?(intent.SourceSlot!=""&&f.Name is IntentName or BackupName or SelectorName)
                :f.Scope=="source"&&(f.Name=="control.json"||System.Text.RegularExpressions.Regex.IsMatch(f.Name,@"^snapshot-[0-9a-f]{32}-[1-9][0-9]*-[0-9a-f]{32}\.json$"));
            if(!valid)throw new InvalidDataException("Invalid upgrade backup scope/name");
        }
        // Revalidate archived metadata and control using detached, strictly read-only bytes.
        var captured=new CapturedUpgradeFiles(b);
        var reader=new RuleMigrationCoordinator(captured,simulation);
        using var old=new LockedPort(intent.SourceSlot==""?captured.Legacy:captured.Slot(intent.SourceSlot));
        var reconstructed=reader.CaptureUpgradeSource(intent.SourceSlot,old);
        if(reconstructed.Fingerprint!=intent.SourceFingerprint)
            throw new InvalidDataException("Upgrade archived provenance changed");
        byte[] payload=ConvertUpgrade(reconstructed,intent);
        if(!intent.StartNew&&Hash(payload)!=intent.PayloadHash)throw new InvalidDataException("Upgrade conversion hash mismatch");
        return b;
    }
    private sealed class CapturedUpgradeFiles : IRuleMigrationFiles
    {
        private readonly UpgradeBackup backup;
        internal CapturedUpgradeFiles(UpgradeBackup backup){this.backup=backup;}
        public ICheckpointFiles Registry=>new CapturedPort(backup.Files.Where(f=>f.Scope=="registry"));
        public ICheckpointFiles Legacy=>new CapturedPort(backup.Files.Where(f=>f.Scope=="source"));
        public string[] Slots()=>backup.SourceSlot==""?Array.Empty<string>():new[]{backup.SourceSlot};
        public ICheckpointFiles Slot(string id)=>id==backup.SourceSlot?Legacy:throw new InvalidDataException("Unknown archived slot");
        private sealed class CapturedPort : ICheckpointFiles
        {
            private readonly Dictionary<string,byte[]> data;
            internal CapturedPort(IEnumerable<UpgradeFile> files){data=files.ToDictionary(f=>f.Name,f=>f.Bytes,StringComparer.Ordinal);}
            private sealed class Lease : IDisposable {public void Dispose(){}}
            public IDisposable AcquireWriter()=>new Lease();
            public string[] Names()=>data.Keys.ToArray();
            public long Length(string n)=>data.TryGetValue(n,out var b)?b.Length:throw new IOException("Missing archived file");
            public byte[] Read(string n,int max)=>data.TryGetValue(n,out var b)&&b.Length<=max?b.ToArray():throw new IOException("Missing/oversize archived file");
            public void AtomicWrite(string n,byte[] b,bool overwrite)=>throw new InvalidOperationException("Archive is read-only");
            public void Delete(string n)=>throw new InvalidOperationException("Archive is read-only");
        }
    }
    private RuleUpgradeInspection SelectedUpgrade()
    {
        var s=UpgradeDecode<UpgradeSelector>(files.Registry.Read(SelectorName,CheckpointStore.ControlLimit));
        var i=ReadUpgradeIntent();
        if(s.Schema!=4||s.Rules!=WaveRules.Version||s.Catalog!=catalog||s.Operation!=i.Operation||s.Run!=i.Run||s.Slot!=i.Run
            ||s.SourceSlot!=i.SourceSlot||s.IntentHash!=Hash(files.Registry.Read(UpgradeIntentName,CheckpointStore.ControlLimit)))
            throw new InvalidDataException("Upgrade selector provenance mismatch");
        ExactSlots(files.Slots(),i.SourceSlot,i.Run,true);
        VerifyUpgradeBackup(i);
        using var target=new LockedPort(files.Slot(i.Run));
        var recovered=NewStore(target).Recover();
        if(recovered.Kind is RecoveryKind.Empty or RecoveryKind.Uncertain)throw new InvalidDataException("Selected target authority unavailable: "+recovered.Detail);
        WaveCheckpointDto? dto=recovered.Payload==null?null:DecodeRecovered(recovered,false);
        return new(RuleUpgradeKind.CurrentSelected,i.SourceFingerprint,i.Run,recovered,dto,null,"Current selected authority; no source fallback");
    }
    private bool IsUpgradeSelector(string[] names)
    {
        if(!names.Contains(SelectorName))return false;
        // Dispatch only; strict exact record/provenance checks follow before authority use.
        byte[] raw=files.Registry.Read(SelectorName,CheckpointStore.ControlLimit);
        RejectDuplicateProperties(raw);
        var envelope=JsonSerializer.Deserialize<Envelope>(raw,Json)??throw new InvalidDataException("Missing selector envelope");
        byte[] body=Convert.FromBase64String(envelope.Payload);
        if(Hash(body)!=envelope.Hash)throw new InvalidDataException("Selector checksum mismatch");
        using var document=JsonDocument.Parse(body);
        return document.RootElement.TryGetProperty("Schema",out var schema)&&schema.GetInt32()==4;
    }
    private void ValidateUpgradeStaging(UpgradeIntent intent)
    {
        var slots=files.Slots();
        ExactSlots(slots,intent.SourceSlot,intent.Run,false);
        if(!slots.Contains(intent.Run))return;
        if(!files.Registry.Names().Contains(UpgradeBackupName))throw new InvalidDataException("Target exists without durable backup");
        using var target=new LockedPort(files.Slot(intent.Run));
        ValidatePendingSlotCapacity(target);
        var state=NewStore(target).Recover();
        ValidateUpgradeTargetArtifacts(intent,target,state);
        if(state.Kind==RecoveryKind.Empty)
        {
            if(target.Names().Any(n=>n!="writer.lock"))throw new InvalidDataException("Orphan target artifacts without control");
            return;
        }
        if(state.Kind==RecoveryKind.PendingStart&&state.Control!.Run==intent.Run&&state.Control.Sequence==1&&state.Control.Operation==intent.Operation)return;
        if(!intent.StartNew&&state.Kind==RecoveryKind.Active&&state.Control!.Run==intent.Run&&state.Control.Sequence==2
            &&state.Control.Operation==SaveOperation(intent.Operation)&&state.Payload!=null&&Hash(state.Payload)==intent.PayloadHash)return;
        throw new InvalidDataException("Target is advanced, closed, damaged or unrelated");
    }
    private static void ValidateUpgradeTargetArtifacts(UpgradeIntent intent,ICheckpointFiles target,Recovery state)
    {
        foreach(string name in target.Names())
        {
            if(name is "writer.lock" or "control.json")continue;
            bool pendingKnown=!intent.StartNew&&state.Kind==RecoveryKind.PendingStart
                &&System.Text.RegularExpressions.Regex.IsMatch(name,"^snapshot-"+intent.Run+"-2-[0-9a-f]{32}\\.json$")
                &&Hash(target.Read(name,CheckpointStore.SnapshotLimit))==intent.PayloadHash;
            bool committedKnown=state.Kind==RecoveryKind.Active&&state.Control!.Snapshots.Any(d=>d.Name==name
                &&d.Length==target.Length(name)&&d.Hash==Hash(target.Read(name,CheckpointStore.SnapshotLimit)));
            if(!pendingKnown&&!committedKnown)throw new InvalidDataException("Unattributed target artifact; no cleanup or authority repair");
        }
    }
    public RuleUpgradeInspection InspectUpgrade()
    {
        try
        {
            using var registry=new LockedPort(files.Registry);
            var names=UpgradeInventory();
            if(IsUpgradeSelector(names))return SelectedUpgrade();
            if(names.Contains(UpgradeIntentName))
            {
                var intent=ReadUpgradeIntent();
                ExactSlots(files.Slots(),intent.SourceSlot,intent.Run,false);
                if(!names.Contains(UpgradeBackupName)&&files.Slots().Contains(intent.Run))throw new InvalidDataException("Missing backup after target creation");
                using var source=new LockedPort(SourcePort(intent.SourceSlot));
                var capture=CaptureUpgradeSource(intent.SourceSlot,source);
                if(capture.Fingerprint!=intent.SourceFingerprint)throw new InvalidDataException("Source changed since consent");
                byte[] payload=ConvertUpgrade(capture,intent);
                if(!intent.StartNew&&Hash(payload)!=intent.PayloadHash)throw new InvalidDataException("Retry conversion changed");
                if(names.Contains(UpgradeBackupName))VerifyUpgradeBackup(intent);
                ValidateUpgradeStaging(intent);
                PreflightUpgrade(intent,capture.Backup);
                return new(RuleUpgradeKind.Incomplete,intent.SourceFingerprint,null,capture.Recovery,capture.Checkpoint,
                    new RuleUpgradeReceipt(this,intent,Hash(files.Registry.Read(UpgradeIntentName,CheckpointStore.ControlLimit))),
                    "Explicit retry of the original operation required; gameplay has not begun");
            }
            if(names.Contains(UpgradeBackupName))throw new InvalidDataException("Orphan upgrade backup");
            if(names.Contains(SelectorName))
            {
                var header=UpgradeDecode<MigrationSelector>(files.Registry.Read(SelectorName,CheckpointStore.ControlLimit));
                if(header.Rules==WaveRules.Version)
                {
                    // A current-version schema2/3 fresh fixture is supported, never reinterpreted as rule2.
                    var selected=Selected(ReadSelector());
                    return new(RuleUpgradeKind.CurrentSelected,selected.Fingerprint,selected.Slot,selected.Recovery,selected.Checkpoint,null,selected.Detail);
                }
            }
            string slot=SourceSlotBeforeUpgrade(names);
            if(files.Slots().Length!=(slot==""?0:1))throw new InvalidDataException("Unattributed slot artifacts");
            using var old=new LockedPort(SourcePort(slot));
            var src=CaptureUpgradeSource(slot,old);
            var kind=slot!=""?(src.Checkpoint==null?RuleUpgradeKind.PriorNotResumable:RuleUpgradeKind.PriorAvailable)
                :src.Recovery.Kind==RecoveryKind.Empty?RuleUpgradeKind.Empty:RuleUpgradeKind.LegacyPreserveOnly;
            return new(kind,src.Fingerprint,null,src.Recovery,src.Checkpoint,null,
                slot==""?"Legacy is preservation-only; no chained rule1 conversion":"Known prior rules; explicit consent required");
        }
        catch(Exception e)when(Expected(e)){return new(RuleUpgradeKind.Blocked,"",null,null,null,null,e.Message);}
    }
    private void PreflightUpgrade(UpgradeIntent intent,byte[] backup)
    {
        var names=UpgradeInventory();
        var selector=new UpgradeSelector(4,intent.Operation,intent.Run,intent.Run,intent.SourceSlot,
            Hash(Encode(intent)),catalog,WaveRules.Version);
        byte[] selectorBytes=Encode(selector), intentBytes=Encode(intent);
        if(backup.Length>BackupLimit||selectorBytes.Length>CheckpointStore.ControlLimit||intentBytes.Length>CheckpointStore.ControlLimit)
            throw new IOException("Upgrade record quota");
        long used=names.Sum(n=>files.Registry.Length(n));
        // Include replacement temporary bytes while old selector still exists. No pruning to fit.
        long needed=(names.Contains(UpgradeIntentName)?0:intentBytes.Length)+(names.Contains(UpgradeBackupName)?0:backup.Length)+selectorBytes.Length;
        if(used+needed>RegistryLimit)throw new IOException("Upgrade registry quota; originals retained");
    }
    public RuleUpgradeInspection ConfirmUpgrade(string operation,string newRun,string expectedFingerprint,bool startNew)
        =>ConfirmUpgradeCore(operation,newRun,expectedFingerprint,startNew,null);
    public RuleUpgradeInspection RetryUpgrade(RuleUpgradeReceipt receipt)
    {
        if(receipt==null||!ReferenceEquals(receipt.Issuer,this))throw new ArgumentException("Read a fresh issuer-bound upgrade receipt");
        var i=receipt.Intent;
        return ConfirmUpgradeCore(i.Operation,i.Run,i.SourceFingerprint,i.StartNew,receipt.IntentHash);
    }
    private RuleUpgradeInspection ConfirmUpgradeCore(string operation,string newRun,string fingerprint,bool startNew,string? requiredIntent)
    {
        if(!Uuid(operation)||!Uuid(newRun)||!Digest(fingerprint))throw new ArgumentException("Invalid upgrade confirmation identity");
        using var registry=new LockedPort(files.Registry);
        var names=UpgradeInventory();
        if(requiredIntent!=null&&(!names.Contains(UpgradeIntentName)||Hash(files.Registry.Read(UpgradeIntentName,CheckpointStore.ControlLimit))!=requiredIntent))
            throw new InvalidOperationException("Upgrade receipt is stale");
        if(IsUpgradeSelector(names))
        {
            var prior=ReadUpgradeIntent();
            if(prior.Operation!=operation||prior.Run!=newRun||prior.SourceFingerprint!=fingerprint||prior.StartNew!=startNew)
                throw new InvalidOperationException("Selected authority rejects a different operation");
            return SelectedUpgrade();
        }
        UpgradeIntent? existing=names.Contains(UpgradeIntentName)?ReadUpgradeIntent():null;
        if(existing!=null&&(existing.Operation!=operation||existing.Run!=newRun||existing.SourceFingerprint!=fingerprint||existing.StartNew!=startNew))
            throw new InvalidOperationException("Only original upgrade operation may retry");
        if(existing==null&&names.Contains(UpgradeBackupName))throw new InvalidDataException("Orphan upgrade backup");
        string sourceSlot=existing?.SourceSlot??SourceSlotBeforeUpgrade(names);
        if(newRun==sourceSlot)throw new InvalidDataException("Target must differ from source slot");
        ExactSlots(files.Slots(),sourceSlot,newRun,false);
        if(existing==null&&files.Slots().Contains(newRun))throw new InvalidDataException("Target existed before attributed intent");
        if(!names.Contains(UpgradeBackupName)&&files.Slots().Contains(newRun))throw new InvalidDataException("Target exists without backup");
        using var source=new LockedPort(SourcePort(sourceSlot));
        var capture=CaptureUpgradeSource(sourceSlot,source);
        if(capture.Fingerprint!=fingerprint)throw new InvalidOperationException("Source changed after review");
        var proposed=new UpgradeIntent(4,operation,newRun,sourceSlot,capture.Rules,fingerprint,fingerprint,"",catalog,WaveRules.Version,startNew);
        byte[] payload=ConvertUpgrade(capture,proposed);
        if(!startNew)proposed=proposed with { PayloadHash=Hash(payload) };
        if(existing!=null&&existing!=proposed)throw new InvalidDataException("Retry differs from immutable intent");
        PreflightUpgrade(proposed,capture.Backup);
        if(existing==null)files.Registry.AtomicWrite(UpgradeIntentName,Encode(proposed),false);
        if(!names.Contains(UpgradeBackupName))files.Registry.AtomicWrite(UpgradeBackupName,capture.Backup,false);
        VerifyUpgradeBackup(proposed);
        ValidateUpgradeStaging(proposed);
        using var target=new LockedPort(files.Slot(newRun));
        ValidatePendingSlotCapacity(target);
        var store=NewStore(target);
        var state=store.Recover();
        if(state.Kind==RecoveryKind.Empty)store.BeginRun(newRun,operation,0);
        state=store.Recover();
        if(!startNew&&state.Kind==RecoveryKind.PendingStart)store.Save(newRun,SaveOperation(operation),1,payload);
        state=store.Recover();
        bool ready=startNew
            ?state.Kind==RecoveryKind.PendingStart&&state.Control!.Run==newRun&&state.Control.Sequence==1&&state.Control.Operation==operation
            :state.Kind==RecoveryKind.Active&&state.Control!.Run==newRun&&state.Control.Sequence==2&&state.Control.Operation==SaveOperation(operation)
                &&state.Payload!=null&&Hash(state.Payload)==proposed.PayloadHash;
        if(!ready)throw new InvalidDataException("Target not exact confirmed pre-selection authority");
        if(CaptureUpgradeSource(sourceSlot,source).Fingerprint!=fingerprint)throw new InvalidOperationException("Source changed before selector commit");
        var selector=new UpgradeSelector(4,operation,newRun,newRun,sourceSlot,Hash(Encode(proposed)),catalog,WaveRules.Version);
        // Sole switch. Existing source selector is retained byte-for-byte in upgrade backup.
        files.Registry.AtomicWrite(SelectorName,Encode(selector),names.Contains(SelectorName));
        // Do not reacquire held target lock. Validate committed bytes and control in this scope.
        if(UpgradeDecode<UpgradeSelector>(files.Registry.Read(SelectorName,CheckpointStore.ControlLimit))!=selector)
            throw new InvalidDataException("Selector readback mismatch");
        state=store.Recover();
        if(state.Kind is RecoveryKind.Empty or RecoveryKind.Uncertain)throw new InvalidDataException("Selected target unavailable");
        return new(RuleUpgradeKind.CurrentSelected,fingerprint,newRun,state,state.Payload==null?null:DecodeRecovered(state,false),null,
            startNew?"Preserved source; explicit character selection required":"Upgrade selected; explicit acknowledged Resume may begin once");
    }
}
