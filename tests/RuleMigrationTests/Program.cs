using Hmi.Core;
using Hmi.Adapters.Persistence;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text;
using System.Text.Json.Nodes;

static partial class Program
{
    static int checks;
    static readonly byte[] Content=File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"gameplay.json"));
    static readonly string Catalog=Convert.ToHexString(SHA256.HashData(Content)).ToLowerInvariant();
    static readonly string OldRun="11111111111111111111111111111111",NewRun="22222222222222222222222222222222",Operation="33333333333333333333333333333333";
    static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
    static void Throws(Action action,string message){bool threw=false;try{action();}catch(Exception e)when(e is IOException or InvalidDataException or UnauthorizedAccessException or InvalidOperationException or ArgumentException){threw=true;}Check(threw,message);}
    static GameSimulation Sim()=>new(JsonSerializer.Deserialize<GameContent>(Content,new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!,new SimulationLimits(32,10,4),Catalog);
    static WaveCheckpointDto Legacy(GameSimulation g)
    {
        g.PrepareRun("honey",1030);Check(g.Phase==RunPhase.Prepared,"prepare run");
        Check(g.TryCapturePrepared(new(Guid.ParseExact(OldRun,"N"),2),out var dto,out _),"capture");
        dto!.RulesVersion=WaveRules.LegacyVersion;
        return dto;
    }
    static Fixture Setup()
    {
        var f=new Fixture();var g=Sim();var dto=Legacy(g);
        var store=new CheckpointStore(f.Legacy,Catalog,WaveRules.LegacyVersion,(_,_)=>true);
        store.BeginRun(OldRun,Guid.NewGuid().ToString("N"),0);
        store.Save(OldRun,Guid.NewGuid().ToString("N"),1,JsonSerializer.SerializeToUtf8Bytes(dto));
        f.Events.Clear();return f;
    }
    static RuleMigrationCoordinator Coordinator(IRuleMigrationFiles files)=>new(files,Sim());
    static string Bytes(MemoryFiles files)=>string.Join("|",files.Data.OrderBy(p=>p.Key,StringComparer.Ordinal).Select(p=>p.Key+":"+Convert.ToHexString(p.Value)));
    static void Main(string[] args)
    {
        if(args.Length==2&&args[0].StartsWith("--inspect-schema2-")){InspectOriginalSchema2(args[1],args[0].EndsWith("active"));return;}
        if(args.Length==3&&args[0]=="--export-preserve-fixture"){ExportPreserveFixture(args[1],int.Parse(args[2]));return;}
        if(args.Length==2&&args[0]=="--retry-pending")
        { RunPendingChild(args[1]);return; }
        if(args.Length==2&&args[0]=="--hold-legacy")
        {
            using var held=new PhysicalCheckpointFiles(Path.Combine(args[1],"legacy")).AcquireWriter();
            File.WriteAllText(Path.Combine(args[1],"writer-ready"),"ready");
            var until=DateTime.UtcNow.AddSeconds(15);
            while(!File.Exists(Path.Combine(args[1],"writer-release"))&&DateTime.UtcNow<until)Thread.Sleep(10);
            if(!File.Exists(Path.Combine(args[1],"writer-release")))Environment.ExitCode=3;
            return;
        }
        PreserveLegacyCases();PendingConsentCases();ColdConsentApiGap();IndependentExtra();StrictRequiredFieldMatrix();Happy();FaultMatrix();Rejections();LegacyStates();RollbackAndStrictJson();QuotasAndWriterRace();ProgressionFixtures();AcquireAndProvenanceFailures();NewGameInitialization();SelectedSessionIntegration();FinalReviewBoundaries();Physical();
        Console.WriteLine($"PASS RuleMigration {checks}");
    }
    static void ColdConsentApiGap()
    {
        var f=Setup();var original=Coordinator(f);var preview=original.Inspect();
        f.WriteNumber=0;f.FailAt=1;f.FailAfter=true;
        Throws(()=>original.Confirm(Operation,NewRun,preview.Fingerprint),"confirmed intent persisted then process lost acknowledgment");f.FailAt=0;
        var fresh=Coordinator(f);var inspect=fresh.Inspect();
        Console.WriteLine("COLD_PROCESS_INSPECT kind="+inspect.Kind+" fingerprintEmpty="+(inspect.Fingerprint.Length==0)+" slotAbsent="+(inspect.Slot==null));
        var method=typeof(RuleMigrationCoordinator).GetMethod("ReadPendingConsent",Type.EmptyTypes);
        Check(method!=null,"cold process requires validated pending-consent API; Host must not parse intent or generate new UUID");
        object result=method!.Invoke(fresh,null)!;
        Check(result.GetType().GetProperty("Kind")!.GetValue(result)!.ToString()=="Retryable","cold persisted intent yields validated explicit retry summary");
    }
    static void StrictRequiredFieldMatrix()
    {
        var f=Setup();var c=Coordinator(f);var preview=c.Inspect();c.Confirm(Operation,NewRun,preview.Fingerprint);
        string old=Bytes(f.Old),slot=Bytes(f.NewSlots[NewRun]);
        var originals=f.Reg.Data.ToDictionary(p=>p.Key,p=>p.Value.ToArray());
        var failures=new List<string>();int cases=0;
        JsonObject Body(string name)=>JsonNode.Parse(Convert.FromBase64String(JsonSerializer.Deserialize<Envelope>(originals[name])!.Payload))!.AsObject();
        void Restore(){f.Reg.Data.Clear();foreach(var p in originals)f.Reg.Data[p.Key]=p.Value.ToArray();}
        void RebindIntent(byte[] intent)
        {
            f.Reg.Data["intent.json"]=intent;var selector=Body("selector.json");
            selector["IntentHash"]=Convert.ToHexString(SHA256.HashData(intent));f.Reg.Data["selector.json"]=PackedRaw(selector.ToJsonString());
        }
        void RebindBackup(byte[] backup)
        {
            f.Reg.Data["backup.json"]=backup;string hash=Convert.ToHexString(SHA256.HashData(backup));
            var intent=Body("intent.json");intent["BackupHash"]=hash;intent["SourceFingerprint"]=hash;
            byte[] encoded=PackedRaw(intent.ToJsonString());RebindIntent(encoded);
            var selector=JsonNode.Parse(Convert.FromBase64String(JsonSerializer.Deserialize<Envelope>(f.Reg.Data["selector.json"])!.Payload))!.AsObject();
            selector["SourceFingerprint"]=hash;f.Reg.Data["selector.json"]=PackedRaw(selector.ToJsonString());
        }
        void ExpectRejected(string label)
        {
            cases++;var result=c.Inspect();
            Console.WriteLine("REQUIRED_FIELD "+label+" actual="+result.Kind);
            if(result.Kind!=MigrationKind.Uncertain)failures.Add(label);
            Check(Bytes(f.Old)==old&&Bytes(f.NewSlots[NewRun])==slot&&f.Locks.Count==0,"malformed protocol read has no mutation/leaked lock");
        }
        foreach(string name in new[]{"selector.json","intent.json","backup.json"})
        {
            foreach(string field in Body(name).Select(p=>p.Key).ToArray())
            {
                Restore();var body=Body(name);body.Remove(field);byte[] changed=PackedRaw(body.ToJsonString());
                if(name=="selector.json")f.Reg.Data[name]=changed;
                else if(name=="intent.json")RebindIntent(changed);
                else RebindBackup(changed);
                ExpectRejected(name+" body missing "+field);
            }
            foreach(string field in new[]{"Payload","Hash"})
            {
                Restore();var envelope=JsonNode.Parse(originals[name])!.AsObject();envelope.Remove(field);
                byte[] changed=JsonSerializer.SerializeToUtf8Bytes(envelope);
                if(name=="selector.json")f.Reg.Data[name]=changed;
                else if(name=="intent.json")RebindIntent(changed);
                else RebindBackup(changed);
                ExpectRejected(name+" envelope missing "+field);
            }
        }
        foreach(string field in new[]{"Name","Hash","Bytes"})
        {
            Restore();var backup=Body("backup.json");backup["Files"]!.AsArray()[0]!.AsObject().Remove(field);
            RebindBackup(PackedRaw(backup.ToJsonString()));ExpectRejected("backup entry missing "+field);
        }
        Console.WriteLine("REQUIRED_FIELD_MATRIX cases="+cases+" accepted_missing="+string.Join(",",failures));
        Check(failures.Count==0,"all protocol fields mandatory, no default enum recovery: "+string.Join(",",failures));
        Restore();Check(c.Inspect().Kind==MigrationKind.Selected,"complete protocol restores valid selected authority");
    }
    static void Happy()
    {
        var f=Setup();string before=Bytes(f.Old);var c=Coordinator(f);var p=c.Inspect();
        Check(p.Kind==MigrationKind.LegacyAvailable&&p.Checkpoint!.CharacterId=="honey","validated preview");
        Check(f.Reg.Data.Count==0&&f.NewSlots.Count==0,"preview writes no intent or slot");
        var s=c.Confirm(Operation,NewRun,p.Fingerprint);
        Check(s.Kind==MigrationKind.Selected&&s.Recovery!.Kind==RecoveryKind.Active,"confirmed active");
        Check(s.Checkpoint!.RunId==Guid.ParseExact(NewRun,"N")&&s.Checkpoint.Sequence==2&&s.Checkpoint.RulesVersion==WaveRules.Version,"new identity/rules");
        var old=p.Checkpoint!;var converted=s.Checkpoint;
        Check(old.Seed==converted.Seed&&old.RandomState==converted.RandomState&&old.Wave==converted.Wave&&old.Coins==converted.Coins,"seed RNG wave wallet preserved");
        Check(Bytes(f.Old)==before,"legacy all bytes unchanged");
        string selected=Bytes(f.Reg),slot=Bytes(f.NewSlots[NewRun]);
        var retry=Coordinator(f).Confirm(Operation,NewRun,p.Fingerprint);
        Check(retry.Kind==MigrationKind.Selected&&Bytes(f.Reg)==selected&&Bytes(f.NewSlots[NewRun])==slot,"lost ack retry no duplicate writes");
        Check(Coordinator(f).Inspect().Kind==MigrationKind.Selected,"new coordinator selected");
        var codec=new CheckpointCodec(Sim());var store=new CheckpointStore(f.Slot(NewRun),Catalog,WaveRules.Version,codec.Validate);
        store.Close(NewRun,Guid.NewGuid().ToString("N"),2);
        Check(Coordinator(f).Inspect().Recovery!.Kind==RecoveryKind.Closed,"selected Closed persists no legacy revive");
        Check(Coordinator(f).Confirm(Operation,NewRun,p.Fingerprint).Recovery!.Kind==RecoveryKind.Closed,"old confirm after Closed does not restart");
        f.Reg.Data.Remove("selector.json");
        Check(Coordinator(f).Inspect().Kind==MigrationKind.Incomplete,"missing selector with Closed traces not fresh");
        Throws(()=>Coordinator(f).Confirm(Operation,NewRun,p.Fingerprint),"missing selector Closed cannot republish or resurrect");
        Check(Bytes(f.Old)==before,"closed/missing-selector old bytes intact");
        Check(f.Events.All(e=>!e.Contains("BADLOCK")),"fixed lock order throughout");
    }
    static void FaultMatrix()
    {
        // Every write boundary before and after atomic acknowledgement. No retries until fault disabled.
        var baseline=Setup();var preview=Coordinator(baseline).Inspect();
        baseline.Writes.Clear();Coordinator(baseline).Confirm(Operation,NewRun,preview.Fingerprint);
        var writes=baseline.Writes.ToArray();Check(writes.Length==6,"intent backup begin snapshot control selector six boundaries");
        for(int target=1;target<=writes.Length;target++)foreach(bool after in new[]{false,true})
        {
            var f=Setup();string old=Bytes(f.Old);var c=Coordinator(f);var p=c.Inspect();
            f.WriteNumber=0;f.FailAt=target;f.FailAfter=after;
            Throws(()=>c.Confirm(Operation,NewRun,p.Fingerprint),$"fault {target}/{after} observed");
            Check(Bytes(f.Old)==old,$"fault {target}/{after} preserves old");
            f.FailAt=0;
            var inspect=Coordinator(f).Inspect();
            Check(inspect.Kind is MigrationKind.LegacyAvailable or MigrationKind.Incomplete or MigrationKind.Selected,"fault recovery state bounded");
            if(inspect.Kind==MigrationKind.LegacyAvailable)Check(target==1&&!after,"only no intent write can return old preview");
            var completed=Coordinator(f).Confirm(Operation,NewRun,p.Fingerprint);
            Check(completed.Kind==MigrationKind.Selected&&completed.Recovery!.Control!.Sequence==2,"exact operation retry resumes once");
            Check(f.NewSlots.Count==1&&Bytes(f.Old)==old,"one slot and original unchanged after retry");
        }
    }
    static void Rejections()
    {
        var f=Setup();var c=Coordinator(f);var p=c.Inspect();
        Throws(()=>c.Confirm(Operation,OldRun,p.Fingerprint),"same UUID rejected");Check(f.Reg.Data.Count==0,"same UUID no staging");
        Throws(()=>c.Confirm(Operation.ToUpperInvariant().Replace('3','A'),NewRun,p.Fingerprint),"noncanonical uppercase operation rejected");
        f.Old.Data["unknown-user-file.txt"]=Encoding.UTF8.GetBytes("not an owned save");
        Check(c.Inspect().Kind==MigrationKind.Uncertain,"unknown old file rejects migration");f.Old.Data.Remove("unknown-user-file.txt");
        f.Old.Data["temp-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"]=new byte[]{1,2};
        Check(c.Inspect().Fingerprint==p.Fingerprint,"canonical temp excluded from backup group");
        f.FailAt=1;f.FailAfter=true;f.WriteNumber=0;Throws(()=>c.Confirm(Operation,NewRun,p.Fingerprint),"intent lostack");f.FailAt=0;
        Throws(()=>c.Confirm("44444444444444444444444444444444",NewRun,p.Fingerprint),"different operation rejects existing intent");
        Throws(()=>c.Confirm(Operation,"55555555555555555555555555555555",p.Fingerprint),"same operation different slot rejected");
        f.Old.Data["control.json"][0]^=1;
        Throws(()=>c.Confirm(Operation,NewRun,p.Fingerprint),"source changed/corrupt after consent rejects");
        Check(f.NewSlots.Count==0,"changed source not staged");
        f=Setup();c=Coordinator(f);p=c.Inspect();c.Confirm(Operation,NewRun,p.Fingerprint);
        f.Reg.Data["selector.json"][0]^=1;
        Check(Coordinator(f).Inspect().Kind==MigrationKind.Uncertain,"corrupt selector no old fallback");
        f=Setup();c=Coordinator(f);p=c.Inspect();c.Confirm(Operation,NewRun,p.Fingerprint);
        f.NewSlots[NewRun].Data["control.json"][0]^=1;
        Check(Coordinator(f).Inspect().Kind==MigrationKind.Uncertain,"selected damaged no valid old fallback");
        f=Setup();f.Reg.Data["backup.json"]=new byte[]{1};
        Check(Coordinator(f).Inspect().Kind==MigrationKind.Incomplete,"unattributed backup blocks old offer");
        Throws(()=>Coordinator(f).Confirm(Operation,NewRun,new string('A',64)),"unattributed artifacts cannot confirm");
        f=Setup();f.Reg.Data["temp-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"]=new byte[RuleMigrationCoordinator.BackupLimit+1];
        Check(Coordinator(f).Inspect().Kind==MigrationKind.Uncertain,"oversize registry stat rejects");
        f=Setup();using(var held=f.Old.AcquireWriter())Check(Coordinator(f).Inspect().Kind==MigrationKind.Uncertain,"busy legacy writer rejects");
        f=Setup();using(var held=f.Reg.AcquireWriter())Check(Coordinator(f).Inspect().Kind==MigrationKind.Uncertain,"busy registry rejects");
    }
    static void LegacyStates()
    {
        foreach(var kind in new[]{RecoveryKind.Empty,RecoveryKind.PendingStart,RecoveryKind.Closed})
        {
            var f=Setup();if(kind==RecoveryKind.Empty)f.Old.Data.Clear();else
            {
                var store=new CheckpointStore(f.Legacy,Catalog,WaveRules.LegacyVersion,(_,_)=>true);
                if(kind==RecoveryKind.Closed)store.Close(OldRun,Guid.NewGuid().ToString("N"),2);
                else store.BeginRun("44444444444444444444444444444444",Guid.NewGuid().ToString("N"),2);
            }
            Check(Coordinator(f).Inspect().Kind==MigrationKind.LegacyNotResumable,$"legacy {kind} never continue");
            Throws(()=>Coordinator(f).Confirm(Operation,NewRun,new string('A',64)),"nonresumable confirmation refuses");
        }
    }
    static void RollbackAndStrictJson()
    {
        var f=Setup();var store=new CheckpointStore(f.Legacy,Catalog,WaveRules.LegacyVersion,(_,_)=>true);
        var dto=Legacy(Sim());dto.Sequence=3;
        store.Save(OldRun,Guid.NewGuid().ToString("N"),2,JsonSerializer.SerializeToUtf8Bytes(dto));
        var latest=store.Recover();f.Old.Data[latest.Control!.Snapshots[0].Name]=new byte[]{7};
        var p=Coordinator(f).Inspect();
        Check(p.Kind==MigrationKind.LegacyAvailable&&p.Recovery!.Kind==RecoveryKind.RolledBack&&p.Checkpoint!.Sequence==2,"rollback uses actual valid descriptor identity");
        string old=Bytes(f.Old);Check(Coordinator(f).Confirm(Operation,NewRun,p.Fingerprint).Kind==MigrationKind.Selected,"rolledback migration validates prior safe point");
        Check(Bytes(f.Old)==old,"bad latest also preserved in full old group");
        var source=JsonSerializer.Serialize(Legacy(Sim()));
        foreach(string bad in new[]{source.Replace("\"SchemaVersion\":1","\"SchemaVersion\":1,\"SchemaVersion\":1"),source.Replace("\"SchemaVersion\":1,",""),source.Replace("\"Count\":0", "\"Unexpected\":0"),source.Replace("\"Count\":0,", ""),source.Replace("hmi-core-wave-start-1",WaveRules.Version)})
        {
            f=new Fixture();store=new(f.Legacy,Catalog,WaveRules.LegacyVersion,(_,_)=>true);
            store.BeginRun(OldRun,Guid.NewGuid().ToString("N"),0);store.Save(OldRun,Guid.NewGuid().ToString("N"),1,Encoding.UTF8.GetBytes(bad));
            if(bad==source)continue; // JSON property's trailing position determines whether this specific omission applies.
            Check(Coordinator(f).Inspect().Kind==MigrationKind.Uncertain,"strict old JSON rejects duplicate/missing/unknown/wrong-rule payload");
        }
    }
    static void QuotasAndWriterRace()
    {
        var f=Setup();var c=Coordinator(f);var p=c.Inspect();bool attempted=false;
        f.BeforeWrite=name=>
        {
            if(name!="registry:selector.json")return;
            attempted=true;
            Throws(()=>{using var held=f.Old.AcquireWriter();},"old writer remains locked at selector publication");
            Throws(()=>{using var held=f.Reg.AcquireWriter();},"registry writer remains locked at publication");
        };
        Check(c.Confirm(Operation,NewRun,p.Fingerprint).Kind==MigrationKind.Selected&&attempted,"source TOCTOU writer excluded through commit");
        f=Setup();p=Coordinator(f).Inspect();string old=Bytes(f.Old);
        f.BeforeWrite=_=>throw new UnauthorizedAccessException("injected denied write");
        Throws(()=>Coordinator(f).Confirm(Operation,NewRun,p.Fingerprint),"write permission failure explicit");
        Check(Bytes(f.Old)==old&&f.Locks.Count==0,"permission failure preserves old/releases all locks");
        f=Setup();f.Old.Data["snapshot-"+OldRun+"-99-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.json"]=new byte[CheckpointStore.StoreLimit];
        Check(Coordinator(f).Inspect().Kind==MigrationKind.Uncertain,"full old group quota rejects before backup");
        f=Setup();for(int i=0;i<65;i++)f.Reg.Data["temp-"+i.ToString("x32")]=Array.Empty<byte>();
        Check(Coordinator(f).Inspect().Kind==MigrationKind.Uncertain,"registry bounded entries");
        f=Setup();for(int i=1;i<=3;i++)f.Reg.Data["temp-"+i.ToString("x32")]=new byte[3*1024*1024];
        Check(Coordinator(f).Inspect().Kind==MigrationKind.Uncertain,"registry total quota");
        f=Setup();f.Slot("../not-a-slot");
        Check(Coordinator(f).Inspect().Kind==MigrationKind.Uncertain,"noncanonical injected slot rejected");
    }
    static void ProgressionFixtures()
    {
        var g=Sim();g.PrepareRun("honey",1030);int captured=0;bool contract=false;
        for(int stage=0;stage<60;stage++)
        {
            Check(g.TryCapturePrepared(new(Guid.ParseExact(OldRun,"N"),2),out var dto,out _),"progression prepared checkpoint");
            dto!.RulesVersion=WaveRules.LegacyVersion;
            if(stage is 0 or 1 or 6 || dto.PendingContractId!=null)
            {
                var f=new Fixture();var store=new CheckpointStore(f.Legacy,Catalog,WaveRules.LegacyVersion,(_,_)=>true);
                store.BeginRun(OldRun,Guid.NewGuid().ToString("N"),0);store.Save(OldRun,Guid.NewGuid().ToString("N"),1,JsonSerializer.SerializeToUtf8Bytes(dto));
                var authority=Sim();int run=authority.RunToken;long tick=authority.Tick;var phase=authority.Phase;
                var c=new RuleMigrationCoordinator(f,authority);var p=c.Inspect();Check(p.Kind==MigrationKind.LegacyAvailable,"finite/endless progression offered after validation");
                var migrated=c.Confirm(Operation,NewRun,p.Fingerprint).Checkpoint!;
                migrated.RunId=dto.RunId;migrated.Sequence=dto.Sequence;migrated.RulesVersion=dto.RulesVersion;
                Check(JsonSerializer.Serialize(migrated)==JsonSerializer.Serialize(dto),"all progression/build/ledger/temp fields preserved exactly");
                Check(authority.RunToken==run&&authority.Tick==tick&&authority.Phase==phase,"adapter never Begin/Restore/tick");
                captured++;if(dto.PendingContractId!=null){contract=true;break;}
            }
            Check(g.BeginPreparedWave(g.PreparedToken),"controlled progression Begin");g.Run.Tick=WaveRules.DurationTicks;g.World.Health[g.PlayerId.Index].Current=100;g.Step();
            if(g.Phase==RunPhase.Victory){g.Enqueue(GameCommand.ContinueEndless());g.Step();}
            Check(g.Phase==RunPhase.Between,"controlled full wave completion");
            for(int slot=0;slot<g.Capacity;slot++)if((g.World.Mask[slot]&Components.Enemy)!=0)g.Damage(slot,10000,g.PlayerId);
            var offers=g.GetOfferSlots();var choice=offers.FirstOrDefault(o=>o.Kind==OfferKind.TemporaryContract&&o.Enabled)??offers.First(o=>o.Enabled);
            g.Enqueue(GameCommand.Choose(choice.Card.Id));g.Step();Check(g.CardPicked&&g.PrepareNextWave(),"controlled legal offer/prepare");
        }
        Check(captured>=3,"finite and endless fixture coverage");
        Check(contract,"actual temporary-contract DTO migrated");
    }
    static void AcquireAndProvenanceFailures()
    {
        var f=Setup();var c=Coordinator(f);var p=c.Inspect();
        f.FailAcquireRank=2;
        Throws(()=>c.Confirm(Operation,NewRun,p.Fingerprint),"new slot lock acquisition fault");
        Check(f.Locks.Count==0&&c.Inspect().Kind==MigrationKind.Incomplete,"new lock failure releases legacy/registry and marks incomplete");
        f.FailAcquireRank=-1;Check(c.Confirm(Operation,NewRun,p.Fingerprint).Kind==MigrationKind.Selected,"exact retry after slot lock failure");
        var before=Bytes(f.Old);
        byte[] selector=f.Reg.Data["selector.json"].ToArray();
        var env=JsonSerializer.Deserialize<Envelope>(selector)!;
        var decoded=JsonSerializer.Deserialize<MigrationSelector>(Convert.FromBase64String(env.Payload))!;
        foreach(var tampered in new[]{decoded with{Slot="../legacy"},decoded with{Schema=99},decoded with{Rules=WaveRules.LegacyVersion},decoded with{IntentHash=new string('A',64)},decoded with{Operation="44444444444444444444444444444444"}})
        {
            byte[] payload=JsonSerializer.SerializeToUtf8Bytes(tampered);
            f.Reg.Data["selector.json"]=JsonSerializer.SerializeToUtf8Bytes(new Envelope(Convert.ToBase64String(payload),Convert.ToHexString(SHA256.HashData(payload))));
            Check(c.Inspect().Kind==MigrationKind.Uncertain,"checksummed selector identity/provenance/path invalid rejected");
        }
        f.Reg.Data["selector.json"]=selector;
        byte[] backup=f.Reg.Data["backup.json"].ToArray();f.Reg.Data["backup.json"][0]^=1;
        Check(c.Inspect().Kind==MigrationKind.Uncertain,"selected backup corruption failclosed, no old fallback");
        f.Reg.Data["backup.json"]=backup;
        Check(c.Inspect().Kind==MigrationKind.Selected&&Bytes(f.Old)==before,"restored test bytes recover selected without touching old");
        var slot=f.NewSlots[NewRun];var codec=new CheckpointCodec(Sim());var store=new CheckpointStore(slot,Catalog,WaveRules.Version,codec.Validate);
        store.BeginRun("55555555555555555555555555555555",Guid.NewGuid().ToString("N"),2);
        Check(c.Inspect().Recovery!.Kind==RecoveryKind.PendingStart,"selected future new game PendingStart not legacy");
        Check(c.Confirm(Operation,NewRun,p.Fingerprint).Recovery!.Kind==RecoveryKind.PendingStart,"old retry never overwrites newer selected new game");
    }
    static Fixture NonResumable(RecoveryKind kind)
    {
        var f=Setup();if(kind==RecoveryKind.Empty)f.Old.Data.Clear();else
        {
            var store=new CheckpointStore(f.Legacy,Catalog,WaveRules.LegacyVersion,(_,_)=>true);
            if(kind==RecoveryKind.Closed)store.Close(OldRun,Guid.NewGuid().ToString("N"),2);
            else store.BeginRun("44444444444444444444444444444444",Guid.NewGuid().ToString("N"),2);
        }
        f.WriteNumber=0;f.Writes.Clear();return f;
    }
    static void NewGameInitialization()
    {
        foreach(var kind in new[]{RecoveryKind.Empty,RecoveryKind.Closed,RecoveryKind.PendingStart})
        {
            var f=NonResumable(kind);var c=Coordinator(f);var p=c.Inspect();string old=Bytes(f.Old);
            var result=c.InitializeNewGame(Operation,NewRun,p.Fingerprint);
            Check(result.Kind==MigrationKind.Selected&&result.Recovery!.Kind==RecoveryKind.PendingStart&&result.Checkpoint==null,"NewGame selects pending only from "+kind);
            Check(!f.Reg.Data.ContainsKey("backup.json")&&f.NewSlots[NewRun].Data.Keys.All(n=>!n.StartsWith("snapshot-")),"NewGame does not fake legacy backup or checkpoint");
            Check(Bytes(f.Old)==old,"NewGame original old group unchanged");
            string registry=Bytes(f.Reg),slot=Bytes(f.NewSlots[NewRun]);
            Check(Coordinator(f).InitializeNewGame(Operation,NewRun,p.Fingerprint).Recovery!.Kind==RecoveryKind.PendingStart,"NewGame lostack idempotent");
            Check(Bytes(f.Reg)==registry&&Bytes(f.NewSlots[NewRun])==slot,"NewGame retry no new writes");
            Throws(()=>Coordinator(f).Confirm(Operation,NewRun,p.Fingerprint),"NewGame selector rejects Migration operation kind");
            for(int target=1;target<=3;target++)foreach(bool after in new[]{false,true})
            {
                f=NonResumable(kind);c=Coordinator(f);p=c.Inspect();old=Bytes(f.Old);f.FailAt=target;f.FailAfter=after;
                Throws(()=>c.InitializeNewGame(Operation,NewRun,p.Fingerprint),"NewGame write fault "+target+"/"+after);
                Check(Bytes(f.Old)==old&&f.Locks.Count==0,"NewGame fault leaves old/locks intact");f.FailAt=0;
                Check(Coordinator(f).InitializeNewGame(Operation,NewRun,p.Fingerprint).Kind==MigrationKind.Selected,"NewGame exact retry completes");
                Check(f.NewSlots.Count==1&&!f.Reg.Data.ContainsKey("backup.json"),"NewGame retry one slot no migration backup");
            }
        }
        var active=Setup();var preview=Coordinator(active).Inspect();
        Throws(()=>Coordinator(active).InitializeNewGame(Operation,NewRun,preview.Fingerprint),"Active cannot silently bypass migration decision");
        Check(active.Reg.Data.Count==0,"Active refusal no staging");
        active.Old.Data["control.json"][0]^=1;
        Throws(()=>Coordinator(active).InitializeNewGame(Operation,NewRun,preview.Fingerprint),"corrupt legacy cannot NewGame bypass");
        var closed=NonResumable(RecoveryKind.Closed);var p0=Coordinator(closed).Inspect();
        Coordinator(closed).InitializeNewGame(Operation,NewRun,p0.Fingerprint);
        var store2=new CheckpointStore(closed.Slot(NewRun),Catalog,WaveRules.Version,new CheckpointCodec(Sim()).Validate);
        store2.Close(NewRun,Guid.NewGuid().ToString("N"),1);closed.Reg.Data.Remove("selector.json");
        Check(Coordinator(closed).Inspect().Kind==MigrationKind.Incomplete,"NewGame missing selector Closed remains incomplete");
        Throws(()=>Coordinator(closed).InitializeNewGame(Operation,NewRun,p0.Fingerprint),"NewGame cannot republish advanced Closed after selector deletion");
    }
    static void SelectedSessionIntegration()
    {
        foreach(bool migrate in new[]{true,false})
        {
            var f=migrate?Setup():NonResumable(RecoveryKind.Closed);string original=Bytes(f.Old);
            var coordinator=Coordinator(f);var preview=coordinator.Inspect();
            var selected=migrate?coordinator.Confirm(Operation,NewRun,preview.Fingerprint):coordinator.InitializeNewGame(Operation,NewRun,preview.Fingerprint);
            string registry=Bytes(f.Reg);
            var g=Sim();var codec=new CheckpointCodec(g);var store=new CheckpointStore(f.Slot(selected.Slot!),Catalog,WaveRules.Version,codec.Validate);
            var session=new CheckpointSession(g,store,codec);long oldEpoch=session.IntentEpoch;
            Check(migrate?session.Resume(oldEpoch):session.StartNew("crit",99,oldEpoch),"selected slot normal Session resume/newgame after durable selection");
            Check(g.Phase==RunPhase.Combat&&g.Tick==0&&g.PendingSpawnCount>0,"single Begin at tick0 with rule2 authoritative markers");
            uint rng=g.Run.Random;int count=g.PendingSpawnCount;
            Check(!session.Resume(oldEpoch)&&g.Tick==0&&g.Run.Random==rng&&g.PendingSpawnCount==count,"stale session ack does not Begin twice");
            Check(session.Close(session.IntentEpoch),"normal selected Session Close");
            Check(coordinator.Inspect().Recovery!.Kind==RecoveryKind.Closed,"registry returns current closed rule2, no old fallback");
            Check(Bytes(f.Reg)==registry&&Bytes(f.Old)==original,"normal selected lifecycle never rewrites registry or legacy");
        }
    }
    static byte[] PackedRaw(string json)
    {
        byte[] payload=Encoding.UTF8.GetBytes(json);
        return JsonSerializer.SerializeToUtf8Bytes(new Envelope(Convert.ToBase64String(payload),Convert.ToHexString(SHA256.HashData(payload))));
    }
    static void FinalReviewBoundaries()
    {
        var f=Setup();var c=Coordinator(f);var p=c.Inspect();f.WriteNumber=0;f.FailAt=6;f.FailAfter=true;
        Throws(()=>c.Confirm(Operation,NewRun,p.Fingerprint),"selector committed lostack setup");f.FailAt=0;
        var oldStore=new CheckpointStore(f.Legacy,Catalog,WaveRules.LegacyVersion,(_,_)=>true);
        oldStore.BeginRun("77777777777777777777777777777777",Guid.NewGuid().ToString("N"),2);
        string changedOld=Bytes(f.Old);
        Check(c.Confirm(Operation,NewRun,p.Fingerprint).Kind==MigrationKind.Selected,"selected lostack retry ignores subsequently changed legacy source");
        Check(Bytes(f.Old)==changedOld,"retry does not touch newer old-binary run");
        Throws(()=>c.InitializeNewGame(Operation,NewRun,p.Fingerprint),"same operation different Kind rejected against Migration selector");
        byte[] originalSelector=f.Reg.Data["selector.json"].ToArray();
        var selector=JsonSerializer.Deserialize<MigrationSelector>(Convert.FromBase64String(JsonSerializer.Deserialize<Envelope>(originalSelector)!.Payload))!;
        foreach(string raw in new[]{"null","{}",JsonSerializer.Serialize(selector).Replace("\"Schema\":2","\"Schema\":2,\"Schema\":2")})
        {
            f.Reg.Data["selector.json"]=PackedRaw(raw);
            Check(c.Inspect().Kind==MigrationKind.Uncertain,"checksummed null/missing/duplicate selector failclosed");
        }
        f.Reg.Data["selector.json"]=originalSelector;
        var originalIntent=JsonSerializer.Deserialize<MigrationIntent>(Convert.FromBase64String(JsonSerializer.Deserialize<Envelope>(f.Reg.Data["intent.json"])!.Payload))!;
        byte[] nullBackup=PackedRaw("null");string backupHash=Convert.ToHexString(SHA256.HashData(nullBackup));
        var changedIntent=originalIntent with{BackupHash=backupHash,SourceFingerprint=backupHash};
        byte[] intentBytes=PackedRaw(JsonSerializer.Serialize(changedIntent));
        f.Reg.Data["intent.json"]=intentBytes;f.Reg.Data["backup.json"]=nullBackup;
        f.Reg.Data["selector.json"]=PackedRaw(JsonSerializer.Serialize(selector with{SourceFingerprint=backupHash,IntentHash=Convert.ToHexString(SHA256.HashData(intentBytes))}));
        Check(c.Inspect().Kind==MigrationKind.Uncertain,"fully checksummed null backup body rejected without crash");
    }
    static void Physical()
    {
        string root=Path.Combine(Path.GetTempPath(),"hmi-rule-migration-"+Guid.NewGuid().ToString("N"));
        try
        {
            var f=new PhysicalRuleMigrationFiles(Path.Combine(root,"registry"),Path.Combine(root,"legacy"));
            var dto=Legacy(Sim());var store=new CheckpointStore(f.Legacy,Catalog,WaveRules.LegacyVersion,(_,_)=>true);
            store.BeginRun(OldRun,Guid.NewGuid().ToString("N"),0);store.Save(OldRun,Guid.NewGuid().ToString("N"),1,JsonSerializer.SerializeToUtf8Bytes(dto));
            var old=Directory.GetFiles(Path.Combine(root,"legacy")).ToDictionary(p=>Path.GetFileName(p)!,File.ReadAllBytes);
            var p=Coordinator(f).Inspect();Check(p.Kind==MigrationKind.LegacyAvailable,"physical preview");
            var start=new System.Diagnostics.ProcessStartInfo("dotnet"){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};
            start.ArgumentList.Add(typeof(Program).Assembly.Location);start.ArgumentList.Add("--hold-legacy");start.ArgumentList.Add(root);
            using(var child=System.Diagnostics.Process.Start(start)!)
            {
                try
                {
                    Check(SpinWait.SpinUntil(()=>File.Exists(Path.Combine(root,"writer-ready"))||child.HasExited,TimeSpan.FromSeconds(8))&&!child.HasExited,"real second process holds legacy writer");
                    Check(Coordinator(f).Inspect().Kind==MigrationKind.Uncertain,"separate-process old writer blocks preview");
                    Throws(()=>Coordinator(f).Confirm(Operation,NewRun,p.Fingerprint),"separate-process old writer blocks confirmation without deadlock");
                }
                finally
                {
                    File.WriteAllText(Path.Combine(root,"writer-release"),"release");
                    if(!child.WaitForExit(8000))child.Kill(true);
                }
                Check(child.ExitCode==0,"separate-process writer normal release");
            }
            Check(Coordinator(f).Confirm(Operation,NewRun,p.Fingerprint).Kind==MigrationKind.Selected,"physical transaction");
            foreach(var file in old)Check(File.ReadAllBytes(Path.Combine(root,"legacy",file.Key!)).SequenceEqual(file.Value),"physical old bytes unchanged");
            Check(Coordinator(new PhysicalRuleMigrationFiles(Path.Combine(root,"registry"),Path.Combine(root,"legacy"))).Inspect().Kind==MigrationKind.Selected,"fresh physical instance selects committed");
            Throws(()=>f.Slot("../escape"),"slot traversal rejected");
            Throws(()=>new PhysicalRuleMigrationFiles(Path.Combine(root,"legacy","nested"),Path.Combine(root,"legacy")),"nested registry rejected");
            string symbolic=Path.Combine(root,"symbolic");Directory.CreateSymbolicLink(symbolic,Path.Combine(root,"legacy"));
            Throws(()=>new PhysicalRuleMigrationFiles(Path.Combine(root,"other"),symbolic),"symbolic old root rejected");
            Directory.CreateDirectory(Path.Combine(root,"legacy","unknown-subdirectory"));
            var freshRegistry=new PhysicalRuleMigrationFiles(Path.Combine(root,"fresh-registry"),Path.Combine(root,"legacy"));
            Check(Coordinator(freshRegistry).Inspect().Kind==MigrationKind.Uncertain,"unknown legacy directory rejects without traversing");
            string sparse=Path.Combine(root,"sparse");Directory.CreateDirectory(sparse);
            using(var stream=File.Create(Path.Combine(sparse,"selector.json")))stream.SetLength(100L*1024*1024);
            long allocated=GC.GetAllocatedBytesForCurrentThread();
            Check(Coordinator(new PhysicalRuleMigrationFiles(sparse,Path.Combine(root,"legacy"))).Inspect().Kind==MigrationKind.Uncertain,"100MB sparse selector bounded rejection");
            Check(GC.GetAllocatedBytesForCurrentThread()-allocated<1024*1024,"sparse selector rejected without full allocation");
        }
        finally{if(Directory.Exists(root))Directory.Delete(root,true);}
    }
    sealed class Fixture : IRuleMigrationFiles
    {
        public readonly MemoryFiles Reg,Old;public readonly Dictionary<string,MemoryFiles> NewSlots=new();
        public readonly List<string> Events=new(),Writes=new();public readonly List<int> Locks=new();
        public int WriteNumber,FailAt,FailAcquireRank=-1;public bool FailAfter;public Action<string>? BeforeWrite;
        public Fixture(){Reg=new(this,"registry",0);Old=new(this,"legacy",1);}
        public ICheckpointFiles Registry=>Reg;public ICheckpointFiles Legacy=>Old;
        public string[] Slots()=>NewSlots.Keys.ToArray();
        public ICheckpointFiles Slot(string id){if(!NewSlots.TryGetValue(id,out var p))NewSlots[id]=p=new(this,"slot",2);return p;}
    }
    sealed class MemoryFiles : ICheckpointFiles
    {
        readonly Fixture owner;readonly string name;readonly int rank;bool locked;
        public readonly Dictionary<string,byte[]> Data=new(StringComparer.Ordinal);
        public MemoryFiles(Fixture owner,string name,int rank){this.owner=owner;this.name=name;this.rank=rank;}
        sealed class Lease : IDisposable{readonly Action action;public Lease(Action action)=>this.action=action;public void Dispose()=>action();}
        public IDisposable AcquireWriter()
        {
            if(owner.FailAcquireRank==rank)throw new IOException("injected acquire failure");
            if(locked)throw new IOException("busy writer");
            if(owner.Locks.Count>0&&owner.Locks[^1]>=rank){owner.Events.Add("BADLOCK");throw new IOException("reverse or nested lock");}
            locked=true;owner.Locks.Add(rank);owner.Events.Add("acquire:"+name);
            return new Lease(()=>{Check(owner.Locks[^1]==rank,"LIFO lock release");owner.Locks.RemoveAt(owner.Locks.Count-1);locked=false;owner.Events.Add("release:"+name);});
        }
        public string[] Names()=>Data.Keys.ToArray();
        public long Length(string n)=>Data.TryGetValue(n,out var b)?b.Length:throw new FileNotFoundException();
        public byte[] Read(string n,int max){if(!Data.TryGetValue(n,out var b))throw new FileNotFoundException();if(b.Length>max)throw new IOException("readlimit");return b.ToArray();}
        public void AtomicWrite(string n,byte[] b,bool overwrite)
        {
            if(!locked)throw new IOException("write without lock");
            owner.BeforeWrite?.Invoke(name+":"+n);owner.Writes.Add(name+":"+n);int index=++owner.WriteNumber;
            if(owner.FailAt==index&&!owner.FailAfter)throw new IOException("injected before write");
            if(Data.ContainsKey(n)&&!overwrite)throw new IOException("immutable exists");Data[n]=b.ToArray();
            if(owner.FailAt==index&&owner.FailAfter)throw new IOException("injected lost ack");
        }
        public void Delete(string n){if(!locked)throw new IOException("delete without lock");Data.Remove(n);}
    }
}
