using System;
using System.IO;
using Godot;
using Hmi.Core;
using Hmi.GodotAdapter;
using Hmi.Adapters.Persistence;

public partial class GameHost
{
    private IRuleMigrationFiles _ruleFiles;
    private RuleMigrationCoordinator _ruleMigration;
    private RuleUpgradeInspection _ruleInspection;
    private bool _ruleSelected;
    private long _ruleUiEpoch;
    private string _ruleCatalogHash="";

    // Before selector acknowledgement, even an accidental ordinary Session write cannot
    // write new-rule data into the legacy directory. No fallback directory is invented.
    private sealed class UnselectedRuleFiles : ICheckpointFiles
    {
        private readonly ICheckpointFiles _read;
        public UnselectedRuleFiles(ICheckpointFiles read){_read=read;}
        public IDisposable AcquireWriter()=>_read!=null?_read.AcquireWriter():throw new IOException("Rule selection unavailable");
        public string[] Names()=>_read.Names();
        public long Length(string name)=>_read.Length(name);
        public byte[] Read(string name,int maximum)=>_read.Read(name,maximum);
        public void AtomicWrite(string name,byte[] bytes,bool overwrite)=>throw new IOException("Legacy store is read-only until rule selection");
        public void Delete(string name)=>throw new IOException("Legacy store is read-only until rule selection");
    }
    private void InitializeRulePersistence(string catalogHash)
    {
        _ruleCatalogHash=catalogHash;_ruleSelected=false;_ruleUiEpoch++;
        try
        {
            _ruleFiles=new PhysicalRuleMigrationFiles(ProjectSettings.GlobalizePath("user://checkpoints/rule2"),ProjectSettings.GlobalizePath("user://checkpoints/v1"));
            _ruleMigration=new RuleMigrationCoordinator(_ruleFiles,_sim);
            _ruleInspection=_ruleMigration.InspectUpgrade();
        }
        catch(Exception e)when(MigrationExpected(e))
        {_ruleFiles=null;_ruleMigration=null;_ruleInspection=new(RuleUpgradeKind.Blocked,"",null,null,null,null,e.Message);}
        if(_ruleInspection.Kind==RuleUpgradeKind.CurrentSelected)BindSelectedRule(_ruleInspection);
        else BindRuleSession(new UnselectedRuleFiles(_ruleFiles?.Legacy));
    }
    private void BindRuleSession(ICheckpointFiles files)
    {
        var codec=new CheckpointCodec(_sim);
        _checkpoint=new CheckpointSession(_sim,new CheckpointStore(files,_ruleCatalogHash,GameSimulation.CheckpointRulesVersion,codec.Validate),codec);
        _checkpoint.RefreshRecovery();
    }
    private void BindSelectedRule(RuleUpgradeInspection selected)
    {
        if(selected.Kind!=RuleUpgradeKind.CurrentSelected||selected.Slot==null||_ruleFiles==null)throw new InvalidOperationException("No validated selected slot");
        // Slot() validates canonical identity/path; a later recovery failure stays in this slot.
        // It must never fall back to legacy, including after Closed.
        BindRuleSession(_ruleFiles.Slot(selected.Slot));
        _ruleInspection=selected;_ruleSelected=true;_ruleUiEpoch++;
    }
    private static bool MigrationExpected(Exception e)=>e is IOException or InvalidDataException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or System.Text.Json.JsonException or FormatException or OverflowException;
    private string PriorRuleSummary(WaveCheckpointDto dto)
    {
        if(dto==null)return "旧规则进度";
        int hero=Array.IndexOf(AssetVisuals.HeroIds,dto.CharacterId);
        return $"{(hero>=0?AssetVisuals.HeroNames[hero]:"守护者")} · 第{dto.Wave}波起点 · 金币{dto.Coins}";
    }
    private string PriorRollbackWarning()=>_ruleInspection.Recovery?.Kind==RecoveryKind.RolledBack
        ?"最近安全点损坏，将使用同一旧局更早的有效波起点。\n":"";
    private void UpdateRuleResumeView()
    {
        long epoch=_ruleUiEpoch;
        _ui.ResumeReviewRequested=()=>{if(epoch==_ruleUiEpoch)ShowRuleReview();};
        _ui.RetrySaveRequested=()=>{if(epoch==_ruleUiEpoch)RefreshRuleInspection(true);};
        bool prior=_ruleInspection.Kind==RuleUpgradeKind.PriorAvailable;
        bool pending=_ruleInspection.Kind==RuleUpgradeKind.Incomplete;
        bool blocked=_ruleInspection.Kind==RuleUpgradeKind.Blocked;
        _ui.ResumeReviewTitle=prior?"升级并继续旧安全点":pending?"检查未完成的存档升级":"查看存档状态";
        string status=_ruleInspection.Kind switch
        {
            RuleUpgradeKind.PriorAvailable=>"旧规则进度尚未升级；需要明确确认",
            RuleUpgradeKind.PriorNotResumable=>"旧规则没有可继续的安全点；可保留旧档另开新局",
            RuleUpgradeKind.LegacyPreserveOnly=>"此旧版进度仅支持保留旧档另开新局",
            RuleUpgradeKind.Empty=>"真实弹丸规则存档尚未准备",
            RuleUpgradeKind.Incomplete=>"升级尚未确认完成；不会自动开始战斗",
            _=>""
        };
        _ui.SetResumeState(new ResumeUi(prior,prior?PriorRuleSummary(_ruleInspection.Checkpoint):"",PriorRollbackWarning(),status,
            blocked?"存档状态不确定，原文件已保留；不会自动覆盖、删除或回退旧局。":"",pending||blocked));
    }
    private void RefreshRuleInspection(bool show)
    {
        if(_ruleMigration==null)InitializeRulePersistence(_ruleCatalogHash);
        else
        {
            _ruleInspection=_ruleMigration.InspectUpgrade();_ruleUiEpoch++;
            if(_ruleInspection.Kind==RuleUpgradeKind.CurrentSelected)BindSelectedRule(_ruleInspection);
        }
        UpdateResumeView();
        if(show&&!_ruleSelected)ShowRuleReview();
    }
    private void ShowRuleReview()
    {
        if(_ruleSelected)return;
        if(_ruleMigration==null){ShowRuleBlocked("暂时无法访问存档目录，原文件不会被覆盖。");return;}
        long epoch=_ruleUiEpoch;
        if(_ruleInspection.Kind==RuleUpgradeKind.PriorAvailable)
        {
            string operation=Guid.NewGuid().ToString("N"),run=Guid.NewGuid().ToString("N"),fingerprint=_ruleInspection.Fingerprint;
            string message=PriorRuleSummary(_ruleInspection.Checkpoint)+"\n"+PriorRollbackWarning()+
                "\n本次升级改用真实弹丸碰撞规则，攻击与命中的时机可能改变。确认后从已保存的本波起点重新开始，不是继续离开时已进行的时间。保留已保存的构筑、金币、种子和随机状态。\n旧档保留，新进度写入独立存档空间。取消不会更改旧档。";
            _ui.ShowRuleMigrationDialog("确认升级旧安全点",message,"确认升级并继续",()=>
            {
                if(epoch!=_ruleUiEpoch)return;
                try{CompleteRuleSelection(_ruleMigration.ConfirmUpgrade(operation,run,fingerprint,false),true);}
                catch(Exception e)when(MigrationExpected(e)){RuleOperationFailed(e);}
            });return;
        }
        if(_ruleInspection.Kind==RuleUpgradeKind.Incomplete)
        {
            var receipt=_ruleInspection.Pending;
            if(receipt==null){ShowRuleBlocked("未完成操作无法安全恢复，原文件已保留。");return;}
            string summary=receipt.StartNew?"保留旧档另开新局的存档空间尚未确认完成。":PriorRuleSummary(_ruleInspection.Checkpoint)+"\n"+PriorRollbackWarning();
            _ui.ShowRuleMigrationDialog("恢复未完成的存档操作",summary+
                "\n\n只有你确认后才重试已验证的原操作，不会换用新操作覆盖旧档。"+
                (receipt.StartNew?"\n成功后返回选人，再明确选择角色开始；不会猜测上次角色或种子。":"\n升级成功后从已保存的本波起点开始，旧档保留。"),"确认重试",()=>
            {
                if(epoch!=_ruleUiEpoch)return;
                try{CompleteRuleSelection(_ruleMigration.RetryUpgrade(receipt),!receipt.StartNew);}
                catch(Exception e)when(MigrationExpected(e)){RuleOperationFailed(e);}
            });return;
        }
        if(_ruleInspection.Kind is RuleUpgradeKind.PriorNotResumable or RuleUpgradeKind.LegacyPreserveOnly or RuleUpgradeKind.Empty)
        {
            string message=_ruleInspection.Kind==RuleUpgradeKind.LegacyPreserveOnly
                ?"此旧版进度不支持直接升级续档。可以保留旧档并另开真实弹丸规则的新局；不会串联旧规则升级。"
                :_ruleInspection.Kind==RuleUpgradeKind.Empty?"尚无存档。请返回选人，明确准备并开始真实弹丸规则的新局。"
                :_ruleInspection.Recovery?.Kind==RecoveryKind.Closed?"旧局已经结束，没有可继续的安全点。可以保留旧档并另开新局。":"旧局尚未提交可继续的波起点。可以保留旧档并另开新局，不会恢复旧快照。";
            _ui.ShowRuleMigrationDialog("没有可继续的安全点",message,"返回选人",()=>{if(epoch==_ruleUiEpoch)_ui.ShowSelection();});return;
        }
        ShowRuleBlocked("存档状态不确定。请重新检查；不会自动修复、覆盖、删除或回退旧局。");
    }
    private void ShowRuleBlocked(string message)
    {
        long epoch=_ruleUiEpoch;
        _ui.ShowRuleMigrationDialog("存档升级未完成",message,"重新检查",()=>{if(epoch==_ruleUiEpoch)RefreshRuleInspection(true);});
    }
    private void RuleOperationFailed(Exception error)
    {
        GD.Print("HMI_RULE_SELECTION_BLOCKED "+error.GetType().Name);
        _ruleInspection=_ruleMigration.InspectUpgrade();_ruleUiEpoch++;
        // Lost acknowledgement only re-inspects/binds. Never Begin, replay a DTO, or retry automatically.
        if(_ruleInspection.Kind==RuleUpgradeKind.CurrentSelected)BindSelectedRule(_ruleInspection);
        UpdateResumeView();
        _ui.ShowRuleMigrationDialog("操作尚未确认完成","原文件已保留。请返回后重新查看存档状态；不会自动开始战斗。","",null);
    }
    private void CompleteRuleSelection(RuleUpgradeInspection selected,bool resume)
    {
        BindSelectedRule(selected);UpdateResumeView();
        if(resume&&_checkpoint.CurrentRecovery.Kind is RecoveryKind.Active or RecoveryKind.RolledBack)
            PerformPersistence(e=>_checkpoint.Resume(e),_checkpoint.IntentEpoch);
        else _ui.ShowSelection();
    }
    private void RequestRuleStart(int hero,uint seed)
    {
        if(_ui.IsPersistenceModalOpen)return;
        if(_ruleInspection.Kind is not (RuleUpgradeKind.PriorAvailable or RuleUpgradeKind.PriorNotResumable or RuleUpgradeKind.LegacyPreserveOnly or RuleUpgradeKind.Empty))
        {ShowRuleReview();return;}
        if(_ruleMigration==null){ShowRuleBlocked("暂时无法访问存档目录，原文件不会被覆盖。");return;}
        long epoch=_ruleUiEpoch;
        string operation=Guid.NewGuid().ToString("N"),run=Guid.NewGuid().ToString("N"),fingerprint=_ruleInspection.Fingerprint;
        bool empty=_ruleInspection.Kind==RuleUpgradeKind.Empty;
        string summary=_ruleInspection.Checkpoint==null?"":PriorRuleSummary(_ruleInspection.Checkpoint)+"\n\n";
        string message=empty?"准备真实弹丸规则的新局存档空间。成功后返回选人，请再次明确选择角色与种子并开始冒险。取消不会写入新局。":
            summary+"保留旧档，另开真实弹丸规则的新局；不继承旧局金币、构筑或种子。"+
            (_ruleInspection.Kind==RuleUpgradeKind.LegacyPreserveOnly?"此旧版进度不支持直接升级续档。":"")+
            "\n确认后先准备独立存档空间，成功后返回选人，请再次明确开始。取消不会更改旧档。";
        _ui.ShowRuleMigrationDialog(empty?"准备新局存档":"保留旧档并另开新局",message,empty?"确认准备新局":"确认保留并准备新局",()=>
        {
            if(epoch!=_ruleUiEpoch)return;
            try{CompleteRuleSelection(_ruleMigration.ConfirmUpgrade(operation,run,fingerprint,true),false);}
            catch(Exception e)when(MigrationExpected(e)){RuleOperationFailed(e);}
        });
    }
}
