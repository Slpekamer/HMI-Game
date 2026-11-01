#nullable enable annotations
// Reconstructed from historical UI source; fresh validation required. Rules remain in Core.
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
namespace Hmi.GodotAdapter;
internal readonly struct OfferUi
{
 public readonly string Id,Name,Detail,Category,Badge,DisabledText; public readonly bool Enabled;
 public bool Temporary=>Badge=="仅下一波";
 public OfferUi(string id,string name,string detail,string category,bool enabled=true,string badge="",string disabledText="不可选择"){Id=id;Name=name;Detail=detail;Category=category;Enabled=enabled;Badge=badge;DisabledText=disabledText;}
}
internal partial class GameUi:CanvasLayer
{
 public Action<int,uint>? StartRequested;
 public Action? PauseRequested,NextRequested,RestartRequested,RecallRequested,StationRequested,ContinueRequested,FinishRequested;
 public Action<string>? PickRequested,BuyRequested;
 public Action? ResumeRequested,ExitToSelectionRequested,RetrySaveRequested,ContinueUnsavedRequested;
 private ResumeUi _resumeState=new();
 private bool _persistenceOpen,_persistenceBusy,_persistenceCancelable;
 private int _persistenceGeneration;
 private Action? _persistenceCancel;
 public Func<string>? CodexRequested; public Action? CodexClosed;
 private bool _codexOpen;
 private Viewport? _mainViewport; private bool _previousDisable3D;
 private readonly List<(Control Control,Control.FocusModeEnum Mode)> _modalFocus = new();
 private readonly List<(BaseButton Button,bool Disabled)> _modalButtons = new();
 private void LockBackground(Node node)
 {
  if(node is Control control){_modalFocus.Add((control,control.FocusMode));control.FocusMode=Control.FocusModeEnum.None;}
  if(node is BaseButton button){_modalButtons.Add((button,button.Disabled));button.Disabled=true;}
  foreach(var child in node.GetChildren(true))LockBackground(child);
 }
 private void UnlockBackground()
 {
  foreach(var entry in _modalFocus)if(GodotObject.IsInstanceValid(entry.Control))entry.Control.FocusMode=entry.Mode;
  foreach(var entry in _modalButtons)if(GodotObject.IsInstanceValid(entry.Button))entry.Button.Disabled=entry.Disabled;
  _modalFocus.Clear();_modalButtons.Clear();
 }

