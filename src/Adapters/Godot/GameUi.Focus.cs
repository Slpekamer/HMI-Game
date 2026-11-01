using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
namespace Hmi.GodotAdapter;
internal partial class GameUi
{
 private long _navigationGeneration;
 private long _gamepadFocusGeneration=-1;
 public long NavigationGeneration=>_navigationGeneration;
 public bool HasGamepadMenu=>_page!="combat"||_pauseInputBlocked||_codexOpen||_persistenceOpen;
 public string GamepadHint {get;private set;}="";
 public string ConfirmButtonLabel {get;set;}="下方键";
 public string CancelButtonLabel {get;set;}="右侧键";
 private void InvalidateNavigation(){_navigationGeneration++;GetViewport()?.GuiGetFocusOwner()?.ReleaseFocus();}
 private static IEnumerable<Control> Descendants(Node n){foreach(Node child in n.GetChildren()){if(child is Control c)yield return c;foreach(var d in Descendants(child))yield return d;}}
 private Control NavigationScope=>_persistenceOpen||_codexOpen||_pauseInputBlocked?_overlay:_screen;
 private Button[] NavigationButtons()=>Descendants(NavigationScope).OfType<Button>().Where(b=>b.IsVisibleInTree()&&!b.IsQueuedForDeletion()&&!b.Disabled&&b.FocusMode!=Control.FocusModeEnum.None).ToArray();
 public void SetGamepadUiAvailable(bool available)
 {
  bool becameAvailable=available&&!_gamepadUiAvailable;_gamepadUiAvailable=available;
  if(available)_deviceNotice="";RefreshInputHint();
  // Restore a safe visible focus once per page/modal scope, not every render:
  // ordinary keyboard/pointer focus must not be stolen while a pad remains connected.
  if(available&&HasGamepadMenu&&(becameAvailable||_gamepadFocusGeneration!=_navigationGeneration))
  {
   _gamepadFocusGeneration=_navigationGeneration;
   var current=GetViewport().GuiGetFocusOwner();
   if(current==null||!current.IsInsideTree()||!current.IsVisibleInTree()||current.IsQueuedForDeletion()||
      !NavigationScope.IsAncestorOf(current)||(current is BaseButton button&&button.Disabled))EnsureGamepadFocus();
  }
 }
 public void ShowGamepadDisconnected(bool touchRestored){_deviceNotice=touchRestored?"控制器已断开 · 可用触控或键盘":"控制器已断开 · 可用键盘";RefreshInputHint();}
 private string _deviceNotice="";
 private void RefreshInputHint()
 {
  GamepadHint=_gamepadUiAvailable?$"{ConfirmButtonLabel} 确认 · {CancelButtonLabel} 返回 · Start 暂停 · 上方键 图鉴":_deviceNotice;
  if(_inputHint==null&&_root!=null){_inputHint=new Label{Name="InputMethodHint",MouseFilter=Control.MouseFilterEnum.Ignore};_inputHint.AddThemeFontSizeOverride("font_size",16);_inputHint.AddThemeColorOverride("font_color",CandyTheme.Ink);_root.AddChild(_inputHint);}
  if(_inputHint!=null){float left=_page=="selection"?190:16;_inputHint.Text=GamepadHint;_inputHint.Visible=(_gamepadUiAvailable||_deviceNotice.Length>0)&&_page!="combat"&&!_codexOpen&&!_persistenceOpen&&(_screenScroll?.ScrollVertical??0)==0;_inputHint.Position=_safeRect.Position+new Vector2(left,0);float textWidth=_inputHint.GetThemeFont("font").GetStringSize(GamepadHint,HorizontalAlignment.Left,-1,16).X+4;_inputHint.Size=new Vector2(Math.Min(textWidth,Math.Max(0,_safeRect.Size.X-left-16)),24);}
  if(_pauseInputInfo!=null&&GodotObject.IsInstanceValid(_pauseInputInfo)&&_pauseInputInfo.IsInsideTree())_pauseInputInfo.Text=PauseInputCaption;
 }
 private Label _inputHint,_pauseInputInfo;
 private string PauseInputCaption=>!_gamepadUiAvailable&&_deviceNotice.Length>0?"控制器已断开 · 战斗与冷却已暂停":"战斗时间与冷却均已暂停";
 private bool _gamepadUiAvailable;
 public void EnsureGamepadFocus()
 {
  var buttons=NavigationButtons();if(buttons.Length==0)return;
  var focus=GetViewport().GuiGetFocusOwner();if(buttons.Contains(focus))return;
  // Destructive modal is never entered with Confirm focused by default.
  var safe=_persistenceOpen?buttons.FirstOrDefault(b=>b.Text.Contains("取消")||b.Text.Contains("返回")):null;
  FocusGamepad(safe??buttons[0]);
 }
 private void FocusGamepad(Button b){b.GrabFocus();for(Node p=b.GetParent();p!=null;p=p.GetParent())if(p is ScrollContainer scroll)scroll.EnsureControlVisible(b);}
 public void NavigateGamepad(int x,int y)
 {
  var buttons=NavigationButtons();if(buttons.Length==0)return;EnsureGamepadFocus();var current=GetViewport().GuiGetFocusOwner() as Button;
  if(current==null)return;
  var origin=current.GetGlobalRect().GetCenter();Button best=null;float score=float.MaxValue;
  foreach(var b in buttons){if(b==current)continue;var d=b.GetGlobalRect().GetCenter()-origin;float forward=d.Dot(new Vector2(x,y));if(forward<=1)continue;float side=Math.Abs(d.Cross(new Vector2(x,y)));float cost=forward+side*3;if(cost<score){score=cost;best=b;}}
  if(best==null){int index=Array.IndexOf(buttons,current);best=buttons[(index+(y<0||x<0?-1:1)+buttons.Length)%buttons.Length];}FocusGamepad(best);
 }
 public void ConfirmGamepad()
 {
  EnsureGamepadFocus();var b=GetViewport().GuiGetFocusOwner() as Button;
  if(b!=null&&NavigationButtons().Contains(b))b.EmitSignal(BaseButton.SignalName.Pressed);
 }
 public void CancelGamepad()
 {
  if(_persistenceOpen){TryCancelPersistenceModal();return;}
  if(_codexOpen){CloseCodex();return;}
  if(_page=="combat"){if(_pauseInputBlocked)PauseRequested?.Invoke();return;}
  if(_page=="selection"){ShowHome();return;}
  // Between / ending do not invent a destructive back path; use visible explicit actions.
 }
 public void ScrollGamepad(float direction)
 {
  if(Math.Abs(direction)<.0001f)return;
  var rich=Descendants(NavigationScope).OfType<RichTextLabel>().FirstOrDefault(r=>r.IsVisibleInTree()&&r.ScrollActive);
  if(rich!=null){var bar=rich.GetVScrollBar();bar.Value+=direction*32;return;}
  if(_screenScroll!=null)_screenScroll.ScrollVertical+=(int)(direction*32);
 }
}
