using Godot;
using Hmi.Core;
using System;

namespace Hmi.GodotAdapter;

/// <summary>One canvas draw list; enemy-only read model, never writes authority health.</summary>
public partial class EnemyHealthVisuals : Control
{
    private struct Bar { public EntityId Entity; public Vector2 Position; public float Ratio; public bool Visible; }
    private readonly Bar[] _bars;
    public int VisibleCount { get; private set; }
    public EnemyHealthVisuals() : this(512) { }
    public EnemyHealthVisuals(int capacity)
    {
        if(capacity<=0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _bars=new Bar[capacity]; MouseFilter=MouseFilterEnum.Ignore;
    }
    public void BeginFrame()
    {
        VisibleCount=0;
        for(int i=0;i<_bars.Length;i++) _bars[i].Visible=false;
    }
    public bool Update(int slot, EntityView view, Node3D actor, Camera3D camera, bool combatVisible)
    {
        if(slot<0 || slot>=_bars.Length) return false;
        bool wasVisible=_bars[slot].Visible; _bars[slot].Visible=false;
        if(wasVisible) VisibleCount--;
        if(view.Id.Index!=slot || !combatVisible || view.Kind!=EntityKind.Enemy || !view.Active ||
           !float.IsFinite(view.Health) || !float.IsFinite(view.MaxHealth) || view.Health<=0 || view.MaxHealth<=0 ||
           !GodotObject.IsInstanceValid(actor) || actor.IsQueuedForDeletion() || !actor.IsVisibleInTree() ||
           !GodotObject.IsInstanceValid(camera) || !camera.IsInsideTree() || camera.GetViewport()!=GetViewport()) return false;
        // Height is a cosmetic enemy-contract offset in world units, independent of HP and collider.
        Vector3 point=actor.GlobalPosition+Vector3.Up*1.12f;
        if(camera.IsPositionBehind(point)) return false;
        Vector2 screen=camera.UnprojectPosition(point);
        if(!float.IsFinite(screen.X) || !float.IsFinite(screen.Y) || !camera.GetViewport().GetVisibleRect().HasPoint(screen)) return false;
        // Camera projection is viewport-space; draw coordinates are local canvas-space.
        // This remains correct if a responsive parent/CanvasLayer later gains a transform.
        var canvas=GetGlobalTransformWithCanvas();
        Vector2 local=canvas.AffineInverse()*screen;
        var localRect=new Rect2(local-new Vector2(17,3),new Vector2(34,6));
        var projectedRect=canvas*localRect;
        if(!camera.GetViewport().GetVisibleRect().Encloses(projectedRect)) return false;
        _bars[slot]=new Bar { Entity=view.Id, Position=local, Ratio=Math.Clamp(view.Health/view.MaxHealth,0,1), Visible=true };
        VisibleCount++; return true;
    }
    public void EndFrame() => QueueRedraw();
    public void Clear() { BeginFrame(); QueueRedraw(); }
    public bool TryGetBar(int slot, out EntityId entity, out float ratio, out Vector2 position)
    {
        entity=default; ratio=0; position=default;
        if(slot<0 || slot>=_bars.Length || !_bars[slot].Visible) return false;
        var bar=_bars[slot];entity=bar.Entity;ratio=bar.Ratio;position=bar.Position;return true;
    }
    public override void _Draw()
    {
        foreach(var bar in _bars)
        {
            if(!bar.Visible) continue;
            Vector2 p=bar.Position-new Vector2(17,3);
            DrawRect(new Rect2(p,new Vector2(34,6)),new Color(.13f,.08f,.16f,.9f));
            DrawRect(new Rect2(p+Vector2.One,new Vector2(32*bar.Ratio,4)),new Color(.94f,.34f,.38f,1));
        }
    }
}