 public TouchJoystick Joystick{get;}=new();
 private Control _root=null!,_screen=null!,_hud=null!,_overlay=null!;
 private Label _saveNotice=null!;
 private Label _health=null!,_wave=null!,_timer=null!,_wallet=null!,_buff=null!,_status=null!,_summary=null!;
 private readonly List<Label> _selectedBadges=new();
 private Control _portrait=null!,_companionTray=null!,_shooterPortrait=null!,_supportPortrait=null!; private bool _hudPreviewsReady; private string _portraitHero=""; private bool _shop;
 private ProgressBar _healthBar=null!; private Button _pause=null!;
 private int _selected=3; private uint _seed=1030; private string _page="";
 private readonly List<Button> _heroButtons=new(); private readonly AssetVisuals _assets=new();
 private readonly Dictionary<string,Texture2D> _selectionPortraits=new();
 public override void _Ready(){
  _mainViewport=GetViewport();_previousDisable3D=_mainViewport.Disable3D;
  _root=new Control{Name="UI",Theme=CandyTheme.Create(),MouseFilter=Control.MouseFilterEnum.Ignore};AddChild(_root);_root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
  _screen=new Control{Name="Screens",MouseFilter=Control.MouseFilterEnum.Ignore};_root.AddChild(_screen);_screen.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);BuildHud();
  _overlay=new Control{MouseFilter=Control.MouseFilterEnum.Ignore};_root.AddChild(_overlay);_overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);ShowSelection();
 }
 public override void _ExitTree()
 {
  _assets.ReleaseCachedReferences();
  if(_mainViewport!=null&&GodotObject.IsInstanceValid(_mainViewport))_mainViewport.Disable3D=_previousDisable3D;
 }
 private static T Place<T>(Control parent,T node,float x,float y,float w,float h)where T:Control{parent.AddChild(node);node.Position=new Vector2(x,y);node.Size=new Vector2(w,h);return node;}
 private static void Icon(Control parent,string id,float x,float y,float size){string path="res://content/ui/icon_"+id+".svg";if(!ResourceLoader.Exists(path))return;Place(parent,new TextureRect{Texture=GD.Load<Texture2D>(path),ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,MouseFilter=Control.MouseFilterEnum.Ignore},x,y,size,size);}
 private static void Illustration(Control parent,string id,float x,float y,float w,float h){string path="res://content/ui/revision/illustrations/"+id+".svg";if(!ResourceLoader.Exists(path)){Icon(parent,id,x+(w-h)/2,y,h);return;}Place(parent,new TextureRect{Texture=GD.Load<Texture2D>(path),ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,MouseFilter=Control.MouseFilterEnum.Ignore},x,y,w,h);}
 private static Label Text(Control parent,string text,float x,float y,float w,float h,int font=20)=>Place(parent,CandyTheme.Label(text,font),x,y,w,h);
 private static Label Wrapped(Control parent,string text,float x,float y,float w,float h,int font=18)
 {
  var label=CandyTheme.Label("",font);
  label.AutowrapMode=TextServer.AutowrapMode.WordSmart;
  label.AddThemeConstantOverride("line_spacing",0);
  label.VerticalAlignment=VerticalAlignment.Top;
  Place(parent,label,x,y,w,h);
  label.Text=text;
  return label;
 }
 private static PanelContainer Panel(Control parent,float x,float y,float w,float h,Color? color=null){var p=Place(parent,new PanelContainer{MouseFilter=Control.MouseFilterEnum.Ignore},x,y,w,h);p.AddThemeStyleboxOverride("panel",CandyTheme.Frame("cream"));return p;}
 private static void Frame(Control parent,float x,float y,float w,float h,string tone="cream") { var p=Panel(parent,x,y,w,h);if(h<90){Color c=tone=="blue"?new("b9e4fc"):tone=="purple"?new("ddc0ee"):tone=="coral"?new("ffc4bb"):tone=="gold"?new("ffe3a0"):CandyTheme.Cream;p.AddThemeStyleboxOverride("panel",CandyTheme.Box(c,24,new Color("fff5df"),3));}else p.AddThemeStyleboxOverride("panel",CandyTheme.Frame(tone)); }
 private static void Backdrop(Control parent) => Place(parent,new TextureRect{Texture=GD.Load<Texture2D>("res://content/ui/revision/candy_city_background.png"),ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.Scale,MouseFilter=Control.MouseFilterEnum.Ignore},0,0,1280,720);
 private static void Heading(Control parent,string title,string subtitle){Frame(parent,370,18,540,95);var h=Text(parent,title,375,23,530,78,56);h.AddThemeColorOverride("font_outline_color",new Color("fff0be"));h.AddThemeConstantOverride("outline_size",7);Frame(parent,374,121,532,40);Text(parent,subtitle,380,122,520,36,20);}
 private static void Clear(Control control){foreach(Node child in control.GetChildren()){control.RemoveChild(child);child.QueueFree();}}
 private static Button Btn(Control parent,string title,Action action,float x,float y,float w,float h,Color? color=null)
 {
  Button? button=null;
  button=CandyTheme.Button(title,()=>{
   // Clear removes controls immediately, before queued native destruction. Retained old
   // delegates must not reinterpret an old screen's click against a newer run/page.
   if(button!=null&&GodotObject.IsInstanceValid(button)&&button.IsInsideTree()&&!button.IsQueuedForDeletion()&&button.IsVisibleInTree()&&!button.Disabled)action();
  },color);
  return Place(parent,button,x,y,w,h);
 }
 private void SetPage(string page){DismissPersistenceModal();if(_codexOpen)CloseCodex();_codexOpen=false;_page=page;GetViewport().Disable3D=page=="selection"||page=="between"||page=="ending";Clear(_screen);Clear(_overlay);_hud.Visible=page=="combat";if(page!="combat")ClearHudPreviews();Joystick.Reset();}
 public void ShowSelection(){
  SetPage("selection");_assets.ReleaseCachedReferences();Backdrop(_screen);_heroButtons.Clear();_selectedBadges.Clear();Heading(_screen,"选择守护者","初始机制不同，成长路线自由融合");
  string[] tones={"coral","coral","gold","gold","blue"};
  for(int i=0;i<5;i++){
   int index=i;float x=24+i*248;
   var button=Btn(_screen,"",()=>SelectHero(index),x,180,240,365);button.Name="HeroCard_"+AssetVisuals.HeroIds[i];_heroButtons.Add(button);
   button.AddThemeStyleboxOverride("normal",CandyTheme.Frame(tones[i]));
   AddSelectionPortrait(button,AssetVisuals.HeroIds[i]);
   Frame(button,24,264,192,42,tones[i]);Text(button,AssetVisuals.HeroNames[i],20,264,200,40,29);
   Icon(button,AssetVisuals.HeroIds[i],25,311,38);Text(button,AssetVisuals.HeroTraits[i],62,312,157,36,20);
   var badge=Text(button,"✓ 已选择",42,-22,156,39,21);badge.Name="SelectedBadge";badge.AddThemeStyleboxOverride("normal",CandyTheme.Box(new Color("58b8ff"),18,Colors.White,3));badge.AddThemeColorOverride("font_color",Colors.White);_selectedBadges.Add(badge);
  }
  Frame(_screen,280,576,595,108);_summary=Text(_screen,"",300,587,550,38,26);_summary.Name="SelectionSummary";Text(_screen,"全员基础近战 · 自动攻击",300,632,550,28,20);
  var seedBox=Place(_screen,new SpinBox{Name="SelectionSeed",MinValue=1,MaxValue=999999,Value=_seed,Step=1,Prefix="种子 "},38,608,205,50);seedBox.ValueChanged+=v=>_seed=(uint)v;
  bool showSave=_resumeState.CanContinue||_resumeState.CanRetry||_resumeState.ErrorText.Length>0||_resumeState.SaveStatus.Length>0;
  if(showSave){
   Btn(_screen,_resumeState.CanContinue?"继续最近安全点":"查看存档状态",ShowResumeDialog,911,570,332,50,CandyTheme.Purple).Name="ResumeSafePoint";
   Btn(_screen,"开始新冒险  ›",()=>StartRequested?.Invoke(_selected,_seed),911,632,332,50,CandyTheme.Coral).Name="StartAdventure";
  }else Btn(_screen,"开始冒险  ›",()=>StartRequested?.Invoke(_selected,_seed),911,595,332,76,CandyTheme.Coral).Name="StartAdventure";
  Text(_screen,"WASD / 方向键 / 左侧摇杆移动 · Esc 暂停",300,690,650,24,15);SelectHero(_selected);
 }
 private void SelectHero(int index){
  _selected=index;string[] tones={"coral","coral","gold","gold","blue"};
  for(int i=0;i<_heroButtons.Count;i++){
   _heroButtons[i].AddThemeStyleboxOverride("normal",CandyTheme.Frame(i==index?"blue":tones[i]));
   _heroButtons[i].Scale=i==index?new Vector2(1.015f,1.015f):Vector2.One;
   _selectedBadges[i].Visible=i==index;
  }
  if(_summary!=null)_summary.Text=AssetVisuals.HeroNames[index]+"  ·  "+AssetVisuals.HeroTraits[index];
 }
 private void AddSelectionPortrait(Control card,string heroId)
 {
  if(!_selectionPortraits.TryGetValue(heroId,out var texture))
  {
   string path="res://content/ui/portraits/hero_"+heroId+"_portrait.tres";
   texture=GD.Load<Texture2D>(path)??throw new InvalidOperationException("Missing required selection portrait: "+path);
   _selectionPortraits.Add(heroId,texture);
  }
  // Fit the complete illustrated silhouette without distortion; align feet to the card baseline.
  Vector2 sourceSize=texture.GetSize();
  float scale=Math.Min(200f/sourceSize.X,230f/sourceSize.Y);
  Vector2 drawSize=sourceSize*scale;
  Place(card,new TextureRect{Name="SelectionPortrait",Texture=texture,ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,MouseFilter=Control.MouseFilterEnum.Ignore},20+(200-drawSize.X)/2,252-drawSize.Y,drawSize.X,drawSize.Y);
 }
 private void ClearHudPreviews()
 {
  Clear(_portrait);Clear(_shooterPortrait);Clear(_supportPortrait);
  _portraitHero="";_hudPreviewsReady=false;
 }
 private void EnsureHudPreviews()
 {
  if(_hudPreviewsReady)return;
  AddHudModelPreview(_shooterPortrait,"companion_shooter",0,0,52,58);
  AddHudModelPreview(_supportPortrait,"companion_support",0,0,52,58);
  _hudPreviewsReady=true;
 }
 private void AddHudModelPreview(Control card,string id,float x=0,float y=0,float width=218,float height=258,bool bust=false){
  var viewport=new SubViewport{Size=new Vector2I((int)width*2,(int)height*2),TransparentBg=true,RenderTargetUpdateMode=SubViewport.UpdateMode.WhenVisible,OwnWorld3D=true};card.AddChild(viewport);var model=_assets.Create(id);viewport.AddChild(model);model.RotationDegrees=new Vector3(0,-12,0);
  // The 1.2 m hero contract includes hats. Keep headroom above it for idle motion;
  // portrait framing must not change the model scale or gameplay collision dimensions.
  float lookHeight=bust?.88f:id.StartsWith("companion_")?.36f:.61f;
  var camera=new Camera3D{Name=bust?"HudHeroCamera":"HudCompanionCamera",Position=new Vector3(0,bust?lookHeight:.84f,-3.2f),Projection=Camera3D.ProjectionType.Orthogonal,Size=bust?.86f:id.StartsWith("companion_")?.85f:1.48f,Current=true};viewport.AddChild(camera);camera.LookAt(new Vector3(0,lookHeight,0));
  viewport.AddChild(new DirectionalLight3D{RotationDegrees=new Vector3(-38,152,0),LightEnergy=.8f});viewport.AddChild(new WorldEnvironment{Environment=new Godot.Environment{BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=Colors.Transparent,AmbientLightSource=Godot.Environment.AmbientSource.Color,AmbientLightColor=Colors.White,AmbientLightEnergy=.4f}});
  PlayIdle(model);
  Place(card,new TextureRect{Texture=viewport.GetTexture(),ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,MouseFilter=Control.MouseFilterEnum.Ignore},x,y,width,height);
 }
 private static void PlayIdle(Node node){if(node is AnimationPlayer player){foreach(var name in player.GetAnimationList())if(name.ToString().EndsWith("Idle",StringComparison.OrdinalIgnoreCase)){player.GetAnimation(name).LoopMode=Animation.LoopModeEnum.Linear;player.Play(name);break;}}foreach(var child in node.GetChildren())PlayIdle(child);}
 private void BuildHud(){
  _hud=new Control{Name="CombatHud",MouseFilter=Control.MouseFilterEnum.Ignore};_root.AddChild(_hud);_hud.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
  Frame(_hud,20,18,345,85);_portrait=Place(_hud,new Control{MouseFilter=Control.MouseFilterEnum.Ignore},22,10,84,92);
  _health=Text(_hud,"棉队",106,23,242,32,23);_healthBar=Place(_hud,new ProgressBar{ShowPercentage=false,MouseFilter=Control.MouseFilterEnum.Ignore},110,64,232,23);_healthBar.AddThemeStyleboxOverride("background",CandyTheme.Box(new Color("84748e"),12));_healthBar.AddThemeStyleboxOverride("fill",CandyTheme.Box(new Color("ff6590"),12,Colors.White,1));
  Frame(_hud,548,12,184,51);_wave=Text(_hud,"第 1 / 6 波",550,15,180,42,25);_timer=Text(_hud,"00:24",568,65,144,37,24);_timer.AddThemeStyleboxOverride("normal",CandyTheme.Box(CandyTheme.Ink,18,Colors.White,2));_timer.AddThemeColorOverride("font_color",Colors.White);
  Frame(_hud,1040,18,145,66);Icon(_hud,"coin",1050,27,45);_wallet=Text(_hud,"0",1098,23,76,48,29);_pause=Btn(_hud,"Ⅱ",()=>PauseRequested?.Invoke(),1200,18,62,64);
  Place(_hud,Joystick,22,515,190,190);
  _saveNotice=Text(_hud,_resumeState.SaveStatus,748,28,272,28,16);_saveNotice.Visible=_resumeState.SaveStatus.Length>0;_saveNotice.Name="SaveStatusNotice";_saveNotice.AddThemeStyleboxOverride("normal",CandyTheme.Box(CandyTheme.Cream,12));
  _companionTray=Place(_hud,new Control{MouseFilter=Control.MouseFilterEnum.Ignore},432,603,416,105);
  Frame(_companionTray,0,0,416,105);Text(_companionTray,"伙伴协战",10,4,396,28,20);
  _shooterPortrait=Place(_companionTray,new Control{MouseFilter=Control.MouseFilterEnum.Ignore},18,32,52,58);_supportPortrait=Place(_companionTray,new Control{MouseFilter=Control.MouseFilterEnum.Ignore},220,32,52,58);
  Btn(_companionTray,"驻守 / 游走",()=>StationRequested?.Invoke(),72,42,128,40);Btn(_companionTray,"召回",()=>RecallRequested?.Invoke(),278,42,114,40);
  Btn(_hud,"构筑图鉴",ShowCodex,1090,649,168,49);_buff=Text(_hud,"",390,109,500,32,17);_status=Text(_hud,"",410,575,460,28,15);
 }
 public void ShowCombat(){if(_page!="combat")SetPage("combat");EnsureHudPreviews();}
 public void UpdateHud(string hero,float health,float max,int wave,float remaining,int coins,string buffs,string status,bool hasCompanions,bool endless=false,int endlessCompleted=0){
  _health.Text=$"{hero}  {Math.Max(0,(int)Math.Ceiling(health))} / {Math.Max(0,(int)Math.Ceiling(max))}";_healthBar.MaxValue=max;_healthBar.Value=health;_wave.AddThemeFontSizeOverride("font_size",endless&&wave>9999?19:25);_wave.Text=endless?$"无尽 {wave}":$"第 {wave} / 6 波";_timer.Text=$"00:{Math.Max(0,(int)Math.Ceiling(remaining)):00}";_wallet.Text=$"{coins}";_buff.Text=buffs;_status.Text=status;_companionTray.Visible=hasCompanions;
  if(_portraitHero!=hero){_portraitHero=hero;Clear(_portrait);int index=Array.IndexOf(AssetVisuals.HeroNames,hero);if(index>=0)AddHudModelPreview(_portrait,"hero_"+AssetVisuals.HeroIds[index],0,0,84,92,true);}
 }
 public void ShowPause(bool paused){if(_page!="combat"||_codexOpen||_persistenceOpen)return;if(!paused){Clear(_overlay);return;}Clear(_overlay);Shade();Panel(_overlay,390,ExitToSelectionRequested!=null?180:200,500,ExitToSelectionRequested!=null?390:310);Text(_overlay,"休息一下",400,223,480,54,34);Text(_overlay,"战斗时间与冷却均已暂停",400,283,480,34,20);Btn(_overlay,"继续守护",()=>PauseRequested?.Invoke(),460,335,360,60,CandyTheme.Coral);Btn(_overlay,"重开整局 · 清空金币",()=>RestartRequested?.Invoke(),460,415,360,52);
  if(ExitToSelectionRequested!=null)Btn(_overlay,_resumeState.CanContinue?"退出至选人 · 保留安全点":"退出至选人 · 无安全点",ShowExitConfirmation,460,480,360,52);
 }
 private void Shade()=>Place(_overlay,new ColorRect{Color=new Color(.15f,.09f,.19f,.46f),MouseFilter=Control.MouseFilterEnum.Stop},0,0,1280,720);
 public void ShowBetween(IReadOnlyList<OfferUi> offers,int coins,int earned,int spent,bool picked,IReadOnlyList<OfferUi> items,bool shop,string selectionStatus="",bool endless=false){
  var pickAction=PickRequested;var buyAction=BuyRequested;var nextAction=NextRequested;var finishAction=FinishRequested;SetPage("between");_shop=shop;Backdrop(_screen);Heading(_screen,shop?"波末补给":"波次完成",shop?"选牌免费 · 道具花费金币":"免费三选一 · 每波仅选一张");
  Frame(_screen,995,20,260,69,"gold");Icon(_screen,"coin",1004,31,42);Text(_screen,$"余额 {coins}",1048,28,195,49,27);Text(_screen,$"获得 {earned} − 花费 {spent}",986,93,280,29,18);
  if(!shop){
   for(int i=0;i<offers.Count;i++){
    var offer=offers[i];float x=123+i*350;string tone=i==0?"purple":i==1?"blue":"coral";Frame(_screen,x,193,334,407,tone);Frame(_screen,x+72,173,190,55,tone);Text(_screen,i==0?"特性":i==1?"基础":"风险",x+72,176,190,46,26);
    string icon=offer.Id.StartsWith("base.health")?"heart":offer.Id.StartsWith("base.")?"speed":offer.Id.StartsWith("risk.")?"risk":offer.Id.Split('.')[0];
    if(!offer.Temporary)Illustration(_screen,icon,x+38,232,258,94);
    if(offer.Badge.Length>0)Text(_screen,offer.Badge,x+80,229,174,27,18);
    Text(_screen,offer.Name,x+18,offer.Temporary?287:325,298,44,27);Wrapped(_screen,offer.Detail,x+28,offer.Temporary?334:372,278,offer.Temporary?190:150,20);
    var button=Btn(_screen,picked?"本波已选择":offer.Enabled?"选择":offer.DisabledText,()=>pickAction?.Invoke(offer.Id),x+28,530,278,56,CandyTheme.Coral);button.Disabled=picked||!offer.Enabled;
   }
   Frame(_screen,365,625,470,66);Text(_screen,picked?(selectionStatus.Length>0?selectionStatus:"卡片已加入构筑"):"选牌免费，商店另行兑换",375,636,450,42,21);
   Btn(_screen,"去商店补给  ›",()=>ShowBetween(offers,coins,earned,spent,picked,items,true,selectionStatus,endless),926,633,280,61,CandyTheme.Purple);
  }else{
   string[] tones={"purple","blue","gold","coral"};
   for(int i=0;i<items.Count;i++){
    var item=items[i];float x=30+i*310;Frame(_screen,x,193,290,411,tones[i]);
    Illustration(_screen,item.Id=="tonic"?"soda":item.Id=="shoes"?"insoles":item.Id=="gloves"?"glove":"shield",x+35,201,220,115);
    Text(_screen,item.Name,x+14,316,262,40,27);Frame(_screen,x+43,359,204,35,"gold");Text(_screen,item.Category,x+45,360,200,32,19);
    Wrapped(_screen,item.Detail,x+24,402,242,135,17).AddThemeConstantOverride("line_spacing",-3);
    var buy=Btn(_screen,item.Enabled?"兑换":"不可购买",()=>buyAction?.Invoke(item.Id),x+25,541,240,56,item.Enabled?CandyTheme.Coral:null);buy.Disabled=!item.Enabled;
   }
   Btn(_screen,"‹ 免费卡片",()=>ShowBetween(offers,coins,earned,spent,picked,items,false,selectionStatus,endless),34,638,230,60);Frame(_screen,355,631,500,65);Text(_screen,selectionStatus.Length>0?selectionStatus:"道具从下一波生效",365,641,480,40,19);
   var next=Btn(_screen,picked?"下一波  ›":"请先领取免费卡",()=>nextAction?.Invoke(),970,634,280,65,CandyTheme.Purple);next.Disabled=!picked;
  }
  Btn(_screen,"构筑图鉴",ShowCodex,32,119,165,49);
  if(endless)Btn(_screen,"结束无尽",()=>finishAction?.Invoke(),1060,119,185,49);
 }
 public bool IsShop=>_page=="between"&&_shop;
 public void ShowEnding(bool won,int kills,int coins,int earned,int spent){SetPage("ending");Backdrop(_screen);Panel(_screen,300,130,680,460);Text(_screen,won?"糖果世界，守住了！":"再试一次，守护者",320,168,640,72,36);Text(_screen,won?"六波挑战完成":"被发条军团击败 · 本局结束",320,255,640,44,24);Text(_screen,$"击败敌人 {kills}  ·  余额 {coins}\n累计获得 {earned} − 花费 {spent}",320,325,640,80,23);Btn(_screen,"重新选择 · 开始新局",()=>RestartRequested?.Invoke(),440,458,400,70,CandyTheme.Coral);}
 public void ShowRunEnding(bool decision,bool defeated,bool finiteCompleted,int endlessCompleted,int kills,int coins,int earned,int spent,string reason)
 {
  var continueAction=ContinueRequested;var finishAction=FinishRequested;var restartAction=RestartRequested;
  SetPage("ending");Backdrop(_screen);Panel(_screen,240,90,800,550);
  Text(_screen,decision?"六波挑战完成":defeated?"本局守护结束":"本局已结算",260,112,760,65,36);
  Text(_screen,$"有限挑战：{(finiteCompleted?"已完成":"未完成")}  ·  无尽通过 {endlessCompleted} 波",260,192,760,42,24);
  Text(_screen,$"击败敌人 {kills}  ·  余额 {coins}\n累计获得 {earned} − 花费 {spent}",260,257,760,76,24);
  Wrapped(_screen,decision?"继续无尽保留当前构筑与金币，先进入本波补给；不会重复发放奖励。":reason,300,347,680,66,21);
  if(decision){Btn(_screen,"继续无尽",()=>continueAction?.Invoke(),300,430,320,65,CandyTheme.Coral);Btn(_screen,"结束本局",()=>finishAction?.Invoke(),660,430,320,65,CandyTheme.Purple);}
  Btn(_screen,"重新选择 · 开始新局",()=>restartAction?.Invoke(),440,535,400,58);
 }
 public bool IsCodexOpen=>_codexOpen;
 public void CloseCodex(){if(!_codexOpen)return;_codexOpen=false;UnlockBackground();Clear(_overlay);CodexClosed?.Invoke();}
 public void ShowCodex(){if(_codexOpen||_persistenceOpen)return;_codexOpen=true;GetViewport().GuiGetFocusOwner()?.ReleaseFocus();LockBackground(_screen);LockBackground(_hud);string text=CodexRequested?.Invoke()??"";Clear(_overlay);Shade();Panel(_overlay,180,70,920,580);Text(_overlay,"构筑 · 技能树 · 道具",200,88,880,50,30);var info=Place(_overlay,new RichTextLabel{Text=text,BbcodeEnabled=false,ScrollActive=true,SelectionEnabled=true},220,154,840,410);info.AddThemeColorOverride("default_color",CandyTheme.Ink);info.AddThemeFontSizeOverride("normal_font_size",20);Btn(_overlay,"关闭",CloseCodex,880,581,170,50).GrabFocus();}
 public bool IsPersistenceModalOpen=>_persistenceOpen;
 public void SetResumeState(ResumeUi state)
 {
  state??=new ResumeUi();
  if(_resumeState.CanContinue==state.CanContinue&&_resumeState.Summary==state.Summary&&_resumeState.RollbackWarning==state.RollbackWarning&&_resumeState.SaveStatus==state.SaveStatus&&_resumeState.ErrorText==state.ErrorText&&_resumeState.CanRetry==state.CanRetry)return;
  _resumeState=state;
  if(_saveNotice!=null){_saveNotice.Text=state.SaveStatus;_saveNotice.Visible=state.SaveStatus.Length>0;}
  if(_page=="selection"&&!_persistenceOpen)ShowSelection();
 }
 private void BeginPersistenceModal(string title,string message,bool cancelable,Action? cancel=null)
 {
  // Mark before closing codex so the host can suppress its normal resume callback.
  _persistenceOpen=true;_persistenceBusy=false;_persistenceCancelable=cancelable;_persistenceCancel=cancel;_persistenceGeneration++;
  if(_codexOpen)CloseCodex();
  UnlockBackground();LockBackground(_screen);LockBackground(_hud);
  GetViewport().GuiGetFocusOwner()?.ReleaseFocus();Joystick.Reset();Clear(_overlay);Shade();
  Panel(_overlay,230,95,820,530);Text(_overlay,title,260,114,760,56,32);
  // Long platform errors remain fully available through scrolling, never ellipsized.
  var body=Place(_overlay,new RichTextLabel{Name="PersistenceMessage",Text=message,BbcodeEnabled=false,ScrollActive=true,SelectionEnabled=true},275,190,730,220);
  body.AddThemeColorOverride("default_color",CandyTheme.Ink);body.AddThemeFontSizeOverride("normal_font_size",21);
 }
 private void PersistenceAction(string title,Action? action,float x,float y,float width,bool close,Color? color=null)
 {
  int generation=_persistenceGeneration;
  var button=Btn(_overlay,title,()=>{
   if(!_persistenceOpen||_persistenceBusy||generation!=_persistenceGeneration)return;
   _persistenceBusy=true;
   if(close)DismissPersistenceModal();
   else foreach(var child in _overlay.GetChildren())if(child is BaseButton item)item.Disabled=true;
   action?.Invoke();
  },x,y,width,60,color);
  if(color==null){button.AddThemeColorOverride("font_focus_color",CandyTheme.Ink);button.AddThemeColorOverride("font_hover_pressed_color",CandyTheme.Ink);button.AddThemeColorOverride("font_color",CandyTheme.Ink);button.AddThemeColorOverride("font_hover_color",CandyTheme.Ink);button.AddThemeColorOverride("font_pressed_color",CandyTheme.Ink);}
  button.Name="PersistenceAction";
  var actions=_overlay.GetChildren().OfType<Button>().ToArray();
  for(int i=0;i<actions.Length;i++){
   actions[i].FocusNext=actions[(i+1)%actions.Length].GetPath();
   actions[i].FocusPrevious=actions[(i+actions.Length-1)%actions.Length].GetPath();
  }
 }
 private void DismissPersistenceModal()
 {
  if(!_persistenceOpen)return;
  _persistenceOpen=false;_persistenceBusy=false;_persistenceCancelable=false;_persistenceCancel=null;_persistenceGeneration++;
  UnlockBackground();if(_overlay!=null)Clear(_overlay);
 }
 public bool TryCancelPersistenceModal()
 {
  if(!_persistenceOpen||!_persistenceCancelable||_persistenceBusy)return false;
  var cancel=_persistenceCancel;DismissPersistenceModal();cancel?.Invoke();return true;
 }
 public void HideSaveFailure()=>DismissPersistenceModal();
 public void ShowSaveFailure(string message,bool canContinueUnsaved)
 {
  string warning=canContinueUnsaved?"\n\n本局不会生成可恢复进度，后续波次也不会自动保存；退出后需重新开始。":"\n\n本次操作尚未保存。不能通过关闭此提示开始或恢复战斗。";
  BeginPersistenceModal("存档未成功",message+warning,false);
  var retry=RetrySaveRequested;var unsaved=ContinueUnsavedRequested;
  PersistenceAction("重试保存",retry,canContinueUnsaved?285:460,470,canContinueUnsaved?300:360,false,CandyTheme.Purple);
  if(canContinueUnsaved)PersistenceAction("继续本局（不保存）",unsaved,645,470,350,false,CandyTheme.Coral);
  (_overlay.GetChildren().OfType<Button>().FirstOrDefault())?.GrabFocus();
 }
 public void ShowReplaceRunConfirmation(Action confirm,Action? cancel=null)
 {
  BeginPersistenceModal("开始新局前确认","开始新冒险会替换现有可继续的局。\n"+_resumeState.Summary+"\n取消不会更改当前角色、种子或已保存进度。",true,cancel);
  PersistenceAction("取消，保留原局",()=>cancel?.Invoke(),285,470,310,true);
  PersistenceAction("确认开始新局",confirm,660,470,310,true,CandyTheme.Coral);
  (_overlay.GetChildren().OfType<Button>().FirstOrDefault())?.GrabFocus();
 }
 private void ShowResumeDialog()
 {
  var state=_resumeState;var resume=ResumeRequested;var retry=RetrySaveRequested;
  BeginPersistenceModal(state.CanContinue?"继续最近安全点":"存档状态",state.Summary+"\n"+state.RollbackWarning+"\n"+state.SaveStatus+"\n"+state.ErrorText,true);
  PersistenceAction("返回选人",null,285,470,310,true);
  if(state.CanContinue)PersistenceAction("确认继续",resume,660,470,310,true,CandyTheme.Purple);
  else if(state.CanRetry)PersistenceAction("重试读取 / 保存",retry,660,470,310,false,CandyTheme.Purple);
  (_overlay.GetChildren().OfType<Button>().FirstOrDefault())?.GrabFocus();
 }
 private void ShowExitConfirmation()
 {
  var exit=ExitToSelectionRequested;
  BeginPersistenceModal("退出至选人",(_resumeState.CanContinue?"将保留最近安全点，但当前波内的进度不会保存。\n再次继续时会回到最近已保存的波次起点。\n"+_resumeState.Summary:"当前没有可恢复安全点，退出后本局无法恢复，需要重新开始。\n"+_resumeState.SaveStatus)+"\n"+_resumeState.RollbackWarning,true,()=>ShowPause(true));
  PersistenceAction("返回暂停",()=>ShowPause(true),285,470,310,true);
  PersistenceAction("确认退出至选人",exit,660,470,310,true,CandyTheme.Purple);
  (_overlay.GetChildren().OfType<Button>().FirstOrDefault())?.GrabFocus();
 }

}
