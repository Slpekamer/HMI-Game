#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using Hmi.Core;

namespace Hmi.GodotAdapter;

/// <summary>
/// Read-only, on-demand presentation of the authoritative graph query.
/// No eligibility, acquisition, spending or stat calculation occurs here.
/// </summary>
internal static class CodexFormatter
{
    internal static string Build(GameContent content, Func<string, int> level,
        Func<string, CardAvailability> availability)
    {
        if (content == null) throw new ArgumentNullException(nameof(content));
        if (level == null) throw new ArgumentNullException(nameof(level));
        if (availability == null) throw new ArgumentNullException(nameof(availability));
        var names = new Dictionary<string, string>(StringComparer.Ordinal);
        var groups = new Dictionary<string, List<CardDefinition>>(StringComparer.Ordinal);
        foreach (var card in content.Cards)
        {
            names.Add(card.Id, card.Name);
            if (string.IsNullOrEmpty(card.ExclusiveGroup)) continue;
            if (!groups.TryGetValue(card.ExclusiveGroup, out var members))
            {
                members = new List<CardDefinition>();
                groups.Add(card.ExclusiveGroup, members);
            }
            members.Add(card);
        }
        var text = new StringBuilder("当前构筑 · 跨角色搭配仍受前置与互斥约束\n" +
            "“满足树条件”仅表示可进入对应抽选池，不代表本波已抽到或可直接领取。\n\n");
        foreach (var card in content.Cards)
        {
            text.Append(card.Name).Append("  Lv ").Append(level(card.Id)).Append('/')
                .Append(card.Cap).Append(" 〔").Append(Status(availability(card.Id))).Append("〕\n");
            if (!string.IsNullOrEmpty(card.Parent))
            {
                text.Append("前置（全部满足）：");
                AppendRequirement(text, card.Parent, card.ParentLevel, names, level);
                text.Append('\n');
            }
            else
            {
                AppendRequirements(text, "前置（全部满足）：", card.RequiresAll, names, level);
                AppendRequirements(text, "前置（任一满足）：", card.RequiresAny, names, level);
            }
            if (!string.IsNullOrEmpty(card.ExclusiveGroup))
            {
                text.Append("互斥支路：");
                bool first = true;
                foreach (var sibling in groups[card.ExclusiveGroup])
                {
                    if (sibling.Id == card.Id) continue;
                    if (!first) text.Append("；");
                    text.Append(sibling.Name).Append("（当前 Lv ").Append(level(sibling.Id)).Append('）');
                    first = false;
                }
                text.Append("。同组不可兼得，已选支路仍按自身上限升级。\n");
            }
            text.Append(card.Effect).Append("\n\n");
        }
        if (content.TemporaryContracts.Length > 0)
        {
            text.Append("临时契约 · 仅下一波，不是永久等级\n类别永久选择耗尽时可能出现；以实际三选一为准。\n\n");
            foreach (var contract in content.TemporaryContracts)
                text.Append(contract.Name).Append("\n").Append(contract.Description).Append("\n\n");
        }
        text.Append("道具 · 下一波生效\n\n");
        foreach (var item in content.Items)
            text.Append(item.Name).Append(" · ").Append(item.Price).Append(" 金币\n")
                .Append(item.Effect).Append('\n').Append(item.Downside).Append("\n\n");
        return text.ToString();
    }
    private static void AppendRequirements(StringBuilder text, string title,
        RequirementDefinition[]? requirements, Dictionary<string, string> names, Func<string, int> level)
    {
        if (requirements == null || requirements.Length == 0) return;
        text.Append(title);
        for (int i = 0; i < requirements.Length; i++)
        {
            if (i > 0) text.Append("；");
            AppendRequirement(text, requirements[i].Id, requirements[i].Rank, names, level);
        }
        text.Append('\n');
    }
    private static void AppendRequirement(StringBuilder text, string id, int rank,
        Dictionary<string, string> names, Func<string, int> level)
    {
        text.Append(names.TryGetValue(id, out var name) ? name : id)
            .Append(" ≥ Lv ").Append(rank).Append("（当前 Lv ").Append(level(id)).Append('）');
    }
    private static string Status(CardAvailability value)
    {
        switch (value)
        {
            case CardAvailability.Available: return "满足树条件";
            case CardAvailability.Capped: return "已达上限";
            case CardAvailability.MissingPrerequisite: return "缺少前置";
            case CardAvailability.ExclusiveLocked: return "互斥锁定";
            case CardAvailability.RiskLimit: return "风险代价将触及上限，不可选";
            case CardAvailability.Unknown: return "未知节点";
            default: return "未知状态";
        }
    }
}
