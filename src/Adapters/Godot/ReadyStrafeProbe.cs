using Godot;
using Hmi.Core;
using Hmi.GodotAdapter;
using Hmi.Adapters.Input;
using System;
using System.Reflection;
using System.IO;
using System.Text;
using System.Globalization;
public partial class ReadyStrafeProbe:Node
{
 const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
 GameHost host;GameSimulation sim;Camera3D camera;SubViewport view;Label label;MoveInputRouter router;int frame,warm;bool ready,setup;string folder;float step,time;string pausedPose;Vector3 pausedCamera;long pausedTick;long maxCaptureSteps,totalCaptureSteps;
 readonly StringBuilder authority=new(),cam=new(),motion=new(),poses=new(),allMelee=new();bool verified;
 T Field<T>(string n)=>(T)typeof(GameHost).GetField(n,F).GetValue(host);
 static string N(float n)=>n.ToString("R",CultureInfo.InvariantCulture);
 void Call(string n,params object[] args)=>typeof(GameHost).GetMethod(n,F).Invoke(host,args);
 public override void _Ready(){try{
 folder=OS.GetEnvironment("READY_OUTPUT");Directory.CreateDirectory(folder);step=float.Parse(OS.GetEnvironment("READY_DELTA"),CultureInfo.InvariantCulture);
 GetWindow().Size=new Vector2I(1280,720);
 var container=new SubViewportContainer{Size=new Vector2(1280,720),Scale=Vector2.One,Position=Vector2.Zero,Stretch=false,MouseFilter=Control.MouseFilterEnum.Ignore};AddChild(container);
 view=new SubViewport{Size=new Vector2I(1280,720),OwnWorld3D=true,HandleInputLocally=true,RenderTargetUpdateMode=SubViewport.UpdateMode.Always};container.AddChild(view);
 host=new GameHost();view.AddChild(host);var layer=new CanvasLayer{Layer=99};view.AddChild(layer);label=new Label{Position=new Vector2(220,680),MouseFilter=Control.MouseFilterEnum.Ignore};label.AddThemeColorOverride("font_color",new Color("392640"));label.AddThemeFontSizeOverride("font_size",16);layer.AddChild(label);host.SetProcess(false);Call("Start",1,(uint)1030);host.SetProcess(false);
 sim=Field<GameSimulation>("_sim");camera=Field<Camera3D>("_camera");if(sim.Phase!=RunPhase.Combat)throw new Exception("fresh rule2 Start failed");
 router=Field<MoveInputRouter>("_moveInputs");host.RegisterMovementProvider("ready-study",new MoveProviderOptions(100,0));
 allMelee.AppendLine("frame|time|tick|index|source|target|directionX|directionY|renderYaw|errorDegrees|nativeSequence|nativeActive|nativeKind");authority.AppendLine("frame|tick|paused|rng|coins|entities|events");cam.AppendLine("frame|camera|size|actorPosition");motion.AppendLine("frame|time|tick|yaw|desired|turn|error|reason|acceptedSequence|acceptedActive|preX|preZ|finalX|finalZ|right|blend|diagnostic|advances|target|gestureKind|gestureError|paused|runPhase|strafePhase");ready=true;
 }catch(Exception e){GD.PushError(e.ToString());GetTree().Quit(1);}}
 object World=>typeof(GameSimulation).GetField("World",F).GetValue(sim);
 Array Arr(string n)=>(Array)World.GetType().GetField(n,F).GetValue(World);
 void Hp(int slot,float hp){var a=Arr("Health");var h=a.GetValue(slot);h.GetType().GetField("Current").SetValue(h,hp);h.GetType().GetField("Maximum").SetValue(h,hp);a.SetValue(h,slot);}
 string Pose(Node n){var b=new StringBuilder();void Walk(Node x){if(x is Skeleton3D s){for(int i=0;i<s.GetBoneCount();i++){var q=s.GetBonePoseRotation(i);var p=s.GetBonePosePosition(i);var scale=s.GetBonePoseScale(i);b.Append(s.GetBoneName(i)).Append(':').Append(N(q.X)).Append(',').Append(N(q.Y)).Append(',').Append(N(q.Z)).Append(',').Append(N(q.W)).Append('/').Append(N(p.X)).Append(',').Append(N(p.Y)).Append(',').Append(N(p.Z)).Append('/').Append(N(scale.X)).Append(',').Append(N(scale.Y)).Append(',').Append(N(scale.Z)).Append(';');}}if(x is MeshInstance3D m && m.Mesh!=null){for(int j=0;j<m.GetBlendShapeCount();j++)b.Append("morph_").Append(m.Name).Append('_').Append(j).Append(':').Append(N(m.GetBlendShapeValue(j))).Append(';');}foreach(Node c in x.GetChildren())Walk(c);}Walk(n);return b.ToString();}

