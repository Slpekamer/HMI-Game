# UI lifecycle investigation — WIP

Baseline: integrated stationary-facing candidate a838e35. This branch does not merge or change gameplay, input rules or accepted gait assets.

## First batch
The historical host lifecycle fixture stopped after 1,500 ticks although the current wave lasts 3,600 ticks. Its bound now uses `WaveRules.DurationTicks` and allows the phase-transition step. This is a test repair, not a shorter wave or a runtime fix. The result still needs a fresh run.

The unmodified responsive UI runner built with zero warnings/errors, then stopped at its strict import gate: editor import returned zero but emitted `EditorSettings not instantiated yet` for `export/android/shutdown_adb_on_exit`. This known editor-shutdown diagnostic remains a failure of the strict runner. A separate diagnostic runtime invocation is not a full runner pass.

## Remaining checks
Reproduce the historical intermittent `script_bindings` teardown fatal; check repeated UI creation/removal, menus and modals, pause/resume, host restart and normal shutdown. Clean samples cannot prove an intermittent failure eliminated. Headless checks cannot establish native rendering, hardware touch/controller behavior, Android export or phone performance.

## Follow-up fixture repair and scoped diagnostics
The older fixture also called `Start` without the newer explicit rule-selection confirmation, leaving no live player. The test now presses the actual confirmation button when present and asserts Combat before touching the player. Existing entity-generation, catch-up, scope, companion, wave, restart and disposal assertions remain. A repeat section exercises three starts, nine codex open/close cycles including duplicate close, and pause/resume tick behavior.

Five separate headless responsive runtime processes completed 893 checks each and exited zero without runtime diagnostics. The repaired host fixture completed its original lifecycle sequence in a separate diagnostic run. The extended host repeat checks also passed headless with a clean runtime exit. Native processes are still being checked; this checkpoint does not certify them. No production source changed, and no root cause for the historical intermittent native teardown fatal has yet been established.

## Native responsive samples
Three separate native responsive processes also completed all 893 checks and exited zero. Their runtime logs retain an ALSA missing-output-device error followed by fallback to the dummy audio driver. They do not contain the historical `script_bindings` fatal. These are scoped clean teardown samples, not evidence of a root-cause fix or a guarantee against future intermittent failures. Native host repeat coverage is recorded below.

## Native host result
The repaired and extended host fixture completed in a native Godot window and exited zero. It retained the original generation-replacement, catch-up/scope, full-wave companion, restart and presenter-disposal checks, plus three repeated starts, nine codex open/close cycles and pause/resume assertions. The log contains only the unsupported V-Sync warning, not an ERROR, exception or `script_bindings` fatal. Audio used the explicit dummy driver; no audio verification is implied.

## Conclusion and limits
Two stale fixture assumptions are fixed and the scoped UI/host checks are usable again for runtime diagnostics. No production runtime change was needed or made. The historical intermittent native fatal was **not reproduced**, not diagnosed or proven fixed. The strict editor-import gate remains red and has not been suppressed. Neither the test-controlled SceneTree quit nor the queued-free checks certify manual window-close interruption, every menu/input timing, physical controllers, Android devices or performance. Keep the historical issue open for an exact crash trace if it recurs; do not make speculative disposal changes from clean samples alone.
