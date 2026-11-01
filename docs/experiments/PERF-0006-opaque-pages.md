# PERF-0006: opaque page render suppression — isolated study

Status 2026-10-31 21:41 UTC: authorized after camera trial; not implemented, not adopted. Baseline is immutable `HMI-Game-health-final`. No gameplay, camera, persistence, collision, or animation-authority changes.

## Observed opportunity

GameUi.SetPage currently disables main viewport 3D only for selection. Between-wave/shop, finite ending and endless decision pages draw the same opaque RGB candy-city TextureRect over the world. The asset is 1672×941 RGB, drawn at logical (0,0,1280,720). Fixed UI layout plus canvas_items stretching means arbitrary aspect ratios may expose world beyond this rectangle; blanket noncombat suppression is not pixel-equivalent.

## Conservative candidate contract

- Disable main viewport 3D for covered opaque noncombat pages only. Keep existing selection semantics unless separately justified.
- Require actual visible viewport extent to be covered by the active backdrop's rectangle in matching coordinates, with identity/modulate assumptions explicit. Unknown coverage falls back to drawing world, not hiding content.
- Re-evaluate after viewport resize/page replacement. Detached/queued backdrops cannot grant suppression to a newer page.
- Combat, pause, combat codex and combat persistence modals preserve world rendering. Noncombat overlays inherit the covered page policy, without disabling their own UI.
- Restore original main-viewport state on UI exit. Never destroy world actors, change simulation phase, skip authoritative ticks, advance paused animations, or alter SubViewport resource ownership to obtain counters.

## Gates before adoption

1. Actual-engine structural probe: selection→combat→pause/codex→between/shop→combat→ending/endless decision→selection, repeated cycles and same-frame transitions. Check main viewport flag, live actors/HUD previews, input focus, paused clip clock and no stale backdrop references. Resize to wide/tall/standard logical extents: uncovered page must preserve world.
2. Native fixed-state A/B screenshots with identical actors/camera/lights/UI, baseline rendering forced on versus candidate covered-page suppression. Decode and compare pixel arrays. Pause/codex remain visually unchanged.
3. Actual FramePostDraw counters after warmup, same page/geometry: reported draw calls and primitives, separate from authored triangle counts. Do not call renderer counters phone performance. Time data if collected has llvmpipe/order/VSync caveats.
4. Native real page transitions after camera slot; all applicable aggregate tests against the final candidate. No merge based on fixture-only aesthetics.

## Stopping / decision

Adopt only if covered pages are pixel-equivalent, world is restored without flicker/stale state, regression suite passes and renderer counters show avoided work. Otherwise retain baseline with measured failure. Camera experiment remains a separate decision and source delta.

## Fresh projection observation (21:43 UTC)

Actual pinned-engine headless Window probe copied the current project settings. At physical1280×720,1600×720,720×1280 and1024×768, `GetVisibleRect()` remains logical1280×720; `content_scale_aspect=1` and final transform show letterboxing. Thus current default settings DO cover the full rendered viewport; the earlier expansion concern was a hypothesis, not observed bug. Native pixel equivalence still must confirm letterbox output. Probe source/log are adjacent. Prefer a minimal known-page condition over a generic rendering framework; coverage fallback can protect explicit future aspect/layout changes if actually exercised.

## Approved minimal implementation scope (21:51 UTC)

Following the real viewport probe, the implementation will change only GameUi.SetPage's main viewport Disable3D expression from `selection` to `selection || between || ending`. No coverage framework or automatic layout fallback is needed for the current fixed logical1280×720 keep-aspect contract. The proposed generic fallback above is superseded by this narrower scoped decision. If future project settings switch to Expand or change backdrop coverage, that contract must be revisited; tests will expose the unsupported assumption rather than claiming arbitrary layouts safe. Combat pause/codex/persistence overlays continue rendering the world.

UI worker owns isolated HMI-Game-menu-render-trial. Required native A/B uses one frozen scene, then only viewport toggle, with equal post-draw warmup; pixel differences cannot be dismissed as animation without investigation. No change to frozen HP or unadopted camera roots.

## Implemented isolated contract (21:58 UTC)

The parent approved a narrower implementation than the early general coverage proposal: exactly one existing `SetPage` expression now suppresses main 3D for `selection || between || ending`. No runtime coverage framework, camera candidate, Core, persistence, model, animation or input changes were added. Unknown pages still render the world. This supports the pinned project's verified logical 1280×720/letterbox contract; a future layout/aspect/translucent-backdrop change must revalidate the optimization, not assume an automatic runtime fallback.

The fixture uses the actual host/UI, visible opaque RGB backdrop, normal saved run setup in a fresh XDG directory, and frozen actual world actor/AnimationPlayer instances. It checks same-frame/repeated transitions, combat/pause/codex/persistence-modal world rendering, preview resource counts, actor and AnimationPlayer identity, paused/resumed clock, unknown-page fallback and both prior viewport states on UI exit. The initial fixture red was a missing actual `RefreshUi` call: `ShowCombat` alone creates two companion previews; the hero preview is created by `UpdateHud`. The fixture now follows the actual host refresh path rather than changing production to satisfy a test.

Native A/B will compare 16 pairs: between/shop/ending/endless across four requested physical sizes. Every variant gets eight `FramePostDraw` warm frames, the same scene/camera/lights/UI and RGBA8 conversion before exact byte comparison. Requested and actual window/framebuffer sizes are recorded separately. Partial results and original screenshots are written before any mismatch failure, so a red pixel gate cannot erase its evidence. Renderer draw-call/primitive counts are not claimed as phone performance or wall-time speedup.
