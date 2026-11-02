using System;

namespace Hmi.Core
{
    internal sealed class BasicMeleeSystem : ISimulationSystem
    {
        public void Update(GameSimulation g)
        {
            g.MeleeTimer -= GameSimulation.FixedDelta;
            if (g.MeleeTimer > 0)
                return;
            int target = Geometry.NearestEnemy(g, g.PlayerPosition, g.GetStat(Stat.BasicRange));
            if (target < 0)
                return;
            g.MeleeTimer = .72f / g.GetStat(Stat.BasicRate);
            Vec2 destination = g.World.Transform[target].Position;
            g.World.Transform[g.PlayerId.Index].Facing = (destination - g.PlayerPosition).Normalized;
            g.Emit(EventKind.Melee, g.PlayerId, g.World.Id(target), g.PlayerPosition, destination, g.GetStat(Stat.BasicDamage), FeedbackMechanism.BasicMelee);
            g.Damage(target, g.GetStat(Stat.BasicDamage), g.PlayerId, FeedbackMechanism.BasicMelee);
        }
    }
    internal sealed class OrbitSystem : ISimulationSystem
    {
        public void Update(GameSimulation g)
        {
            if (g.GetStat(Stat.OrbitEnabled) <= 0)
                return;
            var w = g.World;
            int count = (int)g.GetStat(Stat.OrbitCount);
            float radius = g.GetStat(Stat.OrbitRadius);
            for (int i = 0; i < w.Capacity; i++)
            {
                if (!w.Has(i, Components.Enemy) || w.Enemy[i].OrbitCooldown > 0)
                    continue;
                for (int j = 0; j < count; j++)
                {
                    double angle = g.WaveSeconds * 2.1 + j * Math.PI * 2 / count;
                    Vec2 candy = g.PlayerPosition + new Vec2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius;
                    if ((w.Transform[i].Position - candy).LengthSquared >= 1
                        || !Geometry.LineClear(g, g.PlayerPosition, candy))
                        continue;
                    w.Enemy[i].OrbitCooldown = .3f;
                    g.Damage(i, 2.1f, g.PlayerId, FeedbackMechanism.Orbit);
                    break;
                }
            }
        }
    }
    internal sealed class RicochetSystem : ISimulationSystem
    {
        public void Update(GameSimulation g)
        {
            if (g.GetStat(Stat.RicochetEnabled) <= 0) return;
            g.RicochetTimer -= GameSimulation.FixedDelta;
            if (g.RicochetTimer > 0) return;
            int target = Geometry.NearestEnemy(g, g.PlayerPosition, ProjectileRules.RicochetInitialRange);
            if (target < 0) return;
            g.Projectiles.Request(g, 0, ProjectileKind.Ricochet, g.PlayerId, g.World.Id(target),
                g.PlayerPosition, 3, (int)g.GetStat(Stat.RicochetBounces), g.GetStat(Stat.RicochetRange));
        }
    }
    internal sealed class HoneySystem : ISimulationSystem
    {
        public void Update(GameSimulation g)
        {
            if (g.GetStat(Stat.HoneyEnabled) <= 0)
                return;
            g.HoneyTimer -= GameSimulation.FixedDelta;
            if (g.HoneyTimer > 0)
                return;
            g.HoneyTimer = .6f;
            float radius = g.GetStat(Stat.HoneyRadius);
            for (int i = 0; i < g.Capacity; i++)
            {
                if (g.World.Has(i, Components.Enemy)
                    && (g.World.Transform[i].Position - g.PlayerPosition).LengthSquared <= radius * radius
                    && Geometry.LineClear(g, g.PlayerPosition, g.World.Transform[i].Position))
                    g.Damage(i, 1.25f, g.PlayerId, FeedbackMechanism.Honey);
            }
        }
    }
    internal sealed class ImpactSystem : ISimulationSystem
    {
        private struct PairCooldown
        {
            internal EntityId A, B;
            internal int Run, Wave;
            internal long Until;
        }
        private readonly PairCooldown[] _pairs = new PairCooldown[256];
        private int _next;
        public void Update(GameSimulation g)
        {
            if (g.GetStat(Stat.ImpactEnabled) <= 0)
                return;
            g.ImpactTimer -= GameSimulation.FixedDelta;
            var w = g.World;
            if (g.ImpactTimer <= 0)
            {
                float range = g.GetStat(Stat.ImpactRange);
                int target = Geometry.NearestEnemy(g, g.PlayerPosition, range);
                if (target >= 0)
                {
                    g.ImpactTimer = 1.4f;
                    Vec2 forward = (w.Transform[target].Position - g.PlayerPosition).Normalized;
                    g.Emit(EventKind.Impact, g.PlayerId, w.Id(target), g.PlayerPosition, g.PlayerPosition + forward * range, range, FeedbackMechanism.Impact);
                    for (int i = 0; i < w.Capacity; i++)
                    {
                        if (!w.Has(i, Components.Enemy))
                            continue;
                        Vec2 delta = w.Transform[i].Position - g.PlayerPosition;
                        if (delta.LengthSquared > range * range || Vec2.Dot(delta.Normalized, forward) < .9009689f
                            || !Geometry.LineClear(g, g.PlayerPosition, w.Transform[i].Position))
                            continue;
                        g.Damage(i, 2.1f, g.PlayerId, FeedbackMechanism.Impact);
                        if (!w.Has(i, Components.Enemy))
                            continue;
                        ref var enemy = ref w.Enemy[i];
                        enemy.KnockVelocity = delta.Normalized * g.GetStat(Stat.ImpactForce);
                        enemy.KnockTime = .8f;
                        enemy.Bounces = 0;
                        enemy.BlastUsed = false;
                        enemy.WallCooldown = 0;
                    }
                }
            }
            for (int i = 0; i < w.Capacity; i++)
            {
                if (!w.Has(i, Components.Enemy) || w.Enemy[i].KnockTime <= 0 || w.Enemy[i].BlastUsed)
                    continue;
                for (int j = 0; j < w.Capacity; j++)
                {
                    if (i == j || !w.Has(j, Components.Enemy)
                        || (w.Transform[i].Position - w.Transform[j].Position).LengthSquared > 1.48f * 1.48f)
                        continue;
                    EntityId a = w.Id(Math.Min(i, j)), b = w.Id(Math.Max(i, j));
                    if (!ReservePair(g, a, b))
                        continue;
                    w.Enemy[i].BlastUsed = true;
                    Vec2 center = w.Transform[j].Position;
                    g.Emit(EventKind.Blast, w.Id(i), w.Id(j), center, center, 2.2f, FeedbackMechanism.ImpactBlast);
                    // Damage-only terminal effect: no secondary impulse, hence no recursive chain.
                    for (int k = 0; k < w.Capacity; k++)
                    {
                        if (w.Has(k, Components.Enemy) && (w.Transform[k].Position - center).LengthSquared <= 2.2f * 2.2f)
                            g.Damage(k, 1.8f, g.PlayerId, FeedbackMechanism.ImpactBlast);
                    }
                    break;
                }
            }
        }
        private bool ReservePair(GameSimulation g, EntityId a, EntityId b)
        {
            for (int i = 0; i < _pairs.Length; i++)
            {
                var p = _pairs[i];
                if (p.Run == g.RunToken && p.Wave == g.Wave && p.A == a && p.B == b && p.Until > g.Tick)
                    return false;
            }
            for (int i = 0; i < _pairs.Length; i++)
            {
                int slot = (_next + i) % _pairs.Length;
                var p = _pairs[slot];
                if (p.Run == g.RunToken && p.Wave == g.Wave && p.Until > g.Tick)
                    continue;
                _pairs[slot] = new PairCooldown { A = a, B = b, Run = g.RunToken, Wave = g.Wave, Until = g.Tick + 72 };
                _next = (slot + 1) % _pairs.Length;
                return true;
            }
            return false; // Saturation fails closed; never evict a live cooldown.
        }
    }
}
