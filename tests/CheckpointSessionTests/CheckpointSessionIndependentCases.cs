using Hmi.Core;
using Hmi.Adapters.Persistence;
using System.Text;
static partial class Program
{
 static void IndependentCases()
 {
  var files=new MemoryFiles();var x=Make(files);Check(x.S.StartNew("honey",23,x.S.IntentEpoch),"independent initial honey");
  var rec=x.Store.Recover();var id=new SnapshotIdentity(rec.Control!.Run,rec.Control.Sequence,Hash,GameSimulation.CheckpointRulesVersion);
  var raw=Encoding.UTF8.GetString(rec.Payload!);
  Check(!x.C.Validate(Encoding.UTF8.GetBytes(raw.Replace("\"Count\":1","\"Count\":1,\"Count\":0")),id),"independent duplicate nested Count rejected");
  Check(!x.C.Validate(Encoding.UTF8.GetBytes(raw.Replace("\"Cards\":[","\"Cards\":[null,")),id),"independent null nested card rejected");
  Check(!x.C.Validate(rec.Payload!,id with{Catalog=new string('a',64)}),"independent external catalog identity mismatch");
  var offered=Make(files);long epoch=offered.S.IntentEpoch;
  x.Store.Close(rec.Control.Run,Guid.NewGuid().ToString("N"),rec.Control.Sequence);
  Check(!offered.S.Resume(epoch)&&offered.G.Phase==RunPhase.Selecting&&offered.S.CurrentRecovery.Kind==RecoveryKind.Closed,"independent stale advertised resume rechecks closed authority");
  Check(!offered.S.Resume(epoch),"independent stale epoch remains rejected after authority refresh");
  var active=Make(files);Check(active.S.StartNew("orbit",2,active.S.IntentEpoch),"independent new identity after close");
  var beforeHero=active.G.CharacterId;var beforeToken=active.G.RunToken;files.FailBefore=files.Writes+1;
  Check(!active.S.StartNew("crit",99,active.S.IntentEpoch)&&active.G.CharacterId==beforeHero&&active.G.RunToken==beforeToken,"independent failed new identity cannot mutate prior hero");
  Check(!active.S.ExitSelection(active.S.IntentEpoch)&&!active.S.Resume(active.S.IntentEpoch),"independent pending identity blocks exit/resume bypass");
  Check(active.S.Retry(active.S.IntentEpoch)&&active.G.CharacterId=="crit"&&active.G.Tick==0,"independent retry starts intended new hero exactly once");
  var ufiles=new MemoryFiles();var u=Make(ufiles);ufiles.FailBefore=2;
  Check(!u.S.StartNew("bounce",5,u.S.IntentEpoch)&&u.S.ContinueUnsaved(u.S.IntentEpoch),"independent deliberate first-wave unsaved");
  int writes=ufiles.Writes;Between(u.G);Check(u.S.PrepareNext(u.S.IntentEpoch)&&u.G.Wave==2&&!u.S.CurrentWaveSaved&&ufiles.Writes==writes,"independent unsaved later wave does not claim persistence or write");
  var fresh=Make(ufiles);Check(!fresh.S.Resume(fresh.S.IntentEpoch)&&fresh.S.CurrentRecovery.Kind==RecoveryKind.PendingStart,"independent unsaved process restart has no recovery point");
  ufiles.FailBefore=ufiles.Writes+1;Check(!u.S.Close(u.S.IntentEpoch),"independent unsaved terminal close fails visibly");
  Check(!u.S.ContinueUnsaved(u.S.IntentEpoch)&&!u.S.ExitSelection(u.S.IntentEpoch),"independent close failure cannot degrade or leave gate");
  Check(u.S.Retry(u.S.IntentEpoch)&&Make(ufiles).S.CurrentRecovery.Kind==RecoveryKind.Closed,"independent retry closes unsaved identity");
 }
}
