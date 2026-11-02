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
