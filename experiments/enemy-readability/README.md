# Enemy brass readability study (not visually accepted)

Independent WIP based on `bbece68bfac2ed7e977ba5ab8f7a4c01e827a98a`.
The proposed change is limited to the mechanical enemy's eye rims, winding key,
shoulder bolts and boots: pale warm brass (`#E7BF72`), metallic 0.2 and roughness
0.55. The intent is more stable small-screen part separation while retaining the
dark plum shell and the existing mechanical toy design. Actual improvement has
not been established by native footage and owner review is still required.

## Scope and alternatives

The GLB changes three PBR values in its single `brass` material. Geometry,
normals, mesh surfaces, animations, root height, bounds and all binary chunks are
byte-identical. No runtime C# source, Core/Persistence, gameplay, character,
camera, light, shadow, approved grid-free floor or other environment asset is
changed. This asset-level method avoids new nodes, shaders, resource owners or
runtime material overrides. The orange damage overlay remains separately owned.

Rejected for this study: model-wide tint (loses part separation), emission or
flashing (competes with real combat feedback), enlarged silhouette/colliders,
ground rings, extra icons and rebuilding the mesh through Blender (unnecessary
geometry and animation drift). Lower metallic and broader roughness are only a
material hypothesis; washed-out feet or weak hit contrast remain possible.

## Reproduce

Extract `content/models/enemy_chaser.glb` from the full baseline commit above to
a separate input file. Run `python3 tools/art/retune_enemy_brass.py INPUT OUTPUT`.
The tool pins the source SHA256 and refuses unknown or already modified inputs.
It preserves every non-JSON GLB chunk verbatim. `scripts/test_enemy_brass_asset.py`
checks deterministic generation, exact allowed changes and both asset manifests.

Native video review must use the unchanged normal-game capture route in
`experiments/candy-arena/capture.sh` with identical 1280x720, 30 Hz, seed and input.
Keep the original speed. Inspect front, side and rear parts at actual play size,
wall-shadow transitions, crowd overlap and genuine nonlethal hit/expiry footage.
Do not accept a zoomed model render as gameplay readability evidence. The existing
authority/pose/camera comparison must remain byte-exact.

## Acceptance status

Pending native comparison and owner review. Headless import and behavioral tests
cannot establish appearance, renderer cost or physical-device performance. This
branch is not merged, release-ready, or an APK delivery. Detailed scoped test
results are added after execution; no unrun visual gate is treated as passed.
