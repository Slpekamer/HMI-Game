using Hmi.Adapters.Persistence;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
static class IndependentStoreTests {
 static string Id()=>Guid.NewGuid().ToString("N");
 static byte[] Payload(string run,long seq)=>Encoding.UTF8.GetBytes($"{run}|{seq}|c|r");
 static CheckpointStore Store(ICheckpointFiles f)=>new(f,"c","r",(p,i)=>Encoding.UTF8.GetString(p)==$"{i.Run}|{i.Sequence}|{i.Catalog}|{i.Rules}");
 static void Reject(Action<bool,string> c,Action a,string why){bool bad=false;try{a();}catch(Exception e)when(e is InvalidDataException or IOException or InvalidOperationException or ArgumentException){bad=true;}c(bad,why);}
 static void ControlBytes(MemoryFiles f,Control c){byte[] p=JsonSerializer.SerializeToUtf8Bytes(c);f.Values["control.json"]=JsonSerializer.SerializeToUtf8Bytes(new Envelope(Convert.ToBase64String(p),Convert.ToHexString(SHA256.HashData(p))));}
 public static void Run(Action<bool,string> c){
  var f=new MemoryFiles();var s=Store(f);string r=Id();s.BeginRun(r,Id(),0);
  Reject(c,()=>s.Save(r,Id(),1,Payload(Id(),2)),"independent foreign payload UUID rejected");
  Reject(c,()=>s.Save(r,Id(),1,Payload(r,3)),"independent payload future sequence rejected");
  c(s.Recover().Kind==RecoveryKind.PendingStart,"invalid payload leaves pending identity unchanged");
  s.Save(r,Id(),1,Payload(r,2));s.Save(r,Id(),2,Payload(r,3));
  var control=s.Recover().Control!;
  byte[] wrong=Payload(Id(),3);var d=control.Snapshots[0] with{Hash=Convert.ToHexString(SHA256.HashData(wrong)),Length=wrong.Length};
  f.Values[d.Name]=wrong;ControlBytes(f,control with{Snapshots=[d,control.Snapshots[1]]});
  var recovered=s.Recover();c(recovered.Kind==RecoveryKind.RolledBack && Encoding.UTF8.GetString(recovered.Payload!)==Encoding.UTF8.GetString(Payload(r,2)),"checksummed foreign latest DTO falls back to identity-validated predecessor");
  s.Save(r,Id(),3,Payload(r,4));var latest=s.Recover().Control!;f.Values[latest.Snapshots[0].Name]=[1];
  c(s.Recover().Kind==RecoveryKind.RolledBack,"save after rollback retains valid backup, not bad latest");
  c(s.Recover().Control!.Snapshots[1].Sequence==2,"fallback descriptor keeps actual older sequence");
  string op=Id();s.Close(r,op,4);c(s.Close(r,op,4).AlreadyCommitted,"close acknowledgement retry idempotent");
  Reject(c,()=>s.BeginRun(Id(),op,4),"same operation cannot switch from close to new run");
  c(s.Recover().Kind==RecoveryKind.Closed,"conflicting op cannot revive closed run");
  string uppercase="ABCDEFABCDEFABCDEFABCDEFABCDEFAB";
  Reject(c,()=>Store(new MemoryFiles()).BeginRun(uppercase,Id(),0),"32-digit uppercase run rejected canonically");
  var q=new MemoryFiles();var qs=Store(q);string qr=Id();qs.BeginRun(qr,Id(),0);qs.Save(qr,Id(),1,Payload(qr,2));
  byte[] originalControl=q.Values["control.json"].ToArray();
  for(int n=0;n<9;n++)q.Values["temp-"+Id()]=new byte[256*1024];
  Reject(c,()=>Store(new NoDelete(q)).Save(qr,Id(),2,Payload(qr,3)),"failed cleanup quota refuses new save");
  c(q.Values["control.json"].AsSpan().SequenceEqual(originalControl),"quota rejection preserves committed control");
  c(qs.Recover().Kind==RecoveryKind.Active,"quota failure retains readable safe point");
  var t=new MemoryFiles();t.Values["temp-note"]=[1];c(Store(t).Recover().Kind==RecoveryKind.Empty,"unrelated temp-like name is not corrupt history");
  string dir=Path.Combine(Path.GetTempPath(),"hmi-store-size-"+Id());var p=new PhysicalCheckpointFiles(dir);
  c(!Directory.Exists(dir),"physical port constructor performs no filesystem writes");
  // This test creates corrupt external input explicitly; production creates its directory only while acquiring the writer.
  Directory.CreateDirectory(dir);
  using(var stream=new FileStream(Path.Combine(dir,"control.json"),FileMode.CreateNew)){stream.SetLength(100L*1024*1024);}
  long before=GC.GetAllocatedBytesForCurrentThread();var outcome=Store(p).Recover();long bytes=GC.GetAllocatedBytesForCurrentThread()-before;
  c(outcome.Kind==RecoveryKind.Uncertain && bytes<1024*1024,"100MB sparse corrupt control refused without whole-file allocation");
  Console.WriteLine($"INDEPENDENT_STORE oversize_read_allocated={bytes}");
 }
 sealed class NoDelete(ICheckpointFiles inner):ICheckpointFiles{
  public IDisposable AcquireWriter()=>inner.AcquireWriter();public string[] Names()=>inner.Names();public long Length(string n)=>inner.Length(n);public byte[] Read(string n,int m)=>inner.Read(n,m);public void AtomicWrite(string n,byte[] b,bool o)=>inner.AtomicWrite(n,b,o);public void Delete(string n)=>throw new IOException("locked cleanup");
 }
}
