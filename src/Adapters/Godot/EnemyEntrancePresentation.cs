using System;
using Hmi.Core;
namespace Hmi.GodotAdapter;

/// <summary>Tick-bound visible arrival squash; no hidden enemy, moved feet, or authority grace period.</summary>
internal sealed class EnemyEntrancePresentation
{
    private readonly EntityId[] _ids;
    private readonly long[] _ticks;
    private readonly int[] _runs,_waves;
    public const int DurationTicks=10;
    public EnemyEntrancePresentation(int capacity)
    {
        if(capacity<=0)throw new ArgumentOutOfRangeException(nameof(capacity));
        _ids=new EntityId[capacity];_ticks=new long[capacity];_runs=new int[capacity];_waves=new int[capacity];
    }
    public void Observe(GameSimulation simulation)
    {
        for(int i=0;i<simulation.SpawnTransitionCount;i++)
        {
            var e=simulation.GetSpawnTransition(i);
            int slot=e.Entity.Index;
            if(e.Kind!=SpawnTransitionKind.Activated||slot<0||slot>=_ids.Length||
                e.Reservation.Id.RunToken!=simulation.RunToken||e.Reservation.Id.Wave!=simulation.Wave||
                !simulation.TryGetEntity(e.Entity,out var view)||view.Kind!=EntityKind.Enemy)continue;
            _ids[slot]=e.Entity;_ticks[slot]=e.Tick;_runs[slot]=simulation.RunToken;_waves[slot]=simulation.Wave;
        }
    }
    public float VerticalScale(EntityId entity,int run,int wave,long tick)
    {
        int slot=entity.Index;
        if(slot<0||slot>=_ids.Length||_ids[slot]!=entity||_runs[slot]!=run||_waves[slot]!=wave||tick<_ticks[slot])return 1;
        float t=Math.Clamp((float)(tick-_ticks[slot])/DurationTicks,0,1);
        // Fully opaque and at least90% high immediately. Feet remain at the same root position.
        return .9f+.1f*t;
    }
    public void Clear(){Array.Clear(_ids);Array.Clear(_ticks);Array.Clear(_runs);Array.Clear(_waves);}
}
