using System;using System.Numerics;using Hmi.PresentationStudy;
static class Program
{
 static int checks;static void C(bool b,string n){checks++;if(!b)throw new Exception(n);}
 static void Main(){
 var s=new Scope(1,1);var p=new EntityKey(0,1);var t=new EntityKey(3,1);var box=new AuthoritativeMeleeAim();C(box.Bind(s,p),"bind");
 MeleeAimEvent E(long tick,int index=0)=>new(s,p,t,tick,index,new Vector2(0,-2));
 for(int i=0;i<8;i++)C(box.Capture(E(10+i),10+i,false),"pre-native catchup capture");
 C(box.Captured==8&&box.Merged==7&&box.HasPending,"eight steps latest mailbox");object owner=new();
 C(box.Resolve(owner,1,1,true,false,out var v,out var fresh)&&fresh&&v.Tick==17&&v.Direction==new Vector2(0,-1),"first native owner sees latest");
 C(box.Resolve(owner,1,1,true,false,out _,out fresh)&&!fresh,"same real native active retains");
 C(!box.Capture(E(17),17,false),"repeat buffer dedup");C(!box.Capture(E(16,99),17,false),"old tick reject");C(box.Capture(E(17,1),17,false),"index scoped to tick");
 C(box.Resolve(owner,1,1,true,false,out v,out fresh)&&fresh&&v.Index==1,"latest same tick");
 C(!box.Capture(E(18),18,true),"paused rejects");C(box.HighTick==18,"paused consumes highwater");C(!box.Capture(E(18),18,false),"pause resume cannot replay");
 box.ClearPresentation();box.ClearPresentation();C(box.HighTick==18&&!box.HasLatch&&!box.HasPending,"same scope focus clears only presentation");C(!box.Capture(E(18),18,false),"focus replay blocked");
 C(box.Capture(E(19),19,false),"new event after focus");C(box.Resolve(owner,1,2,true,false,out _,out fresh)&&fresh,"new event genuine sequence");
 C(!box.Resolve(new object(),1,2,true,false,out _,out _),"new native owner same sequence cannot inherit");
 C(box.Capture(E(20),20,false),"capture before rebuild");object newer=new();C(box.Resolve(newer,1,1,true,false,out _,out fresh)&&fresh,"new owner receives freshly captured event");
 C(!box.Resolve(newer,2,1,true,false,out _,out _),"same instance new epoch clears latch");
 C(box.Capture(E(21),21,false),"capture for native end");C(box.Resolve(newer,2,2,true,false,out _,out _),"active bound");C(!box.Resolve(newer,2,2,false,false,out _,out _),"real active ends");
 C(box.Capture(E(22),22,false),"capture no active");C(box.Resolve(newer,2,2,false,false,out _,out fresh)&&fresh,"one render no active cue");C(!box.Resolve(newer,2,2,false,false,out _,out _),"no active has no fake timer");
 C(!box.Capture(E(100),22,false)&&box.HighTick==22,"future stamp does not poison");C(!box.Capture(E(23) with{Source=new EntityKey(0,2)},23,false),"wrong source generation");
 C(!box.Capture(E(23) with{Direction=new Vector2(float.NaN,0)},23,false),"nonfinite reject");C(!box.Capture(E(23) with{Direction=Vector2.Zero},23,false),"zero direction reject");
 C(box.Capture(E(23),23,false),"pending before pause");C(!box.Resolve(newer,2,3,true,true,out _,out _)&&!box.HasPending,"pause discards unrendered event");C(!box.Capture(E(23),23,false),"discard does not replay");
 for(int i=24;i<80;i++){var e=E(i) with{Target=new EntityKey(i%2+3,1),Direction=i%2==0?Vector2.UnitX:-Vector2.UnitX};C(box.Capture(e,i,false),"rapid targets capture");C(box.Resolve(newer,2,3,true,false,out v,out fresh)&&fresh&&v.Direction==e.Direction,"rapid targets latest direction without new sequence");}
 C(!box.Resolve(newer,2,4,true,false,out _,out _),"new unrelated native sequence expires old latch");
 var state=new ReadyAttention();state.Bind(s,p,Vector2.UnitY);var obs=new[]{new Observation(t,new Vector2(0,-1),true,true,true,true),new Observation(new EntityKey(4,1),new Vector2(-1.3f,0),true,true,true,true)};
 C(state.Update(s,p,0,Vector2.Zero,Vector2.Zero,obs,0,false),"observe");obs[0]=obs[0] with{Position=new Vector2(0,-1.29f)};obs[1]=obs[1] with{Position=new Vector2(-1.216f,0)};
 state.Update(s,p,1,Vector2.Zero,Vector2.Zero,obs,.2,false);C(state.Current.AttentionTarget==t,"ordinary hold keeps prior target");
 C(state.AdoptActualMeleeTarget(s,p,new EntityKey(4,1),Vector2.Zero,obs),"actual target bypasses distance advantage");state.Update(s,p,1,Vector2.Zero,Vector2.Zero,obs,0,false);C(state.Current.AttentionTarget==new EntityKey(4,1),"adopt uses existing attention hold");
 obs[1]=obs[1] with{LiveEnemy=false};C(!state.AdoptActualMeleeTarget(s,p,new EntityKey(4,1),Vector2.Zero,obs),"dead target not adopted");
 C(box.Capture(E(80) with{Target=new EntityKey(4,1)},80,false)&&box.Resolve(newer,2,4,true,false,out v,out _),"authentic dead-target direction still representable without actor");
 C(box.Bind(new Scope(1,2),p)&&box.HighTick==-1&&!box.HasLatch,"new scope clears stamp/latch");C(!box.Capture(E(81),81,false),"old scope rejected");
 C(box.Capture(new(new Scope(1,2),p,t,0,0,Vector2.UnitX),0,false),"new wave tick0 legal");
 // Bounded diagnostic history is actual study allocation, not a behavior queue.
 var audit=new AuthoritativeMeleeAim();audit.Bind(s,p);
 for(int i=0;i<24;i++)audit.Capture(E(i),i,false);
 C(audit.Recent(0).Tick==23&&audit.Recent(15).Tick==8,"audit wrap retains exactly last16");
 bool threw=false;try{audit.Recent(16);}catch(ArgumentOutOfRangeException){threw=true;}C(threw,"audit overflow read hard failure");
 audit.Resolve(owner,1,5,true,false,out _,out _);C(audit.HasLatch,"latch before pause");
 C(!audit.Resolve(owner,1,5,true,true,out _,out _)&&audit.HasLatch,"paused latch frozen not applied");
 C(audit.Resolve(owner,1,5,true,false,out v,out fresh)&&!fresh&&v.Tick==23,"same real active resume");
 C(audit.Bind(s,new EntityKey(0,2))&&!audit.HasLatch&&audit.HighTick==-1,"source generation rebind clears old latch/stamp");
 C(!audit.Capture(E(24),24,false),"old source rejected after rebind");
 C(audit.Capture(E(0) with{Source=new EntityKey(0,2)},0,false),"new source generation fresh tick allowed");
 obs[1]=obs[1] with{LiveEnemy=true,Id=new EntityKey(4,2)};
 C(!state.AdoptActualMeleeTarget(s,p,new EntityKey(4,1),Vector2.Zero,obs),"old dead target cannot adopt reused slot");
 Console.WriteLine("AIM_PRIORITY_PURE_PASS "+checks);
 }
}
