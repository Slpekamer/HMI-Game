using Hmi.Core;
namespace Hmi.GodotAdapter;
// Pure presentation mapping: eligibility and all numeric effects remain core-owned.
internal static class EndlessUiFormatter
{
    internal static OfferUi Offer(OfferSlotView slot)
    {
        string badge=slot.Kind==OfferKind.TemporaryContract||slot.Kind==OfferKind.NextWaveSupply?"仅下一波":slot.Kind==OfferKind.PermanentCard?"":"本槽不可选";
        string disabled=slot.UnavailableReason switch
        {
            OfferUnavailableReason.RiskLimit=>"风险已达上限",
            OfferUnavailableReason.ContractLimit=>"无安全临时选择",
            OfferUnavailableReason.Capped=>"本类已达上限",
            _=>"不可选择"
        };
        return new OfferUi(slot.Card.Id,slot.Card.Name,slot.Card.Effect,slot.Slot,slot.Enabled,badge,disabled);
    }
    internal static string Termination(RunTerminationReason reason)=>reason switch
    {
        RunTerminationReason.FinishedByPlayer=>"已主动结束本局。",
        RunTerminationReason.WaveLimit=>"已到安全波次上限，本局结束。",
        RunTerminationReason.LedgerLimit=>"账本已到安全上限，本局结束。",
        _=>"本局已结束；重新选择将清空本局金币与构筑。"
    };
}
