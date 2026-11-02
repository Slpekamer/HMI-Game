using System;
using Hmi.Core;
namespace Hmi.GodotAdapter;
/// <summary>Host-scope event identity and bounded onset bookkeeping, independent of native node lifetime.</summary>
internal sealed class ProjectileEventLedger
{
 internal const int VictimCapacity=64,PendingCapacity=8;
 internal readonly ProjectileTransition[] Pending=new ProjectileTransition[PendingCapacity];
 internal int PendingCount{get;private set;}
 GameSimulation simulation;int run=-1,wave=-1;long lastTick=-1,lastDrop;int lastIndex=-1;
 readonly Victim[] victims=new Victim[VictimCapacity];readonly double[] global=new double[4];int globalCount;
 internal double Clock{get;private set;}
 internal long Captured,Launches,Impacts,TransitionLoss,Duplicates,PendingDrops,ActiveDrops,DarkDrops,VictimRateDrops,GlobalRateDrops,LedgerDrops,FrameDrops,PoolDrops,Cancelled,Onsets;
 struct Victim{internal EntityId Id;internal bool Used,Active,NeedsDark;internal int Count;internal double A,B;}
 internal void Bind(GameSimulation sim){if(ReferenceEquals(simulation,sim)&&run==sim.RunToken&&wave==sim.Wave)return;CancelPending();simulation=sim;run=sim.RunToken;wave=sim.Wave;lastTick=-1;lastIndex=-1;lastDrop=0;Clock=0;globalCount=0;Array.Clear(victims);}
 internal void Capture(GameSimulation sim,Action<ProjectileTransition> launch,bool impactEnabled)
 {
  Bind(sim);long drop=sim.DroppedProjectileTransitions;if(drop>=lastDrop)TransitionLoss+=drop-lastDrop;lastDrop=drop;
  for(int i=0;i<sim.ProjectileTransitionCount;i++)Accept(sim.GetProjectileTransition(i),i,sim.Tick,launch,impactEnabled);
 }
 internal void Accept(ProjectileTransition e,int index,long tick,Action<ProjectileTransition> launch,bool impactEnabled)
 {
  if(e.RunToken!=run||e.Wave!=wave||e.Tick<0||e.Tick!=tick||index<0)return;
  if(e.Tick<lastTick||(e.Tick==lastTick&&index<=lastIndex)){Duplicates++;return;}lastTick=e.Tick;lastIndex=index;Captured++;
  if(e.Kind==ProjectileTransitionKind.Launched){Launches++;launch?.Invoke(e);}
  if(e.Kind!=ProjectileTransitionKind.Impact)return;Impacts++;
  if(!impactEnabled)return;
  if(e.Id.Index<0||e.Id.Generation<=0||e.Target.Index<0||e.Target.Generation<=0||!float.IsFinite(e.Position.X)||!float.IsFinite(e.Position.Y)){PendingDrops++;return;}
  if(PendingCount==PendingCapacity){PendingDrops++;return;}Pending[PendingCount++]=e;
 }
 internal void Advance(double delta){if(double.IsFinite(delta)&&delta>0)Clock+=Math.Min(delta,.25);Prune();}
 void Prune(){int n=0;for(int i=0;i<globalCount;i++)if(Clock-global[i]<1)global[n++]=global[i];globalCount=n;
  for(int i=0;i<victims.Length;i++){ref var v=ref victims[i];if(!v.Used)continue;if(v.Count==2&&Clock-v.A>=1){v.A=v.B;v.Count=1;}if(v.Count==1&&Clock-v.A>=1)v.Count=0;if(v.Count==0&&!v.Active&&!v.NeedsDark)v=default;}}
 internal bool TryOnset(EntityId target,int frameOnsets,bool poolAvailable,out int ledgerSlot)
 {
  ledgerSlot=-1;int free=-1;for(int i=0;i<victims.Length;i++){if(victims[i].Used&&victims[i].Id==target){ledgerSlot=i;break;}if(!victims[i].Used&&free<0)free=i;}
  if(ledgerSlot>=0){ref var old=ref victims[ledgerSlot];if(old.Active){ActiveDrops++;return false;}if(old.NeedsDark){DarkDrops++;return false;}if(old.Count>=2){VictimRateDrops++;return false;}}
  if(globalCount>=4){GlobalRateDrops++;return false;}if(frameOnsets>=2){FrameDrops++;return false;}if(!poolAvailable){PoolDrops++;return false;}
  if(ledgerSlot<0){if(free<0){LedgerDrops++;return false;}ledgerSlot=free;victims[free]=new Victim{Used=true,Id=target};}
  ref var v=ref victims[ledgerSlot];if(v.Count==0)v.A=Clock;else v.B=Clock;v.Count++;v.Active=true;v.NeedsDark=false;global[globalCount++]=Clock;Onsets++;return true;
 }
 internal void EndActive(int slot){if(slot<0||slot>=victims.Length||!victims[slot].Used)return;victims[slot].Active=false;victims[slot].NeedsDark=true;}
 internal void EndEligibleRender(){for(int i=0;i<victims.Length;i++)if(victims[i].Used&&!victims[i].Active)victims[i].NeedsDark=false;PendingCount=0;}
 internal void CancelPending(){Cancelled+=PendingCount;PendingCount=0;}
 internal void ClearPresentation(){CancelPending();for(int i=0;i<victims.Length;i++)if(victims[i].Used&&victims[i].Active){victims[i].Active=false;victims[i].NeedsDark=true;}}
 internal int UsedVictims{get{int n=0;foreach(var v in victims)if(v.Used)n++;return n;}}
}
