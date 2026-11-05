# Gamepad UI fixture: explicit new-run preparation

## Requirement and baseline

The gamepad regression must traverse the production persistence and consent flow, not bypass it. The unchanged probe failed on both baseline `4030b405` and the integrated candidate at `chooser starts through production checkpoint/session`: fresh isolated storage requires rule-space preparation before combat, while the fixture expected the first start action to enter combat immediately.

## Narrow adjustment

Only `tests/Presentation/GamepadUiProbe.cs.template` changes. Before the existing combat-start assertion, the probe now uses routed gamepad confirmation to request start, explicitly select **确认准备新局**, return to selection, reselect the intended hero and start again. Three added assertions verify that preparation requires consent, does not auto-start combat and cannot replay a held confirmation into the returned chooser. Every original assertion remains. No production logic, persistence behavior or runner error threshold changes; no direct state override or confirmation bypass is used. Existing keyboard and host lifecycle probes establish the same preparation-then-start sequence.

## Verification (2026-11-05)

- Godot 4.6.3 Mono isolated build: exit 0, zero warnings and errors.
- Strict `scripts/test_gamepad_ui.py`: **FAIL**, exit 1. Import exits 0 but reports `ERROR: EditorSettings not instantiated yet when getting setting "export/android/shutdown_adb_on_exit".` The strict runner still rejects this error and does not reach runtime.
- Separately invoked runtime in the same isolated copy: exit 0, `GAMEPAD_UI_PROBE PASS 209`. This is supplemental runtime evidence, not a strict-runner pass.
- Earlier adjacent floating-touch supplemental runtime: exit 0, `FLOATING_INPUT_PROBE PASS 130`; its strict runner has the same import failure.

Headless injected events cover behavioral and lifecycle checks, not physical controllers, operating-system delivery, visual acceptance, device performance or Android export correctness. Controlled between-wave and ending UI callbacks remain fixtures, not evidence of naturally completed gameplay.
