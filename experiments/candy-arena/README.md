# Candy arena art candidate

Independent presentation work based on `4030b405`. The final grid-free, original-speed comparison passed owner visual review. This is not merged or a release/device certification.

## Requirement and scope

Improve the playable candy-world art without replacing the five hero designs, reviewed Crit movement/launch layers, menu concept, or mechanical enemies. The baseline used sharp boundary cubes, block-and-sphere buildings, a uniform ground color and plain blue obstacle sides.

- Round the 88 original boundary blocks, retaining their centers, palette order and maximum extents. Original 5 cm gaps remain; rounded corners visually widen the opening only near each corner, not the collision boundary.
- Replace 18 outer block buildings with rounded cake houses, cream bands, biscuit feet, portholes and restrained roof sprinkles. Scenery stays outside `|Z| > 10.7 m`.
- Add a pale, low-contrast poured-icing inlay with organic edges and no grid, keeping the center free of props and new targeting/radius cues. Its surface is `Y=.0005 m`, below the unchanged petal bottoms (`.001 m`) and honey surface.
- Retain the original 20 blue GLB obstacle tiles and their decorations. Add cream/caramel side reveals, inset from the obstacle ends, with a declared 1 mm outward face margin to avoid z-fighting. Height, top surface and authoritative AABBs do not change.

There are no Core/Persistence, character asset, animation, projectile, UI, camera or light changes. The accepted `.015` shadow split remains selected. Hard existing shadows and existing contact/crowd occlusion are not fixed here.

## Design and compatibility

Geometry and standard materials are generated once by `CandyArenaVisuals`. 124 original scenery nodes become 23 instance batches; four wafer batches and one floor plane bring this candidate to 28 nodes, excluding unchanged arena/actor nodes. This is a node-count observation, not a frame-rate claim. No shader, new physics body, engine plugin or external asset dependency is added.

The floor SVG is original explicit-shape vector art. A tiled-floor trial was rejected in review because the user had previously requested no ground grid; it is not the final candidate. An earlier local pattern-based SVG imported as black in Godot, so that trial was rejected. The final asset uses explicit vector shapes and the native probe reads actual imported pixels to prevent that regression. The imported 1400×800 texture adds a texture-memory cost; no mobile budget claim is made.

Alternatives rejected for this batch: extra center props (occlusion), target-like central rings (gameplay ambiguity), lighting/camera changes (would invalidate the approved character comparison), and replacing the obstacle GLBs (unnecessary art/geometry scope).

## Verification

- Fresh isolated .NET build: passed with 0 warnings and 0 errors.
- Native Godot 4.6.3 Compatibility / Mesa software-rendered geometry and lifecycle probe: **94,573 checks passed**, including rounded mesh winding/normals, finite geometry, exact boundary envelopes, outside-arena village bounds, opaque/light imported floor pixels, dual-instance ownership and destroy/rebuild survival.
- Strict import gate: **failed**, retaining the existing `EditorSettings not instantiated ... export/android/shutdown_adb_on_exit` diagnostic. The runner reports import and runtime separately and returns failure for this aggregate. The runtime pass does not erase the import failure.
- Native 1280×720, 30 Hz, original-speed 10-second normal Crit route: 300 simulation/presentation frames. Baseline and art candidate have byte-identical authority, bone/morph pose, camera/root transform, foot/boot geometry, presentation, substep and attachment traces. This proves only the captured route.
- Architecture and preserved-notice checks passed. Visual review uses actual rendered frames/video, not generated concept imagery. The floor, actors and shadows remained readable in the sampled movement/combat frames; the final grid-free comparison subsequently passed owner visual review. This does not broaden the technical test scope.
- Fresh portable Core regression: 646,403 assertions passed. Core source is unchanged; this is not a new balance or full-wave playtest.

Known limits: not all heroes, portrait framing, late crowded waves, physical Android devices, physical controllers, hardware performance or every honey/projectile readability case. The 10-second route includes melee but no actual ranged launches; it is not new projectile validation. No Android package is delivered here.

## Reproduce

Use the repository build instructions. Run `GODOT_NET=/path/to/godot-mono python3 scripts/test_candy_arena.py --native` on a native rendering display; omit `--native` only for the reduced headless geometry checks. A native software renderer is acceptable for geometry/pixel evidence, not performance certification.

Prepare/import the baseline `4030b405` and this candidate in separate worktrees. Capture each with `GODOT_BIN`, `CANDY_REVIEW_ROOT` and a fresh absolute `CANDY_REVIEW_OUTPUT` set, using `bash experiments/candy-arena/capture.sh`. The capture script intentionally requires an already built/imported project and does not silently waive import failures. Compare outputs with `python3 experiments/candy-arena/verify.py BASELINE_OUTPUT CANDIDATE_OUTPUT`. Keep generated recordings, user data and build/import logs outside Git.
