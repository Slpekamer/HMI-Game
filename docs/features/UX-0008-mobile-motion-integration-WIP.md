# UX-0008 mobile input + UI motion integration — WIP

This independent integration starts from mobile R4 `ae9ac5fe56b5926ac23ccf50d7686a4c4a89a93c` (itself based on published UX0004 `f4aa1e45`). It adds the independently reviewed UI-MOTION-0001 R2 presentation controller and reduced-motion preference. It does **not** include OBS0001 or combat-feedback/VFX work, sound, engine upgrades, or a new Android export.

## Conflict resolution contract

Only GameUi.cs overlaps. Layout updates retain mobile input cancellation and add motion cancellation. Focus changes retain input ownership clearing and add cosmetic suspension. Page changes retain navigation epochs, hidden screen-scroll behavior, pointer cancellation, and add motion epochs. `_Process` retains both input hints and preview clock before motion advancement. The instructions modal preserves floating/gamepad instructions and adds the accessible reduced-motion toggle. All native hitboxes remain controlled by existing layout, never animated.

The new preference persists only `ui/reduced_motion` in presentation.cfg, preserving unrelated keys; it never edits a checkpoint. Unknown/corrupt preference data is not silently overwritten. Independent test XDG directories are mandatory.

## Evidence boundaries

Separate mobile R3 native 216/10 images and R4 safe-area red→green 28, plus UI motion normal two-process preference tests, are prerequisites, not evidence that this merged assembly has passed. Merged build passed with zero warnings/errors; an initial wrong-directory build invocation is retained outside this tree. Targeted strict runners and a cross-feature input test are in progress. Existing EditorSettings shutdown ERROR and intermittent ResponsiveUiProbe runtime abort remain unresolved; do not publish or mark a complete aggregate accepted merely because smaller tests pass.

Real phone fullscreen, physical touch, USB/Bluetooth gamepad compatibility, and performance remain unverified. Desktop Joypad event injection is not physical controller certification. Original shared resource ownership and HUD preview shutdown paths are unchanged; no GC, arbitrary delay, warning suppression, or borrowed-resource disposal is introduced.

## Current scoped result (not formal release)

Six exact merged-source behavior probes pass separately: motion174, drawn hooks22, preference29, floating130, gamepad206, safe-area28. Each strict runner stopped at the retained editor shutdown ERROR before its separate behavior execution. One Responsive893 behavior attempt exited clean; this is only a sample and does not resolve the historical intermittent fatal. Full aggregate executes Core624665/assets/imported probes/smoke61, then exits1 on the same editor ERROR; later checks are not falsely counted as aggregate-executed.

Joint test found missing focus on closing instructions; old Focus implementation fails exactly that assertion. Once-per-navigation-generation restoration now preserves any valid current-scope focus (including a newly mouse-selected SpinBox), and restores a safe menu button when absent. Exact joint30 and four remapped-label supplemental assertions pass. Native four focus screenshots and normal mouse main confirm toggle/close/navigation, floating drag/release, pause/restart and clean WM exit. These are injected generic Joypad and desktop mouse, not physical gamepad or Android.

A final one-line configuration consistency fix reads the existing confirm/cancel labels in help rather than hardcoded defaults. Default generated text is unchanged from the normal-main screenshots; updated joint native explicitly tests swapped bindings and help/Hint agreement. Final main build remains 0 warnings/errors. No visual claim is made about unrelated VFX, audio, character gait or Android shadows.
