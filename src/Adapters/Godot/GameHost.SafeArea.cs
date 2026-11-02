using Godot;
using System;

internal readonly struct MobileSafeAreaSnapshot
{
 internal readonly Rect2I PhysicalSafe;
 internal readonly Transform2D ScreenTransform;
 internal MobileSafeAreaSnapshot(Rect2I physicalSafe,Transform2D screenTransform){PhysicalSafe=physicalSafe;ScreenTransform=screenTransform;}
 internal bool IsValid=>PhysicalSafe.Size.X>0&&PhysicalSafe.Size.Y>0&&float.IsFinite(ScreenTransform.Determinant())&&Math.Abs(ScreenTransform.Determinant())>.000001f&&ScreenTransform.Origin.IsFinite();
}
public partial class GameHost
{
 internal const double SafeAreaPollInterval=.25;
 private bool _touchGeometryKnown=true;
 private bool _mobileInsetsEnabled,_safeAreaSeen,_pendingInsetSnapshot,_inputTransformSeen;
 private double _safeAreaClock;
 private MobileSafeAreaSnapshot _safeAreaSnapshot;
 private Transform2D _lastInputScreenTransform;
 private Func<MobileSafeAreaSnapshot> _safeAreaReader;
 private void InitializeSafeAreaProbe(){_mobileInsetsEnabled=OS.GetName() is "Android" or "iOS";SetTouchGeometryKnown(!_mobileInsetsEnabled);}
 // Injectable OS boundary for deterministic tests; not a claim of actual device insets.
 internal void ConfigureSafeAreaReader(Func<MobileSafeAreaSnapshot> reader,bool mobile)
 { _safeAreaReader=reader;_mobileInsetsEnabled=mobile;_safeAreaClock=0;_safeAreaSeen=false;_pendingInsetSnapshot=false;SetTouchGeometryKnown(!mobile); }
 private MobileSafeAreaSnapshot ReadSafeArea()=>_safeAreaReader!=null?_safeAreaReader():new MobileSafeAreaSnapshot(DisplayServer.GetDisplaySafeArea(),GetViewport().GetScreenTransform());
 private bool ObserveSafeArea(MobileSafeAreaSnapshot value)
 {
  if(!value.IsValid||!TryMapSafeArea(value,GetViewport().GetVisibleRect(),out _)){SetTouchGeometryKnown(false);return false;}
  SetTouchGeometryKnown(true);
  bool changed=!_safeAreaSeen||value.PhysicalSafe!=_safeAreaSnapshot.PhysicalSafe||value.ScreenTransform!=_safeAreaSnapshot.ScreenTransform;
  _safeAreaSnapshot=value;_safeAreaSeen=true;return changed;
 }
 private void SetTouchGeometryKnown(bool known){if(_touchGeometryKnown==known)return;_touchGeometryKnown=known;if(_inputReady){_moveInputs.Reset("touch");_ui?.Joystick.ResetInput();UpdateTouchProvider();}}
 private void PollMobileSafeArea(double delta,bool force=false)
 {
  if(!_mobileInsetsEnabled||!IsInsideTree())return;
  if(!force){_safeAreaClock+=double.IsFinite(delta)?Math.Clamp(delta,0,.25):0;if(_safeAreaClock<SafeAreaPollInterval)return;}
  _safeAreaClock=0;
  if(!ObserveSafeArea(ReadSafeArea()))return;
  _pendingInsetSnapshot=true;ApplyViewportLayout();
 }
 private Rect2 CurrentSafeLayout(Rect2 viewportBounds,out Transform2D inputScreenTransform)
 {
  inputScreenTransform=GetViewport().GetScreenTransform();
  if(!_mobileInsetsEnabled)return viewportBounds;
  if(!_pendingInsetSnapshot)ObserveSafeArea(ReadSafeArea());
  _pendingInsetSnapshot=false;
  if(!_safeAreaSeen)return viewportBounds;
  var snapshot=_safeAreaSnapshot;inputScreenTransform=snapshot.ScreenTransform;
  if(TryMapSafeArea(snapshot,viewportBounds,out var mapped))return mapped;
  SetTouchGeometryKnown(false);return viewportBounds;
 }
 private static bool TryMapSafeArea(MobileSafeAreaSnapshot snapshot,Rect2 bounds,out Rect2 mapped)
 {
  var physical=new Rect2((Vector2)snapshot.PhysicalSafe.Position,(Vector2)snapshot.PhysicalSafe.Size);
  mapped=(snapshot.ScreenTransform.AffineInverse()*physical).Intersection(bounds);
  return mapped.Position.IsFinite()&&mapped.Size.IsFinite()&&mapped.Size.X>0&&mapped.Size.Y>0;
 }
 private void ReleaseSafeAreaProbe(){_mobileInsetsEnabled=false;_safeAreaReader=null;_safeAreaSeen=false;_pendingInsetSnapshot=false;_safeAreaClock=0;}
}
