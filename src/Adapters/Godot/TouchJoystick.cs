using Godot;

public partial class TouchJoystick : Control
{
    public Vector2 Direction { get; private set; }
    private int _touch = -1;
    private bool _mouse;
    public void Reset() => ResetInput();
    public void ResetInput() { Direction = Vector2.Zero; _touch = -1; _mouse = false; QueueRedraw(); }
    public override void _Ready() { MouseFilter = MouseFilterEnum.Stop; }
    private void SetDirection(Vector2 local) { Direction = ((local - Size / 2) / (Size.X * .34f)).LimitLength(); QueueRedraw(); }
    public override void _GuiInput(InputEvent e)
    {
        if (e is InputEventScreenTouch touch && touch.Pressed && _touch < 0) { _touch = touch.Index; SetDirection(touch.Position); AcceptEvent(); }
        if (e is InputEventMouseButton mouse && mouse.ButtonIndex == MouseButton.Left && mouse.Pressed) { _mouse = true; SetDirection(mouse.Position); AcceptEvent(); }
    }
    public override void _Input(InputEvent e)
    {
        if (e is InputEventScreenTouch touch && touch.Index == _touch && !touch.Pressed) ResetInput();
        if (e is InputEventScreenDrag drag && drag.Index == _touch) SetDirection(GetGlobalTransformWithCanvas().AffineInverse() * drag.Position);
        if (e is InputEventMouseButton mouse && mouse.ButtonIndex == MouseButton.Left && !mouse.Pressed && _mouse) ResetInput();
        if (e is InputEventMouseMotion motion && _mouse) SetDirection(GetGlobalTransformWithCanvas().AffineInverse() * motion.Position);
    }
    public override void _Draw()
    {
        var center = Size / 2;
        DrawCircle(center + new Vector2(0,3), Size.X * .48f, new Color(.25f,.12f,.3f,.15f));
        DrawCircle(center, Size.X * .48f, new Color(.35f,.25f,.39f,.34f));
        DrawArc(center, Size.X * .29f, 0, Mathf.Tau, 64, new Color(1,1,1,.45f), 2, true);
        for(int i=0;i<4;i++){float a=i*Mathf.Pi/2;var tip=center+Vector2.FromAngle(a)*Size.X*.4f;var side=Vector2.FromAngle(a+Mathf.Pi/2)*7;var back=tip-Vector2.FromAngle(a)*11;DrawColoredPolygon(new[]{tip,back+side,back-side},new Color(1,.95f,.85f,.55f));}
        DrawArc(center, Size.X * .46f, 0, Mathf.Tau, 64, new Color(1,1,1,.7f), 3, true);
        var knob=center + Direction * Size.X * .28f;
        DrawCircle(knob+new Vector2(0,3), Size.X * .17f, new Color(.3f,.15f,.2f,.2f));
        DrawCircle(knob, Size.X * .17f, new Color("fff1d3"));
        DrawArc(knob,Size.X*.165f,0,Mathf.Tau,48,Colors.White,3,true);
    }
}
