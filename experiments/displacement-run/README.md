# Displacement/contact Run — isolated WIP

The old in-place action sample was rejected for skating. Static proportions remain accepted; clearer calves alone are not locomotion acceptance.

This fixed-heading prototype preserves Core speed/collision, render-root movement, upper-local Attack/morph timing and static shape. Phase follows actual rendered displacement. At steady speed 4.8: alternating falls are 1.2 world units apart (4/s), with 0.05 s contact and 0.20 s flight. Hip–ankle contact height is 0.180, cosmetic body hop 0.050 and swing lift 0.040 world units. Crouch is dynamic, not a changed bind. Full foot transforms and actual sole offsets define contact; no limb scaling or hidden IK clamp.

A uses the rejected in-place Run; B uses the contact component on the same rig. Ordinary short-tap handling preserves evaluated pose and explicitly lifts/repositions a foot when required to settle, rather than waiting for Core/root displacement to catch up.

## Requirements
- Repository-compatible .NET SDK and Godot 4.6.3 **.NET** executable (not the standard non-Mono build).
- Python 3 for numeric checks; Pillow and `ffmpeg`/`ffprobe` for video assembly.
- For native capture, an existing desktop display. Scripts do not start a display server.
- Chinese sequential titles require an installed CJK font. Set `DISPLACEMENT_CJK_FONT` when the default Noto Sans CJK font path is unavailable.
- Each runner requires a fresh `DISPLACEMENT_OUTPUT` directory. Set `GODOT_BIN` to the .NET executable.

## Scoped checks
```sh
# Four steady A/B flight-stop/stance-stop runs and numeric checks:
GODOT_BIN=godot DISPLACEMENT_OUTPUT="$PWD/artifacts/steady" bash experiments/displacement-run/run-check.sh headless
# Reset/pause and original quick restart followed by another stop:
GODOT_BIN=godot DISPLACEMENT_OUTPUT="$PWD/artifacts/lifecycle" bash experiments/displacement-run/run-check.sh lifecycle
GODOT_BIN=godot DISPLACEMENT_OUTPUT="$PWD/artifacts/restart" bash experiments/displacement-run/run-check.sh restart

# 28 bounded restart timings (includes two uninterrupted controls):
GODOT_BIN=godot DISPLACEMENT_OUTPUT="$PWD/artifacts/restart-matrix" bash experiments/displacement-run/run-restart-matrix.sh
python3 experiments/displacement-run/verify_restart.py artifacts/restart-matrix
# 16 ordinary-tap A/B patterns, starting idle or already running:
GODOT_BIN=godot DISPLACEMENT_OUTPUT="$PWD/artifacts/tap-matrix" bash experiments/displacement-run/run-tap-matrix.sh
python3 experiments/displacement-run/verify_taps.py artifacts/tap-matrix

# Primary review: full-width A then B, Chinese titles, two native tap chapters:
GODOT_BIN=godot DISPLACEMENT_OUTPUT="$PWD/artifacts/native-taps" bash experiments/displacement-run/run-native-tap-review.sh
```

The restored steady/restart checks, latest 16 tap cases, pause/reset during resume/settle and stationary-facing regressions have passed the recorded bounded tests. Current native tap capture, stamp mapping and CPU-skin parity checks passed. See [the exact design, test status and retained failures](../../docs/public/displacement-run-recovery-wip.md).

These results do **not** certify arbitrary turns/render deltas, immediate-moving Reset, a full moving-fire controller or production adoption. Numerical foot locking and sampled image inspection do not replace original-scale, normal-speed visual judgment. No release, integration or APK is implied.

Video mapping preserves every unique simulation tick and records original stamp RGB values. A repeated terminal encoded frame is removed explicitly; no movement samples are guessed, interpolated or retimed. Private runtime traces and videos are kept outside the public source payload.
