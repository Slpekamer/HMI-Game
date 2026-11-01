using System;
using Godot;

namespace Hmi.GodotAdapter;

/// <summary>Cosmetic, manually advanced UI motion. No gameplay callbacks, input ownership or Tween resources.</summary>
internal sealed class UiMotionController : IDisposable
{
    internal enum Cue { PageEnter, CardEnter, SelectionChange, ValueChanged }
    internal const int Capacity = 32;
    private enum Channel { Fade, Emphasis, PressColor, PressProxy }
    private struct Slot
    {
        internal Control Target;
        internal BaseButton HitTarget;
        internal Color OriginalColor;
        internal Vector2 OriginalScale;
        internal Channel Channel;
        internal double Age, Duration;
        internal bool Held, Releasing;
    }
    private readonly Slot[] _slots = new Slot[Capacity];
    private long _scope;
    private bool _reduced, _disposed, _suspended;
    internal long Scope => _scope;
    internal int ActiveCount { get { int n=0;foreach(var s in _slots)if(s.Target!=null)n++;return n; } }
    internal int Rejected { get; private set; }
    internal int Dropped { get; private set; }
    internal bool ReducedMotion => _reduced;
    internal bool Suspended => _suspended;

    internal long BeginScope()
    {
        CancelAll();
        if (_disposed) return _scope;
        if (_scope == long.MaxValue) throw new InvalidOperationException("UI motion scope exhausted; replace this controller.");
        return ++_scope;
    }
    internal long SetSuspended(bool suspended)
    {
        if(_disposed||_suspended==suspended)return _scope;
        long next=BeginScope();_suspended=suspended;return next;
    }
    // Caller resolves platform accessibility preferences; unsupported platform APIs are not guessed.
    internal void SetReducedMotion(bool userPreference, bool systemPreference = false)
    {
        bool next=userPreference||systemPreference;
        if(next!=_reduced)CancelAll();
        _reduced=next;
    }
    internal bool Play(Control target, Cue cue, long scope)
    {
        if(!Allowed(target,scope))return false;
        int i=Acquire(target);if(i<0)return false;
        ref var s=ref _slots[i];s.Channel=cue==Cue.ValueChanged?Channel.Emphasis:Channel.Fade;
        s.Duration=cue==Cue.CardEnter?.14:cue==Cue.PageEnter?.16:.18;s.Age=0;s.Held=false;s.Releasing=false;s.HitTarget=null;
        Apply(ref s);return true;
    }
    // Whole-control press feedback is colour-only: actual click/focus geometry never changes.
    internal bool SetPressed(Control target, bool pressed, long scope)
    {
        if(!Allowed(target,scope))return false;
        if(target is BaseButton button&&button.Disabled){Cancel(target);Rejected++;return false;}
        int i=Find(target);
        if(!pressed&&(i<0||_slots[i].Channel!=Channel.PressColor))return false;
        if(i<0)i=Acquire(target);
        if(i<0)return false;
        ref var s=ref _slots[i];
        if(s.Channel==Channel.PressColor&&s.Held==pressed)return true;
        Restore(ref s);s.OriginalColor=s.Target.SelfModulate;s.OriginalScale=s.Target.Scale;
        s.Channel=Channel.PressColor;s.Held=pressed;s.Releasing=!pressed;s.Age=0;s.Duration=pressed?.06:.14;s.HitTarget=null;
        Apply(ref s);return true;
    }
    // Optional visual-only child squash. Interactive parent retains its rectangle, transform and signals.
    internal bool SetDecorativePressed(Control visual, BaseButton hitTarget, bool pressed, long scope)
    {
        if(!Allowed(visual,scope))return false;
        if(!Valid(hitTarget)||hitTarget.Disabled||visual==hitTarget||!hitTarget.IsAncestorOf(visual)||!Passive(visual)) {Cancel(visual);Rejected++;return false;}
        int i=Find(visual);if(!pressed&&(i<0||_slots[i].Channel!=Channel.PressProxy))return false;if(i<0)i=Acquire(visual);if(i<0)return false;
        ref var s=ref _slots[i];
        if(s.Channel==Channel.PressProxy&&s.Held==pressed&&s.HitTarget==hitTarget)return true;
        Restore(ref s);s.OriginalColor=s.Target.SelfModulate;s.OriginalScale=s.Target.Scale;
        s.Channel=Channel.PressProxy;s.HitTarget=hitTarget;s.Held=pressed;s.Releasing=!pressed;s.Age=0;s.Duration=pressed?.06:.14;
        Apply(ref s);return true;
    }
    internal void Advance(double delta)
    {
        if(_disposed||_reduced||_suspended||!double.IsFinite(delta)||delta<=0)return;
        // A hitch settles these <=180ms accents rather than leaving a slow-motion UI behind.
        delta=Math.Min(delta,.25);
        for(int i=0;i<_slots.Length;i++)
        {
            ref var s=ref _slots[i];if(s.Target==null)continue;
            if(!Valid(s.Target)||(s.Channel==Channel.PressColor&&s.Target is BaseButton button&&button.Disabled)||(s.Channel==Channel.PressProxy&&(!Valid(s.HitTarget)||s.HitTarget.Disabled||!s.HitTarget.IsAncestorOf(s.Target)||!Passive(s.Target)))) {Restore(ref s);s=default;continue;}
            s.Age+=delta;
            if(s.Age>=s.Duration&&!s.Held){Restore(ref s);s=default;continue;}
            Apply(ref s);
        }
    }
    internal void Cancel(Control target)
    {
        int i=Find(target);if(i<0)return;Restore(ref _slots[i]);_slots[i]=default;
    }
    // Restore only. Lifecycle barriers must use BeginScope or SetSuspended to reject stale tokens.
    internal void CancelAll()
    {
        for(int i=0;i<_slots.Length;i++){Restore(ref _slots[i]);_slots[i]=default;}
    }
    public void Dispose(){if(_disposed)return;CancelAll();_disposed=true;}
    private bool Allowed(Control target,long scope)
    {
        if(_disposed||_suspended||scope!=_scope||scope<=0){Rejected++;return false;}
        if(!Valid(target)){Cancel(target);Rejected++;return false;}
        if(_reduced){Cancel(target);return false;}
        return true;
    }
    private static bool Valid(Control c)=>c!=null&&GodotObject.IsInstanceValid(c)&&c.IsInsideTree()&&c.IsVisibleInTree()&&!c.IsQueuedForDeletion();
    private static bool Passive(Control c)
    {
        var type=c.GetType();
        bool decorative=type==typeof(Control)||type==typeof(Label)||type==typeof(TextureRect)||type==typeof(ColorRect)||type==typeof(Panel)||type==typeof(NinePatchRect);
        if(!decorative||c.MouseFilter!=Control.MouseFilterEnum.Ignore||c.FocusMode!=Control.FocusModeEnum.None)return false;
        foreach(Node child in c.GetChildren())if(child is not Control control||!Passive(control))return false;
        return true;
    }
    private int Find(Control c){for(int i=0;i<_slots.Length;i++)if(_slots[i].Target==c&&c!=null)return i;return -1;}
    private int Acquire(Control c)
    {
        int existing=Find(c);if(existing>=0){Restore(ref _slots[existing]);_slots[existing].OriginalColor=c.SelfModulate;_slots[existing].OriginalScale=c.Scale;return existing;}
        for(int i=0;i<_slots.Length;i++)if(_slots[i].Target==null){_slots[i]=new Slot{Target=c,OriginalColor=c.SelfModulate,OriginalScale=c.Scale};return i;}
        Dropped++;return -1;
    }
    private static void Apply(ref Slot s)
    {
        float t=(float)Math.Clamp(s.Age/s.Duration,0,1);float ease=1-MathF.Pow(1-t,3);
        switch(s.Channel)
        {
            case Channel.Fade:
                s.Target.SelfModulate=new Color(s.OriginalColor.R,s.OriginalColor.G,s.OriginalColor.B,s.OriginalColor.A*(.82f+.18f*ease));break;
            case Channel.Emphasis:
                float factor=.90f+.10f*ease;s.Target.SelfModulate=new Color(s.OriginalColor.R*factor,s.OriginalColor.G*factor,s.OriginalColor.B*factor,s.OriginalColor.A);break;
            case Channel.PressColor:
                float shade=s.Releasing?.92f+.08f*ease:1-.08f*ease;s.Target.SelfModulate=new Color(s.OriginalColor.R*shade,s.OriginalColor.G*shade,s.OriginalColor.B*shade,s.OriginalColor.A);break;
            case Channel.PressProxy:
                float scale=s.Releasing?.97f+.03f*ease:1-.03f*ease;s.Target.Scale=s.OriginalScale*scale;break;
        }
    }
    private static void Restore(ref Slot s)
    {
        if(s.Target==null||!GodotObject.IsInstanceValid(s.Target))return;
        if(s.Channel==Channel.PressProxy)s.Target.Scale=s.OriginalScale;
        else s.Target.SelfModulate=s.OriginalColor;
    }
}
