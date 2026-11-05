# Legacy Host fixtures: fresh-rule consent

## Requirement and scope

Continuing the unmodified aggregate after the floor fixture fix at `d25f7768dac95a2b73ae9cff548dd51c11a01a1b` exposed six more Host fixtures that assumed a first start on empty storage immediately enters combat. Production instead requires explicit rule-space preparation followed by another start. These changes add the actual UI callback and visible/enabled **确认准备新局** confirmation before each original fixture sequence. No production files, assets, existing assertions, fault seams, runner diagnostic thresholds or start/replace policy change.

The selection fixture prepares storage before its hero/seed loop, so its original selected-identity assertions still exercise real input after returning to selection. Other fixtures preserve their original explicit Start calls. Added checks require consent and verify preparation returns to selection without advancing simulation or automatically beginning combat. Every original source line remains in order. CheckpointHost's historical assertion text mentions rule2; its actual selected production rule is current rule3, and its later injected Session-only fault seam remains explicitly separate from registry/selector testing.

## Fresh failures retained and corrected results

Each unchanged runner built/imported successfully before failing at runtime:

- SelectionPortrait: `Start preserves selected hero and seed`. A disposable diagnostic copy confirmed ruleSelected=false, modal=true and phase=Selecting at that assertion. Corrected strict runner passes all five heroes, repeated preview lifecycle, identity/seed, health, start/restart and focus checks.
- Codex: `Codex opening pauses through command without advancing combat`. Corrected strict runner passes 29 checks.
- Endless: `New wave starts with fresh pending previews, not directly spawned enemies`. Corrected strict runner passes 745 checks, retaining its explicitly synthetic compressed-wave scope.
- CheckpointHost: `production Start selects an actual rule2 slot before the Session-only fault seam`. Corrected strict runner passes 36 checks.
- OpaquePage: indexed an invalid player ID before combat. Corrected strict runner passes 130 checks; zero native A/B pixel pairs were run.
- HostResponsive: `production Start durable ack begins combat`. Corrected preparation passes, but the strict runner remains **failed** at the later original assertion `actual combat contains visible enemy bars`. This separate failure is retained for investigation, not guessed away or classified as a pass.

Strict means build/import/runtime plus each original runner's error rejection. The omitted-confirmation negative control in a disposable copy fails the new no-auto-start/selected-rule assertion. The earlier floor negative control changes only a disposable floor center from -0.15 to -0.14; its strict runner fails the original `Actual floor world top is zero` assertion. These controls show that consent and existing geometry checks remain enforced.

## Aggregate boundary

The unchanged aggregate first stopped at SelectionPortrait after passing Core 646,403, CodexFormatter 655, asset/import/layout/smoke gates, primitive ownership 256, FloorContact 115, HostLifecycle, CharacterPresentation 1,450, EnemyHealth 35, the explicitly legacy mixer 17,590, AttackBoundary 100, AttackCadence 5,980 and AssetCacheLifecycle 351. Subsequent commands were run separately to expose downstream failures; this does not turn the original aggregate failure into a pass.

A separate UI-only PersistenceUi harness compile-list omission and the HostResponsive failure remain distinct work. Headless results establish only their stated behavior/lifecycle contracts, not pixels, physical input devices, phone performance, exports, release readiness or a fully green historical suite.
