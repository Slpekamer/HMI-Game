# Legacy migration contracts and preservation Host fixture

## Scope and decisions

This independent follow-up starts from `df3bd08856f1fe316fe477a6542717b3702c0f6d`. It does not change production, Core, persistence formats, assets, the strict runners or `scripts/verify.sh`.

Three reproduced failures have different meanings:

- **RuleMigrationTests:** its original `Inspect()` path validates rule1 snapshots by asking `TryConvertLegacyPreparedCheckpoint` to convert them. Current `CheckpointSystem.TryConvertLegacy` deliberately returns `RulesMismatch`. The suite consequently fails at the explicit historical-contract diagnostic in `PreserveHappy`, with `Uncertain` / `No valid committed snapshot`; the runtime exits 134. A fresh serial build succeeds with zero warnings/errors. A preceding concurrent `dotnet run` attempt failed during build without useful diagnostics and is not counted as a migration execution.
- **RuleMigrationHost:** its first case requires the rule1 Home title to offer an upgrade, then expects direct conversion, older timing copy and the old inspection type/API. Current Host identifies this source as `LegacyPreserveOnly`, offers status/preservation, and never chains rule1 conversion. The strict unchanged runner builds/imports successfully, then fails at `legacy Home explicit upgrade title`.
- **PreserveLegacyHost:** its intended source-byte preservation and separate-new-game behavior remain supported. Its old inspection cast, transaction API/filenames, button copy and immediate-start assumption do not. The unchanged strict runner builds/imports successfully, then fails on the `RuleUpgradeInspection` to `MigrationInspection` cast. This fixture can be adapted without reviving direct conversion.

The first two tests remain unchanged and failed. Their historical fault/format cases must not be silently deleted, skipped, relabeled as passing, or made to pass by restoring retired production behavior. Both runners stop at their first failure, so the remaining assertions have not been individually reclassified or fully mapped to current tests. Future work should retain their historical source/version scope and map applicable invariants to the current upgrade suite explicitly. They are not evidence that all current migration paths fail, nor certification of historical-writer save compatibility.

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


## Negative controls and final verification

All four negative controls use disposable isolated copies and synthetic storage. Each original strict runner builds/imports successfully and exits 1 at the expected assertion:

- Withhold the actual preservation confirmation: selected-PendingStart assertion fails.
- Introduce an erroneous Start of the captured hero after production preparation: the same no-auto-start boundary fails.
- Replace the consent fingerprint with a freshly inspected fingerprint at confirmation: the original changed-source/fail-closed assertion fails.
- Alter original source control bytes after selecting the new run: the exact backup/source-byte assertion fails.

No negative mutation is committed. Independent read-only review of both code batches found no blocking defect and verified retained cancellation/stale/Closed/lost-ack/cold-retry assertions. The initial adaptation is published at `fa125e0fd7ab9d4dc4a779bb5ea9f6f476e1945e`; the strengthened fixture is `baf2b25cdbd7ed72d9295ddfd1d780723f694bec` on `fix/preserve-legacy-current-fixture-20261105`. Published and tested Git trees match.

A separate clean checkout of the published strengthened commit verified all **595 tracked files** against their Git blob hashes, then freshly reran:

- Architecture/notices, Core **646,403**, preservation Host **6 cases / 103**, current upgrade Host **21 / 188**, and exact prior-reader **656**: pass.
- Historical RuleMigrationTests: serial build passes with zero warnings/errors; runtime still aborts with SIGABRT (shell exit134) at the documented direct-conversion incompatibility.
- Historical RuleMigrationHost: strict build/import pass; first rule1-title assertion still fails with exit1.

No production, asset, strict-runner or historical-migration-test file differs from the baseline. This final check reran the affected and supported migration gates plus Core; it did not rerun the entire broad `verify.sh` workflow, which still contains the retained historical failing gate. GitHub reports no commit statuses or associated pull-request workflow runs for the tested commit; scoped cloud execution supplies the evidence, not a green CI badge.

**Classification:** one supported preservation fixture adapted and strengthened; two incompatible historical entry points retained for explicit versioned-test work; no supported-path product defect demonstrated by this investigation. Remaining historical assertions and real historical-writer/user-save compatibility are still open. No merge, deployment or release approval follows.
