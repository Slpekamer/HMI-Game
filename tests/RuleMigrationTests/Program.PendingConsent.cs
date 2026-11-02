using Hmi.Core;
using Hmi.Adapters.Persistence;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Security.Cryptography;

static partial class Program
{
    static void PendingConsentCases()
    {
        PendingColdMemoryMatrix();PendingReaderRejections();PendingReceiptRevalidation();PendingColdPhysicalProcesses();
    }
    // Return only storage. Original request tuple is local to the crashed-process setup and is not
    // available to the recovery helper; Host-equivalent code uses only ReadPendingConsent/RetryPending.
    static Fixture InterruptedInitialization(bool migration,RecoveryKind oldKind,int failAt,bool after)
    {
        var f=migration?Setup():NonResumable(oldKind);var c=Coordinator(f);var preview=c.Inspect();
        string operation=Guid.NewGuid().ToString("N"),run=Guid.NewGuid().ToString("N");
        f.WriteNumber=0;f.FailAt=failAt;f.FailAfter=after;
        Throws(()=>{if(migration)c.Confirm(operation,run,preview.Fingerprint);else c.InitializeNewGame(operation,run,preview.Fingerprint);},"interrupt original initialization");
        f.FailAt=0;return f;
    }
    static string AllStorage(Fixture f)=>Bytes(f.Reg)+"#"+Bytes(f.Old)+"#"+string.Join("|",f.NewSlots.OrderBy(p=>p.Key).Select(p=>p.Key+":"+Bytes(p.Value)));
    static void RecoverOnlyThroughPublicReceipt(Fixture f,bool migration,bool expectNotPending,bool expectSelected)
    {
        string before=AllStorage(f),legacy=Bytes(f.Old);int slots=f.NewSlots.Count,writes=f.WriteNumber;
        var authority=Sim();var fresh=new RuleMigrationCoordinator(f,authority);
        var pending=fresh.ReadPendingConsent();
        Check(AllStorage(f)==before&&f.NewSlots.Count==slots&&f.WriteNumber==writes,"cold query writes no artifacts or absent slot");
        Check(authority.Phase==RunPhase.Selecting&&authority.Tick==0,"cold query never Begin or Restore");
        if(expectNotPending)
        {Check(pending.Kind==PendingConsentKind.NotPending&&pending.Consent==null,"no durable intent cannot reconstruct consent");return;}
        if(expectSelected)
        {Check(pending.Kind==PendingConsentKind.Selected&&pending.Consent==null&&pending.Selected!.Kind==MigrationKind.Selected,"committed selector wins, no pending retry");return;}
        Check(pending.Kind==PendingConsentKind.Retryable&&pending.Consent!=null,"cold query returns validated original pending consent");
        var receipt=pending.Consent!;
        Check(receipt.Kind==(migration?SlotInitializationKind.Migration:SlotInitializationKind.NewGame),"intent Kind preserved");
        Check(migration?receipt.Summary.CharacterId=="honey"&&receipt.Summary.Wave==1&&receipt.Summary.Coins==0:receipt.Summary.CharacterId==null&&receipt.Summary.Wave==null&&receipt.Summary.Coins==null,"display summary does not invent NewGame character/seed");
        string publicJson=JsonSerializer.Serialize(receipt);
        foreach(string field in new[]{"Operation","NewRun","Fingerprint","IntentDigest","Issuer"})Check(!publicJson.Contains(field,StringComparison.Ordinal),"public receipt does not serialize internal tuple: "+field);
        Throws(()=>Coordinator(f).RetryPending(receipt),"cross-instance receipt rejected; new instance must re-read");
        var done=fresh.RetryPending(receipt);
        Check(done.Kind==MigrationKind.Selected&&done.Recovery!.Kind==(migration?RecoveryKind.Active:RecoveryKind.PendingStart),"explicit recovered retry selects exact operation");
        Check(Bytes(f.Old)==legacy&&f.NewSlots.Count==1,"cold retry preserves original group and single slot");
        Check(authority.Phase==RunPhase.Selecting&&authority.Tick==0,"explicit storage retry still does not Begin");
        string selectedBytes=AllStorage(f);
        Check(fresh.RetryPending(receipt).Kind==MigrationKind.Selected&&AllStorage(f)==selectedBytes,"same receipt lostack retry idempotent");
        Check(fresh.ReadPendingConsent().Kind==PendingConsentKind.Selected,"later read only selected route");
    }
    static void PendingColdMemoryMatrix()
    {
        for(int at=1;at<=6;at++)foreach(bool after in new[]{false,true})
            RecoverOnlyThroughPublicReceipt(InterruptedInitialization(true,RecoveryKind.Active,at,after),true,at==1&&!after,at==6&&after);
        foreach(var old in new[]{RecoveryKind.Empty,RecoveryKind.Closed,RecoveryKind.PendingStart})
            for(int at=1;at<=3;at++)foreach(bool after in new[]{false,true})
                RecoverOnlyThroughPublicReceipt(InterruptedInitialization(false,old,at,after),false,at==1&&!after,at==3&&after);
    }
    static void PendingReaderRejections()
    {
        var f=InterruptedInitialization(true,RecoveryKind.Active,1,true);var c=Coordinator(f);
        Check(c.ReadPendingConsent().Consent!.Stage==PendingConsentStage.BeforeSlot&&f.NewSlots.Count==0,"intent-only stage leaves slot absent");
        var original=f.Reg.Data["intent.json"].ToArray();var body=Body(original);body.Remove("Kind");f.Reg.Data["intent.json"]=PackedRaw(body.ToJsonString());
        Check(c.ReadPendingConsent().Kind==PendingConsentKind.Blocked&&c.ReadPendingConsent().Consent==null,"missing Kind never yields pending operation");
        f.Reg.Data["intent.json"]=original;f.Reg.Data.Remove("intent.json");f.Reg.Data["temp-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"]=new byte[]{1};
        Check(c.ReadPendingConsent().Kind==PendingConsentKind.Blocked,"unattributed temp cannot reconstruct tuple");
        f=InterruptedInitialization(true,RecoveryKind.Active,1,true);c=Coordinator(f);
        var oldStore=new CheckpointStore(f.Legacy,Catalog,WaveRules.LegacyVersion,(_,_)=>true);oldStore.Close(OldRun,Guid.NewGuid().ToString("N"),2);
        Check(c.ReadPendingConsent().Kind==PendingConsentKind.Blocked,"old Closed after migration intent cannot revive");
        f=InterruptedInitialization(true,RecoveryKind.Active,1,true);c=Coordinator(f);f.Old.Data["control.json"][0]^=1;
        Check(c.ReadPendingConsent().Kind==PendingConsentKind.Blocked,"old corruption blocks pending DTO");
        f=InterruptedInitialization(false,RecoveryKind.Closed,1,true);c=Coordinator(f);
        oldStore=new(f.Legacy,Catalog,WaveRules.LegacyVersion,(_,_)=>true);oldStore.BeginRun(Guid.NewGuid().ToString("N"),Guid.NewGuid().ToString("N"),3);
        Check(c.ReadPendingConsent().Kind==PendingConsentKind.Blocked,"NewGame old fingerprint change not silently refreshed");
        f=InterruptedInitialization(true,RecoveryKind.Active,2,true);c=Coordinator(f);f.Reg.Data["backup.json"][0]^=1;
        Check(c.ReadPendingConsent().Kind==PendingConsentKind.Blocked,"present corrupt backup is not reconstructed");
        f=InterruptedInitialization(true,RecoveryKind.Active,3,true);c=Coordinator(f);
        Check(c.ReadPendingConsent().Consent!.Stage==PendingConsentStage.PendingCheckpoint,"exact PendingStart stage exposes pending checkpoint only");
        f=InterruptedInitialization(true,RecoveryKind.Active,5,true);c=Coordinator(f);
        Check(c.ReadPendingConsent().Consent!.Stage==PendingConsentStage.ReadyToSelect,"exact saved staging ready to select");
        string slot=f.NewSlots.Keys.Single();var store=new CheckpointStore(f.Slot(slot),Catalog,WaveRules.Version,new CheckpointCodec(Sim()).Validate);
        store.Close(slot,Guid.NewGuid().ToString("N"),2);
        Check(c.ReadPendingConsent().Kind==PendingConsentKind.Blocked,"used Closed staging with missing selector cannot return retry");
        f=InterruptedInitialization(true,RecoveryKind.Active,5,true);c=Coordinator(f);slot=f.NewSlots.Keys.Single();store=new(f.Slot(slot),Catalog,WaveRules.Version,new CheckpointCodec(Sim()).Validate);
        store.BeginRun(Guid.NewGuid().ToString("N"),Guid.NewGuid().ToString("N"),2);
        Check(c.ReadPendingConsent().Kind==PendingConsentKind.Blocked,"advanced new run staging cannot return retry");
        f=InterruptedInitialization(true,RecoveryKind.Active,6,true);c=Coordinator(f);f.Old.Data["control.json"][0]^=1;
        Check(c.ReadPendingConsent().Kind==PendingConsentKind.Selected,"selected lostack ignores changed old source");
        slot=f.NewSlots.Keys.Single();store=new(f.Slot(slot),Catalog,WaveRules.Version,new CheckpointCodec(Sim()).Validate);store.Close(slot,Guid.NewGuid().ToString("N"),2);
        Check(c.ReadPendingConsent().Selected!.Recovery!.Kind==RecoveryKind.Closed,"selected Closed retained even corrupt old source");
        f.Reg.Data["selector.json"][0]^=1;
        Check(c.ReadPendingConsent().Kind==PendingConsentKind.Blocked&&c.ReadPendingConsent().Consent==null,"corrupt selector never falls back to intent");
        f=InterruptedInitialization(true,RecoveryKind.Active,3,true);c=Coordinator(f);slot=f.NewSlots.Keys.Single();f.NewSlots[slot].Data["unknown.txt"]=new byte[]{1};
        Check(c.ReadPendingConsent().Kind==PendingConsentKind.Blocked,"unknown staging file blocks pending receipt");
        f=InterruptedInitialization(true,RecoveryKind.Active,3,true);c=Coordinator(f);slot=f.NewSlots.Keys.Single();f.NewSlots[slot].Data["temp-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"]=new byte[CheckpointStore.StoreLimit+1];
        Check(c.ReadPendingConsent().Kind==PendingConsentKind.Blocked,"staging quota remains bounded");
        f=InterruptedInitialization(true,RecoveryKind.Active,1,true);c=Coordinator(f);f.FailAcquireRank=1;
        Check(c.ReadPendingConsent().Kind==PendingConsentKind.Blocked&&f.Locks.Count==0,"legacy lock acquisition failure yields no receipt and releases registry");
    }
    static void PendingReceiptRevalidation()
    {
        var f=InterruptedInitialization(true,RecoveryKind.Active,1,true);var c=Coordinator(f);var receipt=c.ReadPendingConsent().Consent!;
        f.Reg.Data.Remove("intent.json");string bytes=AllStorage(f);
        Throws(()=>c.RetryPending(receipt),"disappeared intent after inspection does not become fresh Confirm");Check(AllStorage(f)==bytes,"stale receipt does not recreate deleted intent");
        f=InterruptedInitialization(true,RecoveryKind.Active,1,true);c=Coordinator(f);receipt=c.ReadPendingConsent().Consent!;
        var intent=Body(f.Reg.Data["intent.json"]);intent["Kind"]=(int)SlotInitializationKind.NewGame;f.Reg.Data["intent.json"]=PackedRaw(intent.ToJsonString());
        Throws(()=>c.RetryPending(receipt),"changed intent after query rejected under registry lock");
        f=InterruptedInitialization(true,RecoveryKind.Active,1,true);c=Coordinator(f);receipt=c.ReadPendingConsent().Consent!;
        var store=new CheckpointStore(f.Legacy,Catalog,WaveRules.LegacyVersion,(_,_)=>true);store.Close(OldRun,Guid.NewGuid().ToString("N"),2);
        Throws(()=>c.RetryPending(receipt),"legacy Closed between query and click rechecked");
        f=InterruptedInitialization(true,RecoveryKind.Active,1,true);c=Coordinator(f);receipt=c.ReadPendingConsent().Consent!;
        f.FailAt=f.WriteNumber+5;f.FailAfter=true; // remaining backup, Begin, snapshot, control, selector
        Throws(()=>c.RetryPending(receipt),"recovered retry selector lostack");f.FailAt=0;
        store=new(f.Legacy,Catalog,WaveRules.LegacyVersion,(_,_)=>true);store.BeginRun(Guid.NewGuid().ToString("N"),Guid.NewGuid().ToString("N"),2);
        Check(c.RetryPending(receipt).Kind==MigrationKind.Selected,"same recovered receipt retries selected lostack without rereading changed legacy");
    }
    static void RunPendingChild(string root)
    {
        var files=new PhysicalRuleMigrationFiles(Path.Combine(root,"registry"),Path.Combine(root,"legacy"));var simulation=Sim();
        var coordinator=new RuleMigrationCoordinator(files,simulation);var read=coordinator.ReadPendingConsent();
        MigrationInspection done;
        if(read.Kind==PendingConsentKind.Selected)done=read.Selected!;
        else
        {
            if(read.Kind!=PendingConsentKind.Retryable||read.Consent==null)throw new Exception("Child could not recover validated pending tuple: "+read.Detail);
            done=coordinator.RetryPending(read.Consent);
        }
        if(done.Kind!=MigrationKind.Selected||simulation.Phase!=RunPhase.Selecting||simulation.Tick!=0)throw new Exception("Child storage retry altered gameplay");
        File.WriteAllText(Path.Combine(root,"pending-child-result.json"),JsonSerializer.Serialize(new{done.Kind,done.Slot,Recovery=done.Recovery!.Kind,simulation.Tick,simulation.Phase}));
        Console.WriteLine("PENDING_CHILD_SELECTED no tuple arguments; no Core Begin");
    }
    static void PendingColdPhysicalProcesses()
    {
        foreach(var test in new (bool Migration,int After)[]{(true,1),(true,3),(true,5),(true,6),(false,1),(false,2)})
        {
            string root=Path.Combine(Path.GetTempPath(),"hmi-pending-cold-"+Guid.NewGuid().ToString("N"));
            try
            {
                var physical=new PhysicalRuleMigrationFiles(Path.Combine(root,"registry"),Path.Combine(root,"legacy"));
                var oldStore=new CheckpointStore(physical.Legacy,Catalog,WaveRules.LegacyVersion,(_,_)=>true);
                oldStore.BeginRun(OldRun,Guid.NewGuid().ToString("N"),0);oldStore.Save(OldRun,Guid.NewGuid().ToString("N"),1,JsonSerializer.SerializeToUtf8Bytes(Legacy(Sim())));
                if(!test.Migration)oldStore.Close(OldRun,Guid.NewGuid().ToString("N"),2);
                var oldFiles=Directory.GetFiles(Path.Combine(root,"legacy")).ToDictionary(p=>Path.GetFileName(p)!,File.ReadAllBytes);
                var failing=new FaultingMigrationFiles(physical,test.After);var coordinator=Coordinator(failing);var preview=coordinator.Inspect();
                string operation=Guid.NewGuid().ToString("N"),run=Guid.NewGuid().ToString("N");
                Throws(()=>{if(test.Migration)coordinator.Confirm(operation,run,preview.Fingerprint);else coordinator.InitializeNewGame(operation,run,preview.Fingerprint);},"physical initialization interrupted after durable write");
                var start=new System.Diagnostics.ProcessStartInfo("dotnet"){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};
                start.ArgumentList.Add(typeof(Program).Assembly.Location);start.ArgumentList.Add("--retry-pending");start.ArgumentList.Add(root);
                using(var child=System.Diagnostics.Process.Start(start)!)
                {
                    bool exited=child.WaitForExit(15000);if(!exited){child.Kill(true);child.WaitForExit();}
                    string output=child.StandardOutput.ReadToEnd(),error=child.StandardError.ReadToEnd();
                    Console.WriteLine("COLD_CHILD "+test+" exit="+child.ExitCode+" "+output.Trim()+" "+error.Trim());
                    Check(exited&&child.ExitCode==0,"real fresh child process recovers without tuple arguments");
                }
                var recovered=Coordinator(physical).Inspect();
                Check(recovered.Kind==MigrationKind.Selected&&recovered.Slot==run,"cold child reused recorded exact run slot, no new UUID");
                foreach(var item in oldFiles)Check(File.ReadAllBytes(Path.Combine(root,"legacy",item.Key)).SequenceEqual(item.Value),"physical cold process leaves old bytes intact");
            }
            finally{if(Directory.Exists(root))Directory.Delete(root,true);}
        }
    }
    sealed class FaultingMigrationFiles : IRuleMigrationFiles
    {
        readonly IRuleMigrationFiles inner;readonly int after;int writes;
        public FaultingMigrationFiles(IRuleMigrationFiles inner,int after){this.inner=inner;this.after=after;}
        public ICheckpointFiles Registry=>new Port(this,inner.Registry);public ICheckpointFiles Legacy=>new Port(this,inner.Legacy);
        public string[] Slots()=>inner.Slots();public ICheckpointFiles Slot(string id)=>new Port(this,inner.Slot(id));
        sealed class Port : ICheckpointFiles
        {
            readonly FaultingMigrationFiles owner;readonly ICheckpointFiles inner;
            public Port(FaultingMigrationFiles owner,ICheckpointFiles inner){this.owner=owner;this.inner=inner;}
            public IDisposable AcquireWriter()=>inner.AcquireWriter();public string[] Names()=>inner.Names();public long Length(string n)=>inner.Length(n);
            public byte[] Read(string n,int max)=>inner.Read(n,max);public void Delete(string n)=>inner.Delete(n);
            public void AtomicWrite(string n,byte[] b,bool overwrite){inner.AtomicWrite(n,b,overwrite);if(++owner.writes==owner.after)throw new IOException("physical durable write lostack");}
        }
    }
}
