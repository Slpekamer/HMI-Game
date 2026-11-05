using Hmi.Core;
using Hmi.Adapters.Persistence;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

static partial class Program
{
    static int checks;
    static readonly byte[] Content=File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"gameplay.json"));
    static readonly string Catalog=Convert.ToHexString(SHA256.HashData(Content)).ToLowerInvariant();
    const string OldSlot="11111111111111111111111111111111",NewRun="22222222222222222222222222222222",Operation="33333333333333333333333333333333",OldRun="44444444444444444444444444444444";
    static void Check(bool v,string message){checks++;if(!v)throw new Exception(message);}
    static void Throws(Action a,string message){bool threw=false;try{a();}catch(Exception e)when(e is IOException or InvalidDataException or InvalidOperationException or UnauthorizedAccessException or ArgumentException){threw=true;}Check(threw,message);}
    static string Hash(byte[] b)=>Convert.ToHexString(SHA256.HashData(b));
    static byte[] Pack<T>(T item){byte[] b=JsonSerializer.SerializeToUtf8Bytes(item);return JsonSerializer.SerializeToUtf8Bytes(new {Payload=Convert.ToBase64String(b),Hash=Hash(b)});}
    static GameSimulation Sim()=>new(JsonSerializer.Deserialize<GameContent>(Content,new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!,new SimulationLimits(32,10,4),Catalog);
    static RuleMigrationCoordinator Coordinator(IRuleMigrationFiles f)=>new(f,Sim());
    static string Bytes(MemoryFiles p)=>string.Join("|",p.Data.OrderBy(x=>x.Key,StringComparer.Ordinal).Select(x=>x.Key+":"+Hash(x.Value)));
    static WaveCheckpointDto Dto(string rules,string run=OldRun,long sequence=2)
    {var g=Sim();g.PrepareRun("crit",1030);Check(g.TryCapturePrepared(new(Guid.ParseExact(run,"N"),sequence),out var d,out _),"capture synthetic start");d!.RulesVersion=rules;return d;}
    static Fixture Setup(bool legacy=false,StoreStatus status=StoreStatus.Active,SlotInitializationKind kind=SlotInitializationKind.NewGame)
    {
        var f=new Fixture();var source=legacy?f.Old:(MemoryFiles)f.Slot(OldSlot);
        string rules=legacy?WaveRules.LegacyVersion:WaveRules.PriorVersion;
        var store=new CheckpointStore(source,Catalog,rules,(_,_)=>true);
        store.BeginRun(OldRun,Guid.NewGuid().ToString("N"),0);
        if(status!=StoreStatus.PendingStart)store.Save(OldRun,Guid.NewGuid().ToString("N"),1,JsonSerializer.SerializeToUtf8Bytes(Dto(rules)));
        if(status==StoreStatus.Closed)store.Close(OldRun,Guid.NewGuid().ToString("N"),2);
        if(!legacy)
        {
            byte[] backup=Pack(new MigrationBackup(1,Array.Empty<MigrationBackupFile>()));
            string fingerprint=Hash(backup),backupHash=kind==SlotInitializationKind.NewGame?"":Hash(backup);
            int schema=kind==SlotInitializationKind.PreserveLegacyNewGame?3:2;
            var intent=new MigrationIntent(schema,kind,Operation,OldSlot,OldSlot,fingerprint,backupHash,
                kind==SlotInitializationKind.Migration?Hash(new byte[]{1}):"",Catalog,WaveRules.PriorVersion);
            byte[] intentBytes=Pack(intent);
            f.Reg.Data["intent.json"]=intentBytes;
            if(kind!=SlotInitializationKind.NewGame)f.Reg.Data["backup.json"]=backup;
            f.Reg.Data["selector.json"]=Pack(new MigrationSelector(schema,kind,Operation,OldSlot,OldSlot,fingerprint,Hash(intentBytes),Catalog,WaveRules.PriorVersion));
        }
        f.WriteNumber=0;f.Writes.Clear();return f;
    }
    static void Main(string[] args)
    {
        if(args.Length==3&&args[0]=="--upgrade-child"){UpgradeChild(args[1],int.Parse(args[2]));return;}
        if(args.Length==1&&args[0]=="--independent-only"){IndependentCases();Console.WriteLine($"PASS RuleUpgradeIndependent {checks}");return;}
        IndependentCases();BoundaryCases();NonresumableOwnershipCases();AdvancedConversionCases();CoreConversion();Happy();FaultMatrix();Rejections();StatusMatrix();AfterSelection();Physical();KillMatrix();
        if(args.Length==2&&args[0]=="--old-reader")OldReaderCases(args[1]);
        else Console.WriteLine("OLD_READER NOT_RUN: supply --old-reader <exact-baseline-reader.dll>");
        Console.WriteLine($"PASS RuleUpgrade {checks}");
    }
    static void CoreConversion()
    {
        var g=Sim();var d=Dto(WaveRules.PriorVersion);byte[] original=JsonSerializer.SerializeToUtf8Bytes(d);int token=g.RunToken;
        Check(g.TryValidateArchivedCheckpoint(d,new(d.RunId,d.Sequence),out _),"prior valid for archive");
        Check(g.TryConvertPriorPreparedCheckpoint(d,new(d.RunId,d.Sequence),new(Guid.ParseExact(NewRun,"N"),2),out var converted,out _),"prior conversion");
        Check(g.RunToken==token&&g.ProjectileCount==0,"conversion does not mutate authority");
        Check(original.SequenceEqual(JsonSerializer.SerializeToUtf8Bytes(d)),"conversion source detached unchanged");
        var copy=converted!;copy.RunId=d.RunId;copy.Sequence=d.Sequence;copy.RulesVersion=d.RulesVersion;
        Check(original.SequenceEqual(JsonSerializer.SerializeToUtf8Bytes(copy)),"only rules/run/sequence changed");
        copy.Cards[0].Count++;
        Check(original.SequenceEqual(JsonSerializer.SerializeToUtf8Bytes(d)),"all array elements detached");
        Check(!g.TryConvertPriorPreparedCheckpoint(d,new(d.RunId,d.Sequence),new(d.RunId,2),out _,out _),"same run rejected");
        Check(!g.TryConvertPriorPreparedCheckpoint(d,new(d.RunId,d.Sequence),new(Guid.NewGuid(),3),out _,out _),"target sequence must2");
        d.RulesVersion=WaveRules.LegacyVersion;
        Check(g.TryValidateArchivedCheckpoint(d,new(d.RunId,d.Sequence),out _),"legacy validation for preserve only");
        Check(!g.TryConvertPriorPreparedCheckpoint(d,new(d.RunId,d.Sequence),new(Guid.NewGuid(),2),out _,out _),"no rule1 chain conversion");
        Check(!g.TryConvertLegacyPreparedCheckpoint(d,new(d.RunId,d.Sequence),new(Guid.NewGuid(),2),out _,out _),"old converter remains closed");
    }
    static void Happy()
    {
        foreach(var kind in Enum.GetValues<SlotInitializationKind>())
        {
            var f=Setup(kind:kind);var c=Coordinator(f);string old=Bytes(f.NewSlots[OldSlot]);var oldRegistry=f.Reg.Data.ToDictionary(x=>x.Key,x=>x.Value.ToArray());
            var view=c.InspectUpgrade();Check(view.Kind==RuleUpgradeKind.PriorAvailable,"known prior selected not corrupt: "+view.Detail);
            int writes=f.WriteNumber;Check(view.Checkpoint!.RunId.ToString("N")==OldRun,"control run differs from slot supported");
            Check(writes==f.WriteNumber,"inspection no writes");
            var selected=c.ConfirmUpgrade(Operation,NewRun,view.Fingerprint,false);
            Check(selected.Kind==RuleUpgradeKind.CurrentSelected&&selected.Recovery!.Kind==RecoveryKind.Active,"upgrade selects active");
            Check(selected.Checkpoint!.RunId.ToString("N")==NewRun&&selected.Checkpoint.Sequence==2&&selected.Checkpoint.RulesVersion==WaveRules.Version,"new identity/rules");
            Check(Bytes(f.NewSlots[OldSlot])==old,"source slot bytes preserved");
            foreach(var p in oldRegistry.Where(x=>x.Key!="selector.json"))Check(f.Reg.Data[p.Key].SequenceEqual(p.Value),"source registry original preserved");
            Check(Coordinator(f).InspectUpgrade().Kind==RuleUpgradeKind.CurrentSelected,"cold selected");
            int committed=f.WriteNumber;
            Check(c.ConfirmUpgrade(Operation,NewRun,view.Fingerprint,false).Kind==RuleUpgradeKind.CurrentSelected&&f.WriteNumber==committed,"same operation ack retry no writes");
            var g=Sim();var codec=new CheckpointCodec(g);var session=new CheckpointSession(g,new CheckpointStore(f.Slot(NewRun),Catalog,WaveRules.Version,codec.Validate),codec);
            Check(session.Resume(session.IntentEpoch)&&g.Wave==1&&g.Tick==0,"Session only Begin once after selected");
            Check(!session.Resume(session.IntentEpoch-1),"stale Session callback rejected");
        }
    }
    static void FaultMatrix()
    {
        foreach(bool startNew in new[]{false,true})foreach(bool legacy in new[]{false,true})
        {
            if(legacy&&!startNew)continue;
            var sample=Setup(legacy);var sc=Coordinator(sample);var sv=sc.InspectUpgrade();sc.ConfirmUpgrade(Operation,NewRun,sv.Fingerprint,startNew);int writes=sample.WriteNumber;
            for(int n=1;n<=writes;n++)foreach(bool after in new[]{false,true})
            {
                var f=Setup(legacy);var c=Coordinator(f);var v=c.InspectUpgrade();string original=Bytes(legacy?f.Old:f.NewSlots[OldSlot]);
                f.FailAt=n;f.FailAfter=after;
                Throws(()=>c.ConfirmUpgrade(Operation,NewRun,v.Fingerprint,startNew),$"fault write{n} after{after}");
                f.FailAt=0;var cold=Coordinator(f);var state=cold.InspectUpgrade();
                var uncommitted=f.NewSlots.TryGetValue(NewRun,out var stagedPort)
                    ?stagedPort.Data.Keys.Where(k=>k.StartsWith("snapshot-",StringComparison.Ordinal)).ToHashSet(StringComparer.Ordinal)
                    :new HashSet<string>(StringComparer.Ordinal);
                bool targetWasPending=stagedPort!=null&&new CheckpointStore(stagedPort,Catalog,WaveRules.Version,new CheckpointCodec(Sim()).Validate).Recover().Kind==RecoveryKind.PendingStart;
                Check(Bytes(legacy?f.Old:f.NewSlots[OldSlot])==original,"fault retains source");
                if(state.Kind==RuleUpgradeKind.Incomplete)
                {Check(state.Pending!=null&&state.Pending.StartNew==startNew,"cold receipt operation kind");state=cold.RetryUpgrade(state.Pending!);}
                else if(state.Kind!=RuleUpgradeKind.CurrentSelected)state=cold.ConfirmUpgrade(Operation,NewRun,v.Fingerprint,startNew);
                Check(state.Kind==RuleUpgradeKind.CurrentSelected,"fault retry eventually selects");
                Check(state.Recovery!.Kind==(startNew?RecoveryKind.PendingStart:RecoveryKind.Active),"fault retry intended target status");
                foreach(string deletion in f.Writes.Where(w=>w.Contains(":delete:",StringComparison.Ordinal)))
                {
                    string[] parts=deletion.Split(":delete:",StringSplitOptions.None);
                    Check(parts[0]==NewRun&&targetWasPending&&uncommitted.Contains(parts[1]),"only known target PendingStart uncommitted snapshot cleanup");
                    Console.WriteLine($"STORE_EXISTING_CLEANUP failureWrite={n} after={after} newGame={startNew} legacy={legacy} targetStatus=PendingStart operation={Operation} file={deletion}");
                }
                Check(f.Locks.Count==0&&!f.BadLock,"all fault locks ordered/released");
            }
        }
    }
    static void Rejections()
    {
        var f=Setup();var c=Coordinator(f);var v=c.InspectUpgrade();
        Throws(()=>c.ConfirmUpgrade(Operation,OldRun,v.Fingerprint,false),"target not current source run");
        Throws(()=>c.ConfirmUpgrade(Operation,OldSlot,v.Fingerprint,false),"target not source slot");
        var legacy=Setup(true);var lc=Coordinator(legacy);var lv=lc.InspectUpgrade();
        Check(lv.Kind==RuleUpgradeKind.LegacyPreserveOnly,"rule1 explicit preserve-only summary");
        Throws(()=>lc.ConfirmUpgrade(Operation,NewRun,lv.Fingerprint,false),"legacy cannot convert");
        Check(legacy.WriteNumber==0,"legacy conversion refusal writes nothing");
        f.FailAt=3;f.FailAfter=true;
        Throws(()=>c.ConfirmUpgrade(Operation,NewRun,v.Fingerprint,false),"stage target after backup");f.FailAt=0;
        f.Reg.Data.Remove("upgrade-backup.json");
        Check(Coordinator(f).InspectUpgrade().Kind==RuleUpgradeKind.Blocked,"target with missing backup blocked");
        Throws(()=>c.ConfirmUpgrade(Operation,NewRun,v.Fingerprint,false),"missing backup cannot be reconstructed after target");
        var o=Setup();o.Reg.Data["upgrade-backup.json"]=new byte[]{1};Check(Coordinator(o).InspectUpgrade().Kind==RuleUpgradeKind.Blocked,"orphan backup blocked");
        var changed=Setup();var cc=Coordinator(changed);var cv=cc.InspectUpgrade();changed.NewSlots[OldSlot].Data["snapshot-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa-1-bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb.json"]=new byte[]{4};
        Throws(()=>cc.ConfirmUpgrade(Operation,NewRun,cv.Fingerprint,false),"source inventory drift rejects stale consent");
    }
    static void StatusMatrix()
    {
        foreach(bool legacy in new[]{false,true})foreach(var status in Enum.GetValues<StoreStatus>())
        {
            var f=Setup(legacy,status);var c=Coordinator(f);var v=c.InspectUpgrade();
            Check(v.Kind!=RuleUpgradeKind.Blocked,"known source status accepted");
            if(status!=StoreStatus.Active)Check(v.Checkpoint==null,"closed/pending never expose old snapshots");
            if(status!=StoreStatus.Active||legacy)Throws(()=>c.ConfirmUpgrade(Operation,NewRun,v.Fingerprint,false),"nonresumable/legacy cannot upgrade");
            var s=c.ConfirmUpgrade(Operation,NewRun,v.Fingerprint,true);
            Check(s.Recovery!.Kind==RecoveryKind.PendingStart&&s.Checkpoint==null,"preserve and new picker no guessed hero");
        }
        var empty=new Fixture();var ec=Coordinator(empty);var ev=ec.InspectUpgrade();
        Check(ev.Kind==RuleUpgradeKind.Empty,"empty installation");Check(ec.ConfirmUpgrade(Operation,NewRun,ev.Fingerprint,true).Recovery!.Kind==RecoveryKind.PendingStart,"empty explicit new game");
    }
    static void AfterSelection()
    {
        var f=Setup();var c=Coordinator(f);var v=c.InspectUpgrade();c.ConfirmUpgrade(Operation,NewRun,v.Fingerprint,false);
        f.NewSlots[OldSlot].Data["control.json"]=new byte[]{8};
        Check(Coordinator(f).InspectUpgrade().Kind==RuleUpgradeKind.CurrentSelected,"new authority independent of later source writes");
        var g=Sim();var codec=new CheckpointCodec(g);var store=new CheckpointStore(f.Slot(NewRun),Catalog,WaveRules.Version,codec.Validate);
        var next=Dto(WaveRules.Version,NewRun,3);next.Wave=2;next.FinitePicks=new[]{"base.health"};next.Cards.Single(x=>x.Id=="base.health").Count=1;
        store.Save(NewRun,Guid.NewGuid().ToString("N"),2,codec.Encode(next));
        Check(Coordinator(f).InspectUpgrade().Checkpoint!.Wave==2,"selected later committed wave takes precedence over migration wave");
        store.Close(NewRun,Guid.NewGuid().ToString("N"),3);
        var closed=c.ConfirmUpgrade(Operation,NewRun,v.Fingerprint,false);
        Check(closed.Recovery!.Kind==RecoveryKind.Closed&&closed.Checkpoint==null,"lost ack confirmation after Close never revives");
        string newer=Guid.NewGuid().ToString("N");store.BeginRun(newer,Guid.NewGuid().ToString("N"),4);
        Check(Coordinator(f).InspectUpgrade().Recovery!.Control!.Run==newer,"selected slot newer run current authority");
    }
    static void Physical()
    {
        string root=Path.Combine(Path.GetTempPath(),"hmi-p3-"+Guid.NewGuid().ToString("N"));
        try
        {
            var f=Setup();Export(f,root);var files=new PhysicalRuleMigrationFiles(Path.Combine(root,"registry"),Path.Combine(root,"legacy"));
            var c=Coordinator(files);var v=c.InspectUpgrade();Check(v.Kind==RuleUpgradeKind.PriorAvailable,"physical source");
            Check(c.ConfirmUpgrade(Operation,NewRun,v.Fingerprint,false).Kind==RuleUpgradeKind.CurrentSelected,"physical atomic selector replacement");
            Check(Coordinator(files).InspectUpgrade().Kind==RuleUpgradeKind.CurrentSelected,"physical cold current");
            Directory.CreateDirectory(Path.Combine(root,"registry","slots",Guid.NewGuid().ToString("N")));
            Check(Coordinator(files).InspectUpgrade().Kind==RuleUpgradeKind.Blocked,"physical third slot rejected");
        }
        finally{if(Directory.Exists(root))Directory.Delete(root,true);}
    }
    static void OldReaderCases(string dll)
    {
        Check(File.Exists(dll),"exact prior reader assembly exists");
        var sample=Setup();var sampleCoordinator=Coordinator(sample);var sampleView=sampleCoordinator.InspectUpgrade();
        Check(sampleCoordinator.ConfirmUpgrade(Operation,NewRun,sampleView.Fingerprint,false).Kind==RuleUpgradeKind.CurrentSelected,"old reader sample commits");
        string[] writes=sample.Writes.Select(n=>n.StartsWith(NewRun+":snapshot-",StringComparison.Ordinal)?NewRun+":snapshot":n).ToArray();
        Check(writes.SequenceEqual(new[]{"registry:upgrade-intent.json","registry:upgrade-backup.json",NewRun+":control.json",NewRun+":snapshot",NewRun+":control.json","registry:selector.json"}),
            "exact old reader covers every successful upgrade write through selector publication");
        for(int stage=0;stage<=6;stage++)
        {
            var f=Setup();var c=Coordinator(f);var view=c.InspectUpgrade();
            if(stage>0)
            {
                f.FailAt=stage;f.FailAfter=true;
                Throws(()=>c.ConfirmUpgrade(Operation,NewRun,view.Fingerprint,false),"old reader stage produced by real commit fault");
                f.FailAt=0;
            }
            string root=Path.Combine(Path.GetTempPath(),"hmi-p3-oldreader-"+Guid.NewGuid().ToString("N"));
            try
            {
                Export(f,root);
                var before=Directory.GetFiles(root,"*",SearchOption.AllDirectories).Where(p=>Path.GetFileName(p)!="writer.lock").ToDictionary(p=>p,p=>Hash(File.ReadAllBytes(p)));
                var start=new System.Diagnostics.ProcessStartInfo("dotnet"){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};
                start.ArgumentList.Add(dll);start.ArgumentList.Add(root);start.ArgumentList.Add(stage==0?"Selected":"Uncertain");
                using var child=System.Diagnostics.Process.Start(start)!;
                var outputTask=child.StandardOutput.ReadToEndAsync();var errorTask=child.StandardError.ReadToEndAsync();
                if(!child.WaitForExit(15000))
                {child.Kill(entireProcessTree:true);child.WaitForExit();throw new Exception("Exact old reader timed out at stage "+stage);}
                string output=outputTask.GetAwaiter().GetResult(),error=errorTask.GetAwaiter().GetResult();
                Check(child.ExitCode==0,"exact old reader cold-open stage "+stage+": "+error);
                Console.WriteLine("OLD_READER_STAGE "+stage+" "+output.Trim());
                foreach(var p in before)Check(File.Exists(p.Key)&&Hash(File.ReadAllBytes(p.Key))==p.Value,"old reader no saved byte mutation");
                Check(Directory.GetFiles(root,"*",SearchOption.AllDirectories).Count(p=>Path.GetFileName(p)!="writer.lock")==before.Count,"old reader no new checkpoint artifacts");
            }
            finally{if(Directory.Exists(root))Directory.Delete(root,true);}
        }
    }
    static void Export(Fixture f,string root)
    {
        Write(f.Reg,Path.Combine(root,"registry"));Write(f.Old,Path.Combine(root,"legacy"));
        foreach(var p in f.NewSlots)Write(p.Value,Path.Combine(root,"registry","slots",p.Key));
        static void Write(MemoryFiles p,string path){Directory.CreateDirectory(path);foreach(var b in p.Data)File.WriteAllBytes(Path.Combine(path,b.Key),b.Value);}
    }
    sealed class Fixture : IRuleMigrationFiles
    {
        public readonly MemoryFiles Reg,Old;public readonly Dictionary<string,MemoryFiles> NewSlots=new();
        public readonly List<int> Locks=new();public readonly List<string> Writes=new();public bool BadLock;
        public int WriteNumber,FailAt;public bool FailAfter,ThrowDelete;public Action<string>? BeforeWrite=null;
        public Fixture(){Reg=new(this,"registry",0);Old=new(this,"legacy",1);}
        public ICheckpointFiles Registry=>Reg;public ICheckpointFiles Legacy=>Old;
        public string[] Slots()=>NewSlots.Keys.ToArray();
        public ICheckpointFiles Slot(string id){if(!NewSlots.TryGetValue(id,out var p))NewSlots[id]=p=new(this,id,id==OldSlot?1:2);return p;}
    }
    sealed class MemoryFiles : ICheckpointFiles
    {
        readonly Fixture owner;readonly string name;readonly int rank;bool locked;
        public readonly Dictionary<string,byte[]> Data=new(StringComparer.Ordinal);
        public MemoryFiles(Fixture owner,string name,int rank){this.owner=owner;this.name=name;this.rank=rank;}
        sealed class Lease : IDisposable{readonly Action action;public Lease(Action action)=>this.action=action;public void Dispose()=>action();}
        public IDisposable AcquireWriter()
        {
            if(locked)throw new IOException("busy writer");
            if(owner.Locks.Count>0&&owner.Locks[^1]>=rank){owner.BadLock=true;throw new IOException("reverse lock");}
            locked=true;owner.Locks.Add(rank);
            return new Lease(()=>{if(owner.Locks[^1]!=rank)throw new Exception("nonLIFO release");owner.Locks.RemoveAt(owner.Locks.Count-1);locked=false;});
        }
        public string[] Names()=>Data.Keys.ToArray();
        public long Length(string n)=>Data.TryGetValue(n,out var b)?b.Length:throw new FileNotFoundException();
        public byte[] Read(string n,int max)=>Data.TryGetValue(n,out var b)&&b.Length<=max?b.ToArray():throw new IOException("missing/oversize");
        public void AtomicWrite(string n,byte[] b,bool overwrite)
        {
            if(!locked)throw new IOException("unlocked write");owner.BeforeWrite?.Invoke(name+":"+n);owner.Writes.Add(name+":"+n);int step=++owner.WriteNumber;
            if(owner.FailAt==step&&!owner.FailAfter)throw new IOException("before write");
            if(Data.ContainsKey(n)&&!overwrite)throw new IOException("immutable exists");Data[n]=b.ToArray();
            if(owner.FailAt==step&&owner.FailAfter)throw new IOException("after commit ack lost");
        }
        public void Delete(string n){if(!locked)throw new IOException("unlocked delete");owner.Writes.Add(name+":delete:"+n);if(owner.ThrowDelete)throw new IOException("injected cleanup failure");Data.Remove(n);}
    }
}
