using System;
using System.IO;
using System.Text.Json;
using Hmi.Core;

internal static class TimedHealthIndependentTests
{
    private static GameContent Load() => JsonSerializer.Deserialize<GameContent>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"gameplay.json")),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
    private static void Near(Action<bool,string> check,float actual,float expected,string why) => check(Math.Abs(actual-expected)<.0001f,"timed health: "+why+" actual="+actual);
    private static GameSimulation Setup(GameContent c,int pending=-1)
    {
        int item=Array.FindIndex(c.Items,x=>x.Id=="shield");c.Items[item].Duration=1;
        var g=new GameSimulation(c);g.StartRun("orbit",1030);
        RunProgressionSystem.FinishWave(g);g.Wallet.OwnedItems[item]=1;g.Loadout.CardPicked=true;g.Run.PendingContract=pending;
        g.Enqueue(GameCommand.NextWave());g.Step();
        for(int i=0;i<g.Capacity;i++)if(g.World.Has(i,Components.Enemy))g.World.Destroy(i);
        g.SpawnTimer=1000;
        return g;
    }
    private static void Until(GameSimulation g,long tick) { while(g.Tick<tick)g.Step(); }
    public static void Run(Action<bool,string> check)
    {
        var c=Load();var g=Setup(c);
        Near(check,g.World.Health[g.PlayerId.Index].Maximum,125,"real wave start applies timed item");
        g.World.Health[g.PlayerId.Index].Current=120;Until(g,59);
        Near(check,g.World.Health[g.PlayerId.Index].Maximum,125,"before exact crossing retains maximum");
        g.Enqueue(GameCommand.Pause(true));g.Step();long pausedTick=g.Tick;
        for(int i=0;i<120;i++)g.Step();
        check(g.Tick==pausedTick && g.Paused,"pause freezes duration crossing");
        Near(check,g.World.Health[g.PlayerId.Index].Maximum,125,"paused item not expired");
        g.Enqueue(GameCommand.Pause(false));g.Step();
        Near(check,g.GetStat(Stat.MaxHealth),100,"exact crossing changes stat");
        Near(check,g.World.Health[g.PlayerId.Index].Maximum,100,"exact crossing updates entity limit");
        Near(check,g.World.Health[g.PlayerId.Index].Current,100,"over-limit current clamps downward");
        for(int i=0;i<5;i++)g.Step();
        Near(check,g.World.Health[g.PlayerId.Index].Current,100,"subsequent ticks do not reapply health effect");

        g=Setup(Load());g.World.Health[g.PlayerId.Index].Current=20;Until(g,60);
        Near(check,g.World.Health[g.PlayerId.Index].Current,20,"low current preserved on decreasing limit");
        Near(check,g.World.Health[g.PlayerId.Index].Maximum,100,"low current still gets new limit");

        c=Load();c.Items[Array.FindIndex(c.Items,x=>x.Id=="shield")].Effects[0].Value=.8f;
        g=Setup(c);Near(check,g.World.Health[g.PlayerId.Index].Maximum,80,"negative timed health item initialized");
        g.World.Health[g.PlayerId.Index].Current=30;Until(g,60);
        Near(check,g.World.Health[g.PlayerId.Index].Maximum,100,"expiry raises maximum when penalty expires");
        Near(check,g.World.Health[g.PlayerId.Index].Current,30,"raising limit never heals");

        c=Load();int protection=Array.FindIndex(c.TemporaryContracts,x=>x.Slot=="trait"&&Array.Exists(x.Effects,e=>e.Stat=="MaxHealth"&&e.PermanentFraction>0));
        g=Setup(c,protection);Near(check,g.World.Health[g.PlayerId.Index].Maximum,135,"timed shield plus permanent-based contract");
        g.World.Health[g.PlayerId.Index].Current=130;Until(g,60);
        Near(check,g.GetStat(Stat.MaxHealth),110,"item expiry retains contract delta");
        Near(check,g.World.Health[g.PlayerId.Index].Maximum,110,"entity maximum retains only contract");
        Near(check,g.World.Health[g.PlayerId.Index].Current,110,"current clamps to remaining contract limit");
        check(g.ActiveContractId==c.TemporaryContracts[protection].Id,"duration item cannot clear wave contract");
        g.World.Health[g.PlayerId.Index].Current=9;RunProgressionSystem.FinishWave(g);
        Near(check,g.World.Health[g.PlayerId.Index].Maximum,100,"wave completion later removes contract");
        Near(check,g.World.Health[g.PlayerId.Index].Current,9,"wave completion also does not heal");
    }
}
