using System;
using System.Numerics;
namespace Hmi.PresentationStudy;

public readonly record struct MeleeAimEvent(Scope Scope,EntityKey Source,EntityKey Target,long Tick,int Index,Vector2 Direction);
/// <summary>One scope-owned cosmetic mailbox. No engine clock, attack timer, animation replay or world writes.</summary>
public sealed class AuthoritativeMeleeAim
{
    readonly MeleeAimEvent[] audit=new MeleeAimEvent[16];
    public MeleeAimEvent Recent(int newestOffset){if(newestOffset<0||newestOffset>=Math.Min(Captured,audit.Length))throw new ArgumentOutOfRangeException(nameof(newestOffset));return audit[(int)((Captured-1-newestOffset)%audit.Length)];}
    Scope scope;EntityKey source;bool bound,pending,latched;MeleeAimEvent latest,latch;
    long highTick=-1;int highIndex=-1;object nativeOwner;long nativeEpoch=-1,latchSequence=-1;
    public long Captured {get;private set;} public long Merged {get;private set;}
    public long Rejected {get;private set;} public long Discarded {get;private set;}
    public long HighTick=>highTick;public int HighIndex=>highIndex;public bool HasPending=>pending;public bool HasLatch=>latched;
    public bool Bind(Scope value,EntityKey owner)
    {
        if(value.Run<0||value.Wave<1||!owner.Valid)return false;
        if(bound&&scope==value&&source==owner)return true;
        scope=value;source=owner;bound=true;highTick=-1;highIndex=-1;
        Captured=Merged=Rejected=Discarded=0;ClearPresentation();return true;
    }
    // Same-scope presentation teardown retains high-water, even if native owner is reconstructed.
    public void ClearPresentation(){if(pending)Discarded++;pending=latched=false;nativeOwner=null;nativeEpoch=latchSequence=-1;latest=latch=default;}
    public bool Capture(MeleeAimEvent value,long currentTick,bool paused)
    {
        float n=value.Direction.LengthSquared();
        if(!bound||value.Scope!=scope||value.Source!=source||!value.Target.Valid||value.Tick<0||value.Tick>currentTick||value.Index<0||
            !ReadyAttention.Finite(value.Direction)||!float.IsFinite(n)||n<=1e-10f||value.Tick<highTick||(value.Tick==highTick&&value.Index<=highIndex))
        {Rejected++;return false;}
        highTick=value.Tick;highIndex=value.Index;
        if(paused){Rejected++;if(pending)Discarded++;pending=false;return false;}
        if(pending)Merged++;
        latest=value with{Direction=value.Direction/MathF.Sqrt(n)};pending=true;audit[(int)(Captured%audit.Length)]=latest;Captured++;return true;
    }
    public bool Resolve(object owner,long epoch,long sequence,bool active,bool paused,out MeleeAimEvent value,out bool fresh)
    {
        value=default;fresh=false;
        if(owner==null||epoch<1||sequence<0){ClearPresentation();return false;}
        if(!ReferenceEquals(nativeOwner,owner)||nativeEpoch!=epoch){latched=false;latchSequence=-1;nativeOwner=owner;nativeEpoch=epoch;}
        if(paused){if(pending)Discarded++;pending=false;return false;}
        if(pending){value=latest;pending=false;fresh=true;latched=active&&sequence>0;latch=value;latchSequence=sequence;return true;}
        if(latched&&active&&sequence==latchSequence){value=latch;return true;}
        latched=false;latchSequence=-1;return false;
    }
}
