using System;

namespace Hmi.Core
{
    internal static class Geometry
    {
        internal static bool Blocked(GameSimulation g, Vec2 p, float radius)
        {
            if (p.X < -14 + radius || p.X > 14 - radius || p.Y < -8 + radius || p.Y > 8 - radius)
                return true;
            for (int i = 0; i < g.Walls.Count; i++)
            {
                var w = g.Walls[i];
                if (p.X > w.X - radius && p.X < w.X + w.Width + radius
                    && p.Y > w.Y - radius && p.Y < w.Y + w.Height + radius)
                    return true;
            }
            return false;
        }
        internal static bool LineClear(GameSimulation g, Vec2 a, Vec2 b, float radius = .08f)
        {
            Vec2 delta = b - a;
            int steps = Math.Max(1, (int)Math.Ceiling(delta.Length / .16f));
            for (int i = 1; i <= steps; i++)
                if (Blocked(g, a + delta * ((float)i / steps), radius))
                    return false;
            return true;
        }
        internal static Vec2 Move(GameSimulation g, Vec2 position, Vec2 delta, float radius, out int collision)
        {
            collision = 0;
            int steps = Math.Min(256, Math.Max(1, (int)Math.Ceiling(delta.Length / .12f)));
            Vec2 step = delta / steps;
            for (int i = 0; i < steps; i++)
            {
                Vec2 next = new Vec2(position.X + step.X, position.Y);
                if (!Blocked(g, next, radius))
                    position = next;
                else
                    collision |= 1;
                next = new Vec2(position.X, position.Y + step.Y);
                if (!Blocked(g, next, radius))
                    position = next;
                else
                    collision |= 2;
            }
            return position;
        }
        internal static Vec2 ChaseDirection(GameSimulation g, Vec2 position, Vec2 target, float radius)
        {
            if (LineClear(g, position, target, radius))
                return (target - position).Normalized;
            for (int i = 0; i < g.Walls.Count; i++)
            {
                var wall = g.Walls[i];
                if ((position.X < wall.X && target.X > wall.X)
                    || (position.X > wall.X + wall.Width && target.X < wall.X + wall.Width))
                {
                    float bottom = wall.Y - radius - .22f;
                    float top = wall.Y + wall.Height + radius + .22f;
                    float route = Math.Abs(position.Y - bottom) + Math.Abs(target.Y - bottom)
                        < Math.Abs(position.Y - top) + Math.Abs(target.Y - top) ? bottom : top;
                    if (Math.Abs(position.Y - route) > .15f)
                        return (new Vec2(position.X, route) - position).Normalized;
                }
            }
            return (target - position).Normalized;
        }
        internal static int NearestEnemy(GameSimulation g, Vec2 origin, float range, bool los = true,
            int[]? excluded = null, int excludedCount = 0, Vec2? anchor = null, float leash = 0)
        {
            int best = -1;
            float nearest = range * range;
            for (int i = 0; i < g.Capacity; i++)
            {
                if (!g.World.Has(i, Components.Enemy | Components.Health))
                    continue;
                bool skip = false;
                for (int j = 0; j < excludedCount; j++)
                if (excluded![j] == i)
                {
                    skip = true;
                    break;
                }
                if (skip)
                    continue;
                Vec2 point = g.World.Transform[i].Position;
                float distance = (point - origin).LengthSquared;
                if (distance > nearest || (distance == nearest && best >= 0))
                    continue;
                if (anchor.HasValue && ((point - anchor.Value).LengthSquared > leash * leash
                    || !LineClear(g, anchor.Value, point, .35f)))
                    continue;
                if (los && !LineClear(g, origin, point))
                    continue;
                best = i;
                nearest = distance;
            }
            return best;
        }
    }
}
