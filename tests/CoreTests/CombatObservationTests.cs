using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Hmi.Core;

internal static class CombatObservationTests
{
    static GameSimulation New()
    {
        var content=JsonSerializer.Deserialize<GameContent>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"gameplay.json")),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
        var g=new GameSimulation(content,new SimulationLimits(512,256),new string('a',64));g.StartRun("bounce",1030);
        for(int i=0;i<g.Capacity;i++)if(i!=g.PlayerId.Index&&g.World.Mask[i]!=Components.None)g.World.Destroy(i);
        g.World.Transform[g.PlayerId.Index].Position=default; // Explicit controlled setup, not production spawn.
        g.SpawnTimer=999; g.SpawnReservations.ResetScope(); /* Controlled empty-arena fixture removes rule2 previews. */g.MeleeTimer=g.RicochetTimer=g.HoneyTimer=g.ImpactTimer=999;
        g.Stats[(int)Stat.OrbitEnabled]=g.Stats[(int)Stat.HoneyEnabled]=g.Stats[(int)Stat.RicochetEnabled]=g.Stats[(int)Stat.ImpactEnabled]=0;
        g.EventLength=0;g.Observations.ResetScope();return g;
    }
    static EntityId Enemy(GameSimulation g,float x=1,float y=0,float hp=100)=>g.World.Create(Components.Enemy|Components.Health,new Vec2(x,y),.44f,"enemy_chaser",hp);
    static EntityId Companion(GameSimulation g,bool support,float x=0,float y=0)
    {var id=g.World.Create(Components.Companion,new Vec2(x,y),.35f,support?"companion_support":"companion_shooter");g.World.Companion[id.Index].Support=support;g.World.Companion[id.Index].Anchor=new Vec2(x,y);return id;}
    static bool Has(GameSimulation g,EventKind kind,FeedbackMechanism mechanism)
    {for(int i=0;i<g.EventCount;i++)if(g.GetEvent(i).Kind==kind&&g.GetEvent(i).Mechanism==mechanism)return true;return false;}
    static CombatObservation Find(GameSimulation g,CombatObservationKind kind)
    {for(int i=0;i<g.ObservationCount;i++)if(g.GetObservation(i).Kind==kind)return g.GetObservation(i);throw new Exception("Missing observation "+kind);}
    static bool HasImpact(GameSimulation g,ProjectileKind kind)
    {for(int i=0;i<g.ProjectileTransitionCount;i++)if(g.GetProjectileTransition(i).Kind==ProjectileTransitionKind.Impact&&g.GetProjectileTransition(i).ProjectileKind==kind)return true;return false;}
    static void Fly(GameSimulation g)
    {
        // Isolated scheduled authority: no injected transitions or damage; static target fixture.
        for(int step=0;step<60&&g.ProjectileCount>0;step++)
        {g.Run.Tick++;g.Projectiles.CaptureStart(g);g.Projectiles.CaptureEnd(g);g.Projectiles.Flight(g);}
        if(g.ProjectileCount!=0)throw new Exception("Projectile observation fixture did not terminate");
    }
    public static void Run(Action<bool,string> check)
    {
        check(GameSimulation.CombatObservationSchemaVersion==1,"observation schema v1 independent from checkpoint");
        var legacy=new GameEvent(EventKind.Damage,EntityId.None,EntityId.None,default,default,1,2,3,4);
        check(legacy.Mechanism==FeedbackMechanism.Unknown&&legacy.Value==1&&legacy.Tick==2,"old constructor retains fields and Unknown");
        var g=New();var e=Enemy(g);g.MeleeTimer=0;new BasicMeleeSystem().Update(g);
        check(Has(g,EventKind.Melee,FeedbackMechanism.BasicMelee)&&Has(g,EventKind.Damage,FeedbackMechanism.BasicMelee),"basic real attack and damage mechanism");
        g=New();g.Stats[(int)Stat.OrbitEnabled]=1;g.Stats[(int)Stat.OrbitCount]=1;g.Stats[(int)Stat.OrbitRadius]=2;Enemy(g,2,0);new OrbitSystem().Update(g);
        check(Has(g,EventKind.Damage,FeedbackMechanism.Orbit),"orbit actual damage tag");
        g=New();g.Stats[(int)Stat.RicochetEnabled]=1;g.RicochetTimer=0;Enemy(g);new RicochetSystem().Update(g);
        g.Projectiles.Commit(g);
        check(g.ProjectileTransitionCount==1&&g.GetProjectileTransition(0).Kind==ProjectileTransitionKind.Launched
            &&g.GetProjectileTransition(0).ProjectileKind==ProjectileKind.Ricochet&&!Has(g,EventKind.Ray,FeedbackMechanism.Ricochet)
            &&!Has(g,EventKind.Damage,FeedbackMechanism.Ricochet),"ricochet launch has typed transition, no fake Ray or pre-hit damage");
        Fly(g);
        check(HasImpact(g,ProjectileKind.Ricochet)&&Has(g,EventKind.Damage,FeedbackMechanism.Ricochet),"actual ricochet impact retains damage attribution");
        g=New();g.Stats[(int)Stat.HoneyEnabled]=1;g.HoneyTimer=0;Enemy(g);new HoneySystem().Update(g);
        check(Has(g,EventKind.Damage,FeedbackMechanism.Honey),"honey actual pulse damage tag");
        g=New();Companion(g,false);Enemy(g);new CompanionSystem().Update(g);
        g.Projectiles.Commit(g);
        check(g.ProjectileTransitionCount==1&&g.GetProjectileTransition(0).Kind==ProjectileTransitionKind.Launched
            &&g.GetProjectileTransition(0).ProjectileKind==ProjectileKind.CompanionShot&&!Has(g,EventKind.Ray,FeedbackMechanism.CompanionShot)
            &&!Has(g,EventKind.Damage,FeedbackMechanism.CompanionShot),"companion launch has no instant damage or synthetic Ray");
        Fly(g);
        check(HasImpact(g,ProjectileKind.CompanionShot)&&Has(g,EventKind.Damage,FeedbackMechanism.CompanionShot),"actual companion impact retains damage attribution");
        g=New();g.Stats[(int)Stat.ImpactEnabled]=1;g.ImpactTimer=0;Enemy(g);new ImpactSystem().Update(g);
        check(Has(g,EventKind.Impact,FeedbackMechanism.Impact)&&Has(g,EventKind.Damage,FeedbackMechanism.Impact),"impact actual cone and damage tag");
        g=New();g.Stats[(int)Stat.ImpactEnabled]=1;e=Enemy(g);Enemy(g,1.1f);g.World.Enemy[e.Index].KnockTime=1;new ImpactSystem().Update(g);
        check(Has(g,EventKind.Blast,FeedbackMechanism.ImpactBlast)&&Has(g,EventKind.Damage,FeedbackMechanism.ImpactBlast),"collision blast actual terminal damage tag");
        g=New();Enemy(g,.1f);float hp=g.World.Health[g.PlayerId.Index].Current;new ContactSystem().Update(g);
        check(Has(g,EventKind.Damage,FeedbackMechanism.EnemyContact)&&g.World.Health[g.PlayerId.Index].Current==hp-7,"contact tag does not change seven damage");
        g=New();e=Enemy(g,hp:1);g.Damage(e.Index,2,g.PlayerId);check(Has(g,EventKind.Damage,FeedbackMechanism.Unknown)&&Has(g,EventKind.Death,FeedbackMechanism.Unknown),"legacy direct damage and death stay Unknown");int n=g.EventCount;g.Damage(e.Index,2,g.PlayerId,FeedbackMechanism.Honey);check(g.EventCount==n,"pending-death damage does not invent observation");
        g=New();g.Stats[(int)Stat.HoneyEnabled]=g.Stats[(int)Stat.OrbitEnabled]=1;g.Stats[(int)Stat.OrbitRadius]=1;g.Stats[(int)Stat.OrbitCount]=1;g.HoneyTimer=0;Enemy(g);new OrbitSystem().Update(g);new HoneySystem().Update(g);
        check(Has(g,EventKind.Damage,FeedbackMechanism.Orbit)&&Has(g,EventKind.Damage,FeedbackMechanism.Honey),"same-tick overlapping mechanisms distinguish by explicit tag");
        g=New();var support=Companion(g,true);var recipient=Companion(g,false,.4f);g.World.Health[g.PlayerId.Index].Current=99;new CompanionSystem().Update(g);
        var heal=Find(g,CombatObservationKind.SupportHealApplied);var buff=Find(g,CombatObservationKind.SupportBuffApplied);
        check(heal.Value==1&&g.World.Health[g.PlayerId.Index].Current==100&&heal.Source==support&&heal.Target==g.PlayerId,"support heal records actual clamped delta and full IDs");
        check(buff.Value==1.5f&&buff.Source==support&&buff.Target==recipient&&g.World.Companion[recipient.Index].BuffTime>1.48f,"support buff records original duration refresh");
        check(g.World.Companion[support.Index].Cooldown==1.8f,"support original cooldown unchanged");
        g.EventLength=0;g.Observations.BeginStep();g.World.Companion[support.Index].Cooldown=0;new CompanionSystem().Update(g);
        check(Find(g,CombatObservationKind.SupportHealApplied).Value==0&&g.World.Health[g.PlayerId.Index].Current==100,"full HP support emits zero not fictional healing");
        g=New();Companion(g,true,10,5);new CompanionSystem().Update(g);check(g.ObservationCount==0,"support out of range does not invent cast");
        g=New();support=Companion(g,true);recipient=Companion(g,false,.4f);g.World.Health[g.PlayerId.Index].Current=90;
        for(int i=0;i<512;i++)g.Observe(CombatObservationKind.WallBounce,EntityId.None,EntityId.None,default,default,0);
        new CompanionSystem().Update(g);
        check(g.ObservationCount==512&&g.DroppedObservations==2&&g.World.Health[g.PlayerId.Index].Current==92&&g.World.Companion[support.Index].Cooldown==1.8f&&g.World.Companion[recipient.Index].BuffTime>1.48f,"full observation buffer never skips support recipient mutation or cooldown");
        g=New();e=Enemy(g,13.4f,0);g.World.Enemy[e.Index].KnockTime=1;g.World.Enemy[e.Index].KnockVelocity=new Vec2(20,0);new MovementSystem().Update(g);
        var bounce=Find(g,CombatObservationKind.WallBounce);check(bounce.Source==e&&bounce.Target==e&&bounce.Value==1&&g.World.Enemy[e.Index].Bounces==1,"only real wall collision reports original ordinal");
        check(bounce.To.X==g.World.Transform[e.Index].Position.X&&bounce.To.Y==g.World.Transform[e.Index].Position.Y,"bounce uses committed wall position");
        g.Observations.BeginStep();g.World.Transform[e.Index].Position=new Vec2(13.4f,0);g.World.Enemy[e.Index].KnockVelocity=new Vec2(20,0);g.World.Enemy[e.Index].WallCooldown=.08f;new MovementSystem().Update(g);check(g.ObservationCount==0,"wall cooldown suppresses observation just like reflection");
        g=New();e=Enemy(g,13.4f,0);g.World.Enemy[e.Index].Bounces=2;g.World.Enemy[e.Index].KnockTime=1;g.World.Enemy[e.Index].KnockVelocity=new Vec2(20,0);new MovementSystem().Update(g);
        check(Find(g,CombatObservationKind.WallBounce).Value==3&&g.World.Enemy[e.Index].KnockTime==0&&g.World.Enemy[e.Index].KnockVelocity.LengthSquared==0,"third wall observation preserves original stop cap");
        g=New();g.Stats[(int)Stat.HoneyEnabled]=1;g.Stats[(int)Stat.HoneySlow]=.65f;e=Enemy(g);check(!g.TryGetEnemyMotionObservation(e,out _),"no pre-movement slow sample");new MovementSystem().Update(g);
        check(g.TryGetEnemyMotionObservation(e,out var motion)&&motion.HoneyMultiplier==.65f&&!motion.UsedKnockMovement,"sample uses exact chase slow multiplier");
        g.World.Enemy[e.Index].KnockTime=1;check(g.TryGetEnemyMotionObservation(e,out motion)&&!motion.UsedKnockMovement,"later same-tick impact does not rewrite historical movement sample");
        new MovementSystem().Update(g);check(g.TryGetEnemyMotionObservation(e,out motion)&&motion.HoneyMultiplier==1&&motion.UsedKnockMovement,"actual knock branch bypass records one multiplier");
        g.Enqueue(GameCommand.Pause(true));g.Step();check(g.TryGetEnemyMotionObservation(e,out _)&&g.ObservationCount==0,"pause keeps last committed sample but clears discrete notifications");
        g.Run.Tick++;check(!g.TryGetEnemyMotionObservation(e,out _),"sample from earlier unadvanced movement tick rejected");g.Run.Tick--;
        g.World.Destroy(e.Index);var replacement=Enemy(g);check(!g.TryGetEnemyMotionObservation(e,out _)&&!g.TryGetEnemyMotionObservation(replacement,out _),"full generation and dead IDs rejected");
        g.Observations.Dropped=long.MaxValue;for(int i=0;i<513;i++)g.Observe(CombatObservationKind.WallBounce,EntityId.None,EntityId.None,default,default,i);
        check(g.ObservationCount==512&&g.DroppedObservations==long.MaxValue,"independent notifications bounded with saturating drop diagnostic");
        int events=g.EventCount;g.Emit(EventKind.Melee,g.PlayerId,replacement,default,default,7,FeedbackMechanism.BasicMelee);check(g.EventCount==events+1,"full new observation buffer does not consume legacy slots");
        g.Observations.ResetScope();for(int i=0;i<2049;i++)g.Emit(EventKind.Melee,g.PlayerId,replacement,default,default,1);g.Observe(CombatObservationKind.WallBounce,replacement,replacement,default,default,1);
        check(g.EventCount==2048&&g.ObservationCount==1,"full legacy event buffer does not consume independent observation slots");
        g.PrepareRun("honey",1040);check(g.ObservationCount==0&&g.DroppedObservations==0&&!g.TryGetEnemyMotionObservation(replacement,out _),"prepare resets output scope and diagnostics");
        var identity=new CheckpointIdentity(Guid.NewGuid(),1);check(g.TryCapturePrepared(identity,out var dto,out _)&&dto!=null,"new prepared checkpoint still capture schema1");
        g.Observe(CombatObservationKind.SupportHealApplied,EntityId.None,EntityId.None,default,default,2);check(g.TryRestorePrepared(dto!,identity,out _)&&g.ObservationCount==0&&g.DroppedObservations==0,"restore clears observations without replay");
        check(g.BeginPreparedWave(g.PreparedToken)&&g.ObservationCount==0,"begin starts empty observation scope");
        g=New();support=Companion(g,true);int observed=0;long priorTick=g.Tick;
        for(int step=0;step<8;step++)
        {
            g.World.Companion[support.Index].Cooldown=0;g.Step();
            check(g.ObservationCount==1&&g.GetObservation(0).Tick==priorTick+1&&g.GetObservation(0).RunToken==g.RunToken,"each catchup Step has fresh indexed same-scope observation");
            observed+=g.ObservationCount;priorTick=g.Tick;
        }
        check(observed==8&&g.ObservationCount==1,"catchup consumes each buffer, last buffer is not backlog");
        long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<10000;i++){g.Observations.BeginStep();g.Observe(CombatObservationKind.WallBounce,g.PlayerId,g.PlayerId,default,default,1);_ = g.GetObservation(0);}
        check(GC.GetAllocatedBytesForCurrentThread()==before,"observation append/read/reset allocates zero after initialization");
        bool rejected=false;try{g.GetObservation(-1);}catch(ArgumentOutOfRangeException){rejected=true;}check(rejected,"invalid observation index rejected");
    }
}
