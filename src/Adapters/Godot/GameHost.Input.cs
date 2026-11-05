using Godot;
using Hmi.Core;
using Hmi.Adapters.Input;
public partial class GameHost
{
 private readonly MoveInputRouter _moveInputs=new();
 private bool _inputReady,_inputFocused=true,_inputAllowed,_padNeedsNeutral=true,_keyboardNeedsNeutral=true;
 private ulong _inputRun;
 private int _inputWave=-1,_gamepadId=-1;
 internal bool RestoreTouchAfterGamepadDisconnect {get;set;}=true;
 internal const float TouchDeadzone=.08f,GamepadDeadzone=.20f,GamepadHysteresis=.02f;
 private void InitializeMovementInput()
 {
  _moveInputs.Register("keyboard",new MoveProviderOptions(30,0));
  _moveInputs.Register("gamepad",new MoveProviderOptions(20,GamepadDeadzone,GamepadHysteresis));
  _moveInputs.Register("touch",new MoveProviderOptions(10,TouchDeadzone));
  _inputReady=true;_ui.GameplayInputCanceled=ResetMovementInput;
  Input.JoyConnectionChanged+=OnJoyConnectionChanged;
  RefreshGamepadConnection();
 }
 private void OnJoyConnectionChanged(long device,bool connected)
 {
  if(connected&&_gamepadId<0)ApplyGamepadDevice((int)device);
  else if(!connected&&device==_gamepadId){ApplyGamepadDevice(-1);_ui.ShowGamepadDisconnected(RestoreTouchAfterGamepadDisconnect);if(_sim?.Phase==RunPhase.Combat)Send(CommandKind.Pause,flag:true);}
 }
 private void RefreshGamepadConnection()
 {
  int next=-1;foreach(int id in Input.GetConnectedJoypads()){if(id==_gamepadId){next=id;break;}if(next<0||id<next)next=id;}
  ApplyGamepadDevice(next);
 }
 // Device enumeration is isolated from the deterministic connection transition for tests/adapters.
 internal void ApplyGamepadDevice(int next)
 {
  if(next==_gamepadId){UpdateTouchProvider();return;}
  _gamepadId=next;ClearGamepadState();ResetMovementInput();
  if(next>=0)_moveInputs.Disable("touch");else if(RestoreTouchAfterGamepadDisconnect&&_moveInputs.TryToken("touch",out _))_moveInputs.Enable("touch");
  UpdateTouchProvider();_ui.SetGamepadUiAvailable(next>=0);
 }
 internal void SetMovementProviderEnabled(string id,bool enabled){if(enabled)_moveInputs.Enable(id);else _moveInputs.Disable(id);ResetMovementInput();}
 internal void UnregisterMovementProvider(string id){_moveInputs.Unregister(id);ResetMovementInput();}
 internal MoveInputToken RegisterMovementProvider(string id,MoveProviderOptions options){_moveInputs.Register(id,options);ResetMovementInput();return _moveInputs.Token(id);}
 internal bool SubmitMovementIntent(MoveInputToken token,MoveIntent intent)=>token.ProviderId=="touch"&&(!_touchGeometryKnown||_gamepadId>=0)?false:_moveInputs.Submit(token,intent);
 private void UpdateTouchProvider(){if(_inputReady&&_ui!=null){bool present=_moveInputs.TryToken("touch",out var token);_ui.SetTouchInputContext(present?token.Generation:0,present&&_touchGeometryKnown&&_moveInputs.IsEnabled("touch")&&_gamepadId<0);}}
 private void ResetMovementInput(){if(!_inputReady)return;_moveInputs.ResetAll();_padNeedsNeutral=true;_keyboardNeedsNeutral=true;_gamepadMenu.Reset();_ui?.Joystick.ResetInput();UpdateTouchProvider();}
 private void SynchronizeMovementInput()
 {
  if(!_inputReady)return;
  bool allowed=_inputFocused&&_sim.Phase==RunPhase.Combat&&!_sim.Paused&&_ui.IsGameplayInputAllowed;
  ulong run=(ulong)_sim.RunToken;
  if(run!=_inputRun||_inputWave!=_sim.Wave||allowed!=_inputAllowed){ResetMovementInput();_inputRun=run;_inputWave=_sim.Wave;_inputAllowed=allowed;}
 }
 private Vector2 ReadMoveIntent()
 {
  SynchronizeMovementInput();if(!_inputAllowed)return Vector2.Zero;
  bool left=Input.IsPhysicalKeyPressed(Key.A)||Input.IsPhysicalKeyPressed(Key.Left);
  bool right=Input.IsPhysicalKeyPressed(Key.D)||Input.IsPhysicalKeyPressed(Key.Right);
  bool up=Input.IsPhysicalKeyPressed(Key.W)||Input.IsPhysicalKeyPressed(Key.Up);
  bool down=Input.IsPhysicalKeyPressed(Key.S)||Input.IsPhysicalKeyPressed(Key.Down);
  // A context reset cancels held keys just like a held stick or touch capture.
  // Opposed directions may sum to zero, but only all physical keys up is neutral.
  if(!left&&!right&&!up&&!down)_keyboardNeedsNeutral=false;
  var keyboard=new MoveIntent((right?1:0)-(left?1:0),(down?1:0)-(up?1:0));
  if(_moveInputs.TryToken("keyboard",out var keyboardToken))_moveInputs.Submit(keyboardToken,_keyboardNeedsNeutral?MoveIntent.Zero:keyboard);
  var pad=_gamepadId<0?Vector2.Zero:new Vector2(_padAxes[(int)JoyAxis.LeftX],_padAxes[(int)JoyAxis.LeftY]);
  if(pad.Length()<=GamepadDeadzone-GamepadHysteresis)_padNeedsNeutral=false;
  if(_moveInputs.TryToken("gamepad",out var gamepadToken))_moveInputs.Submit(gamepadToken,_padNeedsNeutral?MoveIntent.Zero:new MoveIntent(pad.X,pad.Y));
  var touch=_ui.Joystick.Direction;
  SubmitMovementIntent(new MoveInputToken("touch",_ui.Joystick.InputGeneration),new MoveIntent(touch.X,touch.Y));
  var move=_moveInputs.Resolve();
  if(_moveInputs.SelectedProvider!=null&&_moveInputs.SelectedProvider!="touch"&&_ui.Joystick.IsCaptured){_moveInputs.Reset("touch");_ui.Joystick.ResetInput();}
  UpdateTouchProvider();return new Vector2(move.X,move.Y);
 }
 private void ReleaseMovementInput()
 {
  if(!_inputReady)return;Input.JoyConnectionChanged-=OnJoyConnectionChanged;
  _ui.GameplayInputCanceled=null;ResetMovementInput();
  _moveInputs.Unregister("touch");_moveInputs.Unregister("gamepad");_moveInputs.Unregister("keyboard");_inputReady=false;
 }
}
