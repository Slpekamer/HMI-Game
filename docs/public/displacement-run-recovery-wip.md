# Displacement/contact Run — WIP, not adopted

The isolated fixed-heading Run fixture preserves accepted static proportions, Core speed (4.8 world units/s), collision and event authority. Normal game startup does not enable it. A retains the rejected in-place Run; B uses actual render displacement, sole-anchored two-bone IK and an explicit cartoon flight phase. This is not the full moving-fire controller.

Component SHA256: `c3e0818169dc9656168d1e83bb7dd4e8c9003a4d317554a85137dff0dc0f9b81`. Runtime rig SHA256: `081a1e5472f1ebebd5e8ecd633acc158c5b84ae50f4d4dd1f5c4ce12082e5041` (unchanged accepted-static rebound asset).

## Bounded parameters
Four alternating footfalls/s at steady speed; 1.2 world-unit forward spacing; 0.05 s contact and 0.20 s flight (80% flight). Dynamic crouch is approximately 27.766 model mm / 37.485 world mm; visual body arc 50 world mm. These are presentation offsets, not a gameplay jump or static proportion change. No hidden leg scaling or unreachable-target clamp.

## Fresh validation, 2026-11-02
Checked clean remote source `495d098a3d8653b112470848692e178f0f2f3373` with .NET 10.0.403 and Godot 4.6.3 Mono:

- Build: 0 warnings/errors; headless import and four A/B flight-stop/stance-stop fixture runs passed.
- A/B authority and camera traces byte-identical; non-lower local poses and runtime morphs exact. Attack starts at ticks 60, 112, 164.
- Separate CPU-skinned sole readback: maximum same-anchor stance drift 0.000001078 world units; minimum reach margin 0.001814796; minimum sole height relative to floor approximately -0.000000127. These are CPU pose checks, not GPU contact certification.
- Forward footfall spacing 1.2 within 0.0000031 world units, 15 ticks between falls. Contact spans three intervals/four endpoints at 60 Hz.
- Both bounded stops settle in 0.20 s. Five actual paused presenter calls leave pose/root/diagnostics unchanged; reset followed by idle passed.
- Stationary facing probes passed 225 and host 231 checks.
- **Quick stop/restart fails at tick 154:** restart during settling is unsupported. Immediate moving reset and arbitrary stop phases are not certified. This is a real open defect, not a passed negative test.

Native appearance, continuous normal-speed plant/flight readability and post-draw attachments remain **unverified in this environment**. Earlier recordings are unavailable and are not substituted for fresh evidence. Do not adopt or present as a ready user action candidate until the visual gate and quick-restart defect are resolved.

Use `experiments/displacement-run/run-check.sh` for opt-in headless/native/lifecycle/restart checks. No integration, production replacement, release or APK approval is claimed.

## Restart bridge candidate — untested WIP
A new bounded 50 ms restart bridge preserves the evaluated foot/body state, continues actual-distance phase and transfers back to the unchanged steady gait. The earlier quick-restart failure above describes the frozen predecessor; this new correction has not yet passed build, interval-matrix or native validation. No fix/adoption claim is made by this source checkpoint. The opt-in matrix covers two stop phases and 0..13 ticks before resuming; native review runner captures full-scale steady start/stop and quick-restart comparisons. Stamp decoder was checked on synthetic 181-frame input with a duplicated final stamp; that is tooling validation, not game footage.

The first restart-bridge runtime matrix completed 26/28 bounded cases; flight-stop restart delays 10 and 11 ticks failed the explicit below-ground guard. This is not a 28-case pass. A 240-tick fixture also hit the actual arena boundary, so the fixture now ends at tick 195 without changing gameplay bounds or root placement. Stopping again within the 50 ms resume bridge remains explicitly unsupported; normal arbitrary input switching is not certified.

A follow-on WIP corrects those two descending-foot cases with an analytic vertical braking segment: duration min(25 ms, actual sole clearance / downward speed), then transition into the existing distance-driven target within the original 50 ms bridge. It preserves initial vertical velocity and reserves at least half the starting clearance during braking. Horizontal motion, body arc, steady gait, root and Core are unchanged. This source revision is checkpointed before rerunning the matrix; do not assume the two failures are resolved yet.

## Fresh restart validation
Tested source `f2d4c675081758a74aabe1663ede41b3343c369f`, component SHA256 `c060db27b3fb286cff785dee749f069d6af5458cb2457d6017258da1be177e3c`:
- Build passed. All 28 bounded 195-tick A/B runs and the independent CPU-skin verifier passed: stop phases 150/153, restart delay 0..13 ticks (delay 0 is uninterrupted control).
- Authority/camera, upper-local poses, morphs and event traces remain exact A/B matches. Actual support sole drift <=0.000001616 world units; reach reserve >=0.00327369; floor residual >=-0.000000127.
- Original quick restart followed by a second stop, pause/reset probe and four steady flight/stance-stop headless runs passed.
- Boundary sole movement reaches 0.122474 world units per render tick; this is reported motion, not a visual-continuity pass. Initial bridge velocity is measured render-to-render, not an analytic continuous-time derivative certificate.
- Native appearance and arbitrary repeated input switching remain unverified. The explicit stop-during-50ms-bridge limitation remains. This is still an isolated internal Run prototype, not production adoption.

## Native archive and next input boundary
The frozen restart candidate completed actual native flight-stop, restart-delay-4 and worst-discrete-step restart-delay-1 A/B recordings. Native CPU-skinned soles match the corresponding headless cases; post-draw attachment transforms match with zero measured error. Runtime logs are clean; editor import still emits its existing `EditorSettings` shutdown diagnostic, so this is not an all-logs-error-free claim.

Each source AVI contains one repeated final stamped frame. The comparison preserves every unique simulation tick (180/195/195) and drops only those repeated terminal encoded frames; three side-by-side chapters total 570 frames at 60 fps. The clean experiment video is archived separately, not included in Git. Pixel sampling found clearer foot exchange and no obvious broken limb, but continuous normal-speed naturalness/grounding is still a visual review gate.

The next isolated WIP adds opt-in tap patterns starting from idle or steady Run, with one-/two-tick releases and resumptions, then either held movement or a full stop. It is intended to expose and fix the remaining stop-during-resume exception, not to declare that limitation acceptable. This fixture checkpoint has not yet validated a new component correction.
