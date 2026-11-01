using System;
using System.Text.Json;
using Godot;
using Hmi.Core;
using Hmi.GodotAdapter;
using Hmi.Adapters.Persistence;

public partial class GameHost
{
    private CheckpointSession _checkpoint;
    private bool _afterSaveToSelection;
    private int _terminalCloseRun = -1;

    private void InitializePersistence(string catalogHash)
    {
        var codec = new CheckpointCodec(_sim);
        var files = new PhysicalCheckpointFiles(ProjectSettings.GlobalizePath("user://checkpoints/v1"));
        var store = new CheckpointStore(files, catalogHash, GameSimulation.CheckpointRulesVersion, codec.Validate);
        _checkpoint = new CheckpointSession(_sim, store, codec);
        _checkpoint.RefreshRecovery();
    }

    private void BindPersistenceUi()
    {
        _ui.ExitToSelectionRequested = ExitToSelection;
        _ui.RetrySaveRequested = RetryPersistence;
        _ui.ContinueUnsavedRequested = () =>
        {
            if (!_checkpoint.CanContinueUnsaved) return;
            CompletePersistence(_checkpoint.ContinueUnsaved(_checkpoint.IntentEpoch));
        };
        UpdateResumeView();
    }

    private void UpdateResumeView()
    {
        long viewEpoch = _checkpoint.IntentEpoch;
        _ui.ResumeRequested = () => PerformPersistence(e => _checkpoint.Resume(e), viewEpoch);
        _ui.RetrySaveRequested = () => { if(viewEpoch==_checkpoint.IntentEpoch) RetryPersistence(); };
        var recovery = _checkpoint.CurrentRecovery;
        bool available = recovery.Kind == RecoveryKind.Active || recovery.Kind == RecoveryKind.RolledBack;
        string summary = "", warning = "";
        if (available && recovery.Payload != null)
        {
            // Recovery has already passed the strict codec/Core validator; this is display only.
            var dto = JsonSerializer.Deserialize<WaveCheckpointDto>(recovery.Payload);
            int hero = dto == null ? -1 : Array.IndexOf(AssetVisuals.HeroIds, dto.CharacterId);
            if (dto != null && hero >= 0)
                summary = $"{AssetVisuals.HeroNames[hero]} · 第 {dto.Wave} 波起点 · 金币 {dto.Coins}\n恢复会丢失离开时本波的进度，钱包与随机状态一并回到安全点。";
            else available = false;
            if (recovery.Kind == RecoveryKind.RolledBack)
                warning = "最近安全点损坏，将恢复同一局更早的有效安全点。";
        }
        bool uncertain = recovery.Kind == RecoveryKind.Uncertain;
        _ui.SetResumeState(new ResumeUi(available, summary, warning,
            _sim.Phase == RunPhase.Combat && !_checkpoint.CurrentWaveSaved ? "本局未保存 · 无可恢复安全点" :
            available ? "已保存波起点" : recovery.Kind == RecoveryKind.PendingStart ? "新局尚无可恢复安全点" : "",
            uncertain ? "存档状态不确定，原文件已保留。请重试；不会自动覆盖或删除。" : "", uncertain));
    }

    private void RequestStart(int hero, uint seed)
    {
        if (_ui.IsPersistenceModalOpen || _checkpoint.HasPendingFailure) return;
        long epoch = _checkpoint.IntentEpoch;
        var kind = _checkpoint.CurrentRecovery.Kind;
        if (kind == RecoveryKind.Uncertain)
        {
            _ui.ShowSaveFailure("存档状态不确定，原文件已保留。请重试；不会自动覆盖或删除。", false);
            return;
        }
        if (kind == RecoveryKind.Active || kind == RecoveryKind.RolledBack)
            _ui.ShowReplaceRunConfirmation(() =>
            {
                if (epoch == _checkpoint.IntentEpoch) Start(hero, seed);
            });
        else Start(hero, seed);
    }

    private void PerformPersistence(Func<long, bool> operation, long epoch, bool toSelection = false)
    {
        if (epoch != _checkpoint.IntentEpoch) return;
        _afterSaveToSelection = toSelection;
        CompletePersistence(operation(epoch));
    }

    private void CompletePersistence(bool succeeded)
    {
        if (!succeeded)
        {
            if (!_checkpoint.HasPendingFailure)
            {
                UpdateResumeView();
                if(_checkpoint.LastError.Length>0)
                    _ui.ShowSaveFailure("恢复未完成，请重试。\n"+_checkpoint.LastError,false);
                return; // A rejected stale intent never changes gameplay authority.
            }
            _accumulator = 0; _ui.Joystick.ResetInput(); SetProcess(false);
            long failureEpoch = _checkpoint.IntentEpoch;
            _ui.RetrySaveRequested = () => { if(failureEpoch==_checkpoint.IntentEpoch) RetryPersistence(); };
            _ui.ContinueUnsavedRequested = () => {
                if(failureEpoch==_checkpoint.IntentEpoch && _checkpoint.CanContinueUnsaved)
                    CompletePersistence(_checkpoint.ContinueUnsaved(failureEpoch));
            };
            _ui.ShowSaveFailure((_checkpoint.IsCloseFailure ? "本局已结束，但结束状态尚未保存。请重试；强制退出可能留下旧安全点。\n" : "存档未确认成功。\n") + (_checkpoint.CanContinueUnsaved ? "尚无可恢复安全点。若选择继续，本局后续也不会自动保存，退出后需重新开始。\n" : "") + _checkpoint.LastError,
                _checkpoint.CanContinueUnsaved && !_checkpoint.IsCloseFailure);
            return;
        }
        _ui.HideSaveFailure();
        if (_afterSaveToSelection) { _afterSaveToSelection = false; ShowSelectionAfterPersistence(); return; }
        if (_sim.Phase == RunPhase.Combat)
        {
            _heroIndex = Array.IndexOf(AssetVisuals.HeroIds, _sim.CharacterId);
            _codexOpen = false; _codexResume = false; ClearVisuals();
            _accumulator = 0; _lastWave = -1; _offers = Array.Empty<OfferUi>();
            UpdateResumeView(); SetProcess(true); RefreshUi();
        }
        else { UpdateResumeView(); SetProcess(true); }
    }

    private void RetryPersistence()
    {
        if (_checkpoint.HasPendingFailure)
            CompletePersistence(_checkpoint.Retry(_checkpoint.IntentEpoch));
        else
        {
            _checkpoint.RefreshRecovery();
            _ui.HideSaveFailure(); UpdateResumeView();
        }
    }

    private void ExitToSelection()
    {
        if (_checkpoint.HasPendingFailure || !_checkpoint.ExitSelection(_checkpoint.IntentEpoch)) return;
        ShowSelectionAfterPersistence();
    }

    private void ShowSelectionAfterPersistence()
    {
        Send(CommandKind.Pause, flag: true); _sim.Step(); _accumulator = 0; ClearVisuals();
        _codexOpen = false; _codexResume = false;
        _checkpoint.RefreshRecovery(); UpdateResumeView();
        _ui.ShowSelection(); _assets.ReleaseCachedReferences(); SetProcess(false);
        _ui.StartRequested = RequestStart;
    }
}
