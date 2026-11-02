using System;
namespace Hmi.Adapters.Input;
[Flags] public enum MenuAction { None=0, Confirm=1, Cancel=2, Pause=4, Codex=8 }
public readonly struct MenuIntent
{
 public readonly int X,Y;public readonly MenuAction Action;
 public MenuIntent(int x,int y,MenuAction action){X=x;Y=y;Action=action;}
}
/// <summary>One selected device is sampled by the platform adapter; this has no engine/UI types.</summary>
public sealed class GamepadMenuRouter
{
 public const float Engage=.55f,Release=.35f;
 public const double InitialRepeat=.35,RepeatInterval=.12;
 private long _scope=long.MinValue;private bool _navigationArmed,_armed,_confirm,_cancel,_pause,_codex;
 private int _x,_y;private double _repeat;
 public void Reset(){_scope=long.MinValue;_armed=false;_navigationArmed=false;_confirm=_cancel=_pause=_codex=false;_x=_y=0;_repeat=0;}
 public MenuIntent Sample(long scope,double delta,float x,float y,bool confirm,bool cancel,bool pause,bool codex)
 {
  if(scope!=_scope){_scope=scope;_armed=false;_navigationArmed=false;_x=_y=0;_repeat=0;}
  if(!float.IsFinite(x)||!float.IsFinite(y)){x=y=0;}
  if(!confirm&&!cancel&&!pause&&!codex)_armed=true;
  MenuAction action=MenuAction.None;
  if(_armed){if(confirm&&!_confirm)action|=MenuAction.Confirm;if(cancel&&!_cancel)action|=MenuAction.Cancel;if(pause&&!_pause)action|=MenuAction.Pause;if(codex&&!_codex)action|=MenuAction.Codex;}
  _confirm=confirm;_cancel=cancel;_pause=pause;_codex=codex;
  // One cardinal direction wins; no diagonal duplicate navigation. Keep current within hysteresis.
  int nx=0,ny=0;float threshold=(_x!=0||_y!=0)?Release:Engage;
  if(Math.Abs(x)<=Release&&Math.Abs(y)<=Release)_navigationArmed=true;
  if(_navigationArmed&&(Math.Abs(x)>=threshold||Math.Abs(y)>=threshold)){if(Math.Abs(x)>Math.Abs(y))nx=Math.Sign(x);else ny=Math.Sign(y);}
  bool fire=false;
  if(nx!=_x||ny!=_y){_x=nx;_y=ny;_repeat=InitialRepeat;fire=nx!=0||ny!=0;}
  else if(nx!=0||ny!=0){_repeat-=double.IsFinite(delta)?Math.Clamp(delta,0,.25):0;if(_repeat<=0){fire=true;_repeat=RepeatInterval;}}
  return new MenuIntent(fire?nx:0,fire?ny:0,action);
 }
}
