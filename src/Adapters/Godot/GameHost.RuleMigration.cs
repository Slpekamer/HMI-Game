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
    private MigrationInspection _ruleInspection;
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
            _ruleInspection=_ruleMigration.Inspect();
        }
        catch(Exception e)when(MigrationExpected(e))
        {_ruleFiles=null;_ruleMigration=null;_ruleInspection=new(MigrationKind.Uncertain,"",null,null,null,e.Message);}
        if(_ruleInspection.Kind==MigrationKind.Selected)BindSelectedRule(_ruleInspection);
        else BindRuleSession(new UnselectedRuleFiles(_ruleFiles?.Legacy));
    }
    private void BindRuleSession(ICheckpointFiles files)
    {
        var codec=new CheckpointCodec(_sim);
        _checkpoint=new CheckpointSession(_sim,new CheckpointStore(files,_ruleCatalogHash,GameSimulation.CheckpointRulesVersion,codec.Validate),codec);
        _checkpoint.RefreshRecovery();
    }
    private void BindSelectedRule(MigrationInspection selected)
    {
        if(selected.Kind!=MigrationKind.Selected||selected.Slot==null||_ruleFiles==null)throw new InvalidOperationException("No validated selected slot");
        // Slot() validates canonical identity/path; a later recovery failure stays in this slot.
        // It must never fall back to legacy, including after Closed.
        BindRuleSession(_ruleFiles.Slot(selected.Slot));
        _ruleInspection=selected;_ruleSelected=true;_ruleUiEpoch++;
    }
    private static bool MigrationExpected(Exception e)=>e is IOException or InvalidDataException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or System.Text.Json.JsonException or FormatException or OverflowException;
    private string LegacySummary(WaveCheckpointDto dto)
    {
        if(dto==null)return "旧规则安全点";
        int hero=Array.IndexOf(AssetVisuals.HeroIds,dto.CharacterId);
        string name=hero>=0?AssetVisuals.HeroNames[hero]:dto.CharacterId;
        return $"{name} · 第{dto.Wave}波 · 金币{dto.Coins}";
    }
    private void UpdateRuleResumeView()
    {
        long epoch=_ruleUiEpoch;
        _ui.ResumeReviewRequested=()=>{if(epoch==_ruleUiEpoch)ShowRuleReview();};
        _ui.RetrySaveRequested=()=>{if(epoch==_ruleUiEpoch)RefreshRuleInspection(true);};
        bool legacy=_ruleInspection.Kind==MigrationKind.LegacyAvailable;
        bool pending=_ruleInspection.Kind==MigrationKind.Incomplete;
        bool blocked=_ruleInspection.Kind==MigrationKind.Uncertain;
        _ui.ResumeReviewTitle=legacy?"升级并继续旧安全点":pending?"检查未完成的存档升级":"查看存档状态";
        _ui.SetResumeState(new ResumeUi(legacy,legacy?LegacySummary(_ruleInspection.Checkpoint):"", "",
            legacy?"旧规则进度尚未升级；需要明确确认":pending?"升级尚未完成；不会自动开始战斗":"",
            blocked?"存档状态不确定，旧文件已保留；不会自动覆盖或删除。":"",pending||blocked));
    }
    private void RefreshRuleInspection(bool show)
    {
        if(_ruleMigration==null){InitializeRulePersistence(_ruleCatalogHash);}
        else
        {
            _ruleInspection=_ruleMigration.Inspect();_ruleUiEpoch++;
            if(_ruleInspection.Kind==MigrationKind.Selected)BindSelectedRule(_ruleInspection);
        }
        UpdateResumeView();
        if(show&&!_ruleSelected)ShowRuleReview();
    }
    private void ShowRuleReview()
    {
        if(_ruleSelected)return;
        if(_ruleMigration==null){ShowRuleBlocked("暂时无法访问存档目录。");return;}
        long epoch=_ruleUiEpoch;
        if(_ruleInspection.Kind==MigrationKind.LegacyAvailable)
        {
            string operation=Guid.NewGuid().ToString("N"),run=Guid.NewGuid().ToString("N"),fingerprint=_ruleInspection.Fingerprint;
            string message=LegacySummary(_ruleInspection.Checkpoint)+"\n\n每波改为60秒，敌人先预告1秒再出现。确认后从已保存的本波起点重新开始，保留金币、构筑、种子和随机状态；不是继续旧波已流逝的时间。\n旧规则文件会完整备份并保留，新进度写入独立存档空间。取消不会更改旧档。";
            _ui.ShowRuleMigrationDialog("确认升级旧安全点",message,"确认升级并继续",()=>
            {
                if(epoch!=_ruleUiEpoch)return;
                try{CompleteRuleSelection(_ruleMigration.Confirm(operation,run,fingerprint),true);}
                catch(Exception e)when(MigrationExpected(e)){RuleOperationFailed(e);}
            });
            return;
        }
        if(_ruleInspection.Kind==MigrationKind.Incomplete)
        {
            var pending=_ruleMigration.ReadPendingConsent();
            if(pending.Kind==PendingConsentKind.Selected&&pending.Selected!=null)
            {BindSelectedRule(pending.Selected);UpdateResumeView();return;}
            if(pending.Kind==PendingConsentKind.Retryable&&pending.Consent!=null)
            {
                var consent=pending.Consent;bool migration=consent.Kind==SlotInitializationKind.Migration;
                string summary=migration?$"第{consent.Summary.Wave}波 · 金币{consent.Summary.Coins}":consent.Kind==SlotInitializationKind.PreserveLegacyNewGame?$"旧局已保留：第{consent.Summary.Wave}波 · 金币{consent.Summary.Coins}\n另开新局的存档空间尚未确认完成":"新规则存档空间尚未确认完成";
                _ui.ShowRuleMigrationDialog("恢复未完成的存档操作",summary+"\n\n原操作已重新严格验证。只有你确认重试后才继续；不会改用新操作覆盖旧档。"+(migration?"\n升级后从60秒的已保存波起点开始，保留旧规则备份。":"\n重试只建立存档空间，随后回选人；不会猜测上次角色或种子。"),"确认重试",()=>
                {
                    if(epoch!=_ruleUiEpoch)return;
                    try{CompleteRuleSelection(_ruleMigration.RetryPending(consent),migration);}
                    catch(Exception e)when(MigrationExpected(e)){RuleOperationFailed(e);}
                });
                return;
            }
            ShowRuleBlocked("未完成操作无法安全恢复。旧档或暂存信息可能已改变；不会自动重开旧局。");return;
        }
        if(_ruleInspection.Kind==MigrationKind.LegacyNotResumable)
        {_ui.ShowRuleMigrationDialog("尚无可继续的安全点","可以返回选人，明确开始一局新冒险。旧目录不会被新规则覆盖。","",null);return;}
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
        _ruleInspection=_ruleMigration.Inspect();_ruleUiEpoch++;
        // A commit with a lost acknowledgement still requires an explicit fresh review;
        // failure handling itself never starts or resumes gameplay.
        if(_ruleInspection.Kind==MigrationKind.Selected)BindSelectedRule(_ruleInspection);
        UpdateResumeView();
        _ui.ShowRuleMigrationDialog("操作尚未确认完成","原文件已保留。请返回后重新查看存档状态；不会自动开始战斗。","",null);
    }
    private void CompleteRuleSelection(MigrationInspection selected,bool resume)
    {
        BindSelectedRule(selected);UpdateResumeView();
        if(resume)PerformPersistence(e=>_checkpoint.Resume(e),_checkpoint.IntentEpoch);
        else _ui.ShowSelection();
    }
    private void RequestRuleStart(int hero,uint seed)
    {
        if(_ui.IsPersistenceModalOpen)return;
        if(_ruleInspection.Kind!=MigrationKind.LegacyNotResumable)
        {
            if(_ruleInspection.Kind==MigrationKind.LegacyAvailable)
            {
                long epoch=_ruleUiEpoch;
                string operation=Guid.NewGuid().ToString("N"),run=Guid.NewGuid().ToString("N"),fingerprint=_ruleInspection.Fingerprint;
                string message=LegacySummary(_ruleInspection.Checkpoint)+"\n\n旧局与完整备份都会保留，不覆盖或删除。另开一局60秒波次的新冒险，不继承旧局金币、构筑或种子。\n如果希望继续旧局，请返回首页选择升级继续；取消不会更改旧档。";
                _ui.ShowRuleMigrationDialog("保留旧局并另开新局？",message,"确认保留并开始新局",()=>
                {
                    if(epoch!=_ruleUiEpoch)return;
                    try
                    {
                        var selected=_ruleMigration.PreserveLegacyAndInitializeNewGame(operation,run,fingerprint);
                        BindSelectedRule(selected);UpdateResumeView();Start(hero,seed);
                    }
                    catch(Exception e)when(MigrationExpected(e)){RuleOperationFailed(e);}
                });
            }
            else ShowRuleReview();
            return;
        }
        try
        {
            var selected=_ruleMigration.InitializeNewGame(Guid.NewGuid().ToString("N"),Guid.NewGuid().ToString("N"),_ruleInspection.Fingerprint);
            BindSelectedRule(selected);UpdateResumeView();Start(hero,seed);
        }
        catch(Exception e)when(MigrationExpected(e)){RuleOperationFailed(e);}
    }
}
