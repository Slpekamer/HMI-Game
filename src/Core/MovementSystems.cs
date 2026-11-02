using System;

namespace Hmi.Core
{
    internal sealed class MovementSystem : ISimulationSystem
    {
        public void Update(GameSimulation g)
        {
            var w = g.World;
            int player = g.PlayerId.Index;
            float dt = GameSimulation.FixedDelta;
            float speed = 4.8f * g.GetStat(Stat.PlayerSpeed) + (g.HasteTime > 0 ? .96f : 0);
            w.Transform[player].Position = Geometry.Move(g, g.PlayerPosition,
                g.MoveInput * (speed * dt), w.Transform[player].Radius, out _);
            if (g.MoveInput.LengthSquared > .001f)
                w.Transform[player].Facing = g.MoveInput.Normalized;
            for (int i = 0; i < w.Capacity; i++)
            {
                if (!w.Has(i, Components.Enemy))
                    continue;
                ref var enemy = ref w.Enemy[i];
                ref var transform = ref w.Transform[i];
                enemy.AttackCooldown = Math.Max(0, enemy.AttackCooldown - dt);
                enemy.OrbitCooldown = Math.Max(0, enemy.OrbitCooldown - dt);
                enemy.WallCooldown = Math.Max(0, enemy.WallCooldown - dt);
                if (enemy.KnockTime > 0)
                {
                    g.ObserveMotion(i, 1, true);
                    transform.Position = Geometry.Move(g, transform.Position, enemy.KnockVelocity * dt, transform.Radius, out int hit);
                    enemy.KnockTime -= dt;
                    enemy.KnockVelocity *= (float)Math.Exp(-2.3f * dt);
                    if (hit != 0 && enemy.WallCooldown <= 0)
                    {
                        if ((hit & 1) != 0)
                            enemy.KnockVelocity = new Vec2(-enemy.KnockVelocity.X * .75f, enemy.KnockVelocity.Y);
                        if ((hit & 2) != 0)
                            enemy.KnockVelocity = new Vec2(enemy.KnockVelocity.X, -enemy.KnockVelocity.Y * .75f);
                        enemy.Bounces++;
                        enemy.WallCooldown = .08f;
                        if (enemy.Bounces >= 3)
                        {
                            enemy.KnockTime = 0;
                            enemy.KnockVelocity = default;
                        }
                        g.Observe(CombatObservationKind.WallBounce, w.Id(i), w.Id(i), transform.Position, transform.Position, enemy.Bounces);
                    }
                    continue;
                }
                float slow = 1;
                float honeyRadius = g.GetStat(Stat.HoneyRadius);
                if (g.GetStat(Stat.HoneyEnabled) > 0
                    && (transform.Position - g.PlayerPosition).LengthSquared <= honeyRadius * honeyRadius)
                    slow = g.GetStat(Stat.HoneySlow);
                g.ObserveMotion(i, slow, false);
                Vec2 direction = Geometry.ChaseDirection(g, transform.Position, g.PlayerPosition, transform.Radius);
                transform.Facing = direction;
                float enemySpeed = (1 + i % 3 * .12f) * g.EffectiveEnemySpeedMultiplier * slow;
                transform.Position = Geometry.Move(g, transform.Position, direction * (enemySpeed * dt), transform.Radius, out _);
            }
        }
    }
    internal sealed class CompanionSystem : ISimulationSystem
    {
        public void Update(GameSimulation g)
        {
            var w = g.World;
            float dt = GameSimulation.FixedDelta;
            for (int i = 0; i < w.Capacity; i++)
            {
                if (!w.Has(i, Components.Companion))
                    continue;
                ref var companion = ref w.Companion[i];
                ref var transform = ref w.Transform[i];
                companion.Cooldown -= dt;
                companion.BuffTime = Math.Max(0, companion.BuffTime - dt);
                float range = companion.Support ? 3.04f : 5;
                float leash = companion.Support ? 5 : 5.6f;
                int target = Geometry.NearestEnemy(g, transform.Position, range + leash,
                    true, null, 0, companion.Anchor, leash + range);
                if (!g.Stationary)
                {
                    Vec2 destination = companion.Anchor;
                    if (target >= 0)
                    {
                        destination = w.Transform[target].Position;
                        Vec2 offset = destination - companion.Anchor;
                        if (offset.Length > leash)
                            destination = companion.Anchor + offset.Normalized * leash;
                        if ((destination - transform.Position).Length < range * .68f)
                            destination = transform.Position;
                    }
                    Vec2 delta = destination - transform.Position;
                    if (delta.Length > .1f)
                    {
                        transform.Facing = delta.Normalized;
                        transform.Position = Geometry.Move(g, transform.Position,
                            delta.Normalized * (dt * (companion.Support ? 1.28f : 1.68f)), transform.Radius, out _);
                    }
                }
                if (companion.Cooldown > 0)
                    continue;
                if (companion.Support)
                {
                    bool used = false;
                    if ((g.PlayerPosition - transform.Position).LengthSquared <= range * range)
                    {
                        ref var health = ref w.Health[g.PlayerId.Index];
                        float before = health.Current;
                        health.Current = Math.Min(health.Maximum, health.Current + 2);
                        g.Observe(CombatObservationKind.SupportHealApplied, w.Id(i), g.PlayerId, transform.Position, g.PlayerPosition, health.Current - before);
                        used = true;
                    }
                    for (int j = 0; j < w.Capacity; j++)
                    {
                        if (j == i || !w.Has(j, Components.Companion)
                            || (w.Transform[j].Position - transform.Position).LengthSquared > range * range)
                            continue;
                        w.Companion[j].BuffTime = 1.5f;
                        g.Observe(CombatObservationKind.SupportBuffApplied, w.Id(i), w.Id(j), transform.Position, w.Transform[j].Position, 1.5f);
                        used = true;
                    }
                    if (used)
                        companion.Cooldown = 1.8f;
                }
                else
                {
                    target = Geometry.NearestEnemy(g, transform.Position, range);
                    if (target < 0)
                        continue;
                    g.Emit(EventKind.Ray, w.Id(i), w.Id(target), transform.Position, w.Transform[target].Position, 2.7f, FeedbackMechanism.CompanionShot);
                    g.Damage(target, companion.BuffTime > 0 ? 3.2f : 2.7f, w.Id(i), FeedbackMechanism.CompanionShot);
                    companion.Cooldown = .95f;
                }
            }
        }
    }
    internal sealed class ContactSystem : ISimulationSystem
    {
        public void Update(GameSimulation g)
        {
            var w = g.World;
            for (int i = 0; i < w.Capacity; i++)
            {
                if (!w.Has(i, Components.Enemy) || w.Enemy[i].AttackCooldown > 0
                    || (w.Transform[i].Position - g.PlayerPosition).LengthSquared >= 1.44f)
                    continue;
                w.Health[g.PlayerId.Index].Current = Math.Max(0, w.Health[g.PlayerId.Index].Current - 7);
                w.Enemy[i].AttackCooldown = .7f;
                g.Emit(EventKind.Damage, w.Id(i), g.PlayerId, w.Transform[i].Position, g.PlayerPosition, 7, FeedbackMechanism.EnemyContact);
                Vec2 away = (w.Transform[i].Position - g.PlayerPosition).Normalized;
                w.Transform[i].Position = Geometry.Move(g, w.Transform[i].Position, away * .6f, w.Transform[i].Radius, out _);
            }
        }
    }
    internal sealed class PickupSystem : ISimulationSystem
    {
        public void Update(GameSimulation g)
        {
            var w = g.World;
            for (int i = 0; i < w.Capacity; i++)
            {
                if (!w.Has(i, Components.Pickup))
                    continue;
                ref var pickup = ref w.Pickup[i];
                pickup.Cooldown = Math.Max(0, pickup.Cooldown - GameSimulation.FixedDelta);
                if (pickup.Cooldown > 0 || (w.Transform[i].Position - g.PlayerPosition).LengthSquared >= 1.0816f)
                    continue;
                pickup.Cooldown = 21;
                if (pickup.Type == 0)
                    g.PowerTime = 8;
                else
                    g.HasteTime = 8;
                g.Emit(EventKind.Pickup, g.PlayerId, w.Id(i), g.PlayerPosition, w.Transform[i].Position, pickup.Type);
            }
        }
    }
    internal sealed class CleanupSpawnSystem : ISimulationSystem
    {
        public void Update(GameSimulation g)
        {
            var w = g.World;
            for (int i = 0; i < w.Capacity; i++)
            if (w.PendingDeath[i])
                w.Destroy(i);
            g.SpawnReservations.ActivateDue(g);
            if(g.Tick>=WaveRules.DurationTicks || (w.Alive(g.PlayerId) && w.Health[g.PlayerId.Index].Current<=0)) return;
            g.SpawnTimer -= GameSimulation.FixedDelta;
            if (g.SpawnTimer > 0)
                return;
            g.SpawnTimer = Math.Max(.3f, 1.4f - Math.Min(g.Wave, 8) * .14f);
            int enemies = 0;
            for (int i = 0; i < w.Capacity; i++)
            if (w.Has(i, Components.Enemy))
                enemies++;
            if (enemies + g.PendingSpawnCount < Math.Min(g.Limits.EnemyCapacity, 14 + Math.Min(g.Wave, 1024) * 4))
                g.SpawnEnemy();
            else
                g.SpawnReservations.RejectSoftCapacity(g);
        }
    }
}
