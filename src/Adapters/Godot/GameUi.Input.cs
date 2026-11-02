using Godot;
using System;
namespace Hmi.GodotAdapter;
internal partial class GameUi
{
 public Action GameplayInputCanceled;
 private bool _touchAvailable=true,_pauseInputBlocked;
 private long _touchGeneration=1;
 public bool IsGameplayInputAllowed=>_page=="combat"&&!_pauseInputBlocked&&!_codexOpen&&!_persistenceOpen&&_applicationActive;
 public void SetTouchInputContext(long generation,bool enabled){_touchGeneration=generation;_touchAvailable=enabled;RefreshTouchAvailability();}
 private void RefreshTouchAvailability()=>Joystick.SetInputContext(_touchGeneration,_touchAvailable&&IsGameplayInputAllowed);
 public void CancelGameplayInput(){Joystick.ResetInput();GameplayInputCanceled?.Invoke();RefreshTouchAvailability();}
 private void ConfigureFloatingJoystick(float width,float height)
 {
  Joystick.Position=Vector2.Zero;Joystick.Size=new Vector2(width,height);
  float inset=TouchJoystick.Diameter*.5f+8;
  Joystick.ConfigureRegion(new Rect2(inset,inset,Math.Max(0,width*.5f-16-inset),Math.Max(0,height-2*inset)));
  Joystick.CanStart=()=>IsGameplayInputAllowed;
  Joystick.StartBlocked=JoystickOverlapsHud;
  RefreshTouchAvailability();
 }
 private bool JoystickOverlapsHud(Vector2 viewportPoint)
 {
  // Event coordinates are already viewport-local. OS safe area was mapped once by Host.Layout.
  var center=Joystick.GetGlobalTransformWithCanvas().AffineInverse()*viewportPoint;
  var bounds=new Rect2(center-Vector2.One*(TouchJoystick.Diameter*.5f),Vector2.One*TouchJoystick.Diameter);
  foreach(var node in _hud.GetChildren())
  {
   if(node is not Control c||ReferenceEquals(c,Joystick)||!c.IsVisibleInTree()||c.IsQueuedForDeletion())continue;
   if(c is Label label&&label.Text.Length==0)continue;
   var transform=Joystick.GetGlobalTransformWithCanvas().AffineInverse()*c.GetGlobalTransformWithCanvas();
   var a=transform*Vector2.Zero;var b=transform*c.Size;
   var rect=new Rect2(a.Min(b),(b-a).Abs());
   if(bounds.Intersects(rect))return true;
  }
  return false;
 }
}
