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
 GameHost host;GameSimulation sim;int frame,hero,rate,launches,melee,maxSteps;bool ready,native;string folder;float seconds;int pendingFrame=-1;ColorRect[] stamp=new ColorRect[10];Camera3D camera;ControlledGaitSkin skin;
 readonly StringBuilder authority=new(),poses=new(),bones=new(),cameras=new(),soles=new(),boots=new(),attachments=new(),substeps=new();
 T Field<T>(string name)=>(T)typeof(GameHost).GetField(name,F).GetValue(host);
 void Call(string name,params object[] args)=>typeof(GameHost).GetMethod(name,F).Invoke(host,args);
 static Button ButtonAt(Node n,string text){if(n is Button b&&b.Text==text)return b;foreach(Node c in n.GetChildren()){var b2=ButtonAt(c,text);if(b2!=null)return b2;}return null;}
 static T Find<T>(Node n)where T:Node{if(n is T t)return t;foreach(Node c in n.GetChildren()){var found=Find<T>(c);if(found!=null)return found;}return null;}
 void Require(bool ok,string why){if(!ok)throw new Exception(why);}
 static string N(float n)=>n.ToString("R",System.Globalization.CultureInfo.InvariantCulture);
 static string V(Vector3 v)=>N(v.X)+","+N(v.Y)+","+N(v.Z);
 static string T(Transform3D t)=>V(t.Origin)+"/"+V(t.Basis.X)+"/"+V(t.Basis.Y)+"/"+V(t.Basis.Z);
 string Pose(Node n){var b=new StringBuilder();void Walk(Node x){if(x is Skeleton3D sk)for(int j=0;j<sk.GetBoneCount();j++)b.Append(sk.GetBoneName(j)).Append(':').Append(T(sk.GetBonePose(j))).Append(';');if(x is MeshInstance3D m&&m.Mesh!=null)for(int j=0;j<m.GetBlendShapeCount();j++)b.Append(m.Name).Append(j).Append(':').Append(N(m.GetBlendShapeValue(j))).Append(';');foreach(Node c in x.GetChildren())Walk(c);}Walk(n);return b.ToString();}
 // Fixture-only loadout: real catalog effects/caps, not a production stat override or shop-economy test.
 void MaximumSpeedFixture(){
 Require(hero==1,"maximum-speed loadout is defined for Crit");
 object Get(object o,string name)=>o.GetType().GetField(name,F).GetValue(o);
 var catalog=Get(sim,"Catalog");var cards=(CardDefinition[])Get(catalog,"Cards");var items=(ItemDefinition[])Get(catalog,"Items");
 var cardIndex=(Dictionary<string,int>)Get(catalog,"CardIndex");var itemIndex=(Dictionary<string,int>)Get(catalog,"ItemIndex");
 var levels=(int[])Get(Get(sim,"Loadout"),"Levels");
 foreach(var pair in new[]{("crit.reach",1),("crit.skirmish",1),("risk.fleet",2)}){int i=cardIndex[pair.Item1];Require(pair.Item2<=cards[i].Cap,"catalog speed-card cap changed");levels[i]=pair.Item2;}
 int shoes=itemIndex["shoes"];Require(items[shoes].Cap==2,"catalog shoe cap changed");var wallet=Get(sim,"Wallet");((int[])Get(wallet,"OwnedItems"))[shoes]=2;((int[])Get(wallet,"ActiveItems"))[shoes]=2;
 typeof(GameSimulation).Assembly.GetType("Hmi.Core.StatEffectSystem").GetMethod("Recalculate",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{sim});
 Require(Math.Abs(sim.GetStat(Stat.PlayerSpeed)-1.708f)<1e-5,"maximum-speed catalog expectation changed");
 }
 public override void _Ready(){try{
 folder=OS.GetEnvironment("NORMAL_PROBE_OUTPUT");Require(folder.Length>0,"Missing output directory");Directory.CreateDirectory(folder);
 hero=int.TryParse(OS.GetEnvironment("NORMAL_PROBE_HERO"),out var h)?h:1;rate=int.TryParse(OS.GetEnvironment("NORMAL_PROBE_RATE"),out var fps)?fps:60;
 Require(rate>=15&&rate<=240,"unsupported test rate");Require(!ControlledGaitDrill.Enabled,"normal test accidentally uses controlled gait");
 var box=new SubViewportContainer{Size=new(1280,720)};AddChild(box);var viewport=new SubViewport{Size=new(1280,720),OwnWorld3D=true,RenderTargetUpdateMode=SubViewport.UpdateMode.Always};box.AddChild(viewport);host=new GameHost();viewport.AddChild(host);host.SetProcess(false);
 Call("Start",hero,(uint)1030);var confirm=ButtonAt(host,"确认准备新局");Require(confirm!=null,"Fresh isolated data directory required");confirm.EmitSignal(Button.SignalName.Pressed);Call("Start",hero,(uint)1030);host.SetProcess(false);host.RegisterMovementProvider("route",new MoveProviderOptions(100,0));sim=Field<GameSimulation>("_sim");Require(sim.Phase==RunPhase.Combat,"fresh start not combat");
 if(OS.GetEnvironment("NORMAL_PROBE_MAX_SPEED")=="1")MaximumSpeedFixture();
 if(OS.GetEnvironment("NORMAL_PROBE_HASTE")=="1")typeof(GameSimulation).GetField("HasteTime",F).SetValue(sim,8f);
 Call("RenderEntities",0f);
 var observed=Field<CharacterPresentation[]>("_presentations")[sim.PlayerId.Index];
 observed.ObservePresentationStep=dt=>substeps.AppendLine(JsonSerializer.Serialize(new{frame,subDt=dt,graphCalls=observed.AnimationGraphAdvanceCount,graphSeconds=observed.AnimationGraphAdvancedSeconds,gait=observed.NormalGaitDiagnostics,launchPose=observed.LaunchPoseDiagnostics},J));
 camera=Field<Camera3D>("_camera");native=OS.GetEnvironment("NORMAL_PROBE_NATIVE")=="1";
 if(native){Require(rate==60,"Native capture requires 60 Hz");GetWindow().Size=new(1280,720);GetWindow().ContentScaleSize=new(1280,720);GetWindow().ContentScaleMode=Window.ContentScaleModeEnum.Viewport;GetWindow().ContentScaleAspect=Window.ContentScaleAspectEnum.Keep;var layer=new CanvasLayer{Layer=99};viewport.AddChild(layer);for(int i=0;i<10;i++){stamp[i]=new ColorRect{Position=new(8+i*14,696),Size=new(12,12),Color=Colors.Black,MouseFilter=Control.MouseFilterEnum.Ignore};layer.AddChild(stamp[i]);}RenderingServer.FramePostDraw+=AfterDraw;}
 ready=true;
 }catch(Exception e){Fail(e);}}
 object State(){var entities=new List<object>();for(int i=0;i<sim.Capacity;i++)if(sim.TryGetEntityAtSlot(i,out var e))entities.Add(new{e.Id,e.Kind,e.Position,e.Facing,e.Health,e.MaxHealth});var events=new List<GameEvent>();for(int i=0;i<sim.EventCount;i++)events.Add(sim.GetEvent(i));var pending=new List<object>();for(int i=0;i<sim.PendingSpawnCount;i++)pending.Add(sim.GetPendingSpawn(i));var projectiles=new List<object>();var transitions=new List<object>();var type=typeof(GameSimulation);var count=type.GetProperty("ProjectileCapacity");if(count!=null){var get=type.GetMethod("TryGetProjectileAtSlot");for(int i=0;i<(int)count.GetValue(sim);i++){object[] args={i,null};if((bool)get.Invoke(sim,args))projectiles.Add(args[1]);}int n=(int)type.GetProperty("ProjectileTransitionCount").GetValue(sim);for(int i=0;i<n;i++)transitions.Add(type.GetMethod("GetProjectileTransition").Invoke(sim,new object[]{i}));}var run=type.GetField("Run",F).GetValue(sim);uint rng=(uint)run.GetType().GetField("Random",F).GetValue(run);return new{frame,sim.Tick,sim.RunToken,sim.Wave,sim.Phase,sim.Paused,sim.Coins,sim.Earned,sim.Spent,sim.Kills,rng,sim.DroppedEvents,entities,events,pending,projectiles,transitions};}
 MoveIntent Intent(float t){
 if(OS.GetEnvironment("NORMAL_PROBE_ROUTE")=="interior-stops"){
  if(t<.5f)return new(0,0);if(t<.85f)return new(-1,0);if(t<1.2f)return new(0,0);
  if(t<1.55f)return new(1,0);if(t<1.8f)return new(0,0);if(t<2.2f)return new(0,-1);return new(0,0);
 }
 if(t<.5f||t>=6)return new(0,0);if(t<2)return new(-1,0);if(t<3)return new(0,1);if(t<4)return new(1,0);if(t<5)return new(0,-1);return new(-1,0);}
 public override async void _Process(double ignored){if(!ready)return;try{
 if(native){GetWindow().ContentScaleSize=new(1280,720);GetWindow().ContentScaleMode=Window.ContentScaleModeEnum.Viewport;GetWindow().ContentScaleAspect=Window.ContentScaleAspectEnum.Keep;}
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
 authority.AppendLine(JsonSerializer.Serialize(State(),J));poses.AppendLine(JsonSerializer.Serialize(new{frame,seconds,dt,rootPosition=new[]{root.Position.X,root.Position.Y,root.Position.Z},rootYaw=root.Rotation.Y,p.LastPresentationSteps,p.LaunchAim,launchPose=p.LaunchPoseDiagnostics,gait=typeof(CharacterPresentation).GetField("_turningRun",F).GetValue(p)?.GetType().GetProperty("Diagnostics").GetValue(typeof(CharacterPresentation).GetField("_turningRun",F).GetValue(p))},J));bones.Append(frame).Append('|').Append(Pose(root)).AppendLine();cameras.Append(frame).Append('|').Append(T(camera.GlobalTransform)).Append('|').Append(N(camera.Size)).Append('|').Append(T(root.GlobalTransform)).AppendLine();
 if(expected){skin??=new ControlledGaitSkin(root);soles.AppendLine(JsonSerializer.Serialize(new{frame,feet=skin.Read()},J));boots.AppendLine(JsonSerializer.Serialize(new{frame,geometry=skin.ReadBootEnvelope()},J));}
 if(native){int value=(frame+43)&1023;for(int i=0;i<10;i++)stamp[i].Color=(value&(1<<i))!=0?Colors.White:Colors.Black;pendingFrame=frame;}
 frame++;
 if(seconds>=10||sim.Phase!=RunPhase.Combat){ready=false;SetProcess(false);if(native)await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);File.WriteAllText(folder+"/substeps.jsonl",substeps.ToString());File.WriteAllText(folder+"/pose.tsv",bones.ToString());File.WriteAllText(folder+"/camera.tsv",cameras.ToString());File.WriteAllText(folder+"/sole.jsonl",soles.ToString());File.WriteAllText(folder+"/boot-geometry.jsonl",boots.ToString());File.WriteAllText(folder+"/attachments.jsonl",attachments.ToString());File.WriteAllText(folder+"/authority.jsonl",authority.ToString());File.WriteAllText(folder+"/presentation.jsonl",poses.ToString());File.WriteAllText(folder+"/summary.json",JsonSerializer.Serialize(new{frame,seconds,rate,hero,launches,melee,maxSteps,sim.Phase,candidate=NormalPresentationCandidate.Enabled,hasteFixture=OS.GetEnvironment("NORMAL_PROBE_HASTE")=="1",maximumSpeedLoadoutFixture=OS.GetEnvironment("NORMAL_PROBE_MAX_SPEED")=="1",playerSpeedStat=sim.GetStat(Stat.PlayerSpeed),route=OS.GetEnvironment("NORMAL_PROBE_ROUTE"),scope="Scripted inputs through normal GameHost, no controlled gait flags. Haste timer and maximum catalog loadout injection only in explicitly labeled fixtures; not a shop/progression test."},J));GD.Print($"NORMAL_GAME_PROBE_PASS frames={frame} rate={rate} steps={maxSteps}");GetTree().Quit();}
 }catch(Exception e){Fail(e);}}
 void AfterDraw(){if(pendingFrame<0||sim==null)return;var root=Field<Node3D[]>("_visuals")[sim.PlayerId.Index];var rows=new List<object>();void Walk(Node n){if(n is BoneAttachment3D a){var sk=a.GetSkeleton();int bone=a.BoneIdx;if(sk!=null&&bone>=0){var expected=sk.GlobalTransform*sk.GetBoneGlobalPose(bone);var actual=a.GlobalTransform;rows.Add(new{name=a.Name.ToString(),positionError=(actual.Origin-expected.Origin).Length(),basisError=(actual.Basis.X-expected.Basis.X).Length()+(actual.Basis.Y-expected.Basis.Y).Length()+(actual.Basis.Z-expected.Basis.Z).Length(),a.OverridePose});}}foreach(Node c in n.GetChildren())Walk(c);}Walk(root);attachments.AppendLine(JsonSerializer.Serialize(new{frame=pendingFrame,rows},J));pendingFrame=-1;}
 public override void _ExitTree(){if(native)RenderingServer.FramePostDraw-=AfterDraw;}
 void Fail(Exception e){ready=false;if(!string.IsNullOrEmpty(folder)){File.WriteAllText(folder+"/failure.json",JsonSerializer.Serialize(new{frame,seconds,error=e.ToString(),gait=sim!=null&&sim.PlayerId.Index>=0?Field<CharacterPresentation[]>("_presentations")[sim.PlayerId.Index]?.NormalGaitDiagnostics:null},J));File.WriteAllText(folder+"/substeps-partial.jsonl",substeps.ToString());File.WriteAllText(folder+"/authority-partial.jsonl",authority.ToString());File.WriteAllText(folder+"/presentation-partial.jsonl",poses.ToString());}GD.PushError(e.ToString());GetTree().Quit(1);}
}
