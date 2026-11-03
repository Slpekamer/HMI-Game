using Godot;
using Hmi.Core;
using Hmi.GodotAdapter;
using Hmi.Adapters.Input;
using System;
using System.Reflection;
using System.Text.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;
/// <summary>Scripted inputs through normal GameHost; controlled gait fixture stays disabled.</summary>
public partial class NormalGameProbe:Node
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
 sealed class V2Json:System.Text.Json.Serialization.JsonConverter<Vec2>{public override Vec2 Read(ref Utf8JsonReader r,Type t,JsonSerializerOptions o)=>throw new NotSupportedException();public override void Write(Utf8JsonWriter w,Vec2 v,JsonSerializerOptions o){w.WriteStartArray();w.WriteNumberValue(v.X);w.WriteNumberValue(v.Y);w.WriteEndArray();}}
 static readonly JsonSerializerOptions J=Options();static JsonSerializerOptions Options(){var o=new JsonSerializerOptions{IncludeFields=true};o.Converters.Add(new V2Json());return o;}
 GameHost host;GameSimulation sim;int frame,hero,rate,launches,melee,maxSteps;bool ready;string folder;float seconds;
 readonly StringBuilder authority=new(),poses=new();
 T Field<T>(string name)=>(T)typeof(GameHost).GetField(name,F).GetValue(host);
 void Call(string name,params object[] args)=>typeof(GameHost).GetMethod(name,F).Invoke(host,args);
 static Button ButtonAt(Node n,string text){if(n is Button b&&b.Text==text)return b;foreach(Node c in n.GetChildren()){var b2=ButtonAt(c,text);if(b2!=null)return b2;}return null;}
 static T Find<T>(Node n)where T:Node{if(n is T t)return t;foreach(Node c in n.GetChildren()){var found=Find<T>(c);if(found!=null)return found;}return null;}
 void Require(bool ok,string why){if(!ok)throw new Exception(why);}
 public override void _Ready(){try{
 folder=OS.GetEnvironment("NORMAL_PROBE_OUTPUT");Require(folder.Length>0,"Missing output directory");Directory.CreateDirectory(folder);
 hero=int.TryParse(OS.GetEnvironment("NORMAL_PROBE_HERO"),out var h)?h:1;rate=int.TryParse(OS.GetEnvironment("NORMAL_PROBE_RATE"),out var fps)?fps:60;
 Require(rate>=15&&rate<=240,"unsupported test rate");Require(!ControlledGaitDrill.Enabled,"normal test accidentally uses controlled gait");
 var box=new SubViewportContainer{Size=new(1280,720)};AddChild(box);var viewport=new SubViewport{Size=new(1280,720),OwnWorld3D=true,RenderTargetUpdateMode=SubViewport.UpdateMode.Always};box.AddChild(viewport);host=new GameHost();viewport.AddChild(host);host.SetProcess(false);
 Call("Start",hero,(uint)1030);var confirm=ButtonAt(host,"确认准备新局");Require(confirm!=null,"Fresh isolated data directory required");confirm.EmitSignal(Button.SignalName.Pressed);Call("Start",hero,(uint)1030);host.SetProcess(false);host.RegisterMovementProvider("route",new MoveProviderOptions(100,0));sim=Field<GameSimulation>("_sim");Require(sim.Phase==RunPhase.Combat,"fresh start not combat");
 if(OS.GetEnvironment("NORMAL_PROBE_HASTE")=="1")typeof(GameSimulation).GetField("HasteTime",F).SetValue(sim,8f);
 ready=true;
 }catch(Exception e){Fail(e);}}
 object State(){var entities=new List<object>();for(int i=0;i<sim.Capacity;i++)if(sim.TryGetEntityAtSlot(i,out var e))entities.Add(new{e.Id,e.Kind,e.Position,e.Facing,e.Health,e.MaxHealth});var events=new List<GameEvent>();for(int i=0;i<sim.EventCount;i++)events.Add(sim.GetEvent(i));var pending=new List<object>();for(int i=0;i<sim.PendingSpawnCount;i++)pending.Add(sim.GetPendingSpawn(i));var projectiles=new List<object>();var transitions=new List<object>();var type=typeof(GameSimulation);var count=type.GetProperty("ProjectileCapacity");if(count!=null){var get=type.GetMethod("TryGetProjectileAtSlot");for(int i=0;i<(int)count.GetValue(sim);i++){object[] args={i,null};if((bool)get.Invoke(sim,args))projectiles.Add(args[1]);}int n=(int)type.GetProperty("ProjectileTransitionCount").GetValue(sim);for(int i=0;i<n;i++)transitions.Add(type.GetMethod("GetProjectileTransition").Invoke(sim,new object[]{i}));}var run=type.GetField("Run",F).GetValue(sim);uint rng=(uint)run.GetType().GetField("Random",F).GetValue(run);return new{frame,sim.Tick,sim.RunToken,sim.Wave,sim.Phase,sim.Paused,sim.Coins,sim.Earned,sim.Spent,sim.Kills,rng,sim.DroppedEvents,entities,events,pending,projectiles,transitions};}
 MoveIntent Intent(float t){if(t<.5f||t>=6)return new(0,0);if(t<2)return new(-1,0);if(t<3)return new(0,1);if(t<4)return new(1,0);if(t<5)return new(0,-1);return new(-1,0);}
 public override void _Process(double ignored){if(!ready)return;try{
 float dt=1f/rate;if(OS.GetEnvironment("NORMAL_PROBE_LONG_FRAME")=="1"&&frame==rate*2)dt=.25f;
 Call("SynchronizeMovementInput");var router=Field<MoveInputRouter>("_moveInputs");Require(host.SubmitMovementIntent(router.Token("route"),Intent(seconds)),"route rejected");host._Process(dt);seconds+=dt;
 var p=Field<CharacterPresentation[]>("_presentations")[sim.PlayerId.Index];var root=Field<Node3D[]>("_visuals")[sim.PlayerId.Index];bool expected=NormalPresentationCandidate.Enabled&&hero==1;
 Require(p.UsesNormalPresentation==expected,"player identity selection");Require((typeof(CharacterPresentation).GetField("_turningRun",F).GetValue(p)!=null)==expected,"turning owner mismatch");Require((typeof(CharacterPresentation).GetField("_launchPose",F).GetValue(p)!=null)==expected,"launch owner mismatch");Require(p.MovingAimDiagnostics==null,"old aim leaked into normal path");
 if(expected)Require(root.SceneFilePath.Contains("accepted-static-rebuilt"),"wrong normal rig");
 maxSteps=Math.Max(maxSteps,p.LastPresentationSteps);
 for(int i=0;i<sim.Capacity;i++)if(sim.TryGetEntityAtSlot(i,out var e)&&e.Kind==EntityKind.Enemy){var ep=Field<CharacterPresentation[]>("_presentations")[i];if(ep!=null)Require(!ep.UsesNormalPresentation&&typeof(CharacterPresentation).GetField("_turningRun",F).GetValue(ep)==null,"enemy inherited hero experiment");}
 var light=Find<DirectionalLight3D>(host);Require(Math.Abs(light.DirectionalShadowSplit1-(NormalPresentationCandidate.Enabled?.015f:.1f))<1e-6,"shadow split mismatch");
 for(int i=0;i<sim.ProjectileTransitionCount;i++){var e=sim.GetProjectileTransition(i);if(e.Kind==ProjectileTransitionKind.Launched&&e.Owner==sim.PlayerId)launches++;}
 for(int i=0;i<sim.EventCount;i++){var e=sim.GetEvent(i);if(e.Kind==EventKind.Melee&&e.Source==sim.PlayerId)melee++;}
 authority.AppendLine(JsonSerializer.Serialize(State(),J));poses.AppendLine(JsonSerializer.Serialize(new{frame,seconds,dt,rootPosition=new[]{root.Position.X,root.Position.Y,root.Position.Z},rootYaw=root.Rotation.Y,p.LastPresentationSteps,p.LaunchAim,launchPose=p.LaunchPoseDiagnostics,gait=typeof(CharacterPresentation).GetField("_turningRun",F).GetValue(p)?.GetType().GetProperty("Diagnostics").GetValue(typeof(CharacterPresentation).GetField("_turningRun",F).GetValue(p))},J));frame++;
 if(seconds>=10||sim.Phase!=RunPhase.Combat){ready=false;File.WriteAllText(folder+"/authority.jsonl",authority.ToString());File.WriteAllText(folder+"/presentation.jsonl",poses.ToString());File.WriteAllText(folder+"/summary.json",JsonSerializer.Serialize(new{frame,seconds,rate,hero,launches,melee,maxSteps,sim.Phase,candidate=NormalPresentationCandidate.Enabled,hasteFixture=OS.GetEnvironment("NORMAL_PROBE_HASTE")=="1",scope="Scripted inputs through normal GameHost, no controlled gait flags. Haste field injection only in explicitly labeled fixture."},J));GD.Print($"NORMAL_GAME_PROBE_PASS frames={frame} rate={rate} steps={maxSteps}");GetTree().Quit();}
 }catch(Exception e){Fail(e);}}
 void Fail(Exception e){ready=false;if(!string.IsNullOrEmpty(folder)){File.WriteAllText(folder+"/failure.json",JsonSerializer.Serialize(new{frame,seconds,error=e.ToString()},J));File.WriteAllText(folder+"/authority-partial.jsonl",authority.ToString());File.WriteAllText(folder+"/presentation-partial.jsonl",poses.ToString());}GD.PushError(e.ToString());GetTree().Quit(1);}
}
