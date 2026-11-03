using Godot;
using Hmi.Core;
using Hmi.GodotAdapter;
using Hmi.Adapters.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

/// <summary>Real UI/host checkpoint transitions. Wave-end clock acceleration is a test fixture,
/// not full-wave survival, economy, rendering, or persistence-fault certification.</summary>
public partial class NormalGameTransitionProbe : Node
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 GameHost host;GameSimulation sim;string folder;bool done;int checks,advances,maxSteps;
 readonly List<object> transitions=new();
 static object Field(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
 T Host<T>(string n)=>(T)Field(host,n);
 void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static Button ButtonAt(Node n,string text){if(n is Button b&&b.Text==text)return b;foreach(Node c in n.GetChildren()){var found=ButtonAt(c,text);if(found!=null)return found;}return null;}
 void Input(float x){typeof(GameHost).GetMethod("SynchronizeMovementInput",F).Invoke(host,null);var router=Host<MoveInputRouter>("_moveInputs");Check(host.SubmitMovementIntent(router.Token("transition-probe"),new MoveIntent(x,0)),"current-epoch movement input");}
 CharacterPresentation Presenter=>Host<CharacterPresentation[]>("_presentations")[sim.PlayerId.Index];
 void Step(float x=0){Input(x);host._Process(GameSimulation.FixedDelta);advances++;if(sim.Phase==RunPhase.Combat){var p=Presenter;Check(p!=null&&!p.IsDisposed,"combat presenter alive");maxSteps=Math.Max(maxSteps,p.LastPresentationSteps);Check(p.LastPresentationSteps>=1&&p.LastPresentationSteps<=120,"presentation budget");}}
 void Retired(CharacterPresentation old,LaunchAimSample sample,string label){
  Check(old.IsDisposed,label+": real lifecycle disposed old presenter");
  Check(!old.LaunchAim.HasValue&&!old.AcceptLaunchAim(sample),label+": retired launch cleared/rejected");
  Check((float)Field(old,"_headingBridgeRemaining")==0&&(float)Field(old,"_headingLiftRemaining")==0,label+": heading cleared");
  Check(((IDictionary)Field(host,"_pendingLaunchAim")).Count==0,label+": host aim queue cleared");
  foreach(var value in (Array)Field(host,"_pendingLaunches"))Check(value==null,label+": host launch queue cleared");
 }
 void NewPresentation(bool crit,LaunchAimSample? stale,string label){
  host.SetProcess(false);sim=Host<GameSimulation>("_sim");Check(sim.Phase==RunPhase.Combat,label+": real checkpoint route entered combat");Step();var p=Presenter;
  Check(p.UsesNormalPresentation==crit,label+": hero isolation");
  Check((Field(p,"_turningRun")!=null)==crit&&(Field(p,"_launchPose")!=null)==crit,label+": expected owners");
  Check(Field(p,"_movingAim")==null,label+": no old aim owner");Check(!p.LaunchAim.HasValue,label+": no inherited launch");
  if(stale.HasValue)Check(!p.AcceptLaunchAim(stale.Value),label+": prior scope rejected");
  Check(sim.TryGetEntity(sim.PlayerId,out var view),label+": full current id alive");
  var root=Host<Node3D[]>("_visuals")[sim.PlayerId.Index];var target=new Vector3(view.Position.X,0,view.Position.Y);
  Check(root.Position.DistanceTo(target)<.000001f,label+": initial root at target");
  Check((EntityId)Field(p,"_entity")==sim.PlayerId,label+": full index/generation binding");
  Check((float)Field(p,"_headingBridgeRemaining")==0&&(float)Field(p,"_headingLiftRemaining")==0,label+": no inherited heading bridge");
  transitions.Add(new{label,hero=sim.CharacterId,runToken=sim.RunToken,wave=sim.Wave,index=sim.PlayerId.Index,generation=sim.PlayerId.Generation,rootError=root.Position.DistanceTo(target),crit});
 }
 LaunchAimSample NaturalLaunch(){for(int i=0;i<72;i++)Step(i<30?0:1);Check(Presenter.LaunchAim.HasValue,"natural launch populated held direction before transition");return Presenter.LaunchAim.Value;}
 void SelectHero(int hero,LaunchAimSample stale,string label){
  var old=Presenter;int token=sim.RunToken;var ui=Host<GameUi>("_ui");Check(ui.RestartRequested!=null,label+": selection route bound");ui.RestartRequested();Retired(old,stale,label);
  Check(Array.TrueForAll(Host<CharacterPresentation[]>("_presentations"),p=>p==null),label+": selection cleared all presenters");
  Check(ui.StartRequested!=null,label+": start route bound");ui.StartRequested(hero,1030);
  if(ui.IsPersistenceModalOpen){var confirm=ButtonAt(host,"确认开始新局");Check(confirm!=null,label+": expected replacement confirmation");confirm.EmitSignal(Button.SignalName.Pressed);}
  NewPresentation(hero==1,stale,label);Check(sim.RunToken!=token,label+": distinct new run scope");
 }
 public override void _Ready(){try{
  folder=OS.GetEnvironment("NORMAL_TRANSITION_OUTPUT");if(string.IsNullOrEmpty(folder))throw new Exception("Set NORMAL_TRANSITION_OUTPUT; use fresh isolated XDG_DATA_HOME");Directory.CreateDirectory(folder);
  Check(!Directory.Exists(ProjectSettings.GlobalizePath("user://checkpoints")),"fresh isolated checkpoint directory required");Check(NormalPresentationCandidate.Enabled,"normal enabled");ControlledGaitDrill.Enabled=false;
  var box=new SubViewportContainer{Size=new(1280,720)};AddChild(box);var viewport=new SubViewport{Size=new(1280,720),OwnWorld3D=true};box.AddChild(viewport);host=new GameHost();viewport.AddChild(host);host.SetProcess(false);
  Host<GameUi>("_ui").StartRequested(1,1030);var initial=ButtonAt(host,"确认准备新局");Check(initial!=null,"fresh rule confirmation visible");initial.EmitSignal(Button.SignalName.Pressed);Host<GameUi>("_ui").StartRequested(1,1030);host.SetProcess(false);
  host.RegisterMovementProvider("transition-probe",new MoveProviderOptions(100,0));sim=Host<GameSimulation>("_sim");NewPresentation(true,null,"initial crit");
  var launch=NaturalLaunch();var old=Presenter;var oldId=sim.PlayerId;int oldWave=sim.Wave,oldToken=sim.RunToken;long originalTick=sim.Tick;
  // Sole Core fixture mutation: accelerate clock only. No Phase, PlayerId, health or ResetTransient injection.
  object run=Field(sim,"Run");run.GetType().GetField("Tick",F).SetValue(run,(long)WaveRules.DurationTicks);
  Step();Check(sim.Phase==RunPhase.Between,"clock boundary triggers ordinary wave-end system");OfferSlotView pick=null;foreach(var offer in sim.GetOfferSlots())if(offer.Enabled){pick=offer;break;}
  Check(pick!=null,"eligible actual offer");var ui=Host<GameUi>("_ui");ui.PickRequested(pick.Card.Id);Step();Check(sim.CardPicked,"real UI choose accepted");Check(ui.NextRequested!=null,"real next route bound");ui.NextRequested();
  Retired(old,launch,"next wave");NewPresentation(true,launch,"next wave crit");Check(sim.RunToken==oldToken&&sim.Wave==oldWave+1,"same run next wave");Check(sim.PlayerId!=oldId&&!sim.TryGetEntity(oldId,out _),"old full entity generation invalidated");
  launch=NaturalLaunch();SelectHero(0,launch,"crit to orbit");for(int i=0;i<72;i++)Step(i<30?0:-1);SelectHero(1,launch,"orbit to crit");NaturalLaunch();
  File.WriteAllText(Path.Combine(folder,"summary.json"),JsonSerializer.Serialize(new{pass=true,checks,advances,maxSteps,transitions,clockFixture=new{fromTick=originalTick,toTick=WaveRules.DurationTicks,purpose="trigger actual wave-end; not full-wave survival/economy"},scope="real rule confirmation, UI Choose/Next checkpoint, selection/new-run, disposed presenter, hero isolation; no native visual or persistence fault test"},new JsonSerializerOptions{WriteIndented=true}));GD.Print($"NORMAL_GAME_TRANSITION_PASS {checks}");done=true;
 }catch(Exception e){if(!string.IsNullOrEmpty(folder))File.WriteAllText(Path.Combine(folder,"failure.json"),JsonSerializer.Serialize(new{checks,advances,transitions,error=e.ToString()}));GD.PushError(e.ToString());GetTree().Quit(1);}}
 public override void _Process(double delta){if(done)GetTree().Quit();}
}
