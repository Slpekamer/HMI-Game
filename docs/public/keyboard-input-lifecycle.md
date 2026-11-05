# Keyboard movement lifecycle

## Requirement and scope

Movement held before pause, codex, focus loss, a new run or another input-context
reset must not silently become fresh gameplay intent after resuming. Gamepad
movement already requires neutral and touch already requires a fresh capture.
Keyboard movement needs the same cancellation boundary. This work is confined to
the Godot input adapter; Core, persistence, camera and presentation are unchanged.

## Reproduction before the fix

At integration baseline `4030b405`, hold physical D during combat, press Escape to
pause, then press Escape to resume without releasing D. The first allowed
`ReadMoveIntent` immediately returns right movement: it obtains a new router token
and submits the still-held physical key. Router generation rejection alone cannot
block that fresh-token submission.

`KeyboardInputLifecycleProbe` reproduces the failure in Godot 4.6.3 .NET using the
production Host, real UI start/confirmation, Escape dispatch and engine physical
key state. Its baseline runtime fails with `held keyboard must not resume after
pause`. The first fixture attempt omitted `Input.FlushBufferedEvents` and failed
its key-state precondition; that fixture issue was corrected before the defect
was reproduced. No production behavior was changed to obtain the failure.

## Regression workflow

Run `python3 scripts/test_keyboard_input_lifecycle.py` with the pinned .NET SDK
and `GODOT_NET` pointing to Godot 4.6.3 .NET. The runner copies the project, injects
the probe only into that copy and isolates editor configuration and user saves.
It retains strict failure on engine `ERROR:` diagnostics.
After a zero-exit import with diagnostics, it also collects the independent runtime
result before returning failure; this does not filter or waive the import error.

The probe covers continuous ordinary holds, pause/resume, codex close, injected
focus notifications, confirmed new runs, every nonempty combination of the eight
WASD/arrow keys, partial releases and gamepad fallback. An opposed-key vector of
zero is not a release. The 255-combination matrix leaves one key held to ensure
partial release cannot rearm movement.

At the baseline, build succeeds with no warnings or errors and the existing
232-check input-router suite passes. The isolated editor-import stage exits zero
but emits the previously documented `EditorSettings not instantiated yet` Android
shutdown error, so the strict runner fails before runtime. Running the already
built/imported isolated runtime separately establishes the specific reproduction
above; it does not convert the strict runner into a pass.

## Fix and compatibility

`ResetMovementInput` now latches a keyboard-neutral requirement. The host samples
all WASD/arrow directions and submits zero for the keyboard until none of those
physical keys are held. It then resumes the existing priority/clamping router
with no extra per-frame allocation or change to ordinary held-key movement.
Checking the resulting vector instead was rejected because A+D, W+S and mixed
arrow aliases can cancel without any key being released. Router-only changes
were rejected because new tokens are valid but the sampled device state is old.

Neutral must be observed while gameplay input is allowed, matching the existing
gamepad contract. Releasing during pause and pressing again before resume still
requires another release. This intentionally affects every shared input-context
reset, including provider/layout cancellation, not just Escape pause.

The corrected native runtime passes 6,460 assertion checks, including the 255
nonempty key combinations and the covered real lifecycle routes. The production
build has zero warnings/errors; Core's 646,403 assertions, the 232-check router
suite, architecture check and all nine preserved notice checks pass. Core and
persistence source remain unchanged. Full aggregate verification is not claimed:
the strict editor-import diagnostic remains a failure, as do previously documented
unrelated release gates. Wave/provider/layout routes use the same reset method but
are not individually certified by this probe.

Event injection proves the covered host/input behavior, not physical keyboard,
controller, OS focus delivery, native appearance or export/device certification.
