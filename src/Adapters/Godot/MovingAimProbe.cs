using Godot;
using Hmi.Core;
using Hmi.GodotAdapter;
using Hmi.Adapters.Input;
using System;
using System.Reflection;
using System.Text.Json;
public partial class MovingAimProbe:Node
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
 GameHost host;GameSimulation sim;int checks;bool done;
 T Field<T>(string name)=>(T)typeof(GameHost).GetField(name,F).GetValue(host);
 void Call(string name,params object[] args)=>typeof(GameHost).GetMethod(name,F).Invoke(host,args);
 void C(bool value,string reason){checks++;if(!value)throw new Exception(reason);}
 static Button ButtonAt(Node n,string text){if(n is Button b&&b.Text==text)return b;foreach(Node c in n.GetChildren()){var b2=ButtonAt(c,text);if(b2!=null)return b2;}return null;}
 void Input(float x){Call("SynchronizeMovementInput");var router=Field<MoveInputRouter>("_moveInputs");C(host.SubmitMovementIntent(router.Token("route"),new MoveIntent(x,0)),"route input");}
 public override void _Ready()
 {
  try{
   if(OS.GetEnvironment("MOVING_LAUNCH_AIM")!="1")throw new Exception("Set MOVING_LAUNCH_AIM=1");
   ControlledGaitDrill.Enabled=ControlledGaitDrill.TurningRun=ControlledGaitDrill.DisplacementRun=ControlledGaitDrill.Candidate=ControlledGaitDrill.ProportionCandidate=true;ControlledGaitDrill.Clip="Run";
   var box=new SubViewportContainer{Size=new(1280,720)};AddChild(box);var viewport=new SubViewport{Size=new(1280,720),OwnWorld3D=true};box.AddChild(viewport);host=new GameHost();viewport.AddChild(host);host.SetProcess(false);Call("Start",1,(uint)1030);ButtonAt(host,"确认准备新局").EmitSignal(Button.SignalName.Pressed);Call("Start",1,(uint)1030);host.SetProcess(false);host.RegisterMovementProvider("route",new MoveProviderOptions(100,0));sim=Field<GameSimulation>("_sim");
   for(int i=0;i<72;i++){Input(i<30?0:1);host._Process(GameSimulation.FixedDelta);}
   var p=Field<CharacterPresentation[]>("_presentations")[sim.PlayerId.Index];var root=Field<Node3D[]>("_visuals")[sim.PlayerId.Index];C(p.LaunchAim.HasValue,"real moving launch captured");var sample=p.LaunchAim.Value;
   C(!p.AcceptLaunchAim(sample),"same tick duplicate rejected");C(!p.AcceptLaunchAim(default),"invalid owner rejected");
   string diag=JsonSerializer.Serialize(p.MovingAimDiagnostics);C(diag.Contains("upper-yaw-wip"),"actual pose layer active");var transform=root.Transform;
   for(int i=0;i<5;i++)p.Advance(.1f,true);
   C(JsonSerializer.Serialize(p.MovingAimDiagnostics)==diag && root.Transform==transform,"pause freezes aim and root");
   p.Advance(0,false);p.Advance(float.NaN,false);C(JsonSerializer.Serialize(p.MovingAimDiagnostics)==diag,"invalid delta freezes");
   host._Notification((int)MainLoop.NotificationApplicationFocusOut);C(!p.LaunchAim.HasValue,"focus clears immutable sample");C(JsonSerializer.Serialize(p.MovingAimDiagnostics).Contains("idle"),"focus restores base pose");
   Call("ConsumeEvents");C(!p.LaunchAim.HasValue,"same buffer focus replay absent");
   C(sim.TryGetEntity(sim.PlayerId,out var view),"player view");C(p.Snapshot(view,sim.Tick,sim.RunToken+1,sim.Wave),"context reset accepted");C(!p.AcceptLaunchAim(sample),"old context rejected");
   p.Dispose();C(!p.AcceptLaunchAim(sample),"disposed rejects sample");box.QueueFree();GD.Print($"MOVING_AIM_PROBE_PASS {checks}");done=true;
  }catch(Exception e){GD.PushError(e.ToString());GetTree().Quit(1);}
 }
 public override void _Process(double delta){if(done)GetTree().Quit();}
}
