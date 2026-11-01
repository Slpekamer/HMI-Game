using Godot;
using System;
using System.Linq;
namespace Hmi.GodotAdapter;
/// <summary>One instance-owned cosmetic preview; never references simulation state.</summary>
internal partial class HeroSelectionPreview : Control
{
    private SubViewport _viewport;
    private AnimationPlayer _player;
    private TextureRect _image;
    private Label _loading;
    private bool _awaitingFirstDraw;
    internal bool PreviewReady => !Released && !_awaitingFirstDraw;
    internal string HeroId { get; private set; } = "";
    internal double Clock => _player != null && GodotObject.IsInstanceValid(_player) ? _player.CurrentAnimationPosition : 0;
    internal bool Released { get; private set; }
    internal void Initialize(AssetVisuals assets, string heroId)
    {
        HeroId = heroId;
        MouseFilter = MouseFilterEnum.Ignore;
        _viewport = new SubViewport { Name="SelectionWorld",Size=new Vector2I(600,650),TransparentBg=true,OwnWorld3D=true,RenderTargetUpdateMode=SubViewport.UpdateMode.WhenVisible };
        AddChild(_viewport);
        var model=assets.Create("hero_"+heroId);model.Name="SelectionHero_"+heroId;_viewport.AddChild(model);model.RotationDegrees=new Vector3(0,-12,0);
        _player=model.FindChildren("*","AnimationPlayer",true,false).OfType<AnimationPlayer>().FirstOrDefault();
        if(_player==null)throw new InvalidOperationException("Selection hero has no AnimationPlayer: "+heroId);
        // Loop flags belong to this preview, never to the imported shared animation libraries.
        foreach(var name in _player.GetAnimationLibraryList())
        {
            var local=(AnimationLibrary)_player.GetAnimationLibrary(name).Duplicate(true);
            _player.RemoveAnimationLibrary(name);_player.AddAnimationLibrary(name,local);
        }
        _player.CallbackModeProcess=AnimationMixer.AnimationCallbackModeProcess.Manual;
        var idle=_player.GetAnimationList().FirstOrDefault(n=>n.ToString().EndsWith("Idle",StringComparison.OrdinalIgnoreCase));
        if(string.IsNullOrEmpty(idle))throw new InvalidOperationException("Selection hero has no Idle clip: "+heroId);
        _player.GetAnimation(idle).LoopMode=Animation.LoopModeEnum.Linear;_player.Play(idle);_player.Advance(0);
        var camera=new Camera3D{Position=new Vector3(0,.84f,-3.2f),Projection=Camera3D.ProjectionType.Orthogonal,Size=1.65f,Current=true};_viewport.AddChild(camera);camera.LookAt(new Vector3(0,.61f,0));
        _viewport.AddChild(new DirectionalLight3D{RotationDegrees=new Vector3(-38,152,0),LightEnergy=.8f});
        _viewport.AddChild(new WorldEnvironment{Environment=new Godot.Environment{BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=Colors.Transparent,AmbientLightSource=Godot.Environment.AmbientSource.Color,AmbientLightColor=Colors.White,AmbientLightEnergy=.4f}});
        _image=new TextureRect{Texture=_viewport.GetTexture(),ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,MouseFilter=MouseFilterEnum.Ignore};AddChild(_image);_image.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _loading=CandyTheme.Label("正在载入角色",20);_loading.Name="PreviewLoading";AddChild(_loading);_loading.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _awaitingFirstDraw=true;RenderingServer.FramePostDraw+=ObserveFirstDraw;
    }
    private void ObserveFirstDraw()
    {
        if(Released||_viewport==null||!GodotObject.IsInstanceValid(_viewport))return;
        if(_viewport.GetRenderInfo(Viewport.RenderInfoType.Visible,Viewport.RenderInfo.ObjectsInFrame)<=0)return;
        _awaitingFirstDraw=false;_loading.Hide();RenderingServer.FramePostDraw-=ObserveFirstDraw;
    }
    internal void Advance(double delta,bool frozen)
    {
        if(Released||frozen||!IsVisibleInTree()||delta<=0||!double.IsFinite(delta)||_player==null)return;
        _player.Advance(Math.Min(delta,.1));
    }
    internal void Release()
    {
        if(Released)return;Released=true;
        if(_awaitingFirstDraw)RenderingServer.FramePostDraw-=ObserveFirstDraw;_awaitingFirstDraw=false;
        if(_player!=null&&GodotObject.IsInstanceValid(_player))_player.Stop();
        if(_image!=null&&GodotObject.IsInstanceValid(_image))_image.Texture=null;
        _player=null;_viewport=null;_image=null;
    }
    public override void _ExitTree()=>Release();
}
