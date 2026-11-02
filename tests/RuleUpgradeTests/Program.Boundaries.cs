using Hmi.Core;
using Hmi.Adapters.Persistence;
using System.Text.Json;
using System.Text.Json.Nodes;

static partial class Program
{
    static void AdvancedConversionCases()
    {
        var content=JsonSerializer.Deserialize<GameContent>(Content,new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
        foreach(var hero in content.Characters)
        {
            // Synthetic but fully validated endless safe point; not natural survival evidence.
            var capture=Sim();capture.PrepareRun(hero.Id,1030);
            Check(capture.TryCapturePrepared(new(Guid.ParseExact(OldRun,"N"),2),out var dto,out _),"all hero capture");
            var d=dto!;d.RulesVersion=WaveRules.PriorVersion;d.Wave=20;d.IsEndless=true;d.FiniteCompleted=true;d.EndlessWavesCompleted=13;
            d.FinitePicks=Enumerable.Repeat("base.health",5).ToArray();d.RandomState=1234567;
            foreach(var card in d.Cards)if(card.Id is "base.health" or "base.speed" or "base.range")card.Count=5;
            foreach(var item in d.Items)if(item.Id is "shield" or "tonic" or "shoes")item.Count=1;
            d.Coins=100;d.Earned=110;d.Spent=10;d.Kills=10;d.PendingContractId="temp.base.rate";
            var g=Sim();Check(g.TryValidateArchivedCheckpoint(d,new(d.RunId,d.Sequence),out var validation),"advanced prior valid: "+validation);
            byte[] old=JsonSerializer.SerializeToUtf8Bytes(d);
            Check(g.TryConvertPriorPreparedCheckpoint(d,new(d.RunId,d.Sequence),new(Guid.ParseExact(NewRun,"N"),2),out var converted,out _),"advanced conversion");
            Check(old.SequenceEqual(JsonSerializer.SerializeToUtf8Bytes(d)),"advanced source unchanged");
            var a=Sim();var b=Sim();var identity=new CheckpointIdentity(Guid.ParseExact(NewRun,"N"),2);
            Check(a.TryRestorePrepared(converted!,identity,out _)&&b.TryRestorePrepared(converted!,identity,out _),"advanced double independent restore");
            var token=a.PreparedToken;Check(a.BeginPreparedWave(token)&&b.BeginPreparedWave(b.PreparedToken),"canonical Begin consumes pending once");
            float hp=a.GetStat(Stat.MaxHealth),rate=a.GetStat(Stat.BasicRate);
            Check(!a.BeginPreparedWave(token)&&a.GetStat(Stat.MaxHealth)==hp&&a.GetStat(Stat.BasicRate)==rate,"duplicate Begin cannot compound contract/items");
            Check(a.Coins==100&&a.Earned==110&&a.Spent==10&&a.Wave==20,"advanced economy/progression retained");
            for(int tick=0;tick<90;tick++){a.Step();b.Step();}
            Check(a.Coins==b.Coins&&a.Tick==b.Tick&&a.GetStat(Stat.MaxHealth)==b.GetStat(Stat.MaxHealth)&&a.ProjectileCount==b.ProjectileCount,"same converted pre-init state replays equal public fields");
        }
    }
    static void NonresumableOwnershipCases()
    {
        foreach(bool legacy in new[]{false,true})
        {
            string rules=legacy?WaveRules.LegacyVersion:WaveRules.PriorVersion;
            var closed=Setup(legacy);var port=legacy?closed.Old:closed.NewSlots[OldSlot];
            var store=new CheckpointStore(port,Catalog,rules,(_,_)=>true);
            closed.ThrowDelete=true;store.Close(OldRun,Guid.NewGuid().ToString("N"),2);closed.ThrowDelete=false;
            Check(port.Data.Keys.Any(n=>n.StartsWith("snapshot-",StringComparison.Ordinal)),"real Store Close cleanup IOException leaves snapshot");
            string bytes=Bytes(port);var c=Coordinator(closed);var view=c.InspectUpgrade();
            Check(view.Kind!=RuleUpgradeKind.Blocked&&view.Recovery!.Kind==RecoveryKind.Closed&&view.Checkpoint==null,"Closed leftover validated only as owned archive");
            Throws(()=>c.ConfirmUpgrade(Operation,NewRun,view.Fingerprint,false),"Closed leftover never resumes");
            var selected=c.ConfirmUpgrade(Operation,NewRun,view.Fingerprint,true);
            Check(selected.Recovery!.Kind==RecoveryKind.PendingStart&&selected.Checkpoint==null&&Bytes(port)==bytes,"Closed explicit new preserves every byte without gameplay inheritance");

            var pending=Setup(legacy,StoreStatus.PendingStart);var pp=legacy?pending.Old:pending.NewSlots[OldSlot];
            var ps=new CheckpointStore(pp,Catalog,rules,(_,_)=>true);pending.FailAt=2;pending.FailAfter=false;
            Throws(()=>ps.Save(OldRun,Guid.NewGuid().ToString("N"),1,JsonSerializer.SerializeToUtf8Bytes(Dto(rules))),"real Save snapshot commit then control failure leaves same-run pending bytes");pending.FailAt=0;
            var pc=Coordinator(pending);var pv=pc.InspectUpgrade();
            Check(pv.Kind!=RuleUpgradeKind.Blocked&&pv.Checkpoint==null&&pv.Recovery!.Kind==RecoveryKind.PendingStart,"pending same-run nextseq verified but not used as checkpoint");
            string original=Bytes(pp);Check(pc.ConfirmUpgrade(Operation,NewRun,pv.Fingerprint,true).Recovery!.Kind==RecoveryKind.PendingStart&&Bytes(pp)==original,"pending owned snapshot preserved in independent new game");

            var previous=Setup(legacy);var prev=legacy?previous.Old:previous.NewSlots[OldSlot];var prior=new CheckpointStore(prev,Catalog,rules,(_,_)=>true);
            previous.ThrowDelete=true;prior.BeginRun(Guid.NewGuid().ToString("N"),Guid.NewGuid().ToString("N"),2);previous.ThrowDelete=false;
            Check(prev.Data.Keys.Any(n=>n.StartsWith("snapshot-",StringComparison.Ordinal)),"real BeginRun cleanup IOException leaves old-run bytes");
            Check(Coordinator(previous).InspectUpgrade().Kind==RuleUpgradeKind.Blocked,"previous run lacks current-control provenance; conservative block without deletion");
        }
        foreach(string corruption in new[]{"run","sequence","payload","dtoRun","dtoSequence","dtoRules","dtoCatalog"})
        {
            var bad=Setup();bad.ThrowDelete=true;new CheckpointStore(bad.NewSlots[OldSlot],Catalog,WaveRules.PriorVersion,(_,_)=>true).Close(OldRun,Guid.NewGuid().ToString("N"),2);bad.ThrowDelete=false;
            var p=bad.NewSlots[OldSlot];string name=p.Data.Keys.Single(n=>n.StartsWith("snapshot-",StringComparison.Ordinal));byte[] bytes=p.Data[name];p.Data.Remove(name);
            if(corruption=="run")name=name.Replace(OldRun,OldSlot,StringComparison.Ordinal);
            else if(corruption=="sequence")name=name.Replace("-2-","-99-",StringComparison.Ordinal);
            else if(corruption=="payload")bytes=new byte[]{1};
            else
            {
                var d=JsonSerializer.Deserialize<WaveCheckpointDto>(bytes)!;
                if(corruption=="dtoRun")d.RunId=Guid.ParseExact(OldSlot,"N");
                if(corruption=="dtoSequence")d.Sequence++;
                if(corruption=="dtoRules")d.RulesVersion=WaveRules.LegacyVersion;
                if(corruption=="dtoCatalog")d.CatalogHash=new string('a',64);
                bytes=JsonSerializer.SerializeToUtf8Bytes(d);
            }
            p.Data[name]=bytes;
            Check(Coordinator(bad).InspectUpgrade().Kind==RuleUpgradeKind.Blocked,"unknown Closed leftover blocks: "+corruption);
        }
    }
    static void BoundaryCases()
    {
        foreach(bool registry in new[]{false,true})
        {
            var unknown=Setup();var port=registry?unknown.Reg:unknown.NewSlots[OldSlot];
            port.Data["temp-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"]=new byte[]{1};
            Check(Coordinator(unknown).InspectUpgrade().Kind==RuleUpgradeKind.Blocked,"unknown registry/source temporary artifact blocks without deletion");
            Check(port.Data.ContainsKey("temp-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")&&unknown.WriteNumber==0,"unknown temp retained");
        }
        var activeOrphan=Setup();
        activeOrphan.NewSlots[OldSlot].Data[$"snapshot-{OldRun}-1-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.json"]=new byte[]{1};
        Check(Coordinator(activeOrphan).InspectUpgrade().Kind==RuleUpgradeKind.Blocked,"Active unreferenced snapshot blocks without repair");
        // Genuine latest committed descriptor is damaged: only prior descriptor seq2 is converted.
        var f=Setup();var old=f.NewSlots[OldSlot];var oldStore=new CheckpointStore(old,Catalog,WaveRules.PriorVersion,(_,_)=>true);
        oldStore.Save(OldRun,Guid.NewGuid().ToString("N"),2,JsonSerializer.SerializeToUtf8Bytes(Dto(WaveRules.PriorVersion,OldRun,3)));
        var latest=oldStore.Recover().Control!.Snapshots[0];old.Data[latest.Name]=new byte[]{0};
        var c=Coordinator(f);var view=c.InspectUpgrade();
        Check(view.Kind==RuleUpgradeKind.PriorAvailable&&view.Recovery!.Kind==RecoveryKind.RolledBack&&view.Checkpoint!.Sequence==2,"fallback actual descriptor sequence exposed");
        Check(c.ConfirmUpgrade(Operation,NewRun,view.Fingerprint,false).Kind==RuleUpgradeKind.CurrentSelected,"fallback safe point explicitly converts, not latest corrupt descriptor");
        // Strict unknown target artifacts cannot ride through the existing store cleanup path.
        foreach(string artifact in new[]{"temp-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","snapshot-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa-2-bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb.json",
            "snapshot-22222222222222222222222222222222-3-bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb.json","snapshot-22222222222222222222222222222222-2-bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb.json","unexpected.bin"})
        {
            var bad=Setup();var bc=Coordinator(bad);var bv=bc.InspectUpgrade();bad.FailAt=3;bad.FailAfter=true;
            Throws(()=>bc.ConfirmUpgrade(Operation,NewRun,bv.Fingerprint,false),"create attributed PendingStart");bad.FailAt=0;
            bad.NewSlots[NewRun].Data[artifact]=new byte[]{9};int writes=bad.WriteNumber;
            Check(Coordinator(bad).InspectUpgrade().Kind==RuleUpgradeKind.Blocked,"unknown target artifact blocks: "+artifact);
            Throws(()=>bc.ConfirmUpgrade(Operation,NewRun,bv.Fingerprint,false),"unknown target cannot be cleaned on retry");
            Check(bad.WriteNumber==writes&&bad.NewSlots[NewRun].Data.ContainsKey(artifact),"unknown bytes retained, no cleanup/write");
        }
        var quota=Setup(kind:SlotInitializationKind.PreserveLegacyNewGame);
        var entries=Enumerable.Range(0,6).Select(i=>new MigrationBackupFile($"snapshot-{OldRun}-2-{i+1:x32}.json",Hash(new byte[230000]),new byte[230000])).ToArray();
        byte[] backup=Pack(new MigrationBackup(1,entries));
        var oldIntent=JsonSerializer.Deserialize<MigrationIntent>(Convert.FromBase64String(JsonSerializer.Deserialize<Envelope>(quota.Reg.Data["intent.json"])!.Payload))!;
        var updated=oldIntent with {BackupHash=Hash(backup),SourceFingerprint=Hash(backup)};byte[] ib=Pack(updated);
        quota.Reg.Data["backup.json"]=backup;quota.Reg.Data["intent.json"]=ib;
        quota.Reg.Data["selector.json"]=Pack(new MigrationSelector(3,updated.Kind,updated.Operation,updated.Run,updated.Slot,updated.SourceFingerprint,Hash(ib),Catalog,WaveRules.PriorVersion));
        string unchanged=Bytes(quota.Reg);var q=Coordinator(quota).InspectUpgrade();
        Check(q.Kind==RuleUpgradeKind.Blocked&&q.Detail.Contains("quota",StringComparison.OrdinalIgnoreCase),"encoded nested preservation backup respects unchanged quota");
        Check(Bytes(quota.Reg)==unchanged&&quota.WriteNumber==0,"quota blocks without deleting old backup");
        // Known unsupported catalog/rules do not turn into an available source.
        foreach(string field in new[]{"Catalog","Rules"})
        {
            var bad=Setup();var body=JsonNode.Parse(Convert.FromBase64String(JsonSerializer.Deserialize<Envelope>(bad.Reg.Data["selector.json"])!.Payload))!.AsObject();
            body[field]="unsupported";bad.Reg.Data["selector.json"]=Pack(body);
            Check(Coordinator(bad).InspectUpgrade().Kind==RuleUpgradeKind.Blocked,"unsupported metadata stays fail-closed");
        }
        var conflict=Setup();var cc=Coordinator(conflict);var cv=cc.InspectUpgrade();
        using(var held=conflict.NewSlots[OldSlot].AcquireWriter())
            Throws(()=>cc.ConfirmUpgrade(Operation,NewRun,cv.Fingerprint,false),"busy source lock blocks transaction before writes");
        Check(conflict.WriteNumber==0,"busy writer no staged authority");
    }
}
