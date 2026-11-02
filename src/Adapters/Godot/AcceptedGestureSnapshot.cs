using Hmi.Core;
namespace Hmi.GodotAdapter;
/// <summary>Read-only actual presenter acceptance. Active is sampled from the same native graph.</summary>
public readonly struct AcceptedGestureSnapshot
{
    public readonly EntityId Source;
    public readonly int RunToken,Wave;
    public readonly long Sequence;
    public readonly GameEvent? Event;
    public readonly bool Active;
    public AcceptedGestureSnapshot(EntityId source,int run,int wave,long sequence,GameEvent? value,bool active)
    { Source=source;RunToken=run;Wave=wave;Sequence=sequence;Event=value;Active=active; }
}
