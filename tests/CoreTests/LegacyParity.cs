using System;
using System.Text.Json;
using Hmi.Core;

internal static class LegacyParity
{
    internal static ulong Digest(GameContent content)
    {
        ulong hash = 14695981039346656037;
        void Add(long value) { unchecked { hash ^= (ulong)value; hash *= 1099511628211; } }
        void Text(string value) { foreach (char c in value) Add(c); Add(-1); }
        void Float(float value) => Add(BitConverter.SingleToInt32Bits(value));
        foreach (var hero in content.Characters)
        {
            var g = new GameSimulation(content);
            g.StartRun(hero.Id, 1030);
            for (int tick = 0; tick < 900; tick++)
            {
                g.Enqueue(GameCommand.Move((float)Math.Cos(tick * .02), (float)Math.Sin(tick * .02)));
                g.Step();
                Add(g.Tick); Add(g.Wave); Add((int)g.Phase); Add(g.Coins); Add(g.Kills);
                for (int slot = 0; slot < g.Capacity; slot++)
                {
                    if (!g.TryGetEntityAtSlot(slot, out var entity)) continue;
                    Add(slot); Add(entity.Id.Generation); Add((int)entity.Kind);
                    Float(entity.Position.X); Float(entity.Position.Y); Float(entity.Health); Float(entity.MaxHealth);
                    Float(entity.Facing.X); Float(entity.Facing.Y); Add(entity.Active ? 1 : 0);
                }
                // Rule3 golden includes projectile authority, not only actors/legacy cosmetic events.
                Add(g.Run.Random); Add(g.ProjectileCount);
                for (int slot = 0; slot < g.ProjectileCapacity; slot++)
                {
                    if (!g.TryGetProjectileAtSlot(slot, out var p)) continue;
                    Add(p.Id.Index); Add(p.Id.Generation); Add(p.RunToken); Add(p.Wave); Add(p.Owner.Index); Add(p.Owner.Generation);
                    Add((int)p.Kind); Add((int)p.State); Add(p.HitsCompleted); Add(p.RemainingTicks); Add(p.BirthTick);
                    Float(p.PreviousPosition.X); Float(p.PreviousPosition.Y); Float(p.Position.X); Float(p.Position.Y);
                    Float(p.Direction.X); Float(p.Direction.Y); Float(p.Radius); Float(p.RawDamage); Float(p.RemainingLegDistance); Float(p.RemainingTotalDistance);
                }
                for (int i = 0; i < g.ProjectileTransitionCount; i++)
                {
                    var p = g.GetProjectileTransition(i);
                    Add((int)p.Kind); Add((int)p.Reason); Add((int)p.ProjectileKind); Add(p.RunToken); Add(p.Wave); Add(p.HitsCompleted); Add(p.Id.Index); Add(p.Id.Generation); Add(p.Tick);
                    Add(p.Owner.Index); Add(p.Owner.Generation); Add(p.Target.Index); Add(p.Target.Generation);
                    Float(p.From.X); Float(p.From.Y); Float(p.Position.X); Float(p.Position.Y); Float(p.RawDamage); Float(p.AppliedDamageAmount);
                }
                for (int i = 0; i < g.EventCount; i++)
                {
                    var ev = g.GetEvent(i);
                    Add((int)ev.Kind); Add(ev.Source.Index); Add(ev.Target.Index); Float(ev.Value);
                }
            }
            // Menu/stock progression through five independent original choices; synthetic no-enemy fixture.
            g.StartRun(hero.Id, 1030);
            for (int wave = 1; wave < 6; wave++)
            {
                for (int i = 0; i < g.Capacity; i++) if (g.World.Has(i, Components.Enemy)) g.World.Destroy(i);
                g.SpawnTimer = 999; g.SpawnReservations.ResetScope(); /* Controlled empty-arena fixture removes rule2 previews. */
                while (g.Phase == RunPhase.Combat) g.Step();
                var offers = g.GetOffers();
                foreach (var offer in offers) Text(offer.Id);
                string chosen = offers[wave % 3].Id;
                g.Enqueue(GameCommand.Choose(chosen)); g.Step();
                foreach (var card in content.Cards) Add(g.GetLevel(card.Id));
                g.Enqueue(GameCommand.NextWave()); g.Step();
                Add(g.Wave); Add(g.Coins);
                foreach (Stat stat in Enum.GetValues<Stat>()) Float(g.GetStat(stat));
            }
        }
        return hash;
    }
}
