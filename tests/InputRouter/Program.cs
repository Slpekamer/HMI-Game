using System;
using Hmi.Adapters.Input;
class Program
{
 static int checks;static void Check(bool b,string why){checks++;if(!b)throw new Exception(why);}
 static void Main(){
  var r=new MoveInputRouter();var k=r.Register("k",new(30,0));var g=r.Register("g",new(20,.2f,.02f));var t=r.Register("t",new(10,.08f));
  Check(r.Resolve().IsZero,"empty");r.Submit(t,new(1,0));Check(r.Resolve().X==1&&r.SelectedProvider=="t","touch");
  r.Submit(g,new(.1f,0));Check(r.Resolve().X==1&&r.SelectedProvider=="t","noise no steal");
  r.Submit(k,new(0,1));var v=r.Resolve();Check(v.X==0&&v.Y==1&&r.SelectedProvider=="k","nonadditive priority");Check(!r.Submit(t,new(1,0)),"old touch generation rejected");
  long kg=r.Token("k").Generation;for(int i=0;i<100;i++){r.Submit(r.Token("k"),new(0,1));Check(r.Resolve().Y==1&&r.Token("k").Generation==kg,"held keyboard no generation oscillation");}
  r.Submit(k,MoveIntent.Zero);r.Submit(g,new(.21f,0));Check(r.Resolve().IsZero,"gamepad engage threshold");r.Submit(g,new(.23f,0));Check(r.Resolve().X>0,"engage");r.Submit(g,new(.19f,0));Check(r.Resolve().X>0,"hysteresis hold");r.Submit(g,new(.18f,0));Check(r.Resolve().IsZero,"release threshold");
  r.Submit(r.Token("k"),new(9,9));v=r.Resolve();Check(Math.Abs(v.X*v.X+v.Y*v.Y-1)<.00001,"clamp diagonal");
  r.Submit(r.Token("k"),new(float.NaN,0));Check(r.Resolve().IsZero,"nonfinite clears");
  var old=r.Token("k");r.Disable("k");Check(!r.Submit(old,new(1,0)),"disabled rejects");var fresh=r.Enable("k");Check(fresh.Generation!=old.Generation&&!r.Submit(old,new(1,0)),"enabled rejects stale");
  r.Submit(fresh,new(1,0));r.ResetAll();Check(r.Resolve().IsZero&&!r.Submit(fresh,new(1,0)),"scope resets");
  old=r.Token("g");r.Unregister("g");g=r.Register("g",new(20,.2f,.02f));Check(g.Generation!=old.Generation&&!r.Submit(old,new(1,0)),"re-register rejects prior incarnation");
  r.Disable("t");r.Submit(r.Token("k"),MoveIntent.Zero);Check(r.Resolve().IsZero,"device connected touch unavailable");t=r.Enable("t");Check(r.Resolve().IsZero,"disconnect never resumes old vector");r.Submit(t,new(.04f,0));Check(r.Resolve().IsZero,"touch deadzone");r.Submit(t,new(1,0));Check(r.Resolve().X==1,"fresh touch allowed");
  for(int i=r.Count;i<MoveInputRouter.MaximumProviders;i++)r.Register("extra"+i,new(1,0));bool capped=false;try{r.Register("overflow",new(1,0));}catch(InvalidOperationException){capped=true;}Check(capped,"bounded registry");
  var menu=new GamepadMenuRouter();
  Check(menu.Sample(1,.01,0,0,true,false,false,false).Action==MenuAction.None,"held initial confirm blocked");
  menu.Sample(1,.01,0,0,false,false,false,false);Check(menu.Sample(1,.01,0,0,true,false,false,false).Action==MenuAction.Confirm,"fresh confirm");
  Check(menu.Sample(2,.01,0,0,true,false,false,false).Action==MenuAction.None,"held confirm across page blocked");
  menu.Sample(2,.01,0,0,false,false,false,false);Check(menu.Sample(2,.01,0,0,true,false,false,false).Action==MenuAction.Confirm,"page release rearm");
  for(int i=0;i<100;i++)Check(menu.Sample(2,.016,0,0,true,false,false,false).Action==MenuAction.None,"held cannot multi-purchase");
  Check(menu.Sample(2,.01,.56f,0,false,false,false,false).X==1,"menu direction initial");Check(menu.Sample(2,.1,.5f,0,false,false,false,false).X==0,"repeat waits");Check(menu.Sample(2,.25,.5f,0,false,false,false,false).X==1,"repeat initial delay");
  Check(menu.Sample(2,.01,.36f,0,false,false,false,false).X==0,"menu hysteresis no chatter");menu.Sample(2,.01,0,0,false,false,false,false);Check(menu.Sample(2,.01,.4f,0,false,false,false,false).X==0,"menu noise no engage");
  menu.Reset();Check(menu.Sample(10,.016,1,0,false,false,false,false).X==0,"reset held navigation requires neutral");menu.Sample(10,.016,0,0,false,false,false,false);Check(menu.Sample(10,.016,1,0,false,false,false,false).X==1,"neutral rearms navigation");Check(menu.Sample(11,.016,1,0,false,false,false,false).X==0,"new menu scope rejects held axis");
  Console.WriteLine($"MOVE_INPUT_ROUTER PASS {checks}");
 }
}
