# Input, APK and candy-scene integration candidate

## Scope and history

This independent candidate starts from `4030b405` and preserves the three reviewed histories with conflict-free merge commits. Main and the existing integration branch are unchanged; this is not a release or authorization to merge into either.

- `5d4f830`: keyboard lifecycle branch at `14819af`.
- `336def2`: APK validation branch at `c64b9ea`.
- `609a145`: owner-approved grid-free candy scenery at `a6f9d390`.
- `553ba6a`: reconcile the stale Crit inventory hash with independently verified committed bytes. See [asset audit](crit-manifest-audit.md).
- `464dde8`: correct the old gamepad test's fresh-rule confirmation flow while retaining every original assertion. See [fixture evidence](gamepad-fixture.md).

Core, Persistence, character GLBs, the accepted normal Crit rig, camera/animation code, project settings and export presets remain byte-identical to `4030b405`. Production changes are confined to the keyboard reset behavior and the already-reviewed scenery. The later inventory/test fixes do not change runtime source or assets.

## Fresh cloud verification, 2026-11-05

Godot 4.6.3 Mono and .NET SDK 10.0.403 were used, with isolated writable user/config/cache directories and process-scoped serial build settings. The final build completed with zero warnings and errors. No user desktop or physical device was used.

- Architecture boundary, nine notice/inclusion checks and Git whitespace checks: pass.
- Portable Core: 646,403 assertions. Input router: 232. Codex: 655.
- Checkpoint store: 112. Session: 174. Rule upgrade: 579, with its exact old-reader check explicitly **NOT_RUN**.
- Keyboard lifecycle supplemental runtime: 6,460, including all 255 nonempty key combinations. Floating touch: 130. Corrected gamepad UI: 209.
- Native candy geometry/imported-pixel/ownership/teardown probe: 94,573, using Compatibility rendering on Mesa llvmpipe. This is not a hardware performance result.
- Normal presentation lifecycle: 98. Actual UI/wave/new-run transitions plus active highest-current-speed interruption: 3,253, retaining the 2,485 base checks. Catalog loadout, haste and wave-clock acceleration remain explicitly labeled fixtures.
- Original asset validators pass eight animated entries and all five static food-hero contracts after the evidence-backed inventory correction. No validator was weakened.
- APK validator: 43 tests in ordinary Python and 43 under `-O`. Actual official Build Tools 36.1.0 rechecked the preserved historical UX0004 APK: v2 signature, expected signer, CRC, ZIP 16 KiB alignment, eleven ARM64 ELF libraries, exact Godot/Mono runtimes and three required assemblies pass. Wrong-signer rejection exits 1 without success JSON. These historical APK bytes are not a build of this integration candidate; [full contract and hash](apk-validation-gates.md).

Keyboard, touch, gamepad and candy strict runners still fail on the retained editor-import diagnostic. The runtime counts above are separate evidence, not those strict runners passing.

## Fresh native comparison

The already-approved art commit `a6f9d390` and merged runtime `609a145` were newly built/imported in separate worktrees and freshly recorded on the same native software-rendered display. Each capture records its exact commit; no earlier recording is substituted.

At 1280x720 and 30 Hz, the normal ten-second Crit route has 300 simulation/presentation frames. All eight authority, pose, camera, sole, boot, presentation, substep and attachment traces match byte-for-byte. Independently decoded images at 0, 2, 5 and 8 seconds match pixel-for-pixel; the merged image was also inspected. There are two melee events and zero ranged launches in this route. This verifies preservation of the reviewed scene in that route, not new projectile, all-hero, late-wave or device certification. Later inventory/test/documentation commits leave these runtime bytes unchanged.

## Resource/export boundary

A fresh Linux data-pack operation runs the actual .NET publish and emits a PCK. An otherwise empty project mounts it and loads/instantiates the accepted normal Crit rig, legacy content Crit and production `wall_block.glb`; it also loads the 1400x800 icing texture and reads the corrected Crit inventory hash. No external source tree is used for those loaded resources.

The export process exits zero but emits the same editor-shutdown ERROR, so the strict export gate remains **failed**. A loadable data pack is not a standalone executable, newly built APK or device run. Diagnostic scenes/adapters remain included by the unchanged all-resources export; no minimal release package is claimed.

## Failures and unrun gates retained

- The fresh aggregate `scripts/verify.sh` first reproduced the old mismatched Crit inventory. After the narrow correction, it passes the affected asset checks and proceeds through engine probes/main selection smoke, then exits 1 on `EditorSettings not instantiated yet when getting setting "export/android/shutdown_adb_on_exit"`. No ERROR filter was relaxed. Later aggregate stages are not thereby declared executed; separately run stages are listed above.
- `RuleMigrationTests` still exits 134 with `NullReferenceException` in `Program.PreserveHappy`, `Program.PreserveLegacy.cs:27`. Core, Persistence and this test are unchanged. The exact historical-reader check remains unavailable, not inferred from the 579 other upgrade checks.
- No fresh standalone Linux/Android application acceptance, install/update/save compatibility, physical controller, phone GPU/performance, exhaustive crowd/honey/projectile readability or all-hero native review. Original hard shadows/contact occlusion and unsupported teleport/future-speed limits remain.

This candidate is ready for scoped code review with visible blockers, not a fully green release claim.
