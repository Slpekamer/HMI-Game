namespace Hmi.Core
{
    internal static class EntityLifecycleSystem
    {
        internal static void SpawnEnemy(GameSimulation g)
        {
            int enemies = 0;
            for (int i = 0; i < g.Capacity; i++)
                if (g.World.Has(i, Components.Enemy)) enemies++;
            if (enemies >= g.Limits.EnemyCapacity || g.EntityCount >= g.Capacity || !g.World.HasFreeSlot) return;
            uint random = g.Random();
            int edge = g.SpawnCounter++ % 4;
            float fraction = random % 10000 / 10000f;
            float x, y;
            if (edge < 2)
            {
                x = edge == 0 ? -12.8f : 12.8f;
                y = -6.5f + fraction * 13;
            }
            else
            {
                x = -12.5f + fraction * 25;
                y = edge == 2 ? -6.8f : 6.8f;
            }
            g.World.Create(Components.Enemy | Components.Health, new Vec2(x, y), .44f,
                "enemy_chaser", 8 * g.EffectiveEnemyHealthMultiplier);
        }
        internal static void RecallCompanions(GameSimulation g)
        {
            int count = 0;
            for (int i = 0; i < g.Capacity; i++)
            {
                if (!g.World.Has(i, Components.Companion))
                    continue;
                Vec2 destination = g.PlayerPosition + new Vec2(count % 2 == 0 ? -1.5f : 1.5f, 1.6f + count / 2);
                destination = Geometry.Move(g, g.PlayerPosition, destination - g.PlayerPosition, .35f, out _);
                g.World.Transform[i].Position = destination;
                g.World.Companion[i].Anchor = destination;
                g.World.Companion[i].Cooldown = .2f;
                g.World.Companion[i].BuffTime = 0;
                count++;
            }
        }
    }
    internal static class DamageResolutionSystem
    {
        internal static void Apply(GameSimulation g, int target, float amount, EntityId source)
        {
            var w = g.World;
            if (g.Phase != RunPhase.Combat || !w.Has(target, Components.Enemy | Components.Health))
                return;
            amount *= 1 + (g.PowerTime > 0 ? .25f : 0);
            w.Health[target].Current -= amount;
            g.Emit(EventKind.Damage, source, w.Id(target), w.Alive(source)
                ? w.Transform[source.Index].Position : w.Transform[target].Position, w.Transform[target].Position, amount);
            if (w.Health[target].Current <= 0)
            {
                // Mark before awarding. Later damage in this tick cannot pay for the same life twice.
                w.PendingDeath[target] = true;
                RunProgressionSystem.RewardDeath(g, target, source);
            }
        }
    }
}
