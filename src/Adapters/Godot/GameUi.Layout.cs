using Godot;
using System;
using System.Linq;
namespace Hmi.GodotAdapter;

// Presentation-only reflow. Existing controls, delegates and modal generations survive resize.
internal partial class GameUi
{
 private ScrollContainer _screenScroll;
 private ColorRect _pageBackground;
 private bool _responsiveQueued;
 private void RequestResponsiveLayout()
 {
  if(_responsiveQueued)return;_responsiveQueued=true;CallDeferred(nameof(ApplyResponsiveLayout));
 }
 private static Rect2 Original(Control c)=>c.HasMeta("layout_base")?c.GetMeta("layout_base").AsRect2():new Rect2(c.Position,c.Size);
 private static void Rect(Control c,float x,float y,float w,float h){c.Position=new Vector2(x,y);c.Size=new Vector2(w,h);}
 private void ApplyResponsiveLayout()
 {
  _responsiveQueued=false;if(_screenScroll==null||_overlay==null||!IsInsideTree())return;
  var safe=_safeRect;if(safe.Size.X<=0||safe.Size.Y<=0)return;
  bool portrait=safe.Size.Y>safe.Size.X;
  _pageBackground.Position=Vector2.Zero;_pageBackground.Size=_layoutSize;_pageBackground.Visible=_page=="home"||_page=="selection"||_page=="between"||_page=="ending";
  _screenScroll.Position=safe.Position;_screenScroll.Size=safe.Size;
  _screenScroll.VerticalScrollMode=portrait?ScrollContainer.ScrollMode.Auto:ScrollContainer.ScrollMode.Disabled;
  _screen.Scale=Vector2.One;
  float w=safe.Size.X,h=safe.Size.Y;
  if(!portrait)
  {
   float scale=Math.Min(1,w/1280);_screen.Scale=new Vector2(scale,scale);
   _screen.CustomMinimumSize=new Vector2(w/scale,h/scale);_screen.Size=_screen.CustomMinimumSize;
   var offset=new Vector2((w/scale-1280)/2,(h/scale-720)/2);
   foreach(var c in _screen.GetChildren().OfType<Control>()){var b=Original(c);c.Position=b.Position+offset;c.Size=b.Size;if(c is ColorRect){c.Position=Vector2.Zero;c.Size=_screen.Size;}}
  }
  else
  {
   float content=LayoutPortraitScreen(w,h);_screen.CustomMinimumSize=new Vector2(w,Math.Max(h,content));_screen.Size=_screen.CustomMinimumSize;
  }
  LayoutHud(safe,portrait);LayoutOverlay(safe,portrait);CallDeferred(nameof(RevealLayoutFocus));
 }
 private void RevealLayoutFocus()
 {
  if(!IsInsideTree()||_screenScroll==null||!GodotObject.IsInstanceValid(_screenScroll))return;
  var focus=GetViewport().GuiGetFocusOwner();
  if(focus!=null&&focus.IsVisibleInTree()&&_screenScroll.IsAncestorOf(focus))_screenScroll.EnsureControlVisible(focus);
 }
 private float LayoutPortraitScreen(float w,float h)
 {
  float content=h;
  int cardCount=_shop?4:3;
  foreach(var c in _screen.GetChildren().OfType<Control>())
  {
   var b=Original(c);float x=(w-b.Size.X)/2,y=b.Position.Y,cw=Math.Min(b.Size.X,w-32),ch=b.Size.Y;
   if(c is ColorRect){Rect(c,0,0,w,h);continue;}
   if(_page=="home") { y+=Math.Max(0,(h-720)/2);x=(w-cw)/2; }
   else if(_page=="selection")
   {
    string name=c.Name.ToString();
    if(name=="SelectionBackHome"){x=16;y=16;}
    else if(b.Position.Y==32){y=83;cw=w-32;x=16;}
    else if(ReferenceEquals(c,_selectionStage)){cw=Math.Min(430,w-32);ch=420;x=(w-cw)/2;y=155;}
    else if(name=="SelectionSummary"){x=16;cw=w-32;y=590;}
    else if(b.Position.Y==215){x=16;cw=w-32;y=650;}
    else if(name.StartsWith("HeroCard_",StringComparison.Ordinal)){int i=_heroButtons.IndexOf((Button)c);cw=(w-48)/5;x=16+i*(cw+4);y=710;}
    else if(b.Position.Y==427){x=16;cw=w-32;y=825;}
    else if(name=="StartAdventure"){cw=Math.Min(400,w-32);x=(w-cw)/2;y=890;}
    else if(name=="SelectionSeed"){x=(w-cw)/2;y=985;}
    else if(b.Position.Y==654){x=16;cw=w-32;y=1050;ch=60;if(c is Label l)l.AutowrapMode=TextServer.AutowrapMode.WordSmart;}
    content=Math.Max(content,1130);
   }
   else if(_page=="between")
   {
    float bottom=340+cardCount*(_shop?440:450);
    bool card=false;
    for(int i=0;i<cardCount;i++)
    {
     float left=_shop?30+i*310:123+i*350;float width=_shop?290:334;float top=_shop?193:173;
     if(b.Position.X>=left&&b.Position.X<left+width&&b.Position.Y>=top&&b.Position.Y<610)
     {x=(w-width)/2+b.Position.X-left;y=340+i*(_shop?440:450)+b.Position.Y-top;card=true;break;}
    }
    if(!card)
    {
     if(b.Position.Y<=35){if(b.Position.X>=980){y=165+b.Position.Y-20;x=(w-260)/2+b.Position.X-995;}else{y=25;x=(w-cw)/2;}}
     else if(b.Position.Y==93){y=230;x=(w-cw)/2;}
     else if(b.Position.Y==121){y=110;x=(w-cw)/2;}
     else if(b.Position.Y==119){y=275;x=b.Position.X<500?16:w-cw-16;}
     else if(b.Position.Y>=625)
     {
      if(c is Button){y=bottom+95+(b.Position.X<300?80:0);cw=Math.Min(360,w-32);x=(w-cw)/2;}
      else {y=bottom+b.Position.Y-625;x=(w-cw)/2;}
     }
    }
    content=Math.Max(content,bottom+260);
   }
   else if(_page=="ending")
   {
    x=(w-cw)/2;y=b.Position.Y+Math.Max(0,(h-800)/2);
    if(c is PanelContainer){x=16;cw=w-32;ch=700;}
    else if(c is Label label){label.AutowrapMode=TextServer.AutowrapMode.WordSmart;ch=Math.Max(ch,80);}
    if(c is Button){y=b.Position.Y+Math.Max(0,(h-800)/2);if(b.Position.Y==430)y+=b.Position.X>640?85:0;if(b.Position.Y==535)y+=80;}
    content=Math.Max(content,y+ch+40);
   }
   Rect(c,x,y,cw,ch);
  }
  if(_page=="ending")
  {
   float y=Math.Max(24,(h-780)/2)+24;
   foreach(var c in _screen.GetChildren().OfType<Control>().Where(c=>c is Label||c is Button).OrderBy(c=>Original(c).Position.Y).ThenBy(c=>Original(c).Position.X))
   {
    float width=c is Button?Math.Min(400,w-48):w-48;
    c.Size=new Vector2(width,Original(c).Size.Y);
    if(c is Label label)label.AutowrapMode=TextServer.AutowrapMode.WordSmart;
    float height=Math.Max(c is Button?60:Original(c).Size.Y,c.GetCombinedMinimumSize().Y);
    Rect(c,(w-width)/2,y,width,height);y+=height+20;
   }
   foreach(var panel in _screen.GetChildren().OfType<PanelContainer>())Rect(panel,16,Math.Max(24,(h-780)/2),w-32,y-Math.Max(24,(h-780)/2));
   content=Math.Max(h,y+24);
  }
  return content;
 }
 private void LayoutHud(Rect2 safe,bool portrait)
 {
  float scale=portrait?Math.Min(1,safe.Size.X/720):Math.Min(1,safe.Size.X/1280);
  float w=safe.Size.X/scale,h=safe.Size.Y/scale;_hud.Position=safe.Position;_hud.Scale=new Vector2(scale,scale);_hud.Size=new Vector2(w,h);
  foreach(var c in _hud.GetChildren().OfType<Control>())
  {
   var b=Original(c);float x=b.Position.X,y=b.Position.Y;
   if(ReferenceEquals(c,Joystick)){y=h-205;}
   else if(ReferenceEquals(c,_companionTray)){x=portrait?w-432:(w-416)/2;y=h-117;}
   else if(ReferenceEquals(c,_saveNotice)){x=portrait?(w-b.Size.X)/2:w/2+108;y=portrait?220:28;}
   else if(ReferenceEquals(c,_buff)){x=(w-b.Size.X)/2;y=portrait?270:109;}
   else if(ReferenceEquals(c,_status)){x=(w-b.Size.X)/2;y=h-(portrait?255:145);}
   else if(b.Position.X>=1040){x+=w-1280;if(b.Position.Y>600)y=h-(portrait?305:71);}
   else if(b.Position.X>=548&&b.Position.X<=732){x+=w/2-640;if(portrait)y+=100;}
   Rect(c,x,y,b.Size.X,b.Size.Y);
  }
 }
 private void LayoutOverlay(Rect2 safe,bool portrait)
 {
  _overlay.Position=safe.Position;_overlay.Scale=Vector2.One;_overlay.Size=safe.Size;
  var controls=_overlay.GetChildren().OfType<Control>().ToArray();
  if(!portrait)
  {
   float scale=Math.Min(1,safe.Size.X/1280);_overlay.Scale=new Vector2(scale,scale);var size=safe.Size/scale;
   var offset=(size-new Vector2(1280,720))/2;
   foreach(var c in controls){var b=Original(c);c.Position=b.Position+offset;c.Size=b.Size;if(c is ColorRect){c.Position=-safe.Position/scale;c.Size=_layoutSize/scale;}}
   return;
  }
  var buttons=controls.OfType<Button>().ToArray();
  float w=safe.Size.X,h=safe.Size.Y;float panelHeight=Math.Min(h-32,_codexOpen?h-32:_persistenceOpen?680:200+buttons.Length*76);float top=(h-panelHeight)/2;
  int button=0,label=0;
  foreach(var c in controls)
  {
   var b=Original(c);
   if(c is ColorRect){Rect(c,-safe.Position.X,-safe.Position.Y,_layoutSize.X,_layoutSize.Y);continue;}
   if(c is PanelContainer){Rect(c,16,top,w-32,panelHeight);continue;}
   if(c is RichTextLabel){Rect(c,32,top+100,w-64,Math.Max(100,panelHeight-140-buttons.Length*76));continue;}
   if(c is Button){Rect(c,32,top+panelHeight-24-buttons.Length*76+button++*76,w-64,60);continue;}
   if(c is Label text){text.AutowrapMode=TextServer.AutowrapMode.WordSmart;Rect(c,32,top+20+label++*76,w-64,64);continue;}
   Rect(c,(w-b.Size.X)/2,b.Position.Y,b.Size.X,b.Size.Y);
  }
 }
}
