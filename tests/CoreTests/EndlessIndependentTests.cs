using System;
using System.IO;
using System.Text.Json;
using Hmi.Core;

internal static class EndlessIndependentTests
{
    private static GameContent Load() => JsonSerializer.Deserialize<GameContent>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "legacy-gameplay.json")), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    private static void Near(Action<bool,string> check,float a,float b,string label) => check(Math.Abs(a-b)<.0001f,"independent endless: "+label+" actual="+a);
    private static GameSimulation Victory(GameContent content)
    {
        var g=new GameSimulation(content); g.StartRun("orbit",1030);
        g.Run.Wave=6; RunProgressionSystem.FinishWave(g); // explicit test-only state setup, not player survival.
        return g;
    }
    private static void Continue(GameSimulation g)
    {
        g.Enqueue(new GameCommand(CommandKind.ContinueEndless));g.Step();
    }
    private static void SaturateTraits(GameSimulation g)
    {
        for(int pass=0;pass<g.Catalog.Cards.Length;pass++)
            for(int i=0;i<g.Catalog.Cards.Length;i++)
                if(g.Catalog.Cards[i].Slot=="trait" && g.Catalog.Graph.IsEligible(i,g.Loadout.Levels))
                    g.Loadout.Levels[i]++;
        StatEffectSystem.Recalculate(g);
    }
    public static void Run(Action<bool,string> check)
    {
        var data=Load(); var g=Victory(data);
        check(g.FiniteCompleted&&!g.IsEndless&&g.Phase==RunPhase.Victory,"D finite completion remains explicit decision");
        int token=g.RunToken; g.Wallet.Earned=g.Wallet.Coins=17;
        g.Enqueue(new GameCommand(CommandKind.ContinueEndless));
        g.Enqueue(new GameCommand(CommandKind.ContinueEndless));g.Step();
        check(g.IsEndless&&g.Phase==RunPhase.Between&&g.Wave==6&&g.RunToken==token,"D repeated continue enters one menu in same run");
        check(g.Coins==17&&g.Wallet.Earned==17&&g.Wallet.Spent==0,"D continue grants no duplicated coins");
        g.Enqueue(new GameCommand(CommandKind.FinishRun));g.Step();
        check(g.Phase==RunPhase.Finished&&g.TerminationReason==RunTerminationReason.FinishedByPlayer,"D explicit finish terminal");
        g.Enqueue(new GameCommand(CommandKind.ContinueEndless));g.Step();
        check(g.Phase==RunPhase.Finished&&!g.CardPicked,"D closed run cannot resume from old continue");

        // One permanent health item plus one wave item distinguishes supply's actual baseline.
        data=Load(); var items=new ItemDefinition[data.Items.Length+2];Array.Copy(data.Items,items,data.Items.Length);
        int permanent=items.Length-2,temporary=items.Length-1;
        items[permanent]=new ItemDefinition{Id="qa.health.run",Price=1,Cap=1,Lifetime="run",Effects=new[]{new EffectDefinition{Stat="MaxHealth",Value=20}}};
        items[temporary]=new ItemDefinition{Id="qa.health.wave",Price=1,Cap=1,Lifetime="wave",Effects=new[]{new EffectDefinition{Stat="MaxHealth",Value=50}}};
        data.Items=items;g=Victory(data);SaturateTraits(g);Continue(g);
        var slots=g.GetOfferSlots();
        check(slots[0].Slot=="trait"&&slots[0].Kind==OfferKind.NextWaveSupply&&slots[0].Enabled,"D saturated trait offers named supply");
        g.Wallet.OwnedItems[permanent]=g.Wallet.OwnedItems[temporary]=1;
        g.Enqueue(GameCommand.Choose(GameSimulation.NextWaveSupplyId));g.Enqueue(GameCommand.Choose(GameSimulation.NextWaveSupplyId));g.Step();
        check(g.PendingNextWaveSupply&&g.CardPicked&&g.ActiveSupplyHealth==0,"D duplicated supply is pending once, no immediate heal");
        g.Enqueue(GameCommand.NextWave());g.Enqueue(GameCommand.NextWave());g.Step();
        check(g.Wave==7&&!g.PendingNextWaveSupply,"D duplicate Next does not initialize twice");
        Near(check,g.ActiveSupplyHealth,12,"10 percent excludes wave item and supply itself");
        Near(check,g.GetStat(Stat.MaxHealth),182,"wave health plus permanent health plus one supply");
        g.World.Health[g.PlayerId.Index].Current=5;
        RunProgressionSystem.FinishWave(g);
        Near(check,g.World.Health[g.PlayerId.Index].Current,5,"expiration never heals wounded hero");
        Near(check,g.World.Health[g.PlayerId.Index].Maximum,120,"expiration removes wave health and supply");
        check(g.ActiveSupplyHealth==0&&g.EndlessWavesCompleted==1,"supply expires and endless count advances once");
        g.Enqueue(GameCommand.Choose(GameSimulation.NextWaveSupplyId));g.Step();g.Enqueue(GameCommand.NextWave());g.Step();
        Near(check,g.ActiveSupplyHealth,12,"second-wave supply does not compound");
        Near(check,g.GetStat(Stat.MaxHealth),132,"expired wave item does not return");
        g.World.Health[g.PlayerId.Index].Current=0;RunProgressionSystem.EndTick(g);
        check(g.Phase==RunPhase.Defeat&&g.ActiveSupplyHealth==0,"death expires supply without revival");

        g=Victory(Load());Continue(g);g.Run.Wave=100;
        check(g.GetCardAvailability("risk.bounty")==CardAvailability.RiskLimit,"capped enemy HP cannot offer reward without full risk");
        check(g.GetCardAvailability("risk.fleet")==CardAvailability.RiskLimit,"capped enemy speed cannot offer free player speed");
        g.Loadout.Offers[2]=g.Catalog.CardIndex["risk.bounty"];
        g.Enqueue(GameCommand.Choose("risk.bounty"));g.Step();
        check(g.GetLevel("risk.bounty")==0&&!g.CardPicked,"stale risk offer revalidated at consumption");
        g.Run.Wave=int.MaxValue;g.Loadout.CardPicked=true;
        g.Enqueue(GameCommand.NextWave());g.Step();
        check(g.Phase==RunPhase.Finished&&g.TerminationReason==RunTerminationReason.WaveLimit&&g.Wave==int.MaxValue,"wave boundary terminates before wrap");

        g=new GameSimulation(Load(),new SimulationLimits(32,1,1));g.StartRun("summon",1030);
        uint rng=g.Run.Random;int spawn=g.SpawnCounter,count=g.EntityCount;
        for(int i=0;i<50;i++)g.SpawnEnemy();
        check(g.Run.Random==rng&&g.SpawnCounter==spawn&&g.EntityCount==count,"rejected capped spawns do not consume RNG or IDs");
        int companions=0;for(int i=0;i<g.Capacity;i++)if(g.World.Has(i,Components.Companion))companions++;
        check(companions==1,"configured companion cap is actual entity cap");
        for(int i=0;i<WaveRules.SpawnPreviewTicks;i++)g.Step(); // Wait for rule2 marker activation before rewarding a kill.
        g.Wallet.Earned=g.Wallet.Coins=int.MaxValue;g.Wallet.Spent=0;
        for(int i=0;i<g.Capacity;i++)if(g.World.Has(i,Components.Enemy)){g.Damage(i,10000,g.PlayerId);break;}
        check(g.Phase==RunPhase.Finished&&g.TerminationReason==RunTerminationReason.LedgerLimit,"reward overflow terminates explicitly");
        check(g.Coins==int.MaxValue&&g.Wallet.Earned==int.MaxValue&&g.Wallet.Kills==0,"overflow transaction does not partly reward");
        g.Run.RunToken=int.MaxValue;int oldCoins=g.Coins;bool rejected=false;
        try{g.StartRun("honey",7);}catch(InvalidOperationException){rejected=true;}
        check(rejected&&g.Coins==oldCoins&&g.RunToken==int.MaxValue,"run-token exhaustion rejects before partial reset");
    }
}
