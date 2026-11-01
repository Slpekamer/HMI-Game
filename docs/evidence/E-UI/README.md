# E persistence presentation evidence

This is a pure UI increment in the lead-owned checkpoint integration tree. GameHost/coordinator/Core are owned and tested separately. ResumeUi carries display text/eligibility only; there is no save-file access or authoritative gameplay transition in GameUi.

The isolated runner compiles only GameUi, ResumeUi, CandyTheme, TouchJoystick, AssetVisuals and the test fixture, and uses an absolute per-run XDG_DATA_HOME. Its result cannot establish durable writes or host integration.

## Red gates and corrections

1. Initial focus assertion incorrectly assumed sibling buttons could share the exact same Godot node name. Diagnostic inspection showed PersistenceAction then auto-renamed PersistenceAction2. The assertion now checks the same modal parent and action prefix.
2. That correction exposed a real third-Tab escape to SelectionSeed's internal SpinBoxLineEdit. Existing background locking skipped internal children. LockBackground now traverses GetChildren(true), and persistence actions have an explicit next/previous focus ring. A direct internal-editor FocusModeNone regression complements focus cycling. The original red logs are retained.
3. One manual diagnostic launch supplied a relative XDG_DATA_HOME; Godot ignored it, failed to create its restricted default data directory and crashed before executing the UI fixture. No host/store was compiled into that project. Re-running with absolute isolated data/config directories reproduced the actual focus issue. The committed runner always uses absolute isolation; no ordinary user save is read or overwritten by the UI fixture.

The short independent HUD status uses coordinator SaveStatus without inferring meaning from text. Stable runtime strings plus content/fonts/runtime-text-extra.txt are checked against the shipped font cmap; unchanged OFL and all previous glyphs are preserved. Final host/coordinator strings must also pass the aggregate coverage gate.

## Scope

Coverage includes same-state no-redraw, explicit resume rollback confirmation, replacement cancel/one-shot, long error scrolling, failed-save Escape blocking, repeated/stale retry callbacks, explicit unsaved continuation that does not itself resume gameplay, terminal-background preservation with no ordinary Continue, voluntary exit confirmation/cancel returning to pause, corrupt-state explanation without deletion, full button bounds and modal focus. Native capture and full host persistence integration remain separate gates until their reports are added.

## Fresh pure-UI result, 20:16 UTC

`python3 scripts/test_persistence_ui.py` completed build/import/run exit0 and PASS110 on the final UI source, including internal SpinBox lock and focus ring. Fixture root suffix hmi-persistence-ui-_jkxvoqf. Native rendering review and real coordinator/storage lifecycle remain pending; this count is not evidence of a successful disk transaction.

## Native semantic red gate and revised contract

The first seven native images passed mechanical rendering but exposed inaccurate generic unsaved copy: the first snapshot had failed, yet the UI promised a previous safe point. The coordinator's tightened policy only permits unsaved continuation after a durably confirmed PendingStart with no first checkpoint. Once chosen, this run never auto-saves later waves. The revised button says “继续本局（不保存）” and explicitly warns that no recoverable progress will be generated, later waves will not auto-save and exit requires a new run. Later-wave failures remain Retry-only. Exit confirmation also branches on coordinator CanContinue: without an eligible safe point it must say the run cannot be recovered, never promise preservation. Cancellation/return action text is now dark ink on its light background. Fresh tests/native review are required for this revision; first images are not relabelled as final acceptance.

The reviewer also requested a retained-old-button boundary: the original selection Start and pause Restart lambdas read the latest event delegate. The common Btn wrapper now rejects intents from removed/queued/hidden/disabled controls before invoking the delegate. This is a view-lifecycle guard, not a gameplay rule. The pure UI regression explicitly retains each old button, changes pages without awaiting QueueFree, emits Pressed, and requires no Start/Restart intent; the current visible Start must still work.

## Revised pure-UI result, 20:21 UTC

Final r3 source passes114 checks (build/import/run exit0), including retained-old-button signals, the corrected whole-run unsaved semantics and no-safe-point exit warning. `ui-source-hashes.json` records this exact source/font. Latest native comparison is requested separately; earlier mechanical117 native count does not erase the semantic red gate.

## Final focus contrast correction (r4)

Actual r3 pixels still showed white focused cancel text despite normal/hover/pressed ink overrides. The focused theme state was missing. The local light PersistenceAction style now explicitly sets font_focus_color and font_hover_pressed_color to Ink; the global theme is unchanged. Fresh r4 build/import/run passes115, including an actual focused-button theme assertion. Runtime source hashes above now identify r4. Native pixel confirmation is still required, not inferred solely from setting a property.

## Native pure-UI closure, 20:24 UTC

Independent actual cloud native review completed r4 PASS122 (115 behavioral checks plus seven actual image saves). The corrected unsaved/exit semantics were inspected in r3; r4 rechecked the focused cancellation/return buttons and verified dark ink in actual pixels. Seven r4 JPEG frames and the native log are retained here. This closes the pure-presentation scope only: real host/coordinator transaction ordering, restart recovery, file durability, device behavior and integrated regressions still require their own gates. The fixture's unrefreshed default HUD name is not a production gameplay snapshot.
