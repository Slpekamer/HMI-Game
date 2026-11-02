using Godot;using Hmi.Core;using Hmi.GodotAdapter;using System;
public partial class CommonMeleeCueProbe:Node3D{
 int checks;bool done;void C(bool b,string n){checks++;if(!b)throw new Exception(n);}
 public override void _Ready(){try{var cam=new Camera3D{Position=new Vector3(4,6,7)};AddChild(cam);cam.LookAt(Vector3.Zero);var id=new EntityId(0,1);
 GameEvent E(long tick,float length=1,EntityId? src=null,FeedbackMechanism m=FeedbackMechanism.BasicMelee)=>new(EventKind.Melee,src??id,new EntityId(4,9),new Vec2(0,0),new Vec2(length,0),5,tick,1,1,m);
 using(var fx=new CommonMeleeCue(this,cam)){
 fx.BeginFrame(1,1,id);C(fx.Capture(E(1),0,1),"first capture");fx.EndFrame(.25f,false,true);C(fx.Active&&fx.Age==0&&fx.Started==1,"birth .25 age0");C(fx.Snapshot().NormalDepth&&fx.Snapshot().Visible,"normal depth visible flag");var t=fx.Snapshot().Transform;cam.Position+=Vector3.Right;fx.BeginFrame(1,1,id);fx.EndFrame(.02f,false,true);C(fx.Snapshot().Transform==t,"birth plane frozen when camera moves");C(!fx.Capture(E(1),0,1),"repeatedbuffer dedup");
 double clock=fx.Clock;string sig=fx.Signature;fx.BeginFrame(1,1,id);fx.EndFrame(.25f,true,true);C(fx.Signature==sig&&fx.Clock==clock,"pause frozen");
 fx.BeginFrame(1,1,id);C(fx.Capture(E(2),0,2),"active new event captured");fx.EndFrame(.01f,false,true);C(fx.Active&&fx.Started==1&&fx.ActiveDrops==1,"active no restart");
 fx.BeginFrame(1,1,id);fx.Capture(E(3),0,3);fx.EndFrame(.25f,false,true);C(!fx.Active&&fx.DarkDrops==1,"expiry opportunity dark despite pending");fx.BeginFrame(1,1,id);fx.Capture(E(4),0,4);fx.EndFrame(.01f,false,true);C(fx.Active&&fx.Started==2,"fresh after dark");
 fx.ClearPresentation();double hold=fx.Clock;int rate=fx.RollingOnsets;fx.BeginFrame(1,1,id);fx.EndFrame(.25f,false,false);C(fx.Clock==hold&&fx.RollingOnsets==rate,"hidden no refill");C(!fx.Capture(E(4),0,4),"focus dedup preserved");fx.BeginFrame(1,1,id);fx.Capture(E(5),0,5);fx.EndFrame(0,false,true);C(!fx.Active,"focus return no credited dark");fx.BeginFrame(1,1,id);fx.Capture(E(6),0,6);fx.EndFrame(.01f,false,true);C(!fx.Active&&fx.RateDrops==1,"focus cannot bypass rolling2");
 for(int i=0;i<4;i++){fx.BeginFrame(1,1,id);fx.EndFrame(.25f,false,true);}fx.BeginFrame(1,1,id);fx.Capture(E(7),0,7);fx.EndFrame(0,false,true);C(fx.Active&&fx.Started==3,"later fresh hit allowed");
 fx.BeginFrame(2,1,id);var n=new GameEvent(EventKind.Melee,id,new EntityId(8,2),new Vec2(0,0),new Vec2(1,0),1,0,2,1,FeedbackMechanism.BasicMelee);C(fx.Capture(n,0,0),"newscope pending before renderer");fx.EndFrame(0,false,true);C(fx.Active&&fx.Snapshot().Tick==0,"newscope pending survives");fx.Dispose();fx.Dispose();fx.BeginFrame(1,1,id);C(!fx.Capture(E(8),0,8),"disposed rejects");fx.EndFrame(.25f,false,true);C(!fx.Active,"dispose idempotent");}
 using(var fx=new CommonMeleeCue(this,cam)){fx.BeginFrame(1,1,id);C(!fx.Capture(E(-1),0,-1),"negative tick rejected");C(!fx.Capture(E(0,.001f),0,0)&&fx.ShortDrops==1,"zero/tiny not arbitrary direction");C(!fx.Capture(E(1,1,new EntityId(0,2)),0,1),"wrong generation rejected");C(!fx.Capture(E(1,1,m:FeedbackMechanism.Unknown),1,1),"unknown mechanism not captured");C(!fx.Capture(E(2,float.NaN),0,2)&&fx.InvalidDrops==1,"nonfinite rejected");C(!fx.Capture(E(3,1),0,3,true)&&fx.LossDrops==1,"loss rejection highwater");C(!fx.Capture(E(3),0,3),"loss no replay");C(fx.Capture(E(4,.3f),0,4),"short independent form");fx.EndFrame(.25f,false,true);C(fx.Active&&fx.Snapshot().Petal,"petal not scaled longfan");}
 using(var fx=new CommonMeleeCue(this,cam)){fx.BeginFrame(1,1,id);for(int i=0;i<12;i++)fx.Capture(E(i+1),0,i+1);C(fx.Pending==8&&fx.BudgetDrops==4,"fixed8 mailbox");fx.EndFrame(.25f,false,true);C(fx.Started==1&&fx.ActiveDrops==7,"one onset/render and no backlog");C(fx.Observed==12,"all uniqueMelee counted");fx.BeginFrame(1,1,id);fx.Capture(E(13),0,13);fx.EndFrame(.25f,true,true);C(fx.Pending==0&&fx.PausedDrops==1,"paused pending no replay");}
 Additional(cam,id);GD.Print("COMMON_MELEE_CUE_PASS "+checks);done=true;}catch(Exception e){GD.PushError(e.ToString());GetTree().Quit(1);}}
 public override void _Process(double d){if(done)GetTree().Quit();} void Additional(Camera3D cam,EntityId id){
  foreach(float length in new[]{.18f,.3f,.449f,.45f,1f,2f})foreach(float angle in new[]{0f,1f,3.14f}){
   using var fx=new CommonMeleeCue(this,cam);fx.BeginFrame(7,1,id);var f=new Vec2(MathF.Cos(angle),MathF.Sin(angle));var ev=new GameEvent(EventKind.Melee,id,new EntityId(4,1),new Vec2(0,0),f*length,1,0,7,1,FeedbackMechanism.BasicMelee);bool captured=fx.Capture(ev,0,0);if(!captured){C((ev.To-ev.From).Length<.18f&&fx.ShortDrops==1,"float threshold uses actual event L");continue;}fx.EndFrame(.25f,false,true);
   foreach(var v in fx.ProjectedVertices()){float longitudinal=v.X*f.X+v.Z*f.Y;C(float.IsFinite(v.X)&&float.IsFinite(v.Y)&&float.IsFinite(v.Z)&&longitudinal>=-1e-6f&&longitudinal<=length+1e-6f,"all final vertices finite and endpoint constrained L="+length+" angle="+angle+" projection="+longitudinal);}
  }
  var content=System.Text.Json.JsonSerializer.Deserialize<GameContent>(Godot.FileAccess.GetFileAsString("res://content/gameplay.json"),new System.Text.Json.JsonSerializerOptions{PropertyNameCaseInsensitive=true});var g=new GameSimulation(content);g.StartRun("crit",1030);
  var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
  var emit=typeof(GameSimulation).GetMethod("Emit",flags);var len=typeof(GameSimulation).GetField("EventLength",flags);
  void Emit(){g.TryGetEntity(g.PlayerId,out var playerView);emit.Invoke(g,new object[]{EventKind.Melee,g.PlayerId,new EntityId(5,2),playerView.Position,playerView.Position+new Vec2(1,0),1f,FeedbackMechanism.BasicMelee});}
  for(int i=0;i<2050;i++)Emit();C(g.EventCount==2048&&g.DroppedEvents==2,"actual Emit drop-newest cap");
  using(var fx=new CommonMeleeCue(this,cam)){
   fx.BeginFrame(g.RunToken,g.Wave,g.PlayerId);fx.CaptureStep(g);C(fx.LossDrops==2048&&fx.Pending==0,"CaptureStep first nonzero loss baseline rejects all");fx.CaptureStep(g);C(fx.DuplicateDrops==2048,"same buffer not replayed");
   g.Step();Emit();fx.BeginFrame(g.RunToken,g.Wave,g.PlayerId);fx.CaptureStep(g);fx.EndFrame(.25f,false,true);C(fx.Started==1,"no new loss accepts subsequent Step");
   g.Step();for(int i=0;i<2050;i++)Emit();int loss=fx.LossDrops;fx.BeginFrame(g.RunToken,g.Wave,g.PlayerId);fx.CaptureStep(g);C(fx.LossDrops-loss==2048,"incremental actual loss rejects new Step");
   g.StartRun("crit",1031);Emit();fx.CaptureStep(g);fx.BeginFrame(g.RunToken,g.Wave,g.PlayerId);fx.EndFrame(0,false,true);C(fx.Active&&fx.Snapshot().Run==g.RunToken,"new scope actual CaptureStep survives BeginFrame");
   fx.BeginFrame(99,1,id);fx.Capture(new GameEvent(EventKind.Melee,id,id,new Vec2(0,0),new Vec2(1,0),1,0,99,1,FeedbackMechanism.BasicMelee),0,0);int canceled=fx.ScopeDrops;fx.BeginFrame(100,1,id);C(fx.ScopeDrops==canceled+1,"scope cancellation counted");
  }
 }

}
