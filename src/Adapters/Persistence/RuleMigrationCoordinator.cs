using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hmi.Core;

namespace Hmi.Adapters.Persistence;

public interface IRuleMigrationFiles
{
    ICheckpointFiles Registry { get; }
    ICheckpointFiles Legacy { get; }
    string[] Slots();
    ICheckpointFiles Slot(string id);
}
public enum MigrationKind { LegacyAvailable, LegacyNotResumable, Incomplete, Selected, Uncertain }
public sealed record MigrationInspection(MigrationKind Kind, string Fingerprint, string? Slot,
    Recovery? Recovery, WaveCheckpointDto? Checkpoint, string Detail);
public enum SlotInitializationKind { Migration, NewGame, PreserveLegacyNewGame }
public sealed record MigrationIntent(int Schema, SlotInitializationKind Kind, string Operation, string Run, string Slot,
    string SourceFingerprint, string BackupHash, string ConvertedHash, string Catalog, string Rules);
public sealed record MigrationSelector(int Schema, SlotInitializationKind Kind, string Operation, string Run, string Slot,
    string SourceFingerprint, string IntentHash, string Catalog, string Rules);
public sealed record MigrationBackupFile(string Name, string Hash, byte[] Bytes);
public sealed record MigrationBackup(int Schema, MigrationBackupFile[] Files);

