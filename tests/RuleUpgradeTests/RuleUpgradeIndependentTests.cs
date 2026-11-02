using Hmi.Core;
using Hmi.Adapters.Persistence;
using System.Text.Json;
using System.Text.Json.Nodes;

static partial class Program
{
    // Reviewer-owned adversarial cases. Generated in-memory stores only, never user files.
    static Fixture IndependentIntentOnly(bool startNew=false)
    {
        var f=Setup();var c=Coordinator(f);var v=c.InspectUpgrade();
        f.FailAt=1;f.FailAfter=true;
        Throws(()=>c.ConfirmUpgrade(Operation,NewRun,v.Fingerprint,startNew),"independent intent durable ack lost");
        f.FailAt=0;
        Check(f.Reg.Data.ContainsKey("upgrade-intent.json")&&!f.Reg.Data.ContainsKey("upgrade-backup.json")&&!f.NewSlots.ContainsKey(NewRun),"independent setup is exact intent-only, no target");
        return f;
    }
    static void IndependentCases()
    {
        // Both operation types can write their FIRST backup only at exact intent-only stage.
        foreach(bool startNew in new[]{false,true})
        {
            var f=IndependentIntentOnly(startNew);var c=Coordinator(f);var pending=c.InspectUpgrade();
            Check(pending.Kind==RuleUpgradeKind.Incomplete&&pending.Pending!=null&&pending.Pending.StartNew==startNew,"independent cold pending preserves explicit kind");
            int writes=f.WriteNumber;var other=Coordinator(f);
            Throws(()=>other.RetryUpgrade(pending.Pending!),"independent receipt is issuer-bound");
            Check(f.WriteNumber==writes,"independent foreign receipt rejection writes nothing");
            var selected=c.RetryUpgrade(pending.Pending!);
            Check(selected.Kind==RuleUpgradeKind.CurrentSelected&&selected.Recovery!.Kind==(startNew?RecoveryKind.PendingStart:RecoveryKind.Active),"independent exact intent-only retry completes original operation");
        }
        // A target directory itself is evidence of staging. Even empty or merely temporary
        // target content must never permit regeneration of a missing immutable backup.
        foreach(bool temp in new[]{false,true})
        {
            var f=IndependentIntentOnly();var target=(MemoryFiles)f.Slot(NewRun);
            if(temp)target.Data["temp-55555555555555555555555555555555"]=new byte[]{1};
            string source=Bytes(f.NewSlots[OldSlot]);int writes=f.WriteNumber;
            var c=Coordinator(f);var v=c.InspectUpgrade();
            Check(v.Kind==RuleUpgradeKind.Blocked&&v.Pending==null,"independent target footprint without backup fails closed");
            Check(Bytes(f.NewSlots[OldSlot])==source&&f.WriteNumber==writes,"independent blocked target footprint neither repairs nor writes");
        }
        // A source changed after showing valid pending consent cannot be upgraded on retry.
        {
            var f=IndependentIntentOnly();var c=Coordinator(f);var view=c.InspectUpgrade();
            var store=new CheckpointStore(f.NewSlots[OldSlot],Catalog,WaveRules.PriorVersion,(_,_)=>true);
            store.Close(OldRun,Guid.NewGuid().ToString("N"),2);string bytes=Bytes(f.NewSlots[OldSlot]);int writes=f.WriteNumber;
            Throws(()=>c.RetryUpgrade(view.Pending!),"independent actual source Close invalidates pending receipt");
            Check(Bytes(f.NewSlots[OldSlot])==bytes&&f.WriteNumber==writes&&!f.NewSlots.ContainsKey(NewRun),"independent stale receipt cannot stage target or modify newly Closed source");
        }
        // Protocol omission/unknown/duplicate must fail even with a correctly recomputed
        // transport hash; a checksum alone is not structural validation.
        var prototype=IndependentIntentOnly();var raw=prototype.Reg.Data["upgrade-intent.json"];
        var envelope=JsonNode.Parse(raw)!.AsObject();var body=JsonNode.Parse(Convert.FromBase64String(envelope["Payload"]!.GetValue<string>()))!.AsObject();
        foreach(string field in body.Select(p=>p.Key).ToArray())
        {
            var f=IndependentIntentOnly();var copy=body.DeepClone().AsObject();copy.Remove(field);f.Reg.Data["upgrade-intent.json"]=Pack(copy);
            int writes=f.WriteNumber;var v=Coordinator(f).InspectUpgrade();
            Check(v.Kind==RuleUpgradeKind.Blocked&&v.Pending==null&&writes==f.WriteNumber,"independent missing required upgrade field "+field);
        }
        {
            var f=IndependentIntentOnly();var copy=body.DeepClone().AsObject();copy["FutureAuthority"]=true;f.Reg.Data["upgrade-intent.json"]=Pack(copy);
            Check(Coordinator(f).InspectUpgrade().Kind==RuleUpgradeKind.Blocked,"independent unknown hashed upgrade field blocked");
        }
        foreach(bool envelopeDuplicate in new[]{false,true})
        {
            var f=IndependentIntentOnly();byte[] bad;
            if(envelopeDuplicate)
            {
                string original=System.Text.Encoding.UTF8.GetString(raw);
                bad=System.Text.Encoding.UTF8.GetBytes(original[..^1]+",\"Hash\":"+JsonSerializer.Serialize(envelope["Hash"]!.GetValue<string>())+"}");
            }
            else
            {
                var first=body.First();string json=body.ToJsonString();
                byte[] payload=System.Text.Encoding.UTF8.GetBytes(json[..^1]+","+JsonSerializer.Serialize(first.Key)+":"+first.Value!.ToJsonString()+"}");
                bad=JsonSerializer.SerializeToUtf8Bytes(new{Payload=Convert.ToBase64String(payload),Hash=Hash(payload)});
            }
            f.Reg.Data["upgrade-intent.json"]=bad;int writes=f.WriteNumber;
            var v=Coordinator(f).InspectUpgrade();
            Check(v.Kind==RuleUpgradeKind.Blocked&&v.Pending==null&&writes==f.WriteNumber,"independent duplicate hashed protocol field blocked (envelope="+envelopeDuplicate+")");
        }
        foreach(bool registry in new[]{false,true})
        {
            var f=Setup();var port=registry?f.Reg:f.NewSlots[OldSlot];
            port.Data["temp-55555555555555555555555555555555"]=new byte[]{4,5,6};
            int writes=f.WriteNumber;string original=Bytes(port);var v=Coordinator(f).InspectUpgrade();
            Check(v.Kind==RuleUpgradeKind.Blocked&&v.Pending==null,"independent unattributed source/registry temp blocks upgrade (registry="+registry+")");
            Check(writes==f.WriteNumber&&original==Bytes(port),"independent unknown temp retained without cleanup");
        }
        // Slot UUID is NOT current run UUID. Synthetic retained bytes separately test
        // archive identity, not the author's real cleanup-failure mechanism fixture.
        foreach(bool useSlotAsRun in new[]{false,true})
        {
            var f=Setup(status:StoreStatus.Closed);var old=f.NewSlots[OldSlot];
            string run=useSlotAsRun?OldSlot:OldRun;
            string name="snapshot-"+run+"-2-55555555555555555555555555555555.json";
            old.Data[name]=JsonSerializer.SerializeToUtf8Bytes(Dto(WaveRules.PriorVersion,run));
            string original=Bytes(old);int writes=f.WriteNumber;var c=Coordinator(f);var v=c.InspectUpgrade();
            Check(v.Kind==(useSlotAsRun?RuleUpgradeKind.Blocked:RuleUpgradeKind.PriorNotResumable)&&v.Checkpoint==null,"independent closed archive ownership is run, not containing slot");
            Check(original==Bytes(old)&&writes==f.WriteNumber,"independent closed inspection keeps all bytes without recovering payload");
            if(!useSlotAsRun)
            {
                Throws(()=>c.ConfirmUpgrade(Operation,NewRun,v.Fingerprint,false),"independent Closed archive never upgrades resumed payload");
                Check(writes==f.WriteNumber,"independent invalid Closed resume has no writes");
                var selected=c.ConfirmUpgrade(Operation,NewRun,v.Fingerprint,true);
                Check(selected.Kind==RuleUpgradeKind.CurrentSelected&&selected.Recovery!.Kind==RecoveryKind.PendingStart&&selected.Checkpoint==null,"independent owned Closed archive only permits explicit new PendingStart");
                Check(original==Bytes(old)&&f.Reg.Data.ContainsKey("upgrade-backup.json"),"independent preserved closed bytes backed up and unchanged");
            }
        }
        // Committed selector wins over subsequent source damage and over original migration
        // Active2 expectation: current target Closed is preserved without replay/repair.
        {
            var f=Setup();var c=Coordinator(f);var v=c.InspectUpgrade();c.ConfirmUpgrade(Operation,NewRun,v.Fingerprint,false);
            f.NewSlots[OldSlot].Data["control.json"]=new byte[]{7,8,9};
            var g=Sim();var codec=new CheckpointCodec(g);var store=new CheckpointStore(f.Slot(NewRun),Catalog,WaveRules.Version,codec.Validate);
            store.Close(NewRun,Guid.NewGuid().ToString("N"),2);
            int writes=f.WriteNumber;string all=Bytes(f.Reg)+Bytes(f.NewSlots[NewRun]);
            var cold=Coordinator(f).InspectUpgrade();
            Check(cold.Kind==RuleUpgradeKind.CurrentSelected&&cold.Recovery!.Kind==RecoveryKind.Closed&&cold.Checkpoint==null,"independent selected Closed never resurrects changed old source");
            Check(c.ConfirmUpgrade(Operation,NewRun,v.Fingerprint,false).Recovery!.Kind==RecoveryKind.Closed,"independent duplicate lostack confirm honors current Closed");
            Check(writes==f.WriteNumber&&all==Bytes(f.Reg)+Bytes(f.NewSlots[NewRun]),"independent selected reinspection performs no repair writes");
        }
        Console.WriteLine("INDEPENDENT_RULE_UPGRADE_CASES_DONE");
    }
}