 void Setup(){
 int keep=-1,second=-1;for(int i=0;i<sim.Capacity;i++)if(sim.TryGetEntityAtSlot(i,out var e)&&e.Kind==EntityKind.Enemy){if(keep<0)keep=i;else if(second<0)second=i;else World.GetType().GetMethod("Destroy",F).Invoke(World,new object[]{i});}
 if(keep<0)throw new Exception("no activated enemy at tick60");
 var reservations=typeof(GameSimulation).GetField("SpawnReservations",F).GetValue(sim);reservations.GetType().GetMethod("Cancel",F).Invoke(reservations,new object[]{sim,SpawnOutcome.ScopeEnded});
 typeof(GameSimulation).GetField("SpawnTimer",F).SetValue(sim,999f);Hp(sim.PlayerId.Index,1000);Hp(keep,40);Hp(second,100);
 sim.TryGetEntity(sim.PlayerId,out var player);var arr=Arr("Transform");var tr=arr.GetValue(keep);tr.GetType().GetField("Position").SetValue(tr,new Vec2(player.Position.X,player.Position.Y-2));arr.SetValue(tr,keep);var tr2=arr.GetValue(second);tr2.GetType().GetField("Position").SetValue(tr2,new Vec2(player.Position.X-6,player.Position.Y));arr.SetValue(tr2,second);
 setup=true;GD.Print("CONTROLLED_SETUP initial 2 enemies positioned ONCE at (0,-2) HP40 and (-6,0) HP100 / playerHP1000 / no later mutation");
 }
 string AnimationPhase(CharacterPresentation p,string node){var mixer=typeof(CharacterPresentation).GetField("_mixer",F).GetValue(p);var tree=(AnimationTree)mixer.GetType().GetField("_tree",F).GetValue(mixer);return N(tree.Get("parameters/"+node+"/current_position").AsSingle());}
 void VerifyComposite(CharacterPresentation p){
 if(OS.GetEnvironment("READY_STRAFE_PILOT")!="1")return;
 var type=typeof(CharacterPresentation);var privateClip=(Animation)type.GetField("_privateClip",F).GetValue(p);
 var player=(AnimationPlayer)type.GetField("_player",F).GetValue(p);var run=(string)type.GetField("_run",F).GetValue(p);var original=player.GetAnimation(run);
 var lower=new System.Collections.Generic.HashSet<string>{"Pelvis","Thigh_L","Shin_L","Foot_L","Thigh_R","Shin_R","Foot_R"};
 string Tracks(Animation c){var b=new StringBuilder();for(int i=0;i<c.GetTrackCount();i++){var path=c.TrackGetPath(i);if(path.GetSubNameCount()==1&&lower.Contains(path.GetSubName(0)))continue;b.Append(path).Append(c.TrackGetType(i));for(int k=0;k<c.TrackGetKeyCount(i);k++)b.Append(c.TrackGetKeyTime(i,k)).Append(':').Append(c.TrackGetKeyValue(i,k)).Append(';');}return b.ToString();}
 if(ReferenceEquals(privateClip,original)||Tracks(privateClip)!=Tracks(original))throw new Exception("private composite changed upper tracks/aliased Run");
 GD.Print("COMPOSITE_UPPER_EXACT private resource="+privateClip.GetInstanceId());
 }
 void CheckNewRun(){
 int old=sim.RunToken;Call("Selection");host.SetProcess(false);Call("Start",1,(uint)1030);host.SetProcess(false);
 if(sim.RunToken==old||sim.Phase!=RunPhase.Combat)throw new Exception("new run not established");
 host._Process(step);var p=Field<CharacterPresentation[]>("_presentations")[sim.PlayerId.Index];
 if(p.RightSelected||p.RightBlend!=0||p.AcceptedGesture.Sequence!=0||p.AcceptedGesture.Active)throw new Exception("new run inherited gait/gesture");
 sim.Enqueue(new GameCommand(CommandKind.Pause,flag:true,runToken:sim.RunToken,wave:sim.Wave));host._Process(step);var actor=Field<Node3D[]>("_visuals")[sim.PlayerId.Index];float yaw=actor.Rotation.Y;string pose=Pose(actor);host._Process(step);if(yaw!=actor.Rotation.Y||pose!=Pose(actor))throw new Exception("new run immediate pause moved");
 GD.Print("NEW_RUN_SCOPE_CLEAR run="+sim.RunToken);
 }
 void CheckFirstRenderCapture(){
 Call("Selection");host.SetProcess(false);Call("Start",1,(uint)1030);host.SetProcess(false);
 var observer=Field<ReadyAttentionObserver>("_readyAttention");Hp(sim.PlayerId.Index,1000);
 if(Field<CharacterPresentation[]>("_presentations")[sim.PlayerId.Index]!=null)throw new Exception("first render fixture already has presenter");
 int steps=0;while(observer.AimDiagnostics.Captured==0&&steps++<1200){sim.Step();Call("ConsumeEvents");}
 if(observer.AimDiagnostics.Captured==0||!observer.AimDiagnostics.HasPending)throw new Exception("no genuine Melee before first render");
 long high=observer.AimDiagnostics.HighTick;Call("RenderEntities",GameSimulation.FixedDelta);
 if(observer.AimDiagnostics.HighTick!=high)throw new Exception("first render changed dedup watermark");
 if(OS.GetEnvironment("READY_MELEE_PRIORITY")=="1"&&host.ReadyReason!="AuthoritativeMeleeAim")throw new Exception("scope initialization dropped first-render genuine Melee: "+host.ReadyReason);
 GD.Print("FIRST_RENDER_CAPTURE_PASS steps="+steps+" stamp="+high+" reason="+host.ReadyReason);
 var presentations=Field<CharacterPresentation[]>("_presentations");var visuals=Field<Node3D[]>("_visuals");int slot=sim.PlayerId.Index;
 var oldOwner=presentations[slot];oldOwner.Dispose();visuals[slot].QueueFree();presentations[slot]=null;visuals[slot]=null;
 Call("RenderEntities",GameSimulation.FixedDelta);
 if(ReferenceEquals(oldOwner,presentations[slot])||!observer.OutputValid||observer.AimDiagnostics.HasLatch)throw new Exception("same-source new native owner inherited gesture or invalidated observation");
 long prior=observer.AimDiagnostics.Captured;int follow=0;while(observer.AimDiagnostics.Captured==prior&&follow++<1200&&sim.Phase==RunPhase.Combat){sim.Step();Call("ConsumeEvents");}
 Call("RenderEntities",GameSimulation.FixedDelta);
 if(observer.AimDiagnostics.Captured==prior||!observer.OutputValid||(OS.GetEnvironment("READY_MELEE_PRIORITY")=="1"&&host.ReadyReason!="AuthoritativeMeleeAim"))throw new Exception("rebuilt native owner failed fresh actual event captured="+observer.AimDiagnostics.Captured+" prior="+prior+" valid="+observer.OutputValid+" reason="+host.ReadyReason+" follow="+follow+" phase="+sim.Phase);
 GD.Print("NATIVE_OWNER_REBUILD_PASS followSteps="+follow);

 }
 void CaptureLayout(){var w=GetWindow();w.ContentScaleSize=new Vector2I(1280,720);w.ContentScaleMode=Window.ContentScaleModeEnum.Viewport;w.ContentScaleAspect=Window.ContentScaleAspectEnum.Keep;}
 public override async void _Process(double ignored){if(!ready)return;try{
 if(host.IsProcessing())throw new Exception("automatic host process enabled");
 label.Text="CONTROLLED SETUP / "+(OS.GetEnvironment("READY_MELEE_PRIORITY")=="1"?"B: real Melee aim priority":"A: accepted gesture aim")+" / same Core, input, camera";
 if(!setup){host._Process(GameSimulation.FixedDelta);CaptureLayout();if(++warm==60)Setup();return;}

 bool pause=time>=12&&time<13;if(sim.Paused!=pause)sim.Enqueue(new GameCommand(CommandKind.Pause,flag:pause,runToken:sim.RunToken,wave:sim.Wave));
 float input=time<3?.08f:time>=8&&time<12?.08f:0;
 var cr=camera.GlobalBasis.X;cr.Y=0;cr=cr.Normalized();var cd=camera.GlobalBasis.Z;cd.Y=0;cd=cd.Normalized();
 Call("SynchronizeMovementInput");host.SubmitMovementIntent(router.Token("ready-study"),new MoveIntent(input*cr.X,input*cd.X));
 long capturedBefore=Field<ReadyAttentionObserver>("_readyAttention").AimDiagnostics.Captured;long stepsBefore=Field<ReadyAttentionObserver>("_readyAttention").CapturedStepCalls;long tickBefore=sim.Tick;
 var pres=Field<CharacterPresentation[]>("_presentations")[sim.PlayerId.Index];long before=pres?.NativeAdvances??0;
 host._Process(step);CaptureLayout();pres=Field<CharacterPresentation[]>("_presentations")[sim.PlayerId.Index];var actor=Field<Node3D[]>("_visuals")[sim.PlayerId.Index];
 long advanced=pres.NativeAdvances-before;if(advanced!=(pause?0:1))throw new Exception("nativeAdvance count "+advanced+" paused="+pause);
 if(pause){string pose=Pose(actor);if(pausedPose==null){pausedPose=pose;pausedCamera=camera.GlobalPosition;pausedTick=sim.Tick;}else if(pose!=pausedPose||camera.GlobalPosition!=pausedCamera||sim.Tick!=pausedTick)throw new Exception("pause moved pose/camera/tick");}
 var run=typeof(GameSimulation).GetField("Run",F).GetValue(sim);uint rng=(uint)run.GetType().GetField("Random",F).GetValue(run);
 authority.Append(frame).Append('|').Append(sim.Tick).Append('|').Append(sim.Paused).Append('|').Append(rng).Append('|').Append(sim.Coins).Append('|');
 for(int i=0;i<sim.Capacity;i++)if(sim.TryGetEntityAtSlot(i,out var e))authority.Append(e.Id).Append(':').Append(e.Kind).Append(':').Append(N(e.Position.X)).Append(',').Append(N(e.Position.Y)).Append(':').Append(N(e.Health)).Append(';');
 authority.Append('|');for(int i=0;i<sim.EventCount;i++){var e=sim.GetEvent(i);authority.Append(e.Kind).Append(':').Append(e.Source).Append('>').Append(e.Target).Append(':').Append(N(e.Value)).Append('@').Append(N(e.From.X)).Append(',').Append(N(e.From.Y)).Append('>').Append(N(e.To.X)).Append(',').Append(N(e.To.Y)).Append(';');}authority.AppendLine();
 cam.Append(frame).Append('|').Append(camera.GlobalTransform).Append('|').Append(N(camera.Size)).Append('|').Append(actor.Position).AppendLine();
 if(!verified){VerifyComposite(pres);verified=true;}
 var a=pres.AcceptedGesture;
 var observer=Field<ReadyAttentionObserver>("_readyAttention");long newEvents=observer.AimDiagnostics.Captured-capturedBefore;
 long capturedSteps=observer.CapturedStepCalls-stepsBefore;maxCaptureSteps=Math.Max(maxCaptureSteps,capturedSteps);totalCaptureSteps+=capturedSteps;
 if(capturedSteps<1||capturedSteps>8||capturedSteps<sim.Tick-tickBefore)throw new Exception("missing per-Step capture "+capturedSteps+" vs tick delta "+(sim.Tick-tickBefore));
 if(newEvents>16||newEvents>8)throw new Exception("audit capacity/catchup overflow");
 for(int offset=(int)newEvents-1;offset>=0;offset--){var e=observer.AimDiagnostics.Recent(offset);float angle=Mathf.Atan2(-e.Direction.X,-e.Direction.Y);float err=Math.Abs(Mathf.Wrap(angle-actor.Rotation.Y,-Mathf.Pi,Mathf.Pi));allMelee.AppendJoin('|',frame,N(time),e.Tick,e.Index,e.Source,e.Target,N(e.Direction.X),N(e.Direction.Y),N(actor.Rotation.Y),N(Mathf.RadToDeg(err)),a.Sequence,a.Active,a.Event?.Kind.ToString()??"none").AppendLine();}
 var pre=pres.UsedLocalDirection;var final=host.ReadyFinalLocalDirection;
 motion.AppendJoin('|',frame,N(time),sim.Tick,N(actor.Rotation.Y),N(host.ReadyDesiredYaw),N(host.ReadyAppliedDelta),N(host.ReadyHeadingError),host.ReadyReason,a.Sequence,a.Active,N(pre.X),N(pre.Z),N(final.X),N(final.Z),pres.RightSelected,N(pres.RightBlend),pres.LocomotionDiagnostic,advanced,Field<ReadyAttentionObserver>("_readyAttention").Desired.AttentionTarget?.ToString()??"none",a.Event?.Kind.ToString()??"none",a.Event is GameEvent ge&&ge.Kind==EventKind.Melee?N(Math.Abs(Mathf.Wrap(Mathf.Atan2(-(ge.To.X-ge.From.X),-(ge.To.Y-ge.From.Y))-actor.Rotation.Y,-Mathf.Pi,Mathf.Pi))):"NA",pause,AnimationPhase(pres,"Run"),OS.GetEnvironment("READY_STRAFE_PILOT")=="1"?AnimationPhase(pres,"StrafeRight"):"NA").AppendLine();
 poses.Append(frame).Append('|').Append(Pose(actor)).AppendLine();
 if(OS.GetEnvironment("READY_FRAMECHECK")=="1"&&frame==60){SetProcess(false);long beforeTick=sim.Tick;await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);if(beforeTick!=sim.Tick)throw new Exception("framecheck stepped authority");using(var image=GetTree().Root.GetTexture().GetImage()){if(image.GetSize()!=new Vector2I(1280,720))throw new Exception("root screenshot dimensions "+image.GetSize());image.SavePng(folder+"/root-frame060.png");}using(var image=view.GetTexture().GetImage())image.SavePng(folder+"/game-frame060.png");GD.Print("ROOT_FRAMECHECK_PASS root1280x720 game"+view.Size+" physical"+GetWindow().Size);GetTree().Quit();return;}
 frame++;time=frame*step;if(frame>=(OS.GetEnvironment("READY_SHORT")=="1"?450:Math.Round(15/step))){File.WriteAllText(folder+"/authority.tsv",authority.ToString());File.WriteAllText(folder+"/camera.tsv",cam.ToString());File.WriteAllText(folder+"/motion.tsv",motion.ToString());File.WriteAllText(folder+"/pose.tsv",poses.ToString());File.WriteAllText(folder+"/all-core-melee.tsv",allMelee.ToString());GD.Print("CAPTURE_STEP_AUDIT max="+maxCaptureSteps+" total="+totalCaptureSteps);if(step==.25f&&maxCaptureSteps!=8)throw new Exception("8-step catchup not exercised");if(OS.GetEnvironment("READY_SHORT")!="1"){CheckNewRun();CheckFirstRenderCapture();}GD.Print("READY_STRAFE_PROBE_DONE frames="+frame+" tick="+sim.Tick);ready=false;GetTree().Quit();}
 }catch(Exception e){GD.PushError(e.ToString());GetTree().Quit(1);}}
}