/// <summary>Opt-in storage transaction only. Never restores, begins, ticks, or alters Core authority.</summary>
public sealed partial class RuleMigrationCoordinator
{
    public const int RegistryLimit = 8 * 1024 * 1024, BackupLimit = 4 * 1024 * 1024, EntryLimit = 64;
    private const string IntentName="intent.json", BackupName="backup.json", SelectorName="selector.json";
    private readonly IRuleMigrationFiles files;
    private readonly GameSimulation simulation;
    private readonly CheckpointCodec codec;
    private readonly string catalog;
    private static readonly JsonSerializerOptions Json = new() { UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow, MaxDepth=16 };
    public RuleMigrationCoordinator(IRuleMigrationFiles files, GameSimulation simulation)
    { this.files=files; this.simulation=simulation; catalog=simulation.CatalogHash??throw new ArgumentException("Catalog hash required"); codec=new(simulation); }
    private static string Hash(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes));
    private static bool Uuid(string value)=>Guid.TryParseExact(value,"N",out var id)&&id!=Guid.Empty&&id.ToString("N")==value;
    private static bool Digest(string value)=>value!=null&&value.Length==64&&value.All(c=>c is >= '0' and <= '9' or >= 'A' and <= 'F');
    private static bool Expected(Exception e)=>e is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or ArgumentException or InvalidOperationException or FormatException or OverflowException;
    private static byte[] Encode<T>(T value)
    { byte[] body=JsonSerializer.SerializeToUtf8Bytes(value,Json);return JsonSerializer.SerializeToUtf8Bytes(new Envelope(Convert.ToBase64String(body),Hash(body)),Json); }
    private static T Decode<T>(byte[] raw)
    {
        RejectDuplicateProperties(raw);
        RequireProtocolShape(raw,typeof(Envelope));
        var envelope=JsonSerializer.Deserialize<Envelope>(raw,Json)??throw new InvalidDataException("Missing envelope");
        byte[] body=Convert.FromBase64String(envelope.Payload);
        if(Hash(body)!=envelope.Hash)throw new InvalidDataException("Registry checksum mismatch");
        RejectDuplicateProperties(body);
        RequireProtocolShape(body,typeof(T));
        return JsonSerializer.Deserialize<T>(body,Json)??throw new InvalidDataException("Missing registry data");
    }
    private static void RequireProtocolShape(byte[] bytes,Type type)
    {
        // Disallow alone rejects unknown fields, not omitted constructor parameters:
        // missing Kind otherwise becomes enum0 (Migration). Every schema member is mandatory.
        using var document=JsonDocument.Parse(bytes,new JsonDocumentOptions { MaxDepth=16 });
        var root=document.RootElement;
        string[] fields=type==typeof(Envelope)?new[]{"Payload","Hash"}
            :type==typeof(MigrationIntent)?new[]{"Schema","Kind","Operation","Run","Slot","SourceFingerprint","BackupHash","ConvertedHash","Catalog","Rules"}
            :type==typeof(MigrationSelector)?new[]{"Schema","Kind","Operation","Run","Slot","SourceFingerprint","IntentHash","Catalog","Rules"}
            :type==typeof(MigrationBackup)?new[]{"Schema","Files"}
            :throw new InvalidDataException("Unsupported registry protocol type");
        ExactObject(root,fields);
        if(type==typeof(MigrationBackup))
        {
            var files=root.GetProperty("Files");
            if(files.ValueKind!=JsonValueKind.Array||files.GetArrayLength()>EntryLimit)throw new InvalidDataException("Invalid backup file array");
            foreach(var file in files.EnumerateArray())ExactObject(file,new[]{"Name","Hash","Bytes"});
        }
        static void ExactObject(JsonElement value,string[] required)
        {
            if(value.ValueKind!=JsonValueKind.Object)throw new InvalidDataException("Protocol record must be an object");
            var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(var property in value.EnumerateObject())
                if(!seen.Add(property.Name)||!required.Contains(property.Name,StringComparer.Ordinal))throw new InvalidDataException("Unexpected or duplicate protocol member");
            if(seen.Count!=required.Length)throw new InvalidDataException("Missing required protocol member");
        }
    }
    private static void RejectDuplicateProperties(byte[] bytes)
    {
        using var document=JsonDocument.Parse(bytes,new JsonDocumentOptions { MaxDepth=16 });
        Visit(document.RootElement);
        static void Visit(JsonElement item)
        {
            if(item.ValueKind==JsonValueKind.Object)
            { var seen=new HashSet<string>(StringComparer.Ordinal);foreach(var p in item.EnumerateObject()){if(!seen.Add(p.Name))throw new InvalidDataException("Duplicate JSON property");Visit(p.Value);} }
            else if(item.ValueKind==JsonValueKind.Array)foreach(var child in item.EnumerateArray())Visit(child);
        }
    }
    // Borrowed writer acquisitions are valid only inside this private real-lock scope.
    // No locked port/store escapes public APIs; Dispose never transfers ownership to Session.
    private sealed class LockedPort : ICheckpointFiles, IDisposable
    {
        private readonly ICheckpointFiles inner; private readonly IDisposable writer; private bool active=true;
        internal LockedPort(ICheckpointFiles inner){this.inner=inner;writer=inner.AcquireWriter();}
        private void Guard(){if(!active)throw new ObjectDisposedException(nameof(LockedPort));}
        private sealed class BorrowedLease : IDisposable { public void Dispose(){} }
        public IDisposable AcquireWriter(){Guard();return new BorrowedLease();}
        public string[] Names(){Guard();return inner.Names();} public long Length(string n){Guard();return inner.Length(n);}
        public byte[] Read(string n,int max){Guard();return inner.Read(n,max);}
        public void AtomicWrite(string n,byte[] b,bool overwrite){Guard();inner.AtomicWrite(n,b,overwrite);}
        public void Delete(string n){Guard();inner.Delete(n);}
        public void Dispose(){if(!active)return;active=false;writer.Dispose();}
    }
    private CheckpointStore NewStore(LockedPort port)=>new(port,catalog,WaveRules.Version,codec.Validate);
    private CheckpointStore OldStore(LockedPort port)=>new(port,catalog,WaveRules.LegacyVersion,
        (bytes,id)=>TryDecodeLegacy(bytes,id,out _));
    private bool TryDecodeLegacy(byte[] bytes,SnapshotIdentity identity,out WaveCheckpointDto? result)
    {
        result=null;
        if(bytes.Length==0||bytes.Length>CheckpointStore.SnapshotLimit||identity.Catalog!=catalog||identity.Rules!=WaveRules.LegacyVersion||!Uuid(identity.Run))return false;
        try
        {
            RejectDuplicateProperties(bytes);
            using var document=JsonDocument.Parse(bytes);
            var properties=document.RootElement.EnumerateObject().Select(p=>p.Name).ToHashSet(StringComparer.Ordinal);
            var required=typeof(WaveCheckpointDto).GetProperties().Select(p=>p.Name).ToHashSet(StringComparer.Ordinal);
            if(!required.SetEquals(properties))return false;
            foreach(string array in new[]{"Cards","Items"})
            {
                var values=document.RootElement.GetProperty(array);
                if(values.ValueKind!=JsonValueKind.Array)return false;
                foreach(var value in values.EnumerateArray())
                {
                    if(value.ValueKind!=JsonValueKind.Object)return false;
                    var fields=value.EnumerateObject().Select(p=>p.Name).ToArray();
                    if(fields.Length!=2||!fields.Contains("Id")||!fields.Contains("Count"))return false;
                }
            }
            var dto=JsonSerializer.Deserialize<WaveCheckpointDto>(bytes,Json);
            if(dto==null)return false;
            // Validation-only target, never persisted. Actual confirmation supplies a distinct new UUID.
            Guid old=Guid.ParseExact(identity.Run,"N");
            Guid validation=old==Guid.Parse("11111111-1111-1111-1111-111111111111")?Guid.Parse("22222222-2222-2222-2222-222222222222"):Guid.Parse("11111111-1111-1111-1111-111111111111");
            if(!simulation.TryConvertLegacyPreparedCheckpoint(dto,new(old,identity.Sequence),new(validation,2),out _,out _))return false;
            result=dto;return true;
        }
        catch(Exception e)when(Expected(e)){return false;}
    }
    private (string Fingerprint,byte[] Backup) CaptureLegacy()
    {
        string[] all=files.Legacy.Names();
        if(all.Length>EntryLimit)throw new IOException("Legacy directory entry quota exceeded");
        foreach(string n in all)
            if(n!="writer.lock" && n!="control.json" && !System.Text.RegularExpressions.Regex.IsMatch(n,@"^snapshot-[0-9a-f]{32}-[1-9][0-9]*-[0-9a-f]{32}\.json$") && !(n.StartsWith("temp-",StringComparison.Ordinal)&&Uuid(n[5..])))throw new InvalidDataException("Unknown legacy file; migration does not read or back it up");
        string[] names=all.Where(n=>n=="control.json"||n.StartsWith("snapshot-",StringComparison.Ordinal)).OrderBy(n=>n,StringComparer.Ordinal).ToArray();
        if(names.Length>EntryLimit)throw new IOException("Legacy entry quota exceeded");
        long total=0;var entries=new List<MigrationBackupFile>();
        foreach(string name in names)
        {
            if(string.IsNullOrEmpty(name)||Path.GetFileName(name)!=name||name.Contains('/')||name.Contains('\\'))throw new InvalidDataException("Invalid legacy basename");
            long length=files.Legacy.Length(name);
            if(length<0||length>CheckpointStore.StoreLimit||(total+=length)>CheckpointStore.StoreLimit)throw new IOException("Legacy backup quota exceeded");
            byte[] data=files.Legacy.Read(name,CheckpointStore.StoreLimit);
            if(data.Length!=length)throw new IOException("Legacy length changed");
            entries.Add(new(name,Hash(data),data));
        }
        byte[] backup=Encode(new MigrationBackup(1,entries.ToArray()));
        if(backup.Length>BackupLimit)throw new IOException("Encoded backup quota exceeded");
        return(Hash(backup),backup);
    }
    private string[] Inventory()
    {
        string[] names=files.Registry.Names();
        if(names.Length>EntryLimit)throw new IOException("Registry entry quota exceeded");
        long total=0;
        foreach(string n in names)
        {
            if(n!="writer.lock"&&n!=IntentName&&n!=BackupName&&n!=SelectorName&&!(n.StartsWith("temp-",StringComparison.Ordinal)&&Uuid(n[5..])))throw new InvalidDataException("Unknown registry artifact");
            long size=files.Registry.Length(n);
            if(size<0||size>BackupLimit||(total+=size)>RegistryLimit)throw new IOException("Registry quota exceeded");
        }
        var slots=files.Slots();
        if(slots.Length>1||slots.Any(s=>!Uuid(s)))throw new InvalidDataException("Unknown or excess rule2 slots");
        return names;
    }
    private void Write(string name,byte[] data,int limit)
    {
        Inventory();
        long total=files.Registry.Names().Sum(n=>files.Registry.Length(n));
        if(data.Length>limit||total+data.Length>RegistryLimit)throw new IOException("Registry write quota exceeded");
        files.Registry.AtomicWrite(name,data,false);
    }
    private static bool ValidProtocolVersion(int schema,SlotInitializationKind kind)
        => schema==2 && kind is SlotInitializationKind.Migration or SlotInitializationKind.NewGame
            || schema==3 && kind==SlotInitializationKind.PreserveLegacyNewGame;
    private MigrationIntent ReadIntent()
    {
        var i=Decode<MigrationIntent>(files.Registry.Read(IntentName,CheckpointStore.ControlLimit));
        if(!ValidProtocolVersion(i.Schema,i.Kind)||!Enum.IsDefined(i.Kind)||!Uuid(i.Operation)||!Uuid(i.Run)||i.Slot!=i.Run||!Digest(i.SourceFingerprint)||i.Catalog!=catalog||i.Rules!=WaveRules.Version
            ||(i.Kind==SlotInitializationKind.Migration? !Digest(i.BackupHash)||!Digest(i.ConvertedHash) : i.Kind==SlotInitializationKind.PreserveLegacyNewGame? !Digest(i.BackupHash)||i.ConvertedHash!="" : i.BackupHash!=""||i.ConvertedHash!=""))throw new InvalidDataException("Invalid migration intent");
        return i;
    }
    private MigrationSelector ReadSelector()
    {
        var s=Decode<MigrationSelector>(files.Registry.Read(SelectorName,CheckpointStore.ControlLimit));
        if(!ValidProtocolVersion(s.Schema,s.Kind)||!Enum.IsDefined(s.Kind)||!Uuid(s.Operation)||!Uuid(s.Run)||s.Slot!=s.Run||!Digest(s.SourceFingerprint)||!Digest(s.IntentHash)||s.Catalog!=catalog||s.Rules!=WaveRules.Version)throw new InvalidDataException("Invalid selector");
        byte[] intentBytes=files.Registry.Read(IntentName,CheckpointStore.ControlLimit);
        var i=ReadIntent();
        if(Hash(intentBytes)!=s.IntentHash||i.Schema!=s.Schema||i.Kind!=s.Kind||i.Operation!=s.Operation||i.Run!=s.Run||i.SourceFingerprint!=s.SourceFingerprint)throw new InvalidDataException("Selector provenance mismatch");
        if(i.Kind is SlotInitializationKind.Migration or SlotInitializationKind.PreserveLegacyNewGame)VerifyBackup(i);
        else if(files.Registry.Names().Contains(BackupName))throw new InvalidDataException("NewGame intent cannot own a migration backup");
        if(!files.Slots().SequenceEqual(new[]{s.Slot}))throw new InvalidDataException("Selected slot missing or ambiguous");
        return s;
    }
    private void VerifyBackup(MigrationIntent intent)
    {
        byte[] bytes=files.Registry.Read(BackupName,BackupLimit);
        if(Hash(bytes)!=intent.BackupHash||intent.BackupHash!=intent.SourceFingerprint)throw new InvalidDataException("Backup fingerprint mismatch");
        var backup=Decode<MigrationBackup>(bytes);
        if(backup.Schema!=1||backup.Files==null||backup.Files.Length>EntryLimit)throw new InvalidDataException("Backup schema/count");
        long total=0;var names=new HashSet<string>(StringComparer.Ordinal);
        foreach(var file in backup.Files)
            if(file==null||file.Bytes==null||string.IsNullOrEmpty(file.Name)||!names.Add(file.Name)||!(file.Name=="control.json"||System.Text.RegularExpressions.Regex.IsMatch(file.Name,@"^snapshot-[0-9a-f]{32}-[1-9][0-9]*-[0-9a-f]{32}\.json$"))||Path.GetFileName(file.Name)!=file.Name||file.Name.Contains('/')||file.Name.Contains('\\')||(total+=file.Bytes.Length)>CheckpointStore.StoreLimit||Hash(file.Bytes)!=file.Hash)throw new InvalidDataException("Invalid backup entry");
    }
    private MigrationInspection Selected(MigrationSelector selected)
    {
        var port=files.Slot(selected.Slot);using var writer=new LockedPort(port);
        var recovery=NewStore(writer).Recover();
        if(recovery.Kind is RecoveryKind.Empty or RecoveryKind.Uncertain)throw new InvalidDataException("Selected rule2 authority unavailable");
        WaveCheckpointDto? dto=null;
        if(recovery.Payload!=null)dto=DecodeRecovered(recovery,false);
        return new(MigrationKind.Selected,selected.SourceFingerprint,selected.Slot,recovery,dto,"Selected rule2 slot; never falls back to legacy");
    }
    private WaveCheckpointDto DecodeRecovered(Recovery recovery,bool legacy)
    {
        if(recovery.Payload==null||recovery.Control==null)throw new InvalidDataException("Missing resumable payload");
        foreach(var reference in recovery.Control.Snapshots)
        {
            if(Hash(recovery.Payload)!=reference.Hash)continue;
            var identity=new SnapshotIdentity(reference.Run,reference.Sequence,catalog,legacy?WaveRules.LegacyVersion:WaveRules.Version);
            WaveCheckpointDto? dto;
            bool ok=legacy?TryDecodeLegacy(recovery.Payload,identity,out dto):codec.TryDecode(recovery.Payload,identity,out dto);
            if(ok&&dto!=null)return dto;
        }
        throw new InvalidDataException("Recovered payload identity mismatch");
    }
    public MigrationInspection Inspect()
    {
        try
        {
            using var registry=new LockedPort(files.Registry);
            var names=Inventory();
            if(names.Contains(SelectorName))return Selected(ReadSelector());
            if(names.Any(n=>n!="writer.lock")||files.Slots().Length!=0)return new(MigrationKind.Incomplete,"",null,null,null,"Migration artifacts without selector; no legacy resurrection. Explicit same-operation retry only.");
            using var old=new LockedPort(files.Legacy);
            var recovery=OldStore(old).Recover();
            if(recovery.Kind==RecoveryKind.Uncertain)throw new InvalidDataException(recovery.Detail);
            if(recovery.Kind is not (RecoveryKind.Active or RecoveryKind.RolledBack))return new(MigrationKind.LegacyNotResumable,CaptureLegacy().Fingerprint,null,recovery,null,"No valid legacy safe point; explicit independent NewGame may initialize a slot");
            var dto=DecodeRecovered(recovery,true);var capture=CaptureLegacy();
            return new(MigrationKind.LegacyAvailable,capture.Fingerprint,null,recovery,dto,"Explicit consent required: restart saved wave with new60s rules");
        }
        catch(Exception e)when(Expected(e)){return new(MigrationKind.Uncertain,"",null,null,null,e.Message);}
    }
    public MigrationInspection Confirm(string operation,string newRun,string expectedFingerprint)
        => ConfirmCore(operation,newRun,expectedFingerprint,null);
    private MigrationInspection ConfirmCore(string operation,string newRun,string expectedFingerprint,string? requiredIntentDigest)
    {
        if(!Uuid(operation)||!Uuid(newRun)||!Digest(expectedFingerprint))throw new ArgumentException("Invalid confirmation identity");
        using var registry=new LockedPort(files.Registry);
        var names=Inventory();
        ValidatePendingReceiptDigest(requiredIntentDigest,names);
        if(names.Contains(SelectorName))
        {
            var selected=ReadSelector();
            if(selected.Kind!=SlotInitializationKind.Migration||selected.Operation!=operation||selected.Run!=newRun||selected.SourceFingerprint!=expectedFingerprint)throw new InvalidOperationException("Selected authority rejects different/stale confirmation");
            return Selected(selected);
        }
        MigrationIntent? prior=names.Contains(IntentName)?ReadIntent():null;
        if(prior!=null&&(prior.Kind!=SlotInitializationKind.Migration||prior.Operation!=operation||prior.Run!=newRun||prior.SourceFingerprint!=expectedFingerprint))throw new InvalidOperationException("Only the original confirmed operation may resume");
        if(prior==null&&(names.Any(n=>n!="writer.lock")||files.Slots().Length!=0))throw new InvalidDataException("Unattributed migration artifacts");
        if(files.Slots().Any(s=>s!=newRun))throw new InvalidDataException("Unexpected slot identity");
        using var legacyWriter=new LockedPort(files.Legacy);
        var legacyRecovery=OldStore(legacyWriter).Recover();
        if(legacyRecovery.Kind is not (RecoveryKind.Active or RecoveryKind.RolledBack))throw new InvalidDataException("Legacy is not resumable");
        var legacyDto=DecodeRecovered(legacyRecovery,true);var capture=CaptureLegacy();
        if(capture.Fingerprint!=expectedFingerprint)throw new InvalidOperationException("Legacy changed after consent");
        if(!simulation.TryConvertLegacyPreparedCheckpoint(legacyDto,new(legacyDto.RunId,legacyDto.Sequence),new(Guid.ParseExact(newRun,"N"),2),out var converted,out var error)||converted==null)throw new InvalidDataException("Conversion rejected: "+error);
        byte[] payload=codec.Encode(converted);
        var intent=new MigrationIntent(2,SlotInitializationKind.Migration,operation,newRun,newRun,expectedFingerprint,Hash(capture.Backup),Hash(payload),catalog,WaveRules.Version);
        byte[] intentBytes=Encode(intent);
        if(prior==null)Write(IntentName,intentBytes,CheckpointStore.ControlLimit);
        else if(prior!=intent)throw new InvalidDataException("Retry payload differs from immutable intent");
        if(!files.Registry.Names().Contains(BackupName))Write(BackupName,capture.Backup,BackupLimit);
        VerifyBackup(intent);
        var newPort=files.Slot(newRun);using var newWriter=new LockedPort(newPort);
        var store=NewStore(newWriter);var state=store.Recover();
        if(state.Kind==RecoveryKind.Empty)store.BeginRun(newRun,operation,0);
        state=store.Recover();
        if(state.Kind==RecoveryKind.PendingStart)
        {
            if(state.Control!.Run!=newRun||state.Control.Sequence!=1||state.Control.Operation!=operation)throw new InvalidDataException("Unexpected staging start");
            store.Save(newRun,SaveOperation(operation),1,payload);
        }
        state=store.Recover();
        if(state.Kind!=RecoveryKind.Active||state.Control!.Run!=newRun||state.Control.Sequence!=2||state.Control.Operation!=SaveOperation(operation)||state.Payload==null||Hash(state.Payload)!=intent.ConvertedHash)throw new InvalidDataException("Staging is not exact confirmed safe point");
        DecodeRecovered(state,false);
        if(CaptureLegacy().Fingerprint!=expectedFingerprint)throw new InvalidOperationException("Legacy changed before selector commit");
        var selector=new MigrationSelector(2,SlotInitializationKind.Migration,operation,newRun,newRun,expectedFingerprint,Hash(intentBytes),catalog,WaveRules.Version);
        Write(SelectorName,Encode(selector),CheckpointStore.ControlLimit);
        // Return only after fresh selected-slot validation under the same held new-slot writer.
        var committed=store.Recover();var dto=DecodeRecovered(committed,false);
        return new(MigrationKind.Selected,expectedFingerprint,newRun,committed,dto,"Migration durably selected; caller may acknowledge exactly once before Begin");
    }
    /// <summary>Explicit fresh new-rule slot only for validated Empty/Closed/PendingStart legacy.
    /// Does not migrate a checkpoint or Begin gameplay. Existing selected slots use normal Session.StartNew.</summary>
    public MigrationInspection InitializeNewGame(string operation,string newRun,string expectedFingerprint)
        => InitializeNewGameCore(operation,newRun,expectedFingerprint,null);
    private MigrationInspection InitializeNewGameCore(string operation,string newRun,string expectedFingerprint,string? requiredIntentDigest)
    {
        if(!Uuid(operation)||!Uuid(newRun)||!Digest(expectedFingerprint))throw new ArgumentException("Invalid NewGame confirmation identity");
        using var registry=new LockedPort(files.Registry);
        var names=Inventory();
        ValidatePendingReceiptDigest(requiredIntentDigest,names);
        if(names.Contains(SelectorName))
        {
            var selected=ReadSelector();
            if(selected.Kind!=SlotInitializationKind.NewGame||selected.Operation!=operation||selected.Run!=newRun||selected.SourceFingerprint!=expectedFingerprint)throw new InvalidOperationException("Slot already selected; use normal selected-slot session");
            return Selected(selected);
        }
        MigrationIntent? prior=names.Contains(IntentName)?ReadIntent():null;
        if(prior!=null&&(prior.Kind!=SlotInitializationKind.NewGame||prior.Operation!=operation||prior.Run!=newRun||prior.SourceFingerprint!=expectedFingerprint))throw new InvalidOperationException("Different initialization operation already staged");
        if(prior==null&&(names.Any(n=>n!="writer.lock")||files.Slots().Length!=0))throw new InvalidDataException("Unknown initialization artifacts; no NewGame bypass");
        if(names.Contains(BackupName)||files.Slots().Any(s=>s!=newRun))throw new InvalidDataException("Unexpected NewGame staging artifacts");
        using var legacyWriter=new LockedPort(files.Legacy);
        var legacy=OldStore(legacyWriter).Recover();
        if(legacy.Kind is not (RecoveryKind.Empty or RecoveryKind.Closed or RecoveryKind.PendingStart))throw new InvalidDataException("NewGame initialization requires validated nonresumable legacy; no corrupt/Active bypass");
        if(legacy.Control?.Run==newRun)throw new InvalidDataException("NewGame requires distinct run UUID");
        if(CaptureLegacy().Fingerprint!=expectedFingerprint)throw new InvalidOperationException("Legacy status changed after consent");
        var intent=new MigrationIntent(2,SlotInitializationKind.NewGame,operation,newRun,newRun,expectedFingerprint,"","",catalog,WaveRules.Version);
        byte[] intentBytes=Encode(intent);
        if(prior==null)Write(IntentName,intentBytes,CheckpointStore.ControlLimit);
        else if(prior!=intent)throw new InvalidDataException("NewGame retry differs from immutable intent");
        var port=files.Slot(newRun);using var slotWriter=new LockedPort(port);
        var store=NewStore(slotWriter);var staged=store.Recover();
        if(staged.Kind==RecoveryKind.Empty)store.BeginRun(newRun,operation,0);
        staged=store.Recover();
        if(staged.Kind!=RecoveryKind.PendingStart||staged.Control!.Run!=newRun||staged.Control.Sequence!=1||staged.Control.Operation!=operation)throw new InvalidDataException("NewGame staged authority changed before selection");
        if(CaptureLegacy().Fingerprint!=expectedFingerprint)throw new InvalidOperationException("Legacy changed before NewGame selector commit");
        var selector=new MigrationSelector(2,SlotInitializationKind.NewGame,operation,newRun,newRun,expectedFingerprint,Hash(intentBytes),catalog,WaveRules.Version);
        Write(SelectorName,Encode(selector),CheckpointStore.ControlLimit);
        var committed=store.Recover();
        if(committed.Kind!=RecoveryKind.PendingStart)throw new InvalidDataException("NewGame selection did not retain pending authority");
        return new(MigrationKind.Selected,expectedFingerprint,newRun,committed,null,"Independent rule2 slot selected; no legacy checkpoint or gameplay Begin created");
    }
    private static string SaveOperation(string operation)
    {
        byte[] digest=SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("migration-save:"+operation));
        return new Guid(digest.AsSpan(0,16)).ToString("N");
    }
}
