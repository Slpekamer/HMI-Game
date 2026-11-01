# FEAT-0003 — Cover-faithful 2D character selection

## Requirement and boundaries
2026-10-31 owner feedback: Cotton animation still feels stiff; replace selection-screen characters with 2D textures and improve the other four models separately. Selection is now static illustrated art, not an animated/model viewport. No retrospective animation approval. Preserve current card layout, identities, bright-yellow honey and blue Cotton, selection/trait summary/seed/start behavior, ECS boundary and removed Dodge. Other-model static studies remain separate; no new in-game model import in this increment.

## Design before implementation
Use five transparent portrait PNGs keyed by existing stable hero IDs (`orbit`, `crit`, `honey`, `summon`, `bounce`). Source production must inspect the actual approved cover and lineup and retain provenance; do not capture rejected 3D models. Card images use aspect-preserving TextureRects, ignore pointer input, and retain a safe full-silhouette region. No per-frame portrait work or new core dependency. Keep the existing in-combat HUD preview behavior, but create its model viewports only in combat and destroy them on selection/non-combat pages. Character selection must contain no UI SubViewport, Camera3D, light or AnimationPlayer—even after restart. Disable the obscured main-world 3D viewport pass only while selection is shown, and restore it on entering other pages; verified API from installed Godot4.6.3 SDK metadata. This does not change simulation or camera state.

## Acceptance / regression plan
- Fresh core and existing aggregate remain green; core/gameplay source is unchanged.
- Actual-engine test selects all five cards, verifies exactly one selected badge and matching summary, Texture2D resources, aspect preservation and portrait fit.
- Test repeated selection, seed/start identity, combat-to-selection restart, repeated menu lifecycle and queued-free cleanup; no stale model viewport or hidden animation work in selection.
- Native graphical review checks all five images, pointer/keyboard focus, start/restart and modal dismissal; actual screenshot is the user deliverable, not a generated UI mockup.
- Current selection is the root menu, not a new title-page navigation hierarchy. Existing pause/restart and shop/back/codex routes remain the applicable back paths.
- Missing portraits or import failures must fail validation; do not silently restore rejected 3D portraits.

Status: implementation, assets, fresh aggregate and scoped native graphical/input gate passed. Owner visual acceptance pending. Frozen M1–M3 and prior visual revision are untouched. Remote commit remains separately unverified.
