using Godot;
using System.Linq;

namespace Hmi.GodotAdapter;

// Cosmetic only. No gameplay callback, input-provider token, or authority state enters this adapter.
internal partial class GameUi
{
 private readonly UiMotionController _motion=new();
 private long _motionPage;
 private Vector2 _motionLayout;
 private Rect2 _motionSafe;
 private bool _motionPreferenceLoaded,_motionUserReduced,_motionSystemReduced;
 private string _motionPreferencePath="user://presentation.cfg";
 private string _motionPreferenceStatus="";
 public void SetMotionPreferences(bool userReduced,bool systemReduced=false)
 {
  _motionUserReduced=userReduced;_motionSystemReduced=systemReduced;_motion.SetReducedMotion(userReduced,systemReduced);
 }
 private void LoadMotionPreference()
 {
  if(_motionPreferenceLoaded)return;_motionPreferenceLoaded=true;
  using var config=new ConfigFile();var error=config.Load(_motionPreferencePath);
  if(error==Error.FileNotFound){SetMotionPreferences(false,_motionSystemReduced);return;}
  if(error!=Error.Ok){_motionPreferenceStatus="设置读取失败，本次使用默认动效。";return;}
  var value=config.GetValue("ui","reduced_motion",false);
  if(value.VariantType!=Variant.Type.Bool){_motionPreferenceStatus="设置内容无效，本次使用默认动效。";return;}
  SetMotionPreferences(value.AsBool(),_motionSystemReduced);_motionPreferenceStatus="设置已保存在本机。";
 }
 private void ToggleMotionPreference()
 {
  SetMotionPreferences(!_motionUserReduced,_motionSystemReduced);
  using var config=new ConfigFile();var error=config.Load(_motionPreferencePath);
  // Preserve future presentation settings. Unreadable/corrupt files are never silently replaced.
  if(error==Error.Ok||error==Error.FileNotFound)
  {
   config.SetValue("ui","reduced_motion",_motionUserReduced);error=config.Save(_motionPreferencePath);
  }
  _motionPreferenceStatus=error==Error.Ok?"设置已保存在本机。":"本次生效，保存失败。重开后可能恢复原设置。";
  ShowMotionInstructions();
 }
 private void ShowMotionInstructions()
 {
  LoadMotionPreference();
  string state=_motionUserReduced?"已开启：界面内容立即显示，不播放界面动效。":"已关闭：界面使用短时动效。";
  if(_motionSystemReduced)state+="\n外部减少动态策略已生效。";
  BeginPersistenceModal("玩法说明","减少界面动效："+state+"\n"+_motionPreferenceStatus+"\n\n使用 WASD、方向键或左侧浮动摇杆移动，角色会自动攻击。\n控制器左摇杆移动，"+ConfirmButtonLabel+"确认、"+CancelButtonLabel+"返回，Start 暂停，上方键图鉴；右摇杆滚动图鉴。\n每波结束可免费选择一张卡，再用击杀金币购买道具。\n暂停可查看构筑；没有闪避操作。\n继续冒险会回到最近已保存的波次起点，本波中途进度不会保留。",true);
  MotionContext();
  PersistenceAction("减少界面动效："+(_motionUserReduced?"开启":"关闭"),ToggleMotionPreference,285,470,400,false);
  (_overlay.GetChildren().OfType<Button>().Last()).Name="ReducedMotionToggle";
  PersistenceAction("关闭",null,715,470,270,true);
  (_overlay.GetChildren().OfType<Button>().FirstOrDefault())?.GrabFocus();
 }
 private void MotionPageChanged()
 {
  LoadMotionPreference();_motionPage++;_motion.BeginScope();
 }
 private void MotionContext()
 {
  bool blocked=!_applicationActive||_codexOpen||_persistenceOpen||(_page!="home"&&_page!="selection"&&_page!="between");
  _motion.SetSuspended(blocked);
  if(_motionLayout!=_layoutSize||_motionSafe!=_safeRect){_motion.BeginScope();_motionLayout=_layoutSize;_motionSafe=_safeRect;}
 }
 private void MotionAdvance(double delta){MotionContext();_motion.Advance(delta);}
 private void MotionCue(Control drawn,UiMotionController.Cue cue)
 {
  MotionContext();_motion.Play(drawn,cue,_motion.Scope);
 }
 private void MotionPageReady()
 {
  MotionContext();
  foreach(Control node in _screen.GetChildren().OfType<Control>())
  {
   string name=node.Name.ToString();
   if(name=="MotionHomeTitle"||name=="MotionHomeSubtitle"||name=="HomeStart")MotionCue(node,UiMotionController.Cue.PageEnter);
   else if(name.StartsWith("MotionCard"))MotionCue(node,UiMotionController.Cue.CardEnter);
   else if(name=="MotionPickedStatus")MotionCue(node,UiMotionController.Cue.ValueChanged);
  }
 }
 private void MotionSelectionChanged()
 {
  if(_summary!=null)MotionCue(_summary,UiMotionController.Cue.SelectionChange);
  if(_selected>=0&&_selected<_heroButtons.Count)MotionCue(_heroButtons[_selected],UiMotionController.Cue.SelectionChange);
 }
 private void MotionBind(Button button)
 {
  long page=_motionPage;
  button.ButtonDown+=()=>MotionPress(button,true,page);
  button.ButtonUp+=()=>MotionPress(button,false,page);
  button.MouseExited+=()=>MotionCancel(button,page);
  button.FocusExited+=()=>MotionCancel(button,page);
 }
 private void MotionPress(Button button,bool pressed,long page)
 {
  if(page!=_motionPage)return;
  MotionContext();_motion.SetPressed(button,pressed,_motion.Scope);
 }
 private void MotionCancel(Button button,long page){if(page==_motionPage)_motion.Cancel(button);}
}
