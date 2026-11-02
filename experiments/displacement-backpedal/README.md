# Backpedal displacement pilot — awaiting native review

The user accepted the forward Run/settle motion and static proportions. This separate branch tests **one backward pilot for the same character**, not production integration. Sideways motion and moving-fire heading remain later work; no other hero is changed.

Base: `5c9251e34507a9292112a2a1ed835d8b1c2ee85f`. Approved Run component remains byte-identical: SHA256 `59a33f326c3c4344566243499c7af03703625be853a03d8a66bd12062f69fdfa`. Backpedal component SHA256: `1fb095ec7451766ae328f2a8803e96b4a0b0598c1cef56aa8d8d9e86c140fd2e`.

## Bounded design
Travel axis is explicitly opposite the unchanged body-facing axis. Foot orientation and anatomical knee pole remain body-relative; the character/shoes are not turned to disguise forward Run as backward motion. Actual rendered displacement drives phase. Declared stance anchors remain fixed in world space.

The accepted four-fall/s, 1.2 world-unit alternating step, 0.05 s contact / 0.20 s flight parameters remain unchanged. Core speed stays 4.8 and scale 1.35; no mesh/rest edits, root correction, bone stretching, hidden IK-target clamp or amplitude sweep. This is a deliberately airborne cartoon gait, not continuous walking.

Real rig geometry gives steady theoretical reach reserve 2.0505 mm and boot lateral gap 17.288 mm. The first backward transient trial passed steady/restart checks but failed two one-tick tap cases: a floor-only downward resume brake left the foot too low as the hip moved away. The scoped correction bounds that air-resume brake by actual 3D reach and future hip-rise bounds. It does not alter body/steady amplitude or the horizontal path. Runtime guards remain active; an initial bound is not presented as proof of the full future trajectory.

The approved Run implementation is retained unchanged. The Backpedal adapter is an explicitly isolated derived copy for this pilot, not a silent refactor of accepted behavior. Controlled A/B share the same cosmetic heading, rig and camera; genuine upper-local Attack/morph/event timing remains native. This is not the full moving-fire controller.

## Fresh checks
- Build: 0 warnings/errors. Four steady backward A/B runs, 16 tap cases and 28 restart timings passed (the restart matrix includes two uninterrupted controls).
- Independent CPU-skinned stance drift <=1.221 micrometres world; minimum measured reach reserve 1.270 mm; minimum sole-floor residual approximately -0.000000107 world units.
- Full rigid-boot CPU envelopes preserve a positive body-right separating gap >=17.2858 mm. Maximum evaluated foot-basis error against body-relative rest-flat orientation is 0.000009627; minimum anatomical knee-plane alignment 0.9999999. This does not certify trousers, skin folding or GPU contact.
- Travel/body axes are opposite; actual body-relative movement is backward. Maximum distance-phase accumulation discrepancy is 0.000001493 world units.
- Both Run and Backpedal passed pause/reset probes while resuming and settling.
- A clean remote reconstruction ran all accepted Run steady/tap/restart checks. Across 552 authority/camera/motion/pose/sole/locomotion trace files, the new host path matches the accepted Run reference byte-for-byte.
- Across 92 backward/Run authority trace files with identical input scenarios, Core positions/events/projectiles match exactly. A/B upper-local poses and morph/event timing also match within each gait.

These are bounded numerical and lifecycle results. **Native deformation, reverse-step readability, landing/airborne style and normal-speed appearance are pending.** No backward action or full controller acceptance is claimed.

## Reproduce
Use the dependency setup in `../displacement-run/README.md`. Every runner needs a fresh output directory and a Godot .NET executable.

```sh
DISPLACEMENT_GAIT=Backpedal GODOT_BIN=godot DISPLACEMENT_OUTPUT="$PWD/artifacts/backward-steady" bash experiments/displacement-run/run-check.sh headless
DISPLACEMENT_GAIT=Backpedal GODOT_BIN=godot DISPLACEMENT_OUTPUT="$PWD/artifacts/backward-taps" bash experiments/displacement-run/run-tap-matrix.sh
python3 experiments/displacement-run/verify_taps.py artifacts/backward-taps
python3 experiments/displacement-backpedal/verify_direction.py artifacts/backward-taps
DISPLACEMENT_GAIT=Backpedal GODOT_BIN=godot DISPLACEMENT_OUTPUT="$PWD/artifacts/backward-restarts" bash experiments/displacement-run/run-restart-matrix.sh
python3 experiments/displacement-run/verify_restart.py artifacts/backward-restarts
python3 experiments/displacement-backpedal/verify_direction.py artifacts/backward-restarts
# Existing display only; full-width Chinese A-then-B comparison:
GODOT_BIN=godot DISPLACEMENT_OUTPUT="$PWD/artifacts/backward-native" bash experiments/displacement-backpedal/run-native-review.sh
```

Footage retains every unique simulation tick at 60 fps and records exact frame-stamp mappings; only repeated terminal encoded frames are removed. Source changes are checkpointed before tests. Private runtime logs, environment paths and recordings are not public Git payloads.
