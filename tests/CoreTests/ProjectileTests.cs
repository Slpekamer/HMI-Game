using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Hmi.Core;

internal static class ProjectileTests
{
    private static GameContent Content() => JsonSerializer.Deserialize<GameContent>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "gameplay.json")), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    private static GameSimulation New()
    {
        var g = new GameSimulation(Content(), new SimulationLimits(), new string('a', 64));
        g.StartRun("crit", 1030); g.SpawnReservations.ResetScope(); g.SpawnTimer = 999;
        g.MeleeTimer = 999; g.World.Transform[g.PlayerId.Index].Position = default;
        return g;
    }
    private static EntityId Enemy(GameSimulation g, float x, float y = 0, float hp = 100, float radius = .44f)
        => g.World.Create(Components.Enemy | Components.Health, new Vec2(x, y), radius, "enemy_chaser", hp);
    private static void Launch(GameSimulation g, EntityId target, int bounces = 0, float range = 4.8f)
    {
        g.Projectiles.Request(g, 0, ProjectileKind.Ricochet, g.PlayerId, target, g.PlayerPosition, 3, bounces, range);
        g.Projectiles.Commit(g);
    }
    // Isolated fixed-tick projectile schedule: enemies deliberately stationary, no unrelated AI/attacks.
    private static void Advance(GameSimulation g)
    {
        g.Run.Tick++; g.Projectiles.BeginStep(); g.Projectiles.CaptureStart(g); g.Projectiles.CaptureEnd(g); g.Projectiles.Flight(g);
    }
    internal static void Run(Action<bool, string> check)
    {
        void C(bool condition, string label) => check(condition, "projectile: " + label);
        void Near(float a, float b, string label) => C(Math.Abs(a - b) < .0001f, label + " actual=" + a);
        var g = New(); var target = Enemy(g, 2);
        Launch(g, target); C(g.ProjectileCount == 1, "launch creates authority entity"); Near(g.World.Health[target.Index].Current, 100, "launch alone never damages");
        C(g.TryGetProjectileAtSlot(0, out var view) && view.Owner == g.PlayerId && view.RawDamage == 3, "detached value view");
        var oldId = view.Id; int oldRun = g.RunToken;
        Advance(g); Near(g.World.Health[target.Index].Current, 100, "no damage before actual path contact"); Near(g.Projectiles.Data[0].Position.X, .3f, "18m/s fixed step");
        for (int i = 0; i < 4; i++) Advance(g);
        Near(g.World.Health[target.Index].Current, 97, "swept real contact damages once"); C(g.ProjectileCount == 0, "terminal first shot removed");
        Launch(g, target); C(g.TryGetProjectileAtSlot(0, out var reused) && reused.Id.Generation > oldId.Generation, "generation advances on pool reuse");
        C(!g.TryGetProjectile(oldId, oldRun, 1, out _), "stale projectile id rejected");
        g.PrepareRun("crit", 1030); C(g.ProjectileCount == 0 && !g.TryGetProjectile(reused.Id, oldRun, 1, out _), "new scope removes old bullet");

        g = New(); target = Enemy(g, .2f); Launch(g, target); g.Projectiles.CaptureStart(g); g.Projectiles.CaptureEnd(g); g.Projectiles.Flight(g);
        Near(g.World.Health[target.Index].Current, 97, "birth overlap resolves t0 without invented animation delay");
        g = New(); target = Enemy(g, 4); var intercept = Enemy(g, 1); Launch(g, target);
        for (int i = 0; i < 3; i++) Advance(g);
        Near(g.World.Health[intercept.Index].Current, 97, "unselected enemy intercepts"); Near(g.World.Health[target.Index].Current, 100, "selected enemy not guaranteed hit");
        g = New(); target = Enemy(g, 2); Launch(g, target); g.World.Destroy(target.Index); var replacement = Enemy(g, 2, 2);
        C(replacement.Index == target.Index && replacement.Generation != target.Generation, "target reuse fixture");
        for (int i = 0; i < 40; i++) Advance(g);
        Near(g.World.Health[replacement.Index].Current, 100, "dead aim does not retarget reused actor elsewhere"); C(g.ProjectileCount == 0, "miss expires by finite distance");

        g = New(); target = Enemy(g, 7); Launch(g, target);
        for (int i = 0; i < 30; i++) Advance(g);
        Near(g.World.Health[target.Index].Current, 100, "wall absorbs before enemy"); C(g.ProjectileCount == 0, "wall impact terminates");
        g = New(); target = Enemy(g, 5); g.World.Transform[g.PlayerId.Index].Position = new Vec2(6.52f, 0); float timer = g.RicochetTimer;
        Launch(g, target); C(g.ProjectileCount == 0 && g.Projectiles.GetTransition(0).Reason == ProjectileEndReason.BlockedOrigin, "inside expanded wall rejects birth"); Near(g.RicochetTimer, timer, "blocked shot consumes no cooldown");

        g = New(); target = Enemy(g, 1); var second = Enemy(g, 2); Launch(g, target, 1);
        Advance(g); Advance(g);
        Near(g.World.Health[target.Index].Current, 97, "first hop raw3"); Near(g.World.Health[second.Index].Current, 100, "no instantaneous next hop");
        C(g.TryGetProjectileAtSlot(0, out view) && view.State == ProjectileState.ContinuationPending && view.HitsCompleted == 1, "next leg armed");
        Near((view.PreviousPosition - view.Position).Length, 0, "new segment starts at contact, no cross-leg previous");
        for (int i = 0; i < 4; i++) Advance(g);
        Near(g.World.Health[second.Index].Current, 98.5f, "next real contact halves damage"); Near(g.World.Health[target.Index].Current, 97, "visited life never damaged twice");
        g = New(); target = Enemy(g, 1); second = Enemy(g, 3); Launch(g, target, 4, .5f);
        Advance(g); Advance(g); C(g.ProjectileCount == 0, "no next target in finite search range terminates");

        g = New(); target = Enemy(g, 2); Launch(g, target); g.PowerTime = 1;
        for (int i = 0; i < 5; i++) Advance(g);
        Near(g.World.Health[target.Index].Current, 96.25f, "power applied once at impact");
        g = New(); target = Enemy(g, 2); g.PowerTime = 1; Launch(g, target); g.PowerTime = 0;
        for (int i = 0; i < 5; i++) Advance(g);
        Near(g.World.Health[target.Index].Current, 97, "launch power not frozen or double multiplied");
        g = New(); target = Enemy(g, 2); Launch(g, target); g.Projectiles.Data[0].RemainingTicks = 1; Advance(g);
        C(g.ProjectileCount == 0, "bounded lifetime cancels long miss"); Near(g.World.Health[target.Index].Current, 100, "expiry is not damage");

        g = New(); target = Enemy(g, 2); Launch(g, target); uint rng = g.Run.Random;
        g.Enqueue(GameCommand.Pause(true)); g.Step(); C(g.TryGetProjectileAtSlot(0, out var beforePause), "paused projectile stays live");
        for (int i = 0; i < 8; i++) g.Step(); g.TryGetProjectileAtSlot(0, out var afterPause);
        C(beforePause.Position.X == afterPause.Position.X && beforePause.RemainingTicks == afterPause.RemainingTicks && g.Run.Random == rng, "pause freezes authority and RNG");
        g.Enqueue(GameCommand.Pause(false)); g.Step(); C(g.ProjectileCount > 0, "resume progresses normally");
        g.Run.Tick = WaveRules.DurationTicks - 1; g.Step(); C(g.Phase == RunPhase.Between && g.ProjectileCount == 0, "wave end cancels bullets immediately");

        g = New(); target = Enemy(g, 4); Launch(g, target);
        g.Enqueue(GameCommand.FinishRun()); g.Step();
        C(g.Phase == RunPhase.Combat && g.ProjectileCount > 0, "existing FinishRun command remains ineligible during combat");
        // Defensive fixture retains a projectile in an externally forced eligible terminal phase.
        g.Run.Phase = RunPhase.Victory; g.Enqueue(GameCommand.FinishRun()); g.Step();
        C(g.Phase == RunPhase.Finished && g.ProjectileCount == 0, "noncombat command early return cancels retained projectile");

        g = New(); target = Enemy(g, 2); rng = g.Run.Random; int actorCount = g.EntityCount;
        for (int i = 0; i < ProjectileRules.Capacity; i++) Launch(g, target);
        C(g.ProjectileCount == 32 && g.EntityCount == actorCount, "independent pool does not steal actor slots");
        g.RicochetTimer = 0; Launch(g, target); C(g.ProjectileCount == 32 && g.RicochetTimer == 0 && g.Run.Random == rng, "full pool no cooldown/RNG/eviction");
        var actors = new EntityId[5]; actors[0] = g.PlayerId;
        for (int lane = 1; lane < 5; lane++)
        {
            actors[lane] = g.World.Create(Components.Companion, default, .35f, "fixture_shooter");
            g.World.Companion[actors[lane].Index].SpawnOrdinal = lane - 1;
        }
        var served = new bool[5]; g.Projectiles.NextLane = 0;
        for (int trial = 0; trial < 5; trial++)
        {
            g.Projectiles.Active[0] = false; g.Projectiles.Count--; g.Projectiles.BeginStep();
            for (int lane = 0; lane < 5; lane++) g.Projectiles.Request(g, lane, lane == 0 ? ProjectileKind.Ricochet : ProjectileKind.CompanionShot,
                actors[lane], target, default, lane == 0 ? 3 : 2.7f, 0, lane == 0 ? 4.8f : 0);
            g.Projectiles.Commit(g);
            for (int i = 0; i < g.ProjectileTransitionCount; i++)
            {
                var transition = g.GetProjectileTransition(i);
                if (transition.Kind != ProjectileTransitionKind.Launched) continue;
                for (int lane = 0; lane < 5; lane++) if (transition.Owner == actors[lane]) served[lane] = true;
            }
        }
        for (int lane = 0; lane < 5; lane++) C(served[lane], "five persistent ready lanes served fairly " + lane);

        g = New(); g.PrepareRun("crit", 1030); var identity = new CheckpointIdentity(Guid.NewGuid(), 1);
        C(g.TryCapturePrepared(identity, out var checkpoint, out _), "rule3 fresh Prepared checkpoint valid");
        g.Projectiles.Requests[0] = new ProjectileLaunchIntent { Present = true };
        C(!g.TryCapturePrepared(identity, out _, out var pendingError) && pendingError == CheckpointError.InvalidTemporaryState,
            "Prepared capture explicitly rejects pending projectile authority");
        g.Projectiles.Requests[0] = default;
        C(checkpoint!.RulesVersion == "hmi-core-wave60-preview1-projectiles-3", "rules identity explicitly bumped");
        checkpoint.RulesVersion = WaveRules.PriorVersion;
        C(!g.TryRestorePrepared(checkpoint, identity, out var error) && error == CheckpointError.RulesMismatch, "rule2 never silently replayed under projectile rules");
        C(!g.TryConvertLegacyPreparedCheckpoint(checkpoint, identity, new CheckpointIdentity(Guid.NewGuid(), 1), out _, out error), "P1 does not automatically migrate old rules");
        AuthorityBoundaries(C);
        NaturalFlight(C);
        GeometryCases(C);
        Performance(C);
    }
    private static void AuthorityBoundaries(Action<bool, string> check)
    {
        var a = New(); var b = New(); var ea = Enemy(a, .2f, hp: 1); var eb = Enemy(b, .2f, hp: 1);
        Launch(a, ea); Launch(b, eb);
        a.Projectiles.CaptureStart(a); a.Projectiles.CaptureEnd(a); b.Projectiles.CaptureStart(b); b.Projectiles.CaptureEnd(b);
        b.Projectiles.TransitionCount = 256; // Saturate only cosmetic channel, not authority.
        a.Projectiles.Flight(a); b.Projectiles.Flight(b);
        check(a.Coins == b.Coins && a.Kills == b.Kills && a.ProjectileCount == b.ProjectileCount && b.DroppedProjectileTransitions > 0,
            "transition overflow cannot skip damage, reward or projectile removal");
        var g = New(); var target = Enemy(g, 2); var shooter = g.World.Create(Components.Companion, default, .35f, "fixture");
        g.World.Companion[shooter.Index].SpawnOrdinal = 0;
        g.Projectiles.Request(g, 1, ProjectileKind.CompanionShot, shooter, target, default, 3.2f);
        g.Projectiles.Commit(g); uint rng = g.Run.Random;
        g.World.Destroy(shooter.Index); var reused = g.World.Create(Components.Pickup, new Vec2(10, 5), .3f, "fixture");
        check(reused.Index == shooter.Index && reused.Generation != shooter.Generation, "owner reuse controlled fixture");
        for (int i = 0; i < 5; i++) Advance(g);
        check(Math.Abs(g.World.Health[target.Index].Current - 96.8f) < .0001f && g.Run.Random == rng,
            "already-fired shooter persists after owner removal with captured buff and no RNG");
        bool attributed = false;
        for (int i = 0; i < g.ProjectileTransitionCount; i++)
            if (g.GetProjectileTransition(i).Kind == ProjectileTransitionKind.Impact) attributed = g.GetProjectileTransition(i).Owner == shooter;
        check(attributed, "impact retains old full owner ID, not replacement life");
        g = New(); target = Enemy(g, 2); shooter = g.World.Create(Components.Companion, default, .35f, "fixture");
        g.World.Companion[shooter.Index].SpawnOrdinal = 0;
        g.Projectiles.Request(g, 1, ProjectileKind.CompanionShot, shooter, target, default, 2.7f); g.Projectiles.Commit(g);
        g.RicochetTimer = 999; g.Enqueue(GameCommand.Recall()); g.Step();
        check(g.ProjectileCount == 1 && g.TryGetProjectileAtSlot(0, out var flight) && flight.Owner == shooter
            && Math.Abs(flight.Position.X - .3f) < .0001f, "real Recall moves source but never teleports or cancels emitted shot");
        g = New(); target = Enemy(g, 2);
        g.Projectiles.Request(g, 0, ProjectileKind.Ricochet, g.PlayerId, target, default, 3, 1, 4.8f);
        g.Stats[(int)Stat.RicochetEnabled] = 0; g.Projectiles.Commit(g);
        check(g.ProjectileCount == 0 && g.RicochetTimer == 0, "disabled source rejects already-pending request without cooldown");
        g = New(); target = Enemy(g, 2); shooter = g.World.Create(Components.Companion, default, .35f, "fixture");
        g.World.Companion[shooter.Index].SpawnOrdinal = 0;
        g.Projectiles.Request(g, 1, ProjectileKind.CompanionShot, shooter, target, default, 2.7f);
        g.World.Destroy(shooter.Index); reused = g.World.Create(Components.Companion, default, .35f, "fixture");
        g.Projectiles.Commit(g);
        check(g.ProjectileCount == 0 && g.World.Companion[reused.Index].Cooldown == 0, "pending request cannot transfer to a new owner generation");
        g = New(); target = Enemy(g, 2); g.Projectiles.Generation[0] = int.MaxValue;
        Launch(g, target); check(!g.Projectiles.Active[0] && g.Projectiles.Generation[0] == int.MaxValue && g.Projectiles.Active[1],
            "exhausted generation retired, never wraps or aliases");
    }
    private static void NaturalFlight(Action<bool, string> check)
    {
        // Actual configured run: no injected actors, damage, events, timer changes or forced wave end.
        var g = new GameSimulation(Content()); g.StartRun("crit", 1030);
        ProjectileId tracked = ProjectileId.None; EntityId intended = EntityId.None;
        long launchTick = -1; float launchHp = 0; int coinsAtLaunch = 0;
        for (int tick = 0; tick < 600 && g.Phase == RunPhase.Combat; tick++)
        {
            g.Step();
            for (int i = 0; i < g.ProjectileTransitionCount; i++)
            {
                var t = g.GetProjectileTransition(i);
                if (tracked == ProjectileId.None && t.Kind == ProjectileTransitionKind.Launched && t.ProjectileKind == ProjectileKind.Ricochet)
                {
                    tracked = t.Id; intended = t.Target; launchTick = t.Tick; coinsAtLaunch = g.Coins;
                    check(g.TryGetEntity(intended, out var target), "natural launched target still live"); launchHp = target.Health;
                }
                if (t.Id != tracked || t.Kind != ProjectileTransitionKind.Impact) continue;
                check(t.Tick > launchTick, "natural first actual hit occurs after visible flight ticks");
                check(t.Target == intended && g.TryGetEntity(intended, out _), "natural first projectile hit intended living target");
                g.TryGetEntity(intended, out var hitTarget);
                check(Math.Abs(launchHp - hitTarget.Health - t.AppliedDamageAmount) < .0001f, "natural HP only reflects actual impact amount");
                check(g.Coins == coinsAtLaunch, "natural nonlethal flight/hit pays no coins");
                Console.WriteLine($"PROJECTILE_NATURAL_PROOF hero=crit seed=1030 launch_tick={launchTick} impact_tick={t.Tick} launch_hp={launchHp} impact_hp={hitTarget.Health} applied={t.AppliedDamageAmount} coins_launch={coinsAtLaunch} coins_hit={g.Coins} no_fixture_injection=true");
                return;
            }
        }
        check(false, "natural first launch/impact proof completed within600ticks");
    }
    private static void GeometryCases(Action<bool, string> check)
    {
        check(ProjectileGeometry.Circle(new Vec2(0, 0), new Vec2(10, 0), new Vec2(5, 0), new Vec2(5, 0), .01f, out var t) && t > .49f && t < .51f, "fast path crosses tiny enemy");
        check(ProjectileGeometry.Circle(new Vec2(0, 0), new Vec2(1, 0), new Vec2(.5f, 1), new Vec2(.5f, -1), .08f, out _), "relative sweep detects crossing enemy");
        check(!ProjectileGeometry.Circle(default, default, new Vec2(2, 0), new Vec2(2, 0), .1f, out _), "zero relative velocity outside circle");
        check(ProjectileGeometry.Circle(default, default, default, default, .1f, out t) && t == 0, "zero velocity overlap t0");
        check(ProjectileGeometry.Rectangle(new Vec2(0, 1), new Vec2(2, 1), new Wall(1, 1, 1, 1), 0, out t) && t == .5f, "closed rectangle tangent");
    }
    private static void Performance(Action<bool, string> check)
    {
        var g = New();
        for (int i = 0; i < 256; i++) Enemy(g, -5 + i % 16 * .6f, -5 + i / 16 * .6f, 100000);
        var target = Enemy(g, 2, 5, 100000);
        for (int i = 0; i < 32; i++) Launch(g, target, 4);
        for (int i = 0; i < 30; i++) Advance(g);
        // Synthetic stationary authority load: replenish trajectories in preallocated storage,
        // never claim long natural gameplay or change shipped cooldowns.
        Action tick = () =>
        {
            for (int i = 0; i < 32; i++)
            {
                g.Projectiles.Active[i] = true; g.Projectiles.Remove[i] = false;
                g.Projectiles.Data[i] = new ProjectileComponent { Owner = g.PlayerId, Run = g.RunToken, Wave = g.Wave,
                    Position = new Vec2(0, 6.5f), Direction = new Vec2(1, 0), LegRemaining = 10, TotalRemaining = 10,
                    RemainingTicks = 720, Damage = 3, BirthTick = -1 };
            }
            g.Projectiles.Count = 32; Advance(g);
        };
        for (int i = 0; i < 30; i++) tick();
        var watch = Stopwatch.StartNew(); long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 300; i++) tick();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before; watch.Stop();
        check(allocated == 0, "warm projectile flight allocates zero managed bytes");
        Console.WriteLine($"PROJECTILE_PERF bullets=32 enemies=257 ticks=300 ms={watch.Elapsed.TotalMilliseconds:F2} allocated_bytes={allocated} synthetic=true");
    }
}
