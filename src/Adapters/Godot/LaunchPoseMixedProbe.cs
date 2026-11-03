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
/// <summary>Unmodified simulation mixed-event observation, not synthetic combat events.</summary>
public partial class LaunchPoseMixedProbe:Node
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
 sealed class V2Json:System.Text.Json.Serialization.JsonConverter<Vec2>{public override Vec2 Read(ref Utf8JsonReader r,Type t,JsonSerializerOptions o)=>throw new NotSupportedException();public override void Write(Utf8JsonWriter w,Vec2 v,JsonSerializerOptions o){w.WriteStartArray();w.WriteNumberValue(v.X);w.WriteNumberValue(v.Y);w.WriteEndArray();}}
 static readonly JsonSerializerOptions J=Options();static JsonSerializerOptions Options(){var o=new JsonSerializerOptions{IncludeFields=true};o.Converters.Add(new V2Json());return o;}
 GameHost host;GameSimulation sim;int frame,melee,launches,cancels;bool ready;string folder;
 readonly StringBuilder authority=new(),cues=new();
 T Field<T>(string name)=>(T)typeof(GameHost).GetField(name,F).GetValue(host);
 void Call(string name,params object[] args)=>typeof(GameHost).GetMethod(name,F).Invoke(host,args);
 static Button ButtonAt(Node n,string text){if(n is Button b&&b.Text==text)return b;foreach(Node c in n.GetChildren()){var b2=ButtonAt(c,text);if(b2!=null)return b2;}return null;}
 public override void _Ready(){try{
 folder=OS.GetEnvironment("LAUNCH_POSE_MIXED_OUTPUT");if(folder.Length==0)throw new Exception("Missing mixed output");Directory.CreateDirectory(folder);
   ControlledGaitDrill.Enabled=ControlledGaitDrill.TurningRun=ControlledGaitDrill.DisplacementRun=ControlledGaitDrill.Candidate=ControlledGaitDrill.ProportionCandidate=true;ControlledGaitDrill.Clip="Run";
   var box=new SubViewportContainer{Size=new(1280,720)};AddChild(box);var viewport=new SubViewport{Size=new(1280,720),OwnWorld3D=true};box.AddChild(viewport);host=new GameHost();viewport.AddChild(host);host.SetProcess(false);Call("Start",1,(uint)1030);ButtonAt(host,"确认准备新局").EmitSignal(Button.SignalName.Pressed);Call("Start",1,(uint)1030);host.SetProcess(false);host.RegisterMovementProvider("route",new MoveProviderOptions(100,0));sim=Field<GameSimulation>("_sim");

 ready=true;
 }catch(Exception e){GD.PushError(e.ToString());GetTree().Quit(1);}}
 object State(){var entities=new List<object>();for(int i=0;i<sim.Capacity;i++)if(sim.TryGetEntityAtSlot(i,out var e))entities.Add(new{e.Id,e.Kind,e.Position,e.Facing,e.Health,e.MaxHealth});var events=new List<GameEvent>();for(int i=0;i<sim.EventCount;i++)events.Add(sim.GetEvent(i));var pending=new List<object>();for(int i=0;i<sim.PendingSpawnCount;i++)pending.Add(sim.GetPendingSpawn(i));var projectiles=new List<object>();var transitions=new List<object>();var type=typeof(GameSimulation);var count=type.GetProperty("ProjectileCapacity");if(count!=null){var get=type.GetMethod("TryGetProjectileAtSlot");for(int i=0;i<(int)count.GetValue(sim);i++){object[] args={i,null};if((bool)get.Invoke(sim,args))projectiles.Add(args[1]);}int n=(int)type.GetProperty("ProjectileTransitionCount").GetValue(sim);for(int i=0;i<n;i++)transitions.Add(type.GetMethod("GetProjectileTransition").Invoke(sim,new object[]{i}));}var run=type.GetField("Run",F).GetValue(sim);uint rng=(uint)run.GetType().GetField("Random",F).GetValue(run);return new{frame,sim.Tick,sim.RunToken,sim.Wave,sim.Phase,sim.Paused,sim.Coins,sim.Earned,sim.Spent,sim.Kills,rng,sim.DroppedEvents,entities,events,pending,projectiles,transitions};}
 public override void _Process(double ignored){if(!ready)return;try{
 Call("SynchronizeMovementInput");var router=Field<MoveInputRouter>("_moveInputs");
 // Stand still so real enemies approach and the actual independent melee system fires.
 if(!host.SubmitMovementIntent(router.Token("route"),new MoveIntent(0,0)))throw new Exception("route rejected");
 host._Process(GameSimulation.FixedDelta);
 var p=Field<CharacterPresentation[]>("_presentations")[sim.PlayerId.Index];
 var playerMelee=new List<long>();var playerLaunch=new List<long>();
 for(int i=0;i<sim.EventCount;i++){var e=sim.GetEvent(i);if(e.Source==sim.PlayerId&&e.Kind==EventKind.Melee){melee++;playerMelee.Add(e.Tick);}}
 for(int i=0;i<sim.ProjectileTransitionCount;i++){var e=sim.GetProjectileTransition(i);if(e.Owner==sim.PlayerId&&e.Kind==ProjectileTransitionKind.Launched){launches++;playerLaunch.Add(e.Tick);}}
 string diag=JsonSerializer.Serialize(p.LaunchPoseDiagnostics,J);if(diag.Contains("cancel-to-original-gesture"))cancels++;
 authority.AppendLine(JsonSerializer.Serialize(State(),J));cues.AppendLine(JsonSerializer.Serialize(new{frame,sim.Tick,playerMelee,playerLaunch,sample=p.LaunchAim,pose=p.LaunchPoseDiagnostics},J));frame++;
 if(frame>=1200||sim.Phase!=RunPhase.Combat){ready=false;File.WriteAllText(folder+"/authority.jsonl",authority.ToString());File.WriteAllText(folder+"/cues.jsonl",cues.ToString());File.WriteAllText(folder+"/summary.json",JsonSerializer.Serialize(new{frame,melee,launches,cancels,sim.Phase},J));if(melee==0||launches==0)throw new Exception("No actual mixed-event coverage");GD.Print($"LAUNCH_POSE_MIXED_PASS frames={frame} melee={melee} launches={launches} cancelFrames={cancels}");GetTree().Quit();}
 }catch(Exception e){GD.PushError(e.ToString());GetTree().Quit(1);}}
}
