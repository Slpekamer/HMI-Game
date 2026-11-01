using Godot;
using System;
namespace Hmi.GodotAdapter;
// Reconstructed presentation styles, not a byte-identical recovery of the lost theme.
internal static class CandyTheme
{
 public static readonly Color Cream=new("fff5df"),Ink=new("48214f"),Coral=new("ff6c98"),Purple=new("b18ad8"),Blue=new("70b4ec");
 public static StyleBoxFlat Box(Color color,int radius=20,Color? border=null,int width=0)
 {
  return new StyleBoxFlat{BgColor=color,CornerRadiusTopLeft=radius,CornerRadiusTopRight=radius,CornerRadiusBottomLeft=radius,CornerRadiusBottomRight=radius,
   BorderColor=border??new Color("ead4bb"),BorderWidthLeft=width,BorderWidthRight=width,BorderWidthTop=width,BorderWidthBottom=width,
   ShadowColor=new Color(0,0,0,.12f),ShadowSize=3,ShadowOffset=new Vector2(0,3),ContentMarginLeft=12,ContentMarginRight=12,ContentMarginTop=8,ContentMarginBottom=8};
 }
 public static StyleBoxTexture Frame(string tone)
 {
  var style=new StyleBoxTexture{Texture=GD.Load<Texture2D>("res://content/ui/revision/frame_"+tone+".svg")};
  style.TextureMarginLeft=38;style.TextureMarginRight=38;style.TextureMarginTop=38;style.TextureMarginBottom=38;
  style.ContentMarginLeft=0;style.ContentMarginRight=0;style.ContentMarginTop=0;style.ContentMarginBottom=0;
  return style;
 }
 public static StyleBoxTexture ButtonFace(string kind)
 {
  var style=new StyleBoxTexture{Texture=GD.Load<Texture2D>("res://content/ui/revision/button_"+kind+".svg")};
  style.TextureMarginLeft=40;style.TextureMarginRight=40;style.TextureMarginTop=28;style.TextureMarginBottom=28;
  style.ContentMarginLeft=10;style.ContentMarginRight=10;style.ContentMarginTop=4;style.ContentMarginBottom=4;
  return style;
 }
 public static Label Label(string text,int font=20)
 {
  var label=new Label{Text=text,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,MouseFilter=Control.MouseFilterEnum.Ignore};
  label.AddThemeFontSizeOverride("font_size",font);label.AddThemeColorOverride("font_color",Ink);label.AddThemeColorOverride("font_outline_color",Ink);label.AddThemeConstantOverride("outline_size",0);return label;
 }
 public static Button Button(string text,Action action,Color? color=null)
 {
  var button=new Button{Text=text,MouseDefaultCursorShape=Control.CursorShape.PointingHand,FocusMode=Control.FocusModeEnum.All};
  Color bg=color??Cream;button.AddThemeStyleboxOverride("normal",Box(bg));button.AddThemeStyleboxOverride("hover",Box(bg.Lightened(.08f),20,Ink,2));button.AddThemeStyleboxOverride("pressed",Box(bg.Darkened(.08f)));button.AddThemeStyleboxOverride("disabled",Box(new Color("dbcdbd")));button.AddThemeStyleboxOverride("focus",Box(new Color(0,0,0,0),20,new Color("ffce65"),3));
  button.AddThemeColorOverride("font_color",Ink);button.AddThemeColorOverride("font_hover_color",Ink);button.AddThemeColorOverride("font_pressed_color",Ink);button.AddThemeColorOverride("font_disabled_color",new Color("514457"));button.AddThemeFontSizeOverride("font_size",24);
  button.AddThemeStyleboxOverride("normal",color.HasValue?ButtonFace(color.Value==Purple?"secondary":"primary"):Box(Cream,20,new Color("f1d1ac"),3));
  button.AddThemeStyleboxOverride("hover",color.HasValue?ButtonFace(color.Value==Purple?"secondary":"primary"):Box(new Color("fff0ca"),20,new Color("ecc17d"),3));
  button.AddThemeStyleboxOverride("disabled",ButtonFace("disabled"));
  if(color.HasValue){button.AddThemeColorOverride("font_color",Colors.White);button.AddThemeColorOverride("font_hover_color",Colors.White);button.AddThemeColorOverride("font_pressed_color",Colors.White);button.AddThemeColorOverride("font_outline_color",color.Value.Darkened(.4f));button.AddThemeConstantOverride("outline_size",1);}
  button.Pressed+=action;return button;
 }
 public static Theme Create(){var theme=new Theme{DefaultFontSize=20};if(ResourceLoader.Exists("res://content/fonts/CandySans.otf"))theme.DefaultFont=new FontVariation{BaseFont=GD.Load<FontFile>("res://content/fonts/CandySans.otf"),VariationEmbolden=0.75f};theme.SetStylebox("panel","PanelContainer",Box(Cream));theme.SetColor("font_color","Label",Ink);theme.SetColor("font_color","Button",Ink);return theme;}
}
