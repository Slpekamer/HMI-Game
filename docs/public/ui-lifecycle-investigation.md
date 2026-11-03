# UI lifecycle investigation — WIP

Baseline: integrated stationary-facing candidate a838e35. This branch does not merge or change gameplay, input rules or accepted gait assets.

## First batch
The historical host lifecycle fixture stopped after 1,500 ticks although the current wave lasts 3,600 ticks. Its bound now uses `WaveRules.DurationTicks` and allows the phase-transition step. This is a test repair, not a shorter wave or a runtime fix. The result still needs a fresh run.

The unmodified responsive UI runner built with zero warnings/errors, then stopped at its strict import gate: editor import returned zero but emitted `EditorSettings not instantiated yet` for `export/android/shutdown_adb_on_exit`. This known editor-shutdown diagnostic remains a failure of the strict runner. A separate diagnostic runtime invocation is not a full runner pass.

## Remaining checks
Reproduce the historical intermittent `script_bindings` teardown fatal; check repeated UI creation/removal, menus and modals, pause/resume, host restart and normal shutdown. Clean samples cannot prove an intermittent failure eliminated. Headless checks cannot establish native rendering, hardware touch/controller behavior, Android export or phone performance.
