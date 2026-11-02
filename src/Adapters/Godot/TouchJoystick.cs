using Godot;
using System;

/// <summary>Floating pointer view/driver only. Movement policy and source selection live outside UI.</summary>
public partial class TouchJoystick : Control
{
    public const float Diameter=190, TravelRadius=Diameter*.34f;
    public Vector2 Direction {get;private set;}
    public Vector2 Center {get;private set;}
    public bool IsCaptured => _touch>=0 || _mouse;
    public bool VisualVisible => IsCaptured && _enabled;
    public int ActiveTouch => _touch;
    public long InputGeneration => IsCaptured?_captureGeneration:_generation;
    public Rect2 ActivationRegion {get;private set;}
    public Func<Vector2,bool> StartBlocked;
    public Func<bool> CanStart;
    private int _touch=-1;
    private bool _mouse,_enabled=true;
    private long _generation=1,_captureGeneration;
    public override void _Ready(){MouseFilter=MouseFilterEnum.Ignore;}
    public void SetInputContext(long generation,bool enabled)
    {
        if(generation!=_generation||enabled!=_enabled)ResetInput();
        _generation=generation;_enabled=enabled;QueueRedraw();
    }
    public void ConfigureRegion(Rect2 localRegion)
    {
        if(ActivationRegion==localRegion)return;
        ActivationRegion=localRegion;ResetInput();
    }
    public void Reset()=>ResetInput();
    public void ResetInput(){Direction=Vector2.Zero;_touch=-1;_mouse=false;_captureGeneration=0;QueueRedraw();}
    private static bool Finite(Vector2 p)=>float.IsFinite(p.X)&&float.IsFinite(p.Y);
    public bool CanBeginAtViewport(Vector2 viewportPosition)
    {
        if(!_enabled||IsCaptured||!IsInsideTree()||!IsVisibleInTree()||(CanStart!=null&&!CanStart())||!Finite(viewportPosition))return false;
        var local=GetGlobalTransformWithCanvas().AffineInverse()*viewportPosition;
        return ActivationRegion.HasPoint(local)&&(StartBlocked==null||!StartBlocked(viewportPosition));
    }
    private bool Begin(Vector2 viewportPosition,int touch,bool mouse)
    {
        if(!CanBeginAtViewport(viewportPosition))return false;
        Center=GetGlobalTransformWithCanvas().AffineInverse()*viewportPosition;
        _touch=touch;_mouse=mouse;_captureGeneration=_generation;Direction=Vector2.Zero;QueueRedraw();return true;
    }
    private void Drag(Vector2 viewportPosition)
    {
        if(!_enabled||_captureGeneration!=_generation||!Finite(viewportPosition)){ResetInput();return;}
        var local=GetGlobalTransformWithCanvas().AffineInverse()*viewportPosition;
        Direction=((local-Center)/TravelRadius).LimitLength();QueueRedraw();
    }
    private void Handled(){if(IsInsideTree())GetViewport().SetInputAsHandled();}
    // Acquire AFTER GUI handling. Existing capture updates remain global so dragging outside
    // the legal start area does not abruptly lose movement or transfer to a button.
    public override void _UnhandledInput(InputEvent e)
    {
        if(e is InputEventScreenTouch touch&&touch.Pressed&&!touch.Canceled&&touch.Index>=0&&Begin(touch.Position,touch.Index,false))Handled();
        else if(e is InputEventMouseButton mouse&&mouse.ButtonIndex==MouseButton.Left&&mouse.Pressed&&Begin(mouse.Position,-1,true))Handled();
    }
    public override void _Input(InputEvent e)
    {
        if(!IsCaptured)return;
        if(!_enabled||!IsVisibleInTree()||(CanStart!=null&&!CanStart())||_captureGeneration!=_generation){ResetInput();return;}
        if(e is InputEventScreenTouch touch&&_touch>=0&&touch.Index==_touch&&(!touch.Pressed||touch.Canceled)){ResetInput();Handled();}
        else if(e is InputEventScreenDrag drag&&_touch>=0&&drag.Index==_touch){Drag(drag.Position);Handled();}
        else if(e is InputEventMouseButton mouse&&_mouse&&mouse.ButtonIndex==MouseButton.Left&&!mouse.Pressed){ResetInput();Handled();}
        else if(e is InputEventMouseMotion motion&&_mouse){Drag(motion.Position);Handled();}
    }
    public override void _Notification(int what)
    {
        if(what==NotificationApplicationFocusOut||(what==NotificationVisibilityChanged&&IsInsideTree()&&!IsVisibleInTree()))ResetInput();
    }
    public override void _ExitTree()=>ResetInput();
    public override void _Draw()
    {
        if(!VisualVisible)return;
        var center=Center;
        DrawCircle(center+new Vector2(0,3),Diameter*.48f,new Color(.25f,.12f,.3f,.025f));
        DrawCircle(center,Diameter*.48f,new Color(.35f,.25f,.39f,.055f));
        DrawArc(center,Diameter*.29f,0,Mathf.Tau,48,new Color(1,1,1,.18f),1,true);
        DrawArc(center,Diameter*.46f,0,Mathf.Tau,48,new Color(.25f,.18f,.28f,.38f),3,true);
        DrawArc(center,Diameter*.46f,0,Mathf.Tau,48,new Color(1,1,1,.82f),1.5f,true);
        var knob=center+Direction*Diameter*.28f;
        DrawCircle(knob+new Vector2(0,3),Diameter*.17f,new Color(.3f,.15f,.2f,.06f));
        DrawCircle(knob,Diameter*.17f,new Color(1,.945f,.827f,.82f));
        DrawArc(knob,Diameter*.165f,0,Mathf.Tau,32,new Color(.25f,.18f,.28f,.38f),3,true);
        DrawArc(knob,Diameter*.165f,0,Mathf.Tau,32,new Color(1,1,1,.9f),1.5f,true);
    }
}
