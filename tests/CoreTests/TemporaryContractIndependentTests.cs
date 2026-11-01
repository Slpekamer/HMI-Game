using System;
using System.IO;
using System.Text.Json;
using Hmi.Core;

// Reconstructed independent tests after 19:27 environment reset; old pass is not reused.
internal static class TemporaryContractIndependentTests
{
    private static GameContent Load() => JsonSerializer.Deserialize<GameContent>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"gameplay.json")),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
    private static void Near(Action<bool,string> check,float actual,float expected,string label) => check(Math.Abs(actual-expected)<.0001f,"independent D2: "+label+" actual="+actual);
    private static void Rank(GameSimulation g,string id,int rank=1) => g.Loadout.Levels[g.Catalog.CardIndex[id]]=rank;
    private static int Item(GameContent c,string id) => Array.FindIndex(c.Items,x=>x.Id==id);
    private static int Contract(GameContent c,string stat,bool negative=false) => Array.FindIndex(c.TemporaryContracts,x=>Array.Exists(x.Effects,e=>e.Stat==stat && (negative?e.PermanentFraction<0:e.PermanentFraction>0)));
    private static GameSimulation Start(GameContent c,string hero="orbit") { var g=new GameSimulation(c);g.StartRun(hero,1030);return g; }
    private static void Reject(Action<bool,string> check,Action<GameContent> change,string label)
    {
        var c=Load();change(c);bool rejected=false;
        try { _=new GameSimulation(c); } catch(ArgumentException) { rejected=true; }
        check(rejected,"independent D2 rejects "+label);
    }
    public static void Run(Action<bool,string> check)
    {
        var c=Load();var g=Start(c);int rate=Contract(c,"BasicRate");
        check(rate>=0 && c.TemporaryContracts.Length==6,"D2 six concrete data contracts");
        Rank(g,"base.speed",5);g.Wallet.ActiveItems[Item(c,"tonic")]=1;
        g.Run.ActiveContract=rate;StatEffectSystem.Recalculate(g);
        Near(check,g.GetStat(Stat.BasicRate),1.95f,"permanent1.5 plus tonic.3 plus contract.15, not1.98");
        StatEffectSystem.Recalculate(g);Near(check,g.GetStat(Stat.BasicRate),1.95f,"recalculate cannot compound");
        RunProgressionSystem.FinishWave(g);
        Near(check,g.GetStat(Stat.BasicRate),1.5f,"wave end removes both tonic and contract");
        check(g.ActiveContractId==null && g.PendingContractId==null,"wave end clears contract IDs");
        g.Loadout.CardPicked=true;g.Enqueue(GameCommand.NextWave());g.Step();
        Near(check,g.GetStat(Stat.BasicRate),1.5f,"unselected next wave does not reapply contract");

        g=Start(c,"crit");Rank(g,"crit.skirmish");Rank(g,"risk.fleet",2);
        int shoes=Item(c,"shoes");g.Wallet.OwnedItems[shoes]=g.Wallet.ActiveItems[shoes]=2;
        int slow=Contract(c,"PlayerSpeed",true);g.Run.ActiveContract=slow;StatEffectSystem.Recalculate(g);
        float p=(1+.06f+.16f)*1.4f;
        Near(check,g.GetStat(Stat.PlayerSpeed),p*.9f,"risk speed multiplier applies exactly once to permanent baseline");
        float reach=g.GetStat(Stat.BasicRange);StatEffectSystem.Recalculate(g);
        Near(check,g.GetStat(Stat.BasicRange),reach,"risk reach delta idempotent");
        g.StartRun("honey",22);
        check(g.ActiveContractId==null && g.PendingContractId==null,"new run clears old contract");
        Near(check,g.GetStat(Stat.PlayerSpeed),1,"new run clears item/card/contract speed");

        g=Start(c);Rank(g,"risk.glass");g.Wallet.ActiveItems[Item(c,"shield")]=1;
        g.Run.ActiveContract=Contract(c,"MaxHealth",true);StatEffectSystem.Recalculate(g);
        Near(check,g.GetStat(Stat.MaxHealth),71.5f,"permanent65 shield81.25 minus9.75 not shield-scaled penalty");
        g.World.Health[g.PlayerId.Index].Current=20;RunProgressionSystem.FinishWave(g);
        Near(check,g.World.Health[g.PlayerId.Index].Current,20,"expiration does not heal wounded player");
        Near(check,g.World.Health[g.PlayerId.Index].Maximum,65,"expiration synchronizes actual health maximum");
        g.Loadout.CardPicked=true;g.Enqueue(GameCommand.NextWave());g.Step();
        Near(check,g.GetStat(Stat.MaxHealth),65,"next wave cannot resurrect old shield or contract");

        int scout=Contract(c,"HoneyRadius");g=Start(c);
        check(!TemporaryContractSystem.IsEligible(g,scout),"orbit alone cannot claim disabled mechanism scouting");
        g=Start(c,"honey");
        check(TemporaryContractSystem.IsEligible(g,scout),"enabled honey receives actual scouting benefit");
        float ric=g.GetStat(Stat.RicochetRange),impact=g.GetStat(Stat.ImpactRange);
        g.Run.ActiveContract=scout;StatEffectSystem.Recalculate(g);
        Near(check,g.GetStat(Stat.HoneyRadius),4.12f*1.1f,"scouting applies enabled honey range");
        check(g.GetStat(Stat.RicochetRange)==ric && g.GetStat(Stat.ImpactRange)==impact,"scouting leaves disabled mechanisms unchanged");

        Reject(check,x=>x.TemporaryContracts[0].Name="","empty contract name");
        Reject(check,x=>x.TemporaryContracts[0].Description="","empty contract description");
        Reject(check,x=>x.TemporaryContracts[0].Effects[0].Stat="1","numeric enum alias");
        Reject(check,x=>x.TemporaryContracts[0].Effects[0].PermanentFraction=float.NaN,"nonfinite fraction");
        Reject(check,x=>x.TemporaryContracts[0].Id=x.Cards[0].Id,"permanent card ID collision");
        Reject(check,x=>x.TemporaryContracts[0].Effects[0].Stat="KillReward","unsupported reward domain");
        Reject(check,x=>{int i=Contract(x,"HoneyRadius");foreach(var e in x.TemporaryContracts[i].Effects)if(e.Stat=="HoneyRadius")e.RequiredEnabledStat=null;},"missing enabled-mechanism guard");
    }
}
