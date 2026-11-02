namespace Hmi.Core
{
    internal static class EntityLifecycleSystem
    {
        internal static void SpawnEnemy(GameSimulation g)
        {
            g.SpawnReservations.Schedule(g);
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
        internal static void Apply(GameSimulation g, int target, float amount, EntityId source, FeedbackMechanism mechanism = FeedbackMechanism.Unknown)
        {
            var w = g.World;
            if (g.Phase != RunPhase.Combat || !w.Has(target, Components.Enemy | Components.Health))
                return;
            amount *= 1 + (g.PowerTime > 0 ? .25f : 0);
            w.Health[target].Current -= amount;
            g.Emit(EventKind.Damage, source, w.Id(target), w.Alive(source)
                ? w.Transform[source.Index].Position : w.Transform[target].Position, w.Transform[target].Position, amount, mechanism);
            if (w.Health[target].Current <= 0)
            {
                // Mark before awarding. Later damage in this tick cannot pay for the same life twice.
                w.PendingDeath[target] = true;
                RunProgressionSystem.RewardDeath(g, target, source);
            }
        }
    }
}
