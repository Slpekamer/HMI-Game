using Hmi.Core;
using Hmi.Adapters.Persistence;
using System.Text.Json;
using System.Text.Json.Nodes;

static partial class Program
{
    static void PreserveLegacyCases()
    {
        PreserveHappy();PreserveFaultMatrix();PreserveRejects();PreserveSchema();PreserveQuotaAndRace();PreserveColdProcesses();
    }
    static Fixture PreserveInterrupted(int at,bool after)
    {
        var f=Setup();var c=Coordinator(f);var p=c.Inspect();f.WriteNumber=0;f.FailAt=at;f.FailAfter=after;
        Throws(()=>c.PreserveLegacyAndInitializeNewGame(Operation,NewRun,p.Fingerprint),"preserve-old write interruption");f.FailAt=0;return f;
    }
    static void PreserveHappy()
    {
        foreach(bool rollback in new[]{false,true})
        {
            var f=Setup();if(rollback)
            {
                var store=new CheckpointStore(f.Legacy,Catalog,WaveRules.LegacyVersion,(_,_)=>true);var dto=Legacy(Sim());dto.Sequence=3;
                store.Save(OldRun,Guid.NewGuid().ToString("N"),2,JsonSerializer.SerializeToUtf8Bytes(dto));f.Old.Data[store.Recover().Control!.Snapshots[0].Name]=new byte[]{7};
            }
            var sim=Sim();var c=new RuleMigrationCoordinator(f,sim);var p=c.Inspect();string old=Bytes(f.Old);f.Writes.Clear();
            Check(p.Recovery!.Kind==(rollback?RecoveryKind.RolledBack:RecoveryKind.Active),"valid preserve source");
            var selected=c.PreserveLegacyAndInitializeNewGame(Operation,NewRun,p.Fingerprint);
            Check(selected.Kind==MigrationKind.Selected&&selected.Recovery!.Kind==RecoveryKind.PendingStart&&selected.Checkpoint==null,"preserve creates PendingStart only");
            Check(sim.Phase==RunPhase.Selecting&&sim.Tick==0,"preserve does not prepare or Begin gameplay");
            Check(Bytes(f.Old)==old&&f.NewSlots.Count==1,"preserve source bytes remain exact with one new slot");
            Check(f.Writes.SequenceEqual(new[]{"registry:intent.json","registry:backup.json","slot:control.json","registry:selector.json"}),"exact four-write preserve transaction");
            var intent=Body(f.Reg.Data["intent.json"]);Check((int)intent["Schema"]! ==3&&(int)intent["Kind"]! ==2&&(string)intent["ConvertedHash"]! =="","new kind has schema3 and no converted checkpoint");
            var backup=JsonSerializer.Deserialize<MigrationBackup>(Body(f.Reg.Data["backup.json"]).ToJsonString())!;
            Check(backup.Files.Length==f.Old.Data.Count,"full owned group backup, including damaged latest on rollback");
            foreach(var entry in backup.Files)Check(entry.Bytes.SequenceEqual(f.Old.Data[entry.Name]),"backup entry exact "+entry.Name);
            string all=AllStorage(f);Check(c.PreserveLegacyAndInitializeNewGame(Operation,NewRun,p.Fingerprint).Kind==MigrationKind.Selected&&AllStorage(f)==all,"same confirmation idempotent");
            Throws(()=>c.InitializeNewGame(Operation,NewRun,p.Fingerprint),"old NewGame API cannot claim new kind");
            Throws(()=>c.Confirm(Operation,NewRun,p.Fingerprint),"migration API cannot claim preserve kind");
            var codec=new CheckpointCodec(sim);var newStore=new CheckpointStore(f.Slot(NewRun),Catalog,WaveRules.Version,codec.Validate);var session=new CheckpointSession(sim,newStore,codec);session.RefreshRecovery();
            Check(session.StartNew("crit",55,session.IntentEpoch),"only later explicit current choice begins normal Session");
            Check(c.Inspect().Recovery!.Kind==RecoveryKind.Active&&c.PreserveLegacyAndInitializeNewGame(Operation,NewRun,p.Fingerprint).Recovery!.Kind==RecoveryKind.Active,"existing selector returns later Session Active, not forced PendingStart");
            Check(session.Close(session.IntentEpoch),"later Session closes selected new run");
            f.Old.Data["control.json"][0]^=1;
            Check(c.Inspect().Recovery!.Kind==RecoveryKind.Closed&&c.ReadPendingConsent().Kind==PendingConsentKind.Selected,"selected Closed never reopens old Active even if old becomes corrupt");
            Check(c.PreserveLegacyAndInitializeNewGame(Operation,NewRun,p.Fingerprint).Recovery!.Kind==RecoveryKind.Closed,"selector lostack route ignores changed legacy, retains Closed");
            Throws(()=>c.PreserveLegacyAndInitializeNewGame(Operation,NewRun,new string('A',64)),"new fingerprint cannot replace selected authority");
        }
    }
    static void PreserveFaultMatrix()
    {
        for(int at=1;at<=4;at++)foreach(bool after in new[]{false,true})
        {
            var f=PreserveInterrupted(at,after);string old=Bytes(f.Old),before=AllStorage(f);var sim=Sim();var c=new RuleMigrationCoordinator(f,sim);var read=c.ReadPendingConsent();
            Check(AllStorage(f)==before,"preserve pending reader is read-only");
            if(at==1&&!after)
            {Check(read.Kind==PendingConsentKind.NotPending,"no durable intent needs fresh consent");c.PreserveLegacyAndInitializeNewGame(Operation,NewRun,c.Inspect().Fingerprint);}
            else if(at==4&&after)
            {Check(read.Kind==PendingConsentKind.Selected,"durable selector wins over pending");}
            else
            {
                Check(read.Kind==PendingConsentKind.Retryable&&read.Consent!.Kind==SlotInitializationKind.PreserveLegacyNewGame,"cold receipt identifies explicit preserve kind");
                Check(read.Consent!.Summary.CharacterId=="honey"&&read.Consent.Summary.Wave==1,"summary describes retained old run, not new choice");
                Check(read.Consent.Stage==(at==4||at==3&&after?PendingConsentStage.ReadyToSelect:PendingConsentStage.BeforeSlot),"preserve stage has no PendingCheckpoint");
                Throws(()=>Coordinator(f).RetryPending(read.Consent),"preserve receipt issuer-bound");
                c.RetryPending(read.Consent);string done=AllStorage(f);c.RetryPending(read.Consent);Check(AllStorage(f)==done,"duplicate preserve receipt no extra writes");
            }
            Check(c.Inspect().Recovery!.Kind==RecoveryKind.PendingStart&&sim.Phase==RunPhase.Selecting&&sim.Tick==0,"fault recovery does not guess hero or Begin");
            Check(Bytes(f.Old)==old&&f.Locks.Count==0&&f.NewSlots.Count==1,"fault preserves old and releases all locks");
        }
    }
    static void PreserveRejects()
    {
        foreach(var kind in new[]{RecoveryKind.Empty,RecoveryKind.Closed,RecoveryKind.PendingStart,RecoveryKind.Uncertain})
        {
            var f=NonResumable(kind);var c=Coordinator(f);string before=AllStorage(f);var p=c.Inspect();
            Throws(()=>c.PreserveLegacyAndInitializeNewGame(Operation,NewRun,p.Fingerprint.Length==0?new string('A',64):p.Fingerprint),"preserve API refuses nonresumable legacy "+kind);
            Check(AllStorage(f)==before&&f.NewSlots.Count==0,"nonresumable refusal has no writes");
        }
        var a=Setup();var ca=Coordinator(a);var pa=ca.Inspect();string original=AllStorage(a);
        Throws(()=>ca.InitializeNewGame(Operation,NewRun,pa.Fingerprint),"existing NewGame still rejects Active");
        Throws(()=>ca.PreserveLegacyAndInitializeNewGame(Operation,OldRun,pa.Fingerprint),"preserve distinct UUID required");Check(AllStorage(a)==original,"sameUUID rejected before writes");
        foreach(string mutation in new[]{"legacy","backup","missing-backup","closed","advanced","intent"})
        {
            var f=PreserveInterrupted(3,true);var c=Coordinator(f);var receipt=c.ReadPendingConsent().Consent!;
            if(mutation=="legacy")new CheckpointStore(f.Legacy,Catalog,WaveRules.LegacyVersion,(_,_)=>true).Close(OldRun,Guid.NewGuid().ToString("N"),2);
            if(mutation=="backup")f.Reg.Data["backup.json"][0]^=1;
            if(mutation=="missing-backup")f.Reg.Data.Remove("backup.json");
            if(mutation=="closed")new CheckpointStore(f.Slot(NewRun),Catalog,WaveRules.Version,new CheckpointCodec(Sim()).Validate).Close(NewRun,Guid.NewGuid().ToString("N"),1);
            if(mutation=="advanced")new CheckpointStore(f.Slot(NewRun),Catalog,WaveRules.Version,new CheckpointCodec(Sim()).Validate).BeginRun(Guid.NewGuid().ToString("N"),Guid.NewGuid().ToString("N"),1);
            if(mutation=="intent")f.Reg.Data.Remove("intent.json");
            string before=AllStorage(f);Throws(()=>c.RetryPending(receipt),"receipt rejects changed "+mutation);Check(AllStorage(f)==before,"failed retry no new writes "+mutation);
            Check(c.ReadPendingConsent().Kind==PendingConsentKind.Blocked,"changed source/staging cannot offer retry "+mutation);
        }
        a=PreserveInterrupted(1,true);ca=Coordinator(a);original=AllStorage(a);
        Throws(()=>ca.PreserveLegacyAndInitializeNewGame(Guid.NewGuid().ToString("N"),NewRun,ca.Inspect().Fingerprint),"different operation cannot take staging");Check(AllStorage(a)==original,"wrong operation does not mutate");
    }
    static void PreserveSchema()
    {
        foreach(bool selector in new[]{false,true})
        {
            var f=PreserveInterrupted(selector?4:1,true);string file=selector?"selector.json":"intent.json";var original=f.Reg.Data[file];
            foreach(string field in Body(original).Select(p=>p.Key).ToArray())
            {
                var body=Body(original);body.Remove(field);f.Reg.Data[file]=PackedRaw(body.ToJsonString());
                Check(Coordinator(f).ReadPendingConsent().Kind==PendingConsentKind.Blocked,"schema3 missing field rejects "+file+":"+field);f.Reg.Data[file]=original;
            }
            foreach(int schema in new[]{1,2,4})
            {var body=Body(original);body["Schema"]=schema;f.Reg.Data[file]=PackedRaw(body.ToJsonString());Check(Coordinator(f).ReadPendingConsent().Kind==PendingConsentKind.Blocked,"schema3 kind2 cannot relabel schema "+schema);f.Reg.Data[file]=original;}
            foreach(int kind in new[]{0,1,3})
            {var body=Body(original);body["Kind"]=kind;f.Reg.Data[file]=PackedRaw(body.ToJsonString());Check(Coordinator(f).ReadPendingConsent().Kind==PendingConsentKind.Blocked,"schema3 cannot relabel kind "+kind);f.Reg.Data[file]=original;}
        }
    }
    static void PreserveQuotaAndRace()
    {
        var f=Setup();var c=Coordinator(f);var p=c.Inspect();bool held=false;
        f.BeforeWrite=n=>{if(n!="registry:selector.json")return;held=true;Throws(()=>{using var l=f.Old.AcquireWriter();},"preserve old lock held through selector");Throws(()=>{using var l=f.Reg.AcquireWriter();},"preserve registry lock held through selector");};
        c.PreserveLegacyAndInitializeNewGame(Operation,NewRun,p.Fingerprint);Check(held,"publication lock check exercised");
        f=Setup();c=Coordinator(f);p=c.Inspect();f.BeforeWrite=n=>{if(n=="slot:control.json")f.Old.Data["control.json"][0]^=1;};
        Throws(()=>c.PreserveLegacyAndInitializeNewGame(Operation,NewRun,p.Fingerprint),"rogue source change before selector rejected");Check(!f.Reg.Data.ContainsKey("selector.json"),"source revalidation blocks publish");
        foreach(bool excess in new[]{false,true})
        {
            f=Setup();int bytes=f.Old.Data.Values.Sum(b=>b.Length);f.Old.Data["snapshot-"+OldRun+"-99-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.json"]=new byte[CheckpointStore.StoreLimit-bytes+(excess?1:0)];c=Coordinator(f);p=c.Inspect();string old=Bytes(f.Old);
            if(excess){Throws(()=>c.PreserveLegacyAndInitializeNewGame(Operation,NewRun,new string('A',64)),"old group overquota rejects");Check(f.Reg.Data.Count==0,"overquota before writes");}
            else {Check(p.Kind==MigrationKind.LegacyAvailable,"maximum owned old group is bounded and valid");c.PreserveLegacyAndInitializeNewGame(Operation,NewRun,p.Fingerprint);Check(c.Inspect().Kind==MigrationKind.Selected,"maximum old group backs up successfully");}
            Check(Bytes(f.Old)==old,"quota never trims old snapshots");
        }
        foreach(bool excess in new[]{false,true})
        {
            f=PreserveInterrupted(2,true);c=Coordinator(f);var receipt=c.ReadPendingConsent().Consent!;var intent=JsonSerializer.Deserialize<MigrationIntent>(Body(f.Reg.Data["intent.json"]).ToJsonString())!;
            var selector=new MigrationSelector(3,intent.Kind,intent.Operation,intent.Run,intent.Slot,intent.SourceFingerprint,Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(f.Reg.Data["intent.json"])),Catalog,WaveRules.Version);
            int reserve=PackedRaw(JsonSerializer.Serialize(selector)).Length;long fill=RuleMigrationCoordinator.RegistryLimit-f.Reg.Data.Values.Sum(b=>(long)b.Length)-reserve+(excess?1:0);int i=1;
            while(fill>0){int size=(int)Math.Min(fill,1024*1024);f.Reg.Data["temp-"+(i++).ToString("x32")]=new byte[size];fill-=size;}
            string before=AllStorage(f);
            if(excess){Check(c.ReadPendingConsent().Kind==PendingConsentKind.Blocked,"pending overquota no receipt");Throws(()=>c.RetryPending(receipt),"registry one byte over remaining capacity rejects");Check(AllStorage(f)==before,"quota refusal does not evict backup or temp");}
            else {c.RetryPending(receipt);Check(f.Reg.Data.Values.Sum(b=>(long)b.Length)==RuleMigrationCoordinator.RegistryLimit,"exact maximum registry completes without deletion");}
        }
    }
    static void ExportPreserveFixture(string root,int after)
    {
        var physical=new PhysicalRuleMigrationFiles(Path.Combine(root,"registry"),Path.Combine(root,"legacy"));var old=new CheckpointStore(physical.Legacy,Catalog,WaveRules.LegacyVersion,(_,_)=>true);
        old.BeginRun(OldRun,Guid.NewGuid().ToString("N"),0);old.Save(OldRun,Guid.NewGuid().ToString("N"),1,JsonSerializer.SerializeToUtf8Bytes(Legacy(Sim())));
        var fault=new FaultingMigrationFiles(physical,Math.Abs(after));var c=Coordinator(fault);var p=c.Inspect();
        Throws(()=>c.PreserveLegacyAndInitializeNewGame(Operation,NewRun,p.Fingerprint),"physical preserve interruption");
        if(after<0)new CheckpointStore(physical.Slot(NewRun),Catalog,WaveRules.Version,new CheckpointCodec(Sim()).Validate).Close(NewRun,Guid.NewGuid().ToString("N"),1);
    }
    static void InspectOriginalSchema2(string root,bool active)
    {
        string State()=>string.Join("|",Directory.GetFiles(root,"*",SearchOption.AllDirectories).OrderBy(p=>p).Select(p=>p+Convert.ToHexString(File.ReadAllBytes(p))));
        string before=State();var sim=Sim();var c=new RuleMigrationCoordinator(new PhysicalRuleMigrationFiles(Path.Combine(root,"registry"),Path.Combine(root,"legacy")),sim);var p=c.Inspect();
        Check(p.Kind==MigrationKind.Selected&&p.Recovery!.Kind==(active?RecoveryKind.Active:RecoveryKind.PendingStart),"new reader accepts actual old schema2 writer output");
        Check(c.ReadPendingConsent().Kind==PendingConsentKind.Selected&&State()==before&&sim.Phase==RunPhase.Selecting,"schema2 inspection read-only and no Begin");
        Console.WriteLine("NEW_READER_ACTUAL_SCHEMA2 PASS "+p.Recovery!.Kind);
    }
    static void PreserveColdProcesses()
    {
        foreach(int after in new[]{1,2,3,4})
        {
            string root=Path.Combine(Path.GetTempPath(),"hmi-preserve-cold-"+Guid.NewGuid().ToString("N"));
            try
            {
                ExportPreserveFixture(root,after);string old=string.Join("|",Directory.GetFiles(Path.Combine(root,"legacy")).OrderBy(p=>p).Select(p=>Path.GetFileName(p)+Convert.ToHexString(File.ReadAllBytes(p))));
                var start=new System.Diagnostics.ProcessStartInfo("dotnet"){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};start.ArgumentList.Add(typeof(Program).Assembly.Location);start.ArgumentList.Add("--retry-pending");start.ArgumentList.Add(root);
                using var child=System.Diagnostics.Process.Start(start)!;bool exited=child.WaitForExit(15000);if(!exited){child.Kill(true);child.WaitForExit();}string output=child.StandardOutput.ReadToEnd(),error=child.StandardError.ReadToEnd();Console.WriteLine("PRESERVE_COLD after="+after+" exit="+child.ExitCode+" "+output.Trim()+error.Trim());Check(exited&&child.ExitCode==0,"cold preserve process only receives directory");
                var c=Coordinator(new PhysicalRuleMigrationFiles(Path.Combine(root,"registry"),Path.Combine(root,"legacy")));Check(c.Inspect().Recovery!.Kind==RecoveryKind.PendingStart&&c.Inspect().Slot==NewRun,"cold preserve ends PendingStart with recorded UUID, no guessed character");
                Check(old==string.Join("|",Directory.GetFiles(Path.Combine(root,"legacy")).OrderBy(p=>p).Select(p=>Path.GetFileName(p)+Convert.ToHexString(File.ReadAllBytes(p)))),"cold preserve old bytes unchanged");
            }
            finally{if(Directory.Exists(root))Directory.Delete(root,true);}
        }
    }
}
