# Fixture repairs and final regression, 2026-11-05

## Published scope

Independent branch: `fix/floor-fresh-rule-fixture-20261105`, based on strict candidate `9d070f4955ea2093b5710f66149c7227b879d353`. Main and the existing integration branch were not changed. The final tested code is `b01216a319dd549bcd06ea49300ae74e6730255f`; this summary is a later documentation-only commit.

Five individually published batches:

- `d25f776`: [FloorContact explicit fresh-rule consent](floor-contact-fixture.md).
- `4f12104`: [six related Host fixture consent flows](legacy-host-fixture-consent.md).
- `4d7cb76`: [PersistenceUi's real dependency closure](persistence-ui-fixture-build.md), as a separate harness fix.
- `2cbe226`: [mobile safe-area and motion consent flows](mobile-fixture-consent.md).
- `b01216a`: [responsive test waits for actual spawn activation](responsive-spawn-fixture.md), as a separate timing fix.

Production source, Core/Persistence, content, project settings, export presets and the aggregate script remain unchanged from `9d070f4`. Independent review verified all 250 original assertion call sites across the nine changed templates remain literal and ordered. Existing fault seams remain labeled; no new production state override or relaxed diagnostic filter is introduced. Each published Git tree matched the local tested tree.

## Clean final aggregate

A separate checkout was fetched from the published branch, and all 593 tracked files were individually verified against their Git blob hashes. Godot 4.6.3 Mono and .NET SDK 10.0.403 ran the original `scripts/verify.sh` with writable, isolated cloud data/config/temp directories. No user desktop was used.

The aggregate passes every stage through mobile-motion integration, then **exits 134** at the unchanged historical `RuleMigrationTests` gate. Its intermediate `PASS build / core / import / smoke / host lifecycle` line is not a whole-script pass.

Fresh passes before that stop:

- Architecture, launcher icon, 17 food-contract tests, nine notice/inclusion checks, runtime glyphs, asset inventories, five-hero glTF contracts, authored UI/portrait validation, strict editor import, engine layout/animation probes and main-scene smoke.
- Core 646,403; CodexFormatter 655; CheckpointStore 112; CheckpointSession 174; MoveInputRouter 232.
- Primitive ownership 256; FloorContact 115; HostLifecycle; CharacterPresentation 1,450; EnemyHealth 35; explicitly legacy animation mixer 17,590; AttackBoundary 100; AttackCadence 5,980; AssetCacheLifecycle 351.
- SelectionPortrait all-five/repeat/identity-seed/health/focus/lifecycle checks; Codex 29; Endless 745; PersistenceUi 100; CheckpointHost 36; OpaquePage 130 with zero native pixel pairs; ResponsiveUi 893; HostResponsive 298.
- Floating input 130; keyboard lifecycle 6,460; gamepad UI 209; mobile safe area 31; UI motion 174; visible motion 22; motion preference 29; mobile-motion 34 main plus four supplemental checks, zero captures.

PersistenceUi's isolated build retains 19 CS0649 warnings and zero errors, as documented in its report; no warning-free claim or suppression is made. Its strict runner still rejects runtime/import errors and missing pass markers.

## Separately executed remaining and supported migration gates

The three commands after the aggregate stop were also freshly invoked against the final tested code, each with its own retained result:

- `test_rule_migration_host.py`: **FAIL**, exit 1 in its first legacy case at `legacy Home explicit upgrade title`.
- `test_preserve_legacy_host.py`: **FAIL**, exit 1 in its first happy case because its old template casts `RuleUpgradeInspection` to `MigrationInspection`.
- `test_mechanical_arrival.py`: **PASS 184**, headless behavior only.

The historical pure suite's retained failure explicitly identifies its rule1 direct-conversion fixture incompatibility. These historical expectations were not removed, rewritten into current semantics or declared passing. They remain open migration-test work, not evidence that every current migration behavior is broken.

To check the actual supported path separately, unchanged `test_rule_upgrade_host.py` passed all 21 cases / 188 assertions, including explicit preparation, rule2 upgrade, rule1 preservation, Closed/PendingStart states, stale/lost acknowledgment, selected state and separate-process restart. The exact prior-rule2 reader was freshly compiled from immutable `e4e7e6351e8de39c3376594968022f661e189d12`; `RuleUpgradeTests` passed 656 checks, including the readable original Active positive control and all six actual after-write upgrade states. These current-generated synthetic saves are not old-writer-created saves, real-user-save certification or a replacement for the historical failing suites.

## Negative controls and limits

Original failing runs remain identifiable. Withholding actual consent fails the corrected fixtures' preparation/no-auto-start checks. Raising a disposable floor by .01 fails the original floor-top check. Removing enemies only after verified activation fails the original visible-health-bar assertion. Removing the PersistenceUi helpers or their Core reference recreates the expected compile errors. All negative mutations were confined to disposable test copies.

No native display was available for this task: the floor's 220 actual MultiMesh-instance storage gate remains unrun. Headless behavior does not establish pixels, actual hardware/touch/controller delivery, phone performance, APK installation/export, all-save compatibility or release readiness. No new APK was produced. GitHub returned no commit statuses or associated pull-request workflow runs for the tested commit; cloud-run logs, not an absent CI badge, supply the evidence.

This checkpoint resolves the reproduced fixture blockers while retaining the historical migration failures. It is **not a fully green aggregate or release approval**.
