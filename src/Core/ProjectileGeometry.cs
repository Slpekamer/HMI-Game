using System;

namespace Hmi.Core
{
    internal static class ProjectileGeometry
    {
        internal static bool Circle(Vec2 a, Vec2 b, Vec2 enemyStart, Vec2 enemyEnd, float radius, out float time)
        {
            double x = a.X - enemyStart.X, y = a.Y - enemyStart.Y;
            double dx = b.X - a.X - (enemyEnd.X - enemyStart.X);
            double dy = b.Y - a.Y - (enemyEnd.Y - enemyStart.Y);
            double c = x * x + y * y - (double)radius * radius;
            time = 0;
            if (c <= 0) return true;
            double aa = dx * dx + dy * dy;
            if (aa <= 1e-20) return false;
            double bb = 2 * (x * dx + y * dy);
            double discriminant = bb * bb - 4 * aa * c;
            if (discriminant < 0) return false;
            double t = (-bb - Math.Sqrt(discriminant)) / (2 * aa);
            if (t < 0 || t > 1) return false;
            time = (float)t;
            return true;
        }
        private static bool Slab(double start, double delta, double min, double max, ref double enter, ref double exit)
        {
            if (Math.Abs(delta) < 1e-20) return start >= min && start <= max;
            double a = (min - start) / delta, b = (max - start) / delta;
            if (a > b) { double swap = a; a = b; b = swap; }
            enter = Math.Max(enter, a); exit = Math.Min(exit, b);
            return enter <= exit;
        }
        internal static bool Rectangle(Vec2 a, Vec2 b, Wall wall, float radius, out float time)
        {
            double enter = 0, exit = 1;
            bool hit = Slab(a.X, b.X - a.X, wall.X - radius, wall.X + wall.Width + radius, ref enter, ref exit)
                && Slab(a.Y, b.Y - a.Y, wall.Y - radius, wall.Y + wall.Height + radius, ref enter, ref exit);
            time = (float)enter;
            return hit;
        }
        internal static bool OriginBlocked(GameSimulation g, Vec2 p)
        {
            float r = ProjectileRules.Radius;
            if (p.X <= -14 + r || p.X >= 14 - r || p.Y <= -8 + r || p.Y >= 8 - r) return true;
            for (int i = 0; i < g.Walls.Count; i++)
            {
                var wall = g.Walls[i];
                if (p.X >= wall.X - r && p.X <= wall.X + wall.Width + r
                    && p.Y >= wall.Y - r && p.Y <= wall.Y + wall.Height + r) return true;
            }
            return false;
        }
        internal static bool WallTime(GameSimulation g, Vec2 a, Vec2 b, out float time)
        {
            time = float.PositiveInfinity;
            if (OriginBlocked(g, a)) { time = 0; return true; }
            float r = ProjectileRules.Radius;
            if (b.X <= -14 + r) time = Math.Min(time, (-14 + r - a.X) / (b.X - a.X));
            if (b.X >= 14 - r) time = Math.Min(time, (14 - r - a.X) / (b.X - a.X));
            if (b.Y <= -8 + r) time = Math.Min(time, (-8 + r - a.Y) / (b.Y - a.Y));
            if (b.Y >= 8 - r) time = Math.Min(time, (8 - r - a.Y) / (b.Y - a.Y));
            for (int i = 0; i < g.Walls.Count; i++)
                if (Rectangle(a, b, g.Walls[i], r, out float t)) time = Math.Min(time, t);
            return !float.IsPositiveInfinity(time);
        }
    }
}
