using System;
using Hmi.Core;
namespace Hmi.GodotAdapter;

/// <summary>Immutable visual input captured from a real launch's first ballistic leg.</summary>
internal readonly struct LaunchAimSample
{
    public readonly EntityId Owner;
    public readonly ProjectileId Projectile;
    public readonly int RunToken, Wave;
    public readonly long Tick;
    public readonly Vec2 Direction;
    public readonly string Evidence;
    private LaunchAimSample(ProjectileTransition e, Vec2 direction, string evidence)
    { Owner=e.Owner; Projectile=e.Id; RunToken=e.RunToken; Wave=e.Wave; Tick=e.Tick; Direction=direction; Evidence=evidence; }
    internal static bool TryCapture(GameSimulation sim, ProjectileTransition e, out LaunchAimSample sample)
    {
        sample=default;
        if(e.Kind!=ProjectileTransitionKind.Launched || e.ProjectileKind!=ProjectileKind.Ricochet || e.Tick!=sim.Tick || e.RunToken!=sim.RunToken || e.Wave!=sim.Wave) return false;
        Vec2 direction=default; string evidence=null;
        if(sim.TryGetProjectile(e.Id,e.RunToken,e.Wave,out var p) && p.Owner==e.Owner && p.Kind==e.ProjectileKind && p.BirthTick==e.Tick && p.HitsCompleted==0)
        { direction=p.Direction; evidence="live-first-leg"; }
        else for(int i=0;i<sim.ProjectileTransitionCount;i++)
        {
            var hit=sim.GetProjectileTransition(i);
            if(hit.Kind==ProjectileTransitionKind.Impact && hit.Id==e.Id && hit.Owner==e.Owner && hit.ProjectileKind==e.ProjectileKind && hit.RunToken==e.RunToken && hit.Wave==e.Wave && hit.Tick==e.Tick && hit.HitsCompleted==1)
            { direction=hit.Position-hit.From; evidence="first-impact-leg"; break; }
        }
        float squared=direction.LengthSquared;
        if(evidence==null || !float.IsFinite(direction.X) || !float.IsFinite(direction.Y) || !float.IsFinite(squared) || squared<=1e-10f) return false;
        float scale=1/MathF.Sqrt(squared); sample=new LaunchAimSample(e,new Vec2(direction.X*scale,direction.Y*scale),evidence); return true;
    }
}
