# 2D selection — independent native review
2026-10-31 06:30 UTC. Root: HMI-Game-2d-selection. Scoped gate: PASS.

Actual Godot.NET native llvmpipe run, ordinary mouse/keyboard controls:
- All five cover-derived portraits readable, proportion preserved, within cards; yellow hero bright yellow. No portrait overlap with labels.
- Clicked all five: selected badge and trait summary follow correct identity.
- Tab focus then Enter activated Cotton correctly.
- Cotton start restores visible 3D arena/HUD and movement; Escape pause and restart return intact 2D selection.
- Pink second start correct identity, HP100/100 and coins0. Codex Tab/Enter close followed by Escape pause works. Second restart retains portraits.
- Native log has no managed exception. Environment VSync unsupported and ALSA dummy fallback remain disclosed.

Evidence: selection-2d-cotton.jpg, selection-2d-honey.jpg, selection-2d-start-combat.jpg, selection-2d-native.log.
Read engine SelectionPortraitProbe: all5 identity/seed, repeated starts, zero UI3D nodes/viewports in selection, actual queued-free HUD resources, root rendering state restoration assertions. Those structural properties are supported by engine tests, not inferred from screenshots.

Limits: no new mobile/device/performance test; no independent full six-wave replay for this UI-only change. Existing pink in-combat 3D HUD portrait crops top of head; pre-existing outside selection scope, disclosed not silently fixed. New plush models and rejected offline animation not integrated/approved by this gate. Desktop released, source not edited by reviewer.
