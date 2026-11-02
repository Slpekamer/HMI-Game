using Godot;
using Hmi.Core;

public partial class GameHost
{
    private Viewport _layoutViewport;
    private bool _layoutQueued;
    private bool _portraitFraming;
    private bool _cameraBound;
    private int _cameraRun;
    private EntityId _cameraEntity;
    private Vector2 _cameraLayoutSize;
    private void InitializeViewportLayout()
    {
        InitializeSafeAreaProbe();
        _layoutViewport=GetViewport();
        _layoutViewport.SizeChanged+=QueueViewportLayout;
        ApplyViewportLayout();
    }
    private void QueueViewportLayout()
    {
        if(_layoutQueued || !IsInsideTree()) return;
        _layoutQueued=true;CallDeferred(nameof(ApplyViewportLayout));
    }
    private void ApplyViewportLayout()
    {
        _layoutQueued=false;if(!IsInsideTree())return;
        var window=GetWindow();var pixels=window.Size;
        if(pixels.X<=0||pixels.Y<=0)return;
        var basis=pixels.Y>pixels.X?new Vector2I(720,1280):new Vector2I(1280,720);
        window.ContentScaleMode=Window.ContentScaleModeEnum.CanvasItems;
        window.ContentScaleAspect=Window.ContentScaleAspectEnum.Expand;
        if(window.ContentScaleSize!=basis)window.ContentScaleSize=basis;
        var logical=GetViewport().GetVisibleRect().Size;
        if(logical.X<=0||logical.Y<=0)return;
        var safe=new Rect2(Vector2.Zero,logical);
        // Polling and direct resize share one OS-to-viewport mapping; events themselves
        // already carry viewport positions and are not transformed through this path.
        safe=CurrentSafeLayout(safe,out var inputTransform);
        if(_inputTransformSeen&&inputTransform!=_lastInputScreenTransform)_ui?.CancelGameplayInput();
        _lastInputScreenTransform=inputTransform;_inputTransformSeen=true;
        _ui?.SetLayoutMetrics(logical,safe);
        if(_camera!=null)
        {
            // Preserve baseline landscape framing. Portrait is an explicitly separate
            // short-axis/fixed-center candidate, never a UI-obstacle tracking camera.
            _portraitFraming=logical.Y>logical.X;
            _camera.KeepAspect=Camera3D.KeepAspectEnum.Height;
            // Isolated portrait readability trial: keep the short world axis at 16.8m.
            // It deliberately shows less distant arena than the rejected full-width formula.
            _camera.Size=_portraitFraming?16.8f/(logical.X/logical.Y):Mathf.Max(16.8f,(16.8f*1280/720)/(logical.X/logical.Y));
            bool changed=_cameraLayoutSize!=logical;_cameraLayoutSize=logical;
            // A genuine viewport change may reframe a paused world once. Ordinary
            // paused frames must never continue a trailing camera interpolation.
            if(changed&&_sim!=null&&_sim.TryGetEntity(_sim.PlayerId,out var player))SnapCamera(player);
        }
    }
    private Vector3 CameraFocus(EntityView player)
    {
        if(!_portraitFraming)return new Vector3(Mathf.Clamp(player.Position.X*.32f,-3,3),0,Mathf.Clamp(player.Position.Y*.24f,-1.5f,1.5f));
        int slot=player.Id.Index;
        if(_visuals!=null&&slot>=0&&slot<_visuals.Length&&_ids[slot]==player.Id&&GodotObject.IsInstanceValid(_visuals[slot]))
        {var p=_visuals[slot].Position;return new Vector3(p.X,0,p.Z);}
        return new Vector3(player.Position.X,0,player.Position.Y);
    }
    private void SnapCamera(EntityView player)
    {
        _camera.Position=CameraFocus(player)+new Vector3(12,22,18);
        _cameraBound=true;_cameraRun=_sim.RunToken;_cameraEntity=player.Id;
    }
    private void UpdateCameraFollow(EntityView player,float delta)
    {
        if(!_cameraBound||_cameraRun!=_sim.RunToken||_cameraEntity!=player.Id){SnapCamera(player);return;}
        if(_sim.Paused||_sim.Phase!=RunPhase.Combat)return;
        _camera.Position=_camera.Position.Lerp(CameraFocus(player)+new Vector3(12,22,18),1-Mathf.Exp(-3*delta));
    }
    private void ReleaseViewportLayout()
    {
        if(_layoutViewport!=null&&GodotObject.IsInstanceValid(_layoutViewport))_layoutViewport.SizeChanged-=QueueViewportLayout;
        _layoutViewport=null;ReleaseSafeAreaProbe();
    }
}
