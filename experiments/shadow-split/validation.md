# Initial original-camera shadow A/B — partial validation

Rendering/test baseline: `4d25af4e9cacc7ec81e822452b3eeddc12c6b11b`.
Only candidate property: first directional shadow split 0.1 -> 0.015.
Both variants retain the approved displacement Run, original 1280x720 camera,
all shadow casters, atlas size, remaining light properties, Core and event timing.

## Verified
- Godot 4.6.3 Mono build: zero warnings/errors.
- Import exits successfully but emits the known editor shutdown diagnostic
  `EditorSettings not instantiated ... export/android/shutdown_adb_on_exit`.
  Native runtime logs have no errors/leaks; import is not described as error-free.
- Headless A/B: 195 ticks each; authority, camera, pose, motion, sole and
  locomotion files byte-identical. Same six files also match the approved Run
  native reference. Native A/B and native/headless match those same six traces.
- Both native runs contain 195 post-draw attachment samples with zero
  position/basis error. Each movie has 196 encoded frames; all 195 unique
  simulation stamps are retained, omitting only the repeated terminal frame.
- Sequential comparison: 390 frames, 60 fps, 6.5 s, complete 1280x720 image plus
  32-pixel Chinese label header; decoded successfully. No gameplay crop,
  interpolation, rescale or skipped unique simulation tick.

## Visual finding and remaining gate
Camera-compensated consecutive frames 40–55 show the static foreground tower's
coarse shadow contour substantially smoother/more consistent in B. This is
promising evidence beyond a still-image sharpness comparison, not a declaration
that all flicker is fixed. B also makes shadows visibly harder and more defined;
that style change needs full-size normal-speed review. No obvious new acne was
seen in inspected frames, but this is not exhaustive spatial coverage.

Four arena-side routes and portrait/aspect-ratio coverage remain unrun. Do not
merge or claim production acceptance from this initial route. If original-speed
review finds continuing instability despite sharpness, stop this candidate;
do not expand into a parameter sweep.

## Measurement limits
One cloud desktop Compatibility/llvmpipe capture each: A reports mean CPU/GPU
render 1.29/2.85 ms per frame; B 1.48/3.19 ms. These are single software-renderer
samples, not a controlled performance benchmark or 8155/Android/device claim.
Unchanged atlas size does not establish unchanged GPU cost.
