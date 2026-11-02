#nullable enable annotations
using Godot;
using System;

namespace Hmi.GodotAdapter;
internal partial class GameUi
{
 private WeakReference<Control>? _codexReturnFocus;
 private long _codexReturnFocusEpoch;
 private string _codexReturnPage="";
 private Vector2 _codexReturnLayout;
 private Rect2 _codexReturnSafe;
 private int _codexReturnScroll;
 private bool _codexReturnInScroll;
 private void DiscardCodexReturnFocus(){_codexReturnFocusEpoch++;_codexReturnFocus=null;_codexReturnPage="";}
 private bool IsLegalCodexReturnFocus(Control control)=>GodotObject.IsInstanceValid(control)&&control.IsInsideTree()&&!control.IsQueuedForDeletion()&&control.IsVisibleInTree()&&control.FocusMode!=Control.FocusModeEnum.None&&NavigationScope.IsAncestorOf(control)&&!(control is BaseButton button&&button.Disabled);
 private void CaptureCodexReturnFocus()
 {
  DiscardCodexReturnFocus();var focus=GetViewport().GuiGetFocusOwner();
  if(focus==null||!IsLegalCodexReturnFocus(focus)||_overlay.IsAncestorOf(focus))return;
  _codexReturnFocus=new WeakReference<Control>(focus);_codexReturnPage=_page;
  _codexReturnLayout=_layoutSize;_codexReturnSafe=_safeRect;
  _codexReturnInScroll=_screenScroll.IsAncestorOf(focus);_codexReturnScroll=_screenScroll.ScrollVertical;
 }
 private void RestoreCodexReturnFocus(long closingEpoch)
 {
  // A reentrant close callback may open a newer codex; old close owns only its capture.
  if(closingEpoch!=_codexReturnFocusEpoch)return;
  var captured=_codexReturnFocus;string page=_codexReturnPage;DiscardCodexReturnFocus();
  if(captured==null||!IsInsideTree()||_codexOpen||_persistenceOpen||page!=_page||_codexReturnLayout!=_layoutSize||_codexReturnSafe!=_safeRect)return;
  if(!captured.TryGetTarget(out var focus)||!IsLegalCodexReturnFocus(focus))return;
  // A callback/pointer may have chosen a newer valid focus while the modal closed.
  var current=GetViewport().GuiGetFocusOwner();if(current!=null&&IsLegalCodexReturnFocus(current))return;
  focus.GrabFocus();
  if(_codexReturnInScroll)_screenScroll.ScrollVertical=_codexReturnScroll;
 }
}
