# Actual-displacement Run — isolated WIP

**Status:** bounded technical and sampled-pixel review completed; the full-width forward-Run/tap-stop video is awaiting user motion/style review. Cadence and airborne style are not yet accepted. No production integration, lateral/backward controller or moving-fire acceptance is implied.

## Scope and authority
This is one fixed-heading forward Run presentation prototype, not the moving-fire controller or a production replacement. Normal startup does not enable it. The old in-place action sample was rejected for skating; improved leg visibility alone was not acceptance. Static proportions remain accepted and unchanged.

A/B use the same rebound rig, camera and Core input/event lifecycle. Core speed remains 4.8 world units/s, uniform model scale 1.35, with no collision/gameplay jump, root correction or leg scaling. The component owns seven lower pose bones. Genuine upper-local Attack tracks, morphs and event timing remain native; descendant world transforms naturally move with the pelvis.

Runtime rig SHA256: `081a1e5472f1ebebd5e8ecd633acc158c5b84ae50f4d4dd1f5c4ce12082e5041`.
Current component SHA256: `59a33f326c3c4344566243499c7af03703625be853a03d8a66bd12062f69fdfa` (source checkpoint `e2c6cce21ec90cee1e5955a981a8602cabf12cf1`; subsequent capture-helper commits do not change it).

## Motion design
Phase follows actual rendered XZ displacement, not elapsed animation time or nominal Core speed. At steady speed, alternating falls are 1.2 world units apart / four falls per second. Contact is 0.05 s (three 60 Hz intervals, four endpoints), followed by 0.20 s flight: **80% flight**, a deliberately airborne cartoon run, not continuous walking.

Contact hip-to-ankle height is 0.18 world units. Dynamic crouch is approximately 27.766 model mm / 37.485 world mm; visual body arc 50 world mm, swing lift 40 world mm. These do not alter approved mesh/rest proportions. Full rigid-foot transforms are anchored during declared stance. Two-bone IK rejects unreachable targets; it does not clamp them or stretch bones.

Restart begins from evaluated foot/body pose and measured render-to-render velocity. A bounded 50 ms bridge rejoins the existing distance curve. Special downward braking is used only when actual sole clearance and descent require it, avoiding repeated unnecessary braking during one-tick taps.

Interrupting that bridge or stopping immediately after a short initial tap now uses explicit lift-and-settle: a planted foot lifts from its world anchor, then moves to a reachable neutral landing target. Airborne foot braking accounts for relative 3D foot/hip velocity as well as ground clearance. Root motion is unchanged. The initial 3D bound is not treated as proof of every future point: runtime reach/ground guards and measured samples remain required.

## Fresh validation and limits
Tests use .NET 10.0.403 / Godot 4.6.3 Mono. Evidence is not inferred from unavailable earlier recordings.

| Evidence | Result |
|---|---|
| Current tap candidate: 16 A/B sequences, 195 ticks each | Build and runtime/independent CPU-skin verifier passed. Starts from idle or steady Run; one-/two-tick pulses and repeated alternating input; ends moving or fully stopped. |
| Current tap authority/camera, upper-local poses, morph/event timing | Exact A/B parity. |
| Current tap actual CPU-skinned stance drift | <= 0.000001416 world units; minimum reach reserve 0.001818035; minimum floor residual approximately -0.000000127. |
| Current boundary motion | Maximum sole movement 0.122474 world units per rendered tick. This is reported motion, not visual-continuity certification. |
| Current candidate regressions | All 28 bounded restart timings passed (26 real restarts plus two zero-interval controls), four steady A/B runs, pause/reset during active resume and tap-stop, and stationary-facing probes 225/231. |
| Current candidate native captures | Running one-tick alternating taps and idle short-tap stopping: four A/B recordings, all 195 simulation ticks preserved. Native CPU skin/locomotion match headless exactly; A/B authority/camera match; each stream has 195 post-draw attachment samples with zero measured transform error. |
| Frozen earlier native candidate (`c060db…`) | Flight stop, restart delay 4 and worst boundary delay 1 recorded. Native CPU skin matched headless; post-draw attachment errors measured zero. This is not native validation of the new tap-stop code. |

Actual sole checks use a separate CPU-skin readback, not target-to-target comparison. Duplicate material vertices are not independent contacts or GPU contact certification. Bridge initial velocity is a measured finite difference; the whole discrete system is not claimed analytically C1.

Native source AVIs contain one repeated final stamped frame each. Comparison tooling records every original stamp-center RGB sample, rejects ambiguous values and preserves all unique simulation ticks. Only repeated terminal encoded frames are omitted. The archived three-chapter side-by-side experiment totals 570 frames / 9.5 s; the current primary review uses full-width A then B with Chinese labels: four streams each preserve 195 unique ticks (one repeated terminal encoded frame removed per source), totaling 780 frames / 13 s at 1280x752 including the label margin. Full decode and stamp mapping passed. Videos and private traces are not Git payloads.

Runtime logs were clean for both the frozen reference and current tap native captures. Editor import still emits its existing `EditorSettings` shutdown diagnostic; this is not an all-logs-error-free claim.

## Counterevidence retained
- Original in-place gait: rejected for skating.
- Earlier contact prototype: quick restart failed; fixed only within tested bounds, not retroactively called passed.
- First tap-stop revision: 12/16 completed; one-tick repetition repeatedly restarted an unnecessary vertical brake.
- Conditional-brake revision: 14/16 completed; two idle alternating cases exposed a real 3D reach failure as vertical separation changed. No guard was removed to pass them.
- Current 3D-bound candidate: all 16 tap cases and stated regressions pass; fresh native capture/parity checks pass. Actual appearance and continuous normal-speed grounding still require review.

The prototype remains fixed-heading and bounded to tested timing/render steps. No arbitrary-turn, general variable-framerate, all-direction controller, release, integration, APK or user-animation acceptance is claimed. Further ordinary-input failures must be fixed rather than relabeled acceptable limitations.
