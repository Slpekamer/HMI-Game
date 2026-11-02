using Godot;
using Hmi.Core;
using Hmi.Adapters.Input;
public partial class GameHost
{
 private readonly GamepadMenuRouter _gamepadMenu=new();
 private readonly bool[] _padButtons=new bool[128];
 private readonly float[] _padAxes=new float[16];
 internal JoyButton GamepadConfirmButton{get;private set;}=JoyButton.A;
 internal JoyButton GamepadCancelButton{get;private set;}=JoyButton.B;
 // Adapter configuration API, not a user-facing remapping/settings screen.
 internal void ConfigureGamepadConfirmCancel(JoyButton confirm,JoyButton cancel,string confirmLabel,string cancelLabel)
 {
  if(confirm==cancel||confirm is not (JoyButton.A or JoyButton.B)||cancel is not (JoyButton.A or JoyButton.B)||
     (int)confirm<0||(int)confirm>=128||(int)cancel<0||(int)cancel>=128||string.IsNullOrWhiteSpace(confirmLabel)||string.IsNullOrWhiteSpace(cancelLabel))
   throw new System.ArgumentException("Distinct valid confirm/cancel controls and labels required");
  GamepadConfirmButton=confirm;GamepadCancelButton=cancel;_ui.ConfirmButtonLabel=confirmLabel;_ui.CancelButtonLabel=cancelLabel;
  _gamepadMenu.Reset();_ui.SetGamepadUiAvailable(_gamepadId>=0);
 }
 private void ClearGamepadState(){System.Array.Clear(_padButtons);System.Array.Clear(_padAxes);_gamepadMenu.Reset();}
 private bool HandleGamepadEvent(InputEvent ev)
 {
  if(ev is not InputEventJoypadButton&&ev is not InputEventJoypadMotion)return false;
  // Consume ALL devices: default ui_accept must never bypass single-device routing.
  GetViewport().SetInputAsHandled();
  if(!_inputReady)return true;
  // After active-device loss, an already-connected pad may explicitly claim with Start.
  // The claim itself never activates Pause/Confirm and requires a fresh neutral/release.
  if(_gamepadId<0&&ev.Device>=0&&ev is InputEventJoypadButton claim&&claim.Pressed&&claim.ButtonIndex==JoyButton.Start){ApplyGamepadDevice(ev.Device);_padButtons[(int)JoyButton.Start]=true;return true;}
  if(ev.Device!=_gamepadId)return true;
  if(ev is InputEventJoypadButton b){int n=(int)b.ButtonIndex;if(n>=0&&n<_padButtons.Length)_padButtons[n]=b.Pressed;}
  else if(ev is InputEventJoypadMotion m){int n=(int)m.Axis;if(n>=0&&n<_padAxes.Length)_padAxes[n]=float.IsFinite(m.AxisValue)?m.AxisValue:0;}
  return true;
 }
 private bool PadButton(JoyButton button)=>(int)button>=0&&(int)button<_padButtons.Length&&_padButtons[(int)button];
 private void UpdateGamepadUi(double delta)
 {
  if(!_inputReady||_ui==null||_gamepadId<0||!_inputFocused)return;
  _ui.SetGamepadUiAvailable(true);
  float x=(PadButton(JoyButton.DpadRight)?1:0)-(PadButton(JoyButton.DpadLeft)?1:0);
  float y=(PadButton(JoyButton.DpadDown)?1:0)-(PadButton(JoyButton.DpadUp)?1:0);
  if(x==0&&y==0){x=_padAxes[(int)JoyAxis.LeftX];y=_padAxes[(int)JoyAxis.LeftY];}
  var intent=_gamepadMenu.Sample(_ui.NavigationGeneration,delta,x,y,PadButton(GamepadConfirmButton),PadButton(GamepadCancelButton),PadButton(JoyButton.Start),PadButton(JoyButton.Y));
  // One semantic action per sample; safe cancel takes precedence over confirmation.
  if((intent.Action&MenuAction.Cancel)!=0)_ui.CancelGamepad();
  else if((intent.Action&MenuAction.Pause)!=0){if(_sim.Phase==RunPhase.Combat&&!_ui.IsPersistenceModalOpen&&!_ui.IsCodexOpen)_ui.PauseRequested?.Invoke();}
  else if((intent.Action&MenuAction.Codex)!=0){if(_ui.IsCodexOpen)_ui.CloseCodex();else if(!_ui.IsPersistenceModalOpen)_ui.ShowCodex();}
  else if((intent.Action&MenuAction.Confirm)!=0&&_ui.HasGamepadMenu)_ui.ConfirmGamepad();
  if(_ui.HasGamepadMenu&&(intent.X!=0||intent.Y!=0))_ui.NavigateGamepad(intent.X,intent.Y);
  if(_ui.HasGamepadMenu&&System.Math.Abs(_padAxes[(int)JoyAxis.RightY])>.35f)_ui.ScrollGamepad(_padAxes[(int)JoyAxis.RightY]*(float)delta*8);
 }
}
