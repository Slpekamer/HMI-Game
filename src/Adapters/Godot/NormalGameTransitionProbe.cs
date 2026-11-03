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
 // Explicit catalog-loadout fixture, not a shop/economy or natural pickup acquisition test.
 void MaximumSpeedFixture(){
  var catalog=Field(sim,"Catalog");var cards=(CardDefinition[])Field(catalog,"Cards");var items=(ItemDefinition[])Field(catalog,"Items");
  var cardIndex=(Dictionary<string,int>)Field(catalog,"CardIndex");var itemIndex=(Dictionary<string,int>)Field(catalog,"ItemIndex");
  var levels=(int[])Field(Field(sim,"Loadout"),"Levels");
  foreach(var pair in new[]{("crit.reach",1),("crit.skirmish",1),("risk.fleet",2)}){int i=cardIndex[pair.Item1];Check(pair.Item2<=cards[i].Cap,"catalog speed-card cap unchanged");levels[i]=pair.Item2;}
  int shoes=itemIndex["shoes"];Check(items[shoes].Cap==2,"catalog shoe cap unchanged");var wallet=Field(sim,"Wallet");((int[])Field(wallet,"OwnedItems"))[shoes]=2;((int[])Field(wallet,"ActiveItems"))[shoes]=2;
  typeof(GameSimulation).Assembly.GetType("Hmi.Core.StatEffectSystem").GetMethod("Recalculate",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{sim});
  Check(Math.Abs(sim.GetStat(Stat.PlayerSpeed)-1.708f)<1e-5,"highest catalog speed recalculated");
  typeof(GameSimulation).GetField("HasteTime",F).SetValue(sim,8f);
 }
 object ActiveFastTransition(LaunchAimSample sample){
  MaximumSpeedFixture();var old=Presenter;var gait=Field(old,"_turningRun");int hit=-1;
  for(int i=0;i<30;i++){
   Step(-1);
   if((bool)Field(old,"_normalFastHeading")&&(float)Field(old,"_headingBridgeRemaining")>0&&(bool)Field(gait,"_turning")&&(float)Field(old,"_speed")>6){hit=i;break;}
  }
  Check(hit>=0,"UNCOVERED: real reverse input did not reach active high-speed heading within30ticks");
  var evidence=new{inputTicks=hit+1,tick=sim.Tick,runToken=sim.RunToken,wave=sim.Wave,
   playerSpeedStat=sim.GetStat(Stat.PlayerSpeed),sampledSpeed=(float)Field(old,"_speed"),
   normalFastHeading=(bool)Field(old,"_normalFastHeading"),headingRemaining=(float)Field(old,"_headingBridgeRemaining"),
   activeTurn=(bool)Field(gait,"_turning"),turnAge=(float)Field(gait,"_turnAge"),
   highSpeedRequested=(bool)Field(gait,"_normalHighSpeedRequested"),boundedTurn=(bool)Field(gait,"_normalBoundedTurn"),
   hadLaunch=old.LaunchAim.HasValue,fixture="highest catalog loadout plus8s haste; actual input, no injected heading flags"};
  // Capture before the actual UI disposal, so later failure still records the real hit.
  File.WriteAllText(Path.Combine(folder,"active-fast-before.json"),JsonSerializer.Serialize(evidence));
  SelectHero(0,sample,"active fast crit to orbit");
  Check(!(bool)Field(old,"_normalFastHeading"),"disposed active fast heading cleared");
  foreach(string n in new[]{"_normalEntryLiftPending","_normalEntryLift","_normalHighSpeedRequested","_normalBoundedTurn","_turning"})
   Check(!(bool)Field(gait,n),"disposed active gait state cleared: "+n);
  Check(!(bool)Field(Presenter,"_normalFastHeading"),"new other hero has no fast-heading residue");
  for(int i=0;i<72;i++)Step(i<30?0:-1);
  return evidence;
 }
 public override void _Ready(){try{
  folder=OS.GetEnvironment("NORMAL_TRANSITION_OUTPUT");if(string.IsNullOrEmpty(folder))throw new Exception("Set NORMAL_TRANSITION_OUTPUT; use fresh isolated XDG_DATA_HOME");Directory.CreateDirectory(folder);
  Check(!Directory.Exists(ProjectSettings.GlobalizePath("user://checkpoints")),"fresh isolated checkpoint directory required");Check(NormalPresentationCandidate.Enabled,"normal enabled");ControlledGaitDrill.Enabled=false;
  var box=new SubViewportContainer{Size=new(1280,720)};AddChild(box);var viewport=new SubViewport{Size=new(1280,720),OwnWorld3D=true};box.AddChild(viewport);host=new GameHost();viewport.AddChild(host);host.SetProcess(false);
  Host<GameUi>("_ui").StartRequested(1,1030);var initial=ButtonAt(host,"确认准备新局");Check(initial!=null,"fresh rule confirmation visible");initial.EmitSignal(Button.SignalName.Pressed);Host<GameUi>("_ui").StartRequested(1,1030);host.SetProcess(false);
  host.RegisterMovementProvider("transition-probe",new MoveProviderOptions(100,0));sim=Host<GameSimulation>("_sim");NewPresentation(true,null,"initial crit");
  var launch=NaturalLaunch();var old=Presenter;var oldId=sim.PlayerId;int oldWave=sim.Wave,oldToken=sim.RunToken;long originalTick=sim.Tick;
  // Wave-end fixture mutates the clock only; separate labeled catalog/haste fixture follows below.
  // No Phase, PlayerId, health or ResetTransient injection.
  object run=Field(sim,"Run");run.GetType().GetField("Tick",F).SetValue(run,(long)WaveRules.DurationTicks);
  Step();Check(sim.Phase==RunPhase.Between,"clock boundary triggers ordinary wave-end system");OfferSlotView pick=null;foreach(var offer in sim.GetOfferSlots())if(offer.Enabled){pick=offer;break;}
  Check(pick!=null,"eligible actual offer");var ui=Host<GameUi>("_ui");ui.PickRequested(pick.Card.Id);Step();Check(sim.CardPicked,"real UI choose accepted");Check(ui.NextRequested!=null,"real next route bound");ui.NextRequested();
  Retired(old,launch,"next wave");NewPresentation(true,launch,"next wave crit");Check(sim.RunToken==oldToken&&sim.Wave==oldWave+1,"same run next wave");Check(sim.PlayerId!=oldId&&!sim.TryGetEntity(oldId,out _),"old full entity generation invalidated");
  launch=NaturalLaunch();SelectHero(0,launch,"crit to orbit");for(int i=0;i<72;i++)Step(i<30?0:-1);SelectHero(1,launch,"orbit to crit");launch=NaturalLaunch();
  int baseChecks=checks,baseAdvances=advances;var activeFast=ActiveFastTransition(launch);
  File.WriteAllText(Path.Combine(folder,"summary.json"),JsonSerializer.Serialize(new{pass=true,checks,advances,maxSteps,baseChecks,baseAdvances,activeFast,transitions,clockFixture=new{fromTick=originalTick,toTick=WaveRules.DurationTicks,purpose="trigger actual wave-end; not full-wave survival/economy"},scope="real rule confirmation, UI Choose/Next checkpoint, selection/new-run, disposed presenter, hero isolation; no native visual or persistence fault test"},new JsonSerializerOptions{WriteIndented=true}));GD.Print($"NORMAL_GAME_TRANSITION_PASS {checks}");done=true;
 }catch(Exception e){if(!string.IsNullOrEmpty(folder))File.WriteAllText(Path.Combine(folder,"failure.json"),JsonSerializer.Serialize(new{checks,advances,transitions,error=e.ToString()}));GD.PushError(e.ToString());GetTree().Quit(1);}}
 public override void _Process(double delta){if(done)GetTree().Quit();}
}
