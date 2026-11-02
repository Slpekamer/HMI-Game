using Godot;using Hmi.Core;using Hmi.GodotAdapter;using System;using System.Reflection;
public partial class CommonMeleeHostProbe:Node{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;GameHost host;GameSimulation sim;int checks;bool done;
 T Field<T>(string n)=>(T)typeof(GameHost).GetField(n,F).GetValue(host);void Call(string n,params object[] a)=>typeof(GameHost).GetMethod(n,F).Invoke(host,a);void C(bool b,string n){checks++;if(!b)throw new Exception(n);}
 object World=>typeof(GameSimulation).GetField("World",F).GetValue(sim);Array Arr(string n)=>(Array)World.GetType().GetField(n,F).GetValue(World);
 public override void _Ready(){try{
  host=new GameHost();AddChild(host);host.SetProcess(false);Call("Start",1,(uint)1030);host.SetProcess(false);sim=Field<GameSimulation>("_sim");var fx=Field<CommonMeleeCue>("_meleeCue");C(fx!=null&&sim.Phase==RunPhase.Combat,"actual Host owner and fresh Start");
  bool first=false;int actualMelee=0;for(int i=0;i<1800&&sim.Phase==RunPhase.Combat;i++){int before=fx.Started;host._Process(GameSimulation.FixedDelta);for(int j=0;j<sim.EventCount;j++)if(sim.GetEvent(j).Kind==EventKind.Melee)actualMelee++;if(fx.Started>before){C(fx.Active&&fx.Age==0,"actual Host ConsumeEvents to first Render birth age0");first=true;break;}}
  C(first&&actualMelee>0,"natural real Melee reaches owner");double clock=fx.Clock;host.Visible=false;host._Process(0);C(!fx.Active&&fx.Clock==clock,"actual Host hidden clears without aging");host.Visible=true;host._Process(0);C(!fx.Active,"same-buffer hidden replay absent");
  // Independent controlled lethal setup: only test fixture modifies target HP/position and timer.
  int enemy=-1;for(int i=0;i<sim.Capacity;i++)if(sim.TryGetEntityAtSlot(i,out var e)&&e.Kind==EntityKind.Enemy){enemy=i;break;}C(enemy>=0,"lethal target exists");sim.TryGetEntityAtSlot(enemy,out var victim);var oldId=victim.Id;
  sim.TryGetEntity(sim.PlayerId,out var playerView);var health=Arr("Health");var hp=health.GetValue(enemy);hp.GetType().GetField("Current").SetValue(hp,.01f);health.SetValue(hp,enemy);var tr=Arr("Transform");var t=tr.GetValue(enemy);t.GetType().GetField("Position").SetValue(t,playerView.Position+new Vec2(.7f,0));tr.SetValue(t,enemy);typeof(GameSimulation).GetField("MeleeTimer",F).SetValue(sim,0f);
  // New scope resets rolling budget legitimately, then same test setup must be re-established; instead retain budget and test event capture irrespective of visual suppression.
  int observed=fx.Observed;host._Process(GameSimulation.FixedDelta);bool melee=false,damage=false,death=false;for(int j=0;j<sim.EventCount;j++){var e=sim.GetEvent(j);if(e.Target!=oldId)continue;melee|=e.Kind==EventKind.Melee;damage|=e.Kind==EventKind.Damage;death|=e.Kind==EventKind.Death;}
  C(melee&&damage&&death&&!sim.TryGetEntity(oldId,out _),"real Step lethal Melee Damage Death then destroyed target");C(fx.Observed>observed,"lethal real event captured despite missing target at render");
  // Source-dead entry uses actual Emit buffer after fixture destroys source, not animation accepted path.
  var source=sim.PlayerId;World.GetType().GetMethod("Destroy",F).Invoke(World,new object[]{source.Index});typeof(GameSimulation).GetField("EventLength",F).SetValue(sim,0);typeof(GameSimulation).GetMethod("Emit",F).Invoke(sim,new object[]{EventKind.Melee,source,oldId,new Vec2(0,0),new Vec2(1,0),1f,FeedbackMechanism.BasicMelee});
  fx.BeginFrame(sim.RunToken+1,sim.Wave,source); // force independent owner binding; CaptureStep rebinds to actual scope before capture
  observed=fx.Observed;fx.CaptureStep(sim);C(fx.Observed==observed+1,"source destroyed actual buffer not live-filtered");Call("Selection");host.SetProcess(false);Call("Start",1,(uint)1032);host.SetProcess(false);var ph=Arr("Health");var pv=ph.GetValue(sim.PlayerId.Index);pv.GetType().GetField("Current").SetValue(pv,0f);ph.SetValue(pv,sim.PlayerId.Index);host._Process(GameSimulation.FixedDelta);C(sim.Phase!=RunPhase.Combat&&!fx.Active&&fx.Pending==0,"actual terminal phase clears presentation");
  Call("Selection");host.SetProcess(false);Call("Start",1,(uint)1031);host.SetProcess(false);host._Process(0);C(!fx.Active&&fx.Pending==0,"new run clears old cue");
  host.QueueFree();GD.Print("COMMON_MELEE_HOST_PASS "+checks+" naturalMelee="+actualMelee);done=true;
 }catch(Exception e){GD.PushError(e.ToString());GetTree().Quit(1);}}
 public override void _Process(double d){if(done)GetTree().Quit();}
}
