using Hmi.Core;
using Hmi.Adapters.Persistence;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text;
using System.Security.Cryptography;
static partial class Program
{
 static JsonObject Body(byte[] raw)=>JsonNode.Parse(Convert.FromBase64String(JsonSerializer.Deserialize<Envelope>(raw)!.Payload))!.AsObject();
 static void RehashDependencies(Fixture f,string changed)
 {
  if(changed=="selector.json")return;
  var selector=Body(f.Reg.Data["selector.json"]);
  if(changed=="backup.json")
  {
   string hash=Convert.ToHexString(SHA256.HashData(f.Reg.Data[changed]));var intent=Body(f.Reg.Data["intent.json"]);
   intent["BackupHash"]=hash;intent["SourceFingerprint"]=hash;selector["SourceFingerprint"]=hash;f.Reg.Data["intent.json"]=PackedRaw(intent.ToJsonString());
  }
  selector["IntentHash"]=Convert.ToHexString(SHA256.HashData(f.Reg.Data["intent.json"]));f.Reg.Data["selector.json"]=PackedRaw(selector.ToJsonString());
 }
 static void IndependentExtra()
 {
  int before=checks;var defects=new List<string>();
  foreach(string filename in new[]{"selector.json","intent.json"})
  {
   var f=Setup();var c=Coordinator(f);var preview=c.Inspect();c.Confirm(Operation,NewRun,preview.Fingerprint);
   var body=Body(f.Reg.Data[filename]);body.Remove("Kind");f.Reg.Data[filename]=PackedRaw(body.ToJsonString());
   if(filename=="intent.json") {var selector=Body(f.Reg.Data["selector.json"]);selector["IntentHash"]=Convert.ToHexString(SHA256.HashData(f.Reg.Data[filename]));f.Reg.Data["selector.json"]=PackedRaw(selector.ToJsonString());}
   var inspected=c.Inspect();Console.WriteLine("INDEPENDENT_MISSING_KIND file="+filename+" actual="+inspected.Kind);
   if(inspected.Kind!=MigrationKind.Uncertain)defects.Add(filename+" missingKind accepted");
  }
  foreach(string file in new[]{"intent.json","selector.json","backup.json"})
  {
   var baseline=Setup();var bc=Coordinator(baseline);var bp=bc.Inspect();bc.Confirm(Operation,NewRun,bp.Fingerprint);
   foreach(string property in Body(baseline.Reg.Data[file]).Select(k=>k.Key).ToArray())
   {
    var f=Setup();var c=Coordinator(f);var p=c.Inspect();c.Confirm(Operation,NewRun,p.Fingerprint);
    var body=Body(f.Reg.Data[file]);body.Remove(property);f.Reg.Data[file]=PackedRaw(body.ToJsonString());RehashDependencies(f,file);
    var result=c.Inspect();Console.WriteLine($"INDEPENDENT_FIELD file={file} missing={property} actual={result.Kind}");
    if(result.Kind!=MigrationKind.Uncertain)defects.Add(file+" missing "+property);
   }
  }
  foreach(string property in new[]{"Name","Hash","Bytes"})
  {
   var f=Setup();var c=Coordinator(f);var p=c.Inspect();c.Confirm(Operation,NewRun,p.Fingerprint);
   var backup=Body(f.Reg.Data["backup.json"]);backup["Files"]!.AsArray()[0]!.AsObject().Remove(property);
   f.Reg.Data["backup.json"]=PackedRaw(backup.ToJsonString());RehashDependencies(f,"backup.json");
   Check(c.Inspect().Kind==MigrationKind.Uncertain,"fully checksummed backup entry required field "+property);
  }
  foreach(string file in new[]{"intent.json","selector.json","backup.json"})foreach(string property in new[]{"Payload","Hash"})
  {
   var f=Setup();var c=Coordinator(f);var p=c.Inspect();c.Confirm(Operation,NewRun,p.Fingerprint);
   var outer=JsonNode.Parse(f.Reg.Data[file])!.AsObject();outer.Remove(property);f.Reg.Data[file]=Encoding.UTF8.GetBytes(outer.ToJsonString());RehashDependencies(f,file);
   Check(c.Inspect().Kind==MigrationKind.Uncertain,"registry envelope requires field "+file+"/"+property);
  }
  foreach(string name in new[]{"intent.json","backup.json","selector.json"})
  {
   var f=Setup();var c=Coordinator(f);var p=c.Inspect();c.Confirm(Operation,NewRun,p.Fingerprint);string old=Bytes(f.Old);
   f.Reg.Data.Remove(name);var result=c.Inspect();Check(result.Kind is MigrationKind.Uncertain or MigrationKind.Incomplete,"deleted selected provenance never offers legacy "+name);Check(Bytes(f.Old)==old,"missing provenance did not alter old group");
  }
  foreach(string name in new[]{"intent.json","backup.json","selector.json"})
  {
   var f=Setup();var c=Coordinator(f);var p=c.Inspect();c.Confirm(Operation,NewRun,p.Fingerprint);f.Reg.Data[name]=f.Reg.Data[name][..^1];
   Check(c.Inspect().Kind==MigrationKind.Uncertain,"truncated registry file failclosed "+name);
  }
  {
   var f=Setup();var c=Coordinator(f);var p=c.Inspect();c.Confirm(Operation,NewRun,p.Fingerprint);
   var slot=f.NewSlots[NewRun];var codec=new CheckpointCodec(Sim());var store=new CheckpointStore(slot,Catalog,WaveRules.Version,codec.Validate);
   var run2="88888888888888888888888888888888";store.BeginRun(run2,Guid.NewGuid().ToString("N"),2);store.Close(run2,Guid.NewGuid().ToString("N"),3);
   f.Old.Data["control.json"]=new byte[]{0};f.Old.Data["unrelated-existing-file"]=new byte[]{1};
   var selected=c.Confirm(Operation,NewRun,p.Fingerprint);Check(selected.Recovery!.Kind==RecoveryKind.Closed&&selected.Recovery.Control!.Run==run2,"old operation retry returns current new-run Closed despite now-corrupt legacy");
   Check(c.Inspect().Recovery!.Kind==RecoveryKind.Closed,"selected closed ignores unrelated legacy artifacts without revival");
   f.Reg.Data.Remove("selector.json");Check(c.Inspect().Kind==MigrationKind.Incomplete,"lost selector after newer rule2 Closed cannot reoffer old");
   Throws(()=>c.Confirm(Operation,NewRun,p.Fingerprint),"same stale operation cannot rebuild absent selector from changed/closed storage");
  }
  Console.WriteLine("INDEPENDENT_EXTRA checks="+(checks-before)+" defects="+string.Join(";",defects));
  Check(defects.Count==0,"schema2 transaction Kind must be explicit: "+string.Join(";",defects));
 }
}
