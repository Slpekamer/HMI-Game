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
byte-identical. No runtime C# source, Core/Persistence, gameplay, player/companion asset,
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
branch is not merged, release-ready, or an APK delivery. No unrun visual gate is
treated as passed.

## Fresh scoped verification

Executed with Godot 4.6.3 Mono and .NET SDK 10.0.403:

- The baseline build and isolated candidate builds passed with zero warnings or
  errors. Baseline/candidate strict editor import passed without `ERROR:`,
  exception or resource-leak diagnostics.
- Material-only binary/JSON contract and deterministic regeneration passed.
  Candidate GLB SHA256 is
  `ecf3ffa0b5696fdef1773572b4d375bd4177f8af5c19024f543f20e4390023a1`.
  The unchanged geometry/animation BIN chunk SHA256 is
  `c25e9db7cd5fcffc630e7d511065925f9818c3972ca058f0a0b53836c1e65c57`.
- `scripts/test_enemy_brass_runtime.py`: **34 checks passed** on the actual
  imported enemy. Imported PBR/color values, opaque nonemissive material,
  per-instance damage overlay, pause/expiry restoration, peer isolation, cache
  release and instance destruction were checked. These are resource/state
  assertions, not rendered-pixel or hit-contrast evidence.
- Existing asset-cache lifecycle: **351 checks passed**. Existing enemy damage
  semantics: **41 checks passed**, including cooldown, actual dark intervals,
  stale identities, death-before-render and disposal.
- Portable Core: **646,403 assertions passed**. Architecture and nine preserved
  notice checks passed. No Core or persistence source changed.
- Matched headless normal Crit route: **300 frames at 30 Hz**, seed 1030,
  unchanged inputs and ten seconds. Seven nonempty authority, bone/morph pose,
  camera/root, foot, boot, presentation and substep traces were byte-identical.
  The route contains two melee events and zero player projectile launches.
  Native post-draw attachment traces and the original native video verifier
  were **not run**; headless output is not a replacement for them.
- The older `IntegratedDamageHostProbe` and `IntegratedDamageFirstDrawProbe`
  still **fail** at their new-run assumptions (`actual durable new run started
  with original stats` and `initial reference differs`). Both also fail in the
  same isolated project after restoring the exact baseline enemy asset and
  reimporting, with fresh separate data folders. Their templates omit the current
  fresh-run consent sequence. No original assertion or strict diagnostic was
  removed. These failures remain open, so the integrated damage aggregate is red.

No native comparison video was produced. Gameplay-sized brass readability,
orange hurt-tint contrast, front/back recognition, crowd/wall-shadow behavior,
performance, physical devices and owner acceptance remain unverified. No merged
or production recommendation should be inferred from the passing scoped tests.
