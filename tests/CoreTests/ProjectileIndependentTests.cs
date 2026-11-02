using System;
using System.IO;
using System.Text.Json;
using Hmi.Core;

// Reviewer-owned deterministic collision/lifecycle counterexamples. Controlled storage setup,
// not a natural-run/balance fixture; production stages are invoked without changing production.
internal static class ProjectileIndependentTests
{
    static GameSimulation New()
    {
        var c=JsonSerializer.Deserialize<GameContent>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"gameplay.json")),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
        var g=new GameSimulation(c,new SimulationLimits(512,256),new string('a',64));
        g.StartRun("crit",1030);g.SpawnReservations.ResetScope();g.Run.Tick=10;
        return g;
    }
    static EntityId Enemy(GameSimulation g,Vec2 p,float hp=20,float radius=.02f)
        =>g.World.Create(Components.Enemy|Components.Health,p,radius,"enemy_chaser",hp);
    static void Bullet(GameSimulation g,int slot=0,float remaining=5,int bounces=0)
    {
        g.Projectiles.Active[slot]=true;g.Projectiles.Generation[slot]++;g.Projectiles.Count++;
        g.Projectiles.Data[slot]=new ProjectileComponent{Owner=g.PlayerId,Run=g.RunToken,Wave=g.Wave,
            BirthTick=9,RemainingTicks=100,Kind=ProjectileKind.Ricochet,Position=new Vec2(0,0),Previous=new Vec2(0,0),
            Direction=new Vec2(1,0),Damage=3,LegRemaining=remaining,TotalRemaining=20,
            RemainingBounces=bounces,BounceRange=4};
    }
    static int Impacts(GameSimulation g)
    {int n=0;for(int i=0;i<g.ProjectileTransitionCount;i++)if(g.GetProjectileTransition(i).Kind==ProjectileTransitionKind.Impact)n++;return n;}
    internal static void Run(Action<bool,string> check)
    {
        // Range endpoint reached one third through this tick. Enemy crosses only later:
        // treating its full motion as simultaneous with the short segment fabricates a hit.
        var g=New();var e=Enemy(g,new Vec2(.1f,1));Bullet(g,remaining:.1f);
        g.Projectiles.CaptureStart(g);g.World.Transform[e.Index].Position=new Vec2(.1f,-1);g.Projectiles.CaptureEnd(g);
        g.Projectiles.Flight(g);
        check(g.World.Health[e.Index].Current==20&&Impacts(g)==0,"independent range clipping also clips enemy time, no late crossing hit");
        check(g.Projectiles.Count==0,"independent exhausted short leg terminates");
        // Full tick crossing genuinely intersects a stationary/slow bullet path.
        g=New();e=Enemy(g,new Vec2(.15f,1));Bullet(g);g.Projectiles.CaptureStart(g);
        g.World.Transform[e.Index].Position=new Vec2(.15f,-1);g.Projectiles.CaptureEnd(g);g.Projectiles.Flight(g);
        check(g.World.Health[e.Index].Current==17&&Impacts(g)==1,"independent relative sweep catches midtick crossing");
        // Newborn may not collide with a pre-birth crossing that has already ended.
        g=New();e=Enemy(g,new Vec2(.15f,1));Bullet(g);g.Projectiles.Data[0].BirthTick=g.Tick;
        g.Projectiles.CaptureStart(g);g.World.Transform[e.Index].Position=new Vec2(.15f,-1);g.Projectiles.CaptureEnd(g);g.Projectiles.Flight(g);
        check(g.World.Health[e.Index].Current==20&&Impacts(g)==0,"independent newborn does not rewind enemy motion");
        // CaptureEnd freezes movement endpoint; a later synthetic knock displacement must
        // not overwrite the agreed movement sweep model by reading mutable current position.
        g=New();e=Enemy(g,new Vec2(.15f,1));Bullet(g);g.Projectiles.CaptureStart(g);g.Projectiles.CaptureEnd(g);
        g.World.Transform[e.Index].Position=new Vec2(.15f,0);g.Projectiles.Flight(g);
        check(g.World.Health[e.Index].Current==20,"independent post-movement displacement is not retroactive old-bullet sweep");
        // Exact same-tick lethal contention: first stable slot kills, later slot ignores
        // pending-dead life; reward and damage cannot happen twice.
        g=New();e=Enemy(g,new Vec2(.2f,0),2);Bullet(g,0);Bullet(g,1);g.Projectiles.CaptureStart(g);g.Projectiles.CaptureEnd(g);
        int coins=g.Coins;g.Projectiles.Flight(g);
        check(Impacts(g)==1&&g.World.PendingDeath[e.Index],"independent two bullets lethal contention yields one actual victim impact");
        check(g.Coins==coins+1,"independent lethal contention rewards once");
        check(g.Projectiles.Active[1]&&!g.Projectiles.Active[0],"independent stable projectile slot order selects first lethal resolver");
        // Continuation is not recursively advanced; previous point is the contact, not
        // source center/old endpoint. Next tick moves at most one ordinary step.
        g=New();e=Enemy(g,new Vec2(.2f,0));var e2=Enemy(g,new Vec2(1,0));Bullet(g,bounces:1);
        g.Projectiles.CaptureStart(g);g.Projectiles.CaptureEnd(g);g.Projectiles.Flight(g);
        var p=g.Projectiles.Data[0];
        check(p.State==ProjectileState.ContinuationPending&&p.Position.X<.2f&&p.Previous.X==p.Position.X,"independent bounce pending pins both endpoints to real contact");
        check(g.World.Health[e2.Index].Current==20&&Impacts(g)==1,"independent no instant second hop");
        float x=p.Position.X;g.Run.Tick++;g.Projectiles.BeginStep();g.Projectiles.CaptureStart(g);g.Projectiles.CaptureEnd(g);g.Projectiles.Flight(g);
        p=g.Projectiles.Data[0];check(Math.Abs(p.Previous.X-x)<1e-6&&Math.Abs(p.Position.X-x-.3f)<1e-5,"independent next leg uses contact previous and ordinary travel");
        // A visited slot recycled to a new generation is not permanently immune.
        g=New();e=Enemy(g,new Vec2(.2f,0));Bullet(g);g.Projectiles.Data[0].VisitedCount=1;g.Projectiles.Visited[0]=e;
        g.World.Destroy(e.Index);var fresh=Enemy(g,new Vec2(.2f,0));g.Projectiles.CaptureStart(g);g.Projectiles.CaptureEnd(g);g.Projectiles.Flight(g);
        check(fresh.Index==e.Index&&fresh.Generation!=e.Generation&&g.World.Health[fresh.Index].Current==17,"independent visited fullID does not exempt reused slot");
        // Scope mismatch cancels without damage even with a target already intersecting.
        g=New();e=Enemy(g,new Vec2(.05f,0));Bullet(g);g.Projectiles.Data[0].Run--;
        g.Projectiles.CaptureStart(g);g.Projectiles.CaptureEnd(g);g.Projectiles.Flight(g);
        check(g.Projectiles.Count==0&&g.World.Health[e.Index].Current==20,"independent stale scope cannot damage new run");
        // Public-command phase legality: Combat FinishRun is rejected, not termination.
        g=New();Bullet(g);g.SpawnTimer=g.MeleeTimer=g.HoneyTimer=g.ImpactTimer=g.RicochetTimer=999;
        g.Enqueue(GameCommand.FinishRun());g.Step();
        check(g.Phase==RunPhase.Combat&&g.Projectiles.Count==1,"independent FinishRun rejects Combat without deleting valid flight");
        float pausedX=g.Projectiles.Data[0].Position.X;long pausedTick=g.Tick;
        g.Enqueue(GameCommand.Pause(true));g.Step();g.Step();
        check(g.Paused&&g.Tick==pausedTick&&g.Projectiles.Count==1&&g.Projectiles.Data[0].Position.X==pausedX,"independent public pause preserves frozen active projectile");
        g.Enqueue(GameCommand.Pause(false));g.Step();
        check(!g.Paused&&g.Projectiles.Data[0].Position.X>pausedX,"independent resume advances preserved projectile");
        g.Run.Wave=GameSimulation.TotalWaves;g.Projectiles.Data[0].Wave=g.Run.Wave;g.Run.Tick=WaveRules.DurationTicks-1;
        g.Step();
        check(g.Phase==RunPhase.Victory&&g.Projectiles.Count==0,"independent actual final wave Step cancels active projectile");
        g.Enqueue(GameCommand.FinishRun());g.Step();
        check(g.Phase==RunPhase.Finished&&g.Projectiles.Count==0,"independent legal Victory FinishRun stays empty");
    }
}
