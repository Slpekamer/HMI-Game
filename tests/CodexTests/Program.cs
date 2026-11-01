using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using Hmi.Core;
using Hmi.GodotAdapter;
static class Program
{
 private static int assertions;
 private static void Check(bool ok,string reason){assertions++;if(!ok)throw new InvalidOperationException(reason);}
 private static void Contains(string text,string expected)=>Check(text.Contains(expected,StringComparison.Ordinal),"Missing: "+expected);
 private static GameContent Load(string name)=>JsonSerializer.Deserialize<GameContent>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,name)),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
 private static void FixtureFormatting()
 {
  var content=new GameContent{Cards=new[]{
   new CardDefinition{Id="root",Name="根节点",Cap=2,Effect="原始效果甲"},
   new CardDefinition{Id="legacy",Name="旧分支",Parent="root",ParentLevel=2,Effect="原始效果乙"},
   new CardDefinition{Id="other",Name="另一支",Cap=3},
   new CardDefinition{Id="left",Name="左支",Cap=2,ExclusiveGroup="route",RequiresAll=new[]{new RequirementDefinition{Id="root",Rank=2}}},
   new CardDefinition{Id="right",Name="右支",ExclusiveGroup="route"},
   new CardDefinition{Id="fusion",Name="融合",RequiresAll=new[]{new RequirementDefinition{Id="root",Rank=1}},RequiresAny=new[]{new RequirementDefinition{Id="legacy",Rank=1},new RequirementDefinition{Id="other",Rank=2}},Effect="精确原文 −0.25米；不增加次数。"},
   new CardDefinition{Id="unknown",Name="未知测试"}},Items=new[]{new ItemDefinition{Name="道具",Price=3,Effect="完整效果",Downside="完整代价"}}};
  var ranks=new Dictionary<string,int>{["root"]=2,["left"]=1,["legacy"]=0,["other"]=1};int queries=0;
  int Level(string id)=>ranks.GetValueOrDefault(id);
  CardAvailability State(string id){queries++;return id switch{"root"=>CardAvailability.Capped,"legacy"=>CardAvailability.Available,"left"=>CardAvailability.Available,"right"=>CardAvailability.ExclusiveLocked,"fusion"=>CardAvailability.MissingPrerequisite,"unknown"=>CardAvailability.Unknown,_=>CardAvailability.Available};}
  string text=CodexFormatter.Build(content,Level,State);
  Check(queries==content.Cards.Length,"One authoritative status query per card");
  Contains(text,"根节点  Lv 2/2 〔已达上限〕");Contains(text,"旧分支  Lv 0/1 〔满足树条件〕");
  Contains(text,"前置（全部满足）：根节点 ≥ Lv 2（当前 Lv 2）");Contains(text,"左支  Lv 1/2 〔满足树条件〕");
  Contains(text,"右支  Lv 0/1 〔互斥锁定〕");Contains(text,"互斥支路：左支（当前 Lv 1）");
  Contains(text,"融合  Lv 0/1 〔缺少前置〕");Contains(text,"前置（任一满足）：旧分支 ≥ Lv 1（当前 Lv 0）；另一支 ≥ Lv 2（当前 Lv 1）");
  Contains(text,"未知测试  Lv 0/1 〔未知节点〕");Contains(text,"精确原文 −0.25米；不增加次数。");
  Contains(text,"道具 · 3 金币\n完整效果\n完整代价");Contains(text,"不代表本波已抽到或可直接领取");
  Check(!text.Contains("可跨角色学习所有特性"),"Do not promise incompatible branches");Check(!text.Contains("route"),"Do not expose internal group IDs");
  ranks["other"]=2;string changed=CodexFormatter.Build(content,Level,State);
  Contains(changed,"另一支 ≥ Lv 2（当前 Lv 2）");Contains(changed,"融合  Lv 0/1 〔缺少前置〕");
  Contains(CodexFormatter.Build(content,_=>0,_=>CardAvailability.Capped),"根节点  Lv 0/2 〔已达上限〕");
  Contains(CodexFormatter.Build(content,Level,_=>(CardAvailability)999),"未知状态");
  Contains(CodexFormatter.Build(content,Level,_=>CardAvailability.RiskLimit),"风险代价将触及上限，不可选");
 }
 private static void RealCatalogIntegration(string filename)
 {
  var content=Load(filename);string original=JsonSerializer.Serialize(content);
  foreach(var hero in content.Characters)
  {
   var sim=new GameSimulation(content);sim.StartRun(hero.Id,1030);int queries=0;
   CardAvailability State(string id){queries++;return sim.GetCardAvailability(id);}
   long tick=sim.Tick;int coins=sim.Coins,entities=sim.EntityCount;var ranks=content.Cards.Select(c=>sim.GetLevel(c.Id)).ToArray();
   string text=CodexFormatter.Build(content,sim.GetLevel,State);
   Check(queries==content.Cards.Length,"Every real node queries simulation");
   foreach(var card in content.Cards){Contains(text,card.Name+"  Lv "+sim.GetLevel(card.Id)+"/"+card.Cap);Contains(text,card.Effect);}
   foreach(var contract in content.TemporaryContracts){Contains(text,contract.Name);Contains(text,contract.Description);}
   Check(sim.Tick==tick&&sim.Coins==coins&&sim.EntityCount==entities,"Formatter does not advance/mutate run");
   Check(content.Cards.Select(c=>sim.GetLevel(c.Id)).SequenceEqual(ranks),"Read-only loadout");
   Check(JsonSerializer.Serialize(content)==original,"Read-only catalog");
   Check(CodexFormatter.Build(content,sim.GetLevel,sim.GetCardAvailability)==text,"Repeated open deterministic");
   Contains(text,content.Cards.Single(c=>c.Id==hero.Root).Name+"  Lv 1/");
   if(content.SchemaVersion==2){Contains(text,"前置（任一满足）：");Contains(text,"互斥支路：");Contains(text,"接力弹射 ≥ Lv 2（当前 Lv 0）");}
  }
 }
 public static void Main(){FixtureFormatting();RealCatalogIntegration("legacy-gameplay.json");RealCatalogIntegration("gameplay.json");Console.WriteLine($"PASS CodexFormatter {assertions} assertions: schema1/schema2, authoritative status, all/any/rank/exclusive/cap, five heroes, repeated read-only formatting");}
}
