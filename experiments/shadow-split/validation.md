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

## Visual finding and user review
Camera-compensated consecutive frames 40–55 show the static foreground tower's
coarse shadow contour substantially smoother/more consistent in B. This is
promising evidence beyond a still-image sharpness comparison, not a declaration
that all flicker is fixed. B also makes shadows visibly harder and more defined;
the user approved shadow B's visual style on 2026-11-03. No obvious new acne was
seen in inspected frames, but this is not exhaustive spatial coverage.

Played arena-side routes and general aspect-ratio coverage remain unvalidated;
the bounded paused landscape/portrait coverage completed later is recorded below. Do not
merge or claim production acceptance from this initial route. If original-speed
review finds continuing instability despite sharpness, stop this candidate;
do not expand into a parameter sweep.

## Measurement limits
One cloud desktop Compatibility/llvmpipe capture each: A reports mean CPU/GPU
render 1.29/2.85 ms per frame; B 1.48/3.19 ms. These are single software-renderer
samples, not a controlled performance benchmark or 8155/Android/device claim.
Unchanged atlas size does not establish unchanged GPU cost.

## Bounded side-view coverage result
Fixture checkpoint: `bad23045e2d7c4d9b7ec533e17f1552a15b722aa`.
Native Compatibility/llvmpipe coverage completed successfully:
- Four side focus positions, five neighboring 8-cm samples each, at landscape
  1280x720 and portrait 720x1280. Both A/B variants: 80 original-resolution PNGs
  in total, with three identical raw framebuffer hashes at every view (240 reads).
- A/B actual camera position, basis, size, near/far, projection, paused state and
  frozen simulation tick match exactly. Recorded light properties match except
  Split1; shadows remain enabled. No production camera or motion source changed.
- Actual recorded camera ray/floor intersections, not assumed edge reachability:
  landscape receiver depth 22.594507–39.114487 m; portrait 16.170072–45.538923 m.
  B first cascade ends at 60.04925 m. All four viewport corner intersections at
  every sampled pose lie inside the existing 80x80 m floor; largest absolute X/Z
  is 32.106674 m. `verify-receiver-depth.py` reproduces these bounds from manifests.
- Center A/B boards, all B neighboring-view boards, and original-resolution
  representative landscape/portrait images were inspected. Visible environment
  and hero shadows remain; no obvious new acne, missing shadow band, or abrupt
  coverage dropout was seen in this bounded set. This is not a claim that all
  subpixel aliasing has disappeared, or that every unseen/offscreen caster was
  tested. The visible floor stays within the first cascade, so these checks do
  not intentionally cross its boundary or certify other camera configurations.

These are labeled **frozen paused-world coverage diagnostics, not played routes**.
The player was not moved to each edge, and no traversal/gameplay claim follows.
The user approved the reviewed shadow B candidate's visual style on 2026-11-03.
Harder, more defined shadows remain an explicit appearance tradeoff; softness
was not tuned. This approval does not broaden the measured coverage or authorize
a production merge. Runtime code remains frozen. Production integration,
all aspect ratios, arbitrary scene changes, and Android/8155 remain unvalidated.
