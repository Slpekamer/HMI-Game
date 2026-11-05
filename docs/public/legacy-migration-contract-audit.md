# Legacy migration contracts and preservation Host fixture

## Scope and decisions

This independent follow-up starts from `df3bd08856f1fe316fe477a6542717b3702c0f6d`. It does not change production, Core, persistence formats, assets, the strict runners or `scripts/verify.sh`.

Three reproduced failures have different meanings:

- **RuleMigrationTests:** its original `Inspect()` path validates rule1 snapshots by asking `TryConvertLegacyPreparedCheckpoint` to convert them. Current `CheckpointSystem.TryConvertLegacy` deliberately returns `RulesMismatch`. The suite consequently fails at the explicit historical-contract diagnostic in `PreserveHappy`, with `Uncertain` / `No valid committed snapshot`; the runtime exits 134. A fresh serial build succeeds with zero warnings/errors. A preceding concurrent `dotnet run` attempt failed during build without useful diagnostics and is not counted as a migration execution.
- **RuleMigrationHost:** its first case requires the rule1 Home title to offer an upgrade, then expects direct conversion, older timing copy and the old inspection type/API. Current Host identifies this source as `LegacyPreserveOnly`, offers status/preservation, and never chains rule1 conversion. The strict unchanged runner builds/imports successfully, then fails at `legacy Home explicit upgrade title`.
- **PreserveLegacyHost:** its intended source-byte preservation and separate-new-game behavior remain supported. Its old inspection cast, transaction API/filenames, button copy and immediate-start assumption do not. The unchanged strict runner builds/imports successfully, then fails on the `RuleUpgradeInspection` to `MigrationInspection` cast. This fixture can be adapted without reviving direct conversion.

The first two tests remain unchanged and failed. Their historical fault/format cases must not be silently deleted, skipped, relabeled as passing, or made to pass by restoring retired production behavior. Future work should retain their historical source/version scope and map applicable invariants to the current upgrade suite explicitly. They are not evidence that all current migration paths fail, nor certification of historical-writer save compatibility.

## Preservation adaptation

The six original cases remain: happy, selected Closed in a new process, changed source, selector lost acknowledgment, interrupted intent in one process, and explicit retry in another. The fixture now uses `InspectUpgrade` / `ConfirmUpgrade(startNew: true)`, schema4 upgrade artifacts and the actual **确认保留并准备新局** UI action.

After preparation, it requires a selected `PendingStart`, no checkpoint, tick zero and the actual character chooser. It emits the stale preparation callback again and proves no automatic start. Only a later visible Start action can begin; the happy case deliberately selects a different hero and seed to prove the earlier choice is not replayed. Current-rule identity, wallet reset, new UUID/wave1, cancellation/default focus, old-epoch callbacks, repeated confirmation, stale-source refusal, lost acknowledgment, cold receipt purpose and Closed authority remain checked.

The source backup check now verifies the schema4 envelope checksum, original rule1 source identity, exact full source-file inventory, every entry's scope/hash, and each original byte sequence. A rule1 status-review check additionally forbids the retired direct-upgrade button. No raw production-state overwrite or storage-selection bypass was added. Existing explicit injected I/O fault seams remain controlled synthetic fixtures.

## Initial verification, 2026-11-05

- Unchanged failures reproduced as classified above.
- Adapted strict `test_preserve_legacy_host.py`: build/import/all six processes exit 0, **93 assertions** (32 + 6 + 7 + 20 + 3 + 25). Strict error/leak rejection and required pass markers are unchanged.
- Architecture and whitespace checks pass.
- Independent audit, negative controls and broader supported-path regression were pending at this first published batch; subsequent results are recorded below.

These are headless synthetic-save behavior checks. They do not establish native pixels, historical-writer compatibility, real user saves, mobile hardware/performance, exports, a fully green aggregate or release readiness. Main and integration are not changed.


## Strengthened preservation coverage

Read-only independent audit found no blocking adaptation issue and identified three useful test gaps. A second narrow batch uses a deliberately constructed, semantically validated synthetic rule1 checkpoint at wave2 with a nonzero wallet and a picked health card. The new checkpoint must reset that wallet and build as well as use the later explicit hero/seed. This remains a current-generated fixture, not historical-writer evidence.

The interrupted-intent case now compares original source bytes immediately against a baseline taken before the operation, then passes that same baseline to the cold process. Preparation and cold retry also require the simulation run token to remain unchanged, strengthening final phase/tick checks.

- Strict preservation runner: all six cases pass **103 assertions** (35 + 6 + 8 + 22 + 5 + 27), build/import/runtime exit 0.
- Current supported upgrade Host: all **21 cases / 188 assertions** pass.
- Exact prior-rule2 reader is built from immutable `e4e7e6351e8de39c3376594968022f661e189d12`; **RuleUpgrade 656** passes, including the original readable Active positive control and all six after-write upgrade states. This validates old-reader behavior on current-generated synthetic fixtures, not an old-writer-created save corpus.
- Negative controls and final remote verification are still in progress at this checkpoint. The unchanged historical pure and direct-migration Host tests remain failed; a green full aggregate is not claimed.
