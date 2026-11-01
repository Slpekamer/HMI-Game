# Food hero runtime provenance — five-hero derivatives

This repository documents **Kaotuan r23, Buding r3, Juanjuan r9, Feiling r2 and Cuilan r2**. It is not a mobile-performance claim. The manifest records exact delivered GLB SHA256, native/high source hashes, Library identities, texture hashes, contract, and residual close-up limits. Retain history rather than relabel older failures as passes.

## Reproduction and authority

The **postprocessed GLB is the production runtime authority**. Native source is an actual Blender 4.3.2 baked, skinned model; high-poly procedural source is preserved separately in Library, not committed into the game repository. The rejected r12 Blender face split is not an equivalent production source.

1. Materialize the declared native/source bundle from Library and verify its SHA. The exact raw Blender exports are also preserved there, so the runtime postprocess can be reproduced independently of exporter serialization differences.
2. Optional native re-export:
   `blender -b SOURCE.blend --python tools/art/export_runtime_glb.py -- --output raw.glb`
3. Kaotuan:
   `python3 tools/art/runtime_postprocess.py raw.glb split.glb --split-local-morph --repair-zero-tangents --mesh-prefix Kaotuan --report split.json`
4. Buding uses the same command without `--mesh-prefix Kaotuan`.
5. Both:
   `python3 tools/art/clamp_atlas.py split.glb hero.glb --report clamp.json`
6. Run repository static validation, actual Godot import/morph/instance/root probes, and actual close-up + game-scale visual checks. Never infer visual correctness from JSON alone.

The portable pipeline has no cloud absolute-path dependencies. It needs Python 3 and NumPy. It preserves triangle-corner values, uses a 1 micrometre motion-selection tolerance to exclude floating-point noise, adds two untouched boundary rings, compacts unused rows, and repairs only demonstrably zero tangents from real incident-triangle UV derivatives. If a repair is ambiguous/UV-degenerate it fails rather than fabricating data.

Kaotuan portable reproduction against prior r21 has **identical JSON values and BIN bytes**; whole-file SHA differs solely because JSON property serialization order differs. Exact delivered SHA stays authoritative. Cross-version Blender re-export byte identity is not promised.

## Important decisions / limits

- Split local facial morph draw geometry from static skinned geometry: actual Godot Compatibility at morph weight 1 showed artifacts on untouched forehead when all geometry participated. A .999 cap was rejected. Exact index partition solved the tested extreme states without changing source appearance.
- Explicit baked tangent basis, no COLOR attributes, exactly one atlas UV set, and CLAMP_TO_EDGE on both axes are required. Clamp fixed the demonstrated image-border sampling issue; it is not proof that every atlas island is flawless.
- Kaotuan r23 includes the verified 211 partial-alpha bake-coverage fix; actual r22 side-view seam disappeared. r23 only removes dead storage and must retain exact effective values.
- Attack is presentation only, not a new damage rule. All heroes retain the code team's approved common-melee logic and separate trait mechanics. No Dodge was added.
- High-source microtexture is simplified at 1K and <30k triangles. Texture memory values are uncompressed estimates; software-rendered native checks are not mobile GPU certification.

## Kaotuan final coverage step
After clamp, run `resolve_bake_coverage.py clamped.glb opaque.glb --report coverage.json`, then `runtime_postprocess.py opaque.glb final.glb --report compact.json` without split/repair flags. This corrects only verified coverage-premultiplied base-color edge pixels for the opaque material; all full-coverage colors are unchanged. Actual pixel checks cleared the former side line. No geometry/UV/animation redesign.

Independent reproduction used Python 3.12.14, NumPy 2.3.5 and Pillow 13.0.0; tooling pins are in `tools/art/requirements-runtime.txt`. These are offline authoring dependencies, not game runtime dependencies. The verification consumed the already-installed environment and did not install or change global packages.

Godot's current GLB importer extracts embedded atlas images beside the model (`hero_*_basecolor.png`, `*_normal.png`, `*_orm.png`). These are generated derivatives, ignored in Git along with their sidecars. The GLB remains the texture authority; do not hand-edit the extracted PNGs or bundle duplicates in source archives. A clean import regenerates them. The final r23 imported base-color pixels were compared against its embedded PNG in both main and candidate projects and matched exactly, ruling out an older extracted atlas being displayed during the host check.

## Additional three heroes

The same committed portable tools reproduce the other three exact delivered files from the raw exports identified in `content/models/food-heroes-provenance.json`:

- Juanjuan: split/repair with `--mesh-prefix Juanjuan`, then clamp.
- Feiling: split/repair with the default prefix, then clamp.
- Cuilan: split/repair with the default prefix; its raw exporter already uses clamp.

Independent execution reproduced Buding, Juanjuan, Feiling and Cuilan whole-file SHA-256 exactly. Kaotuan's JSON values and BIN bytes are identical; only JSON serialization order differs. The five-entry report is in `docs/evidence/FEAT-0004/five-hero/independent-reproduction.json`. Native re-export across Blender builds is not promised byte-identical.

Each hero uses three 1024² atlas images. Five heroes' theoretical uncompressed RGBA8 image storage is 60 MiB without mipmaps (80 MiB with a complete chain), excluding mesh and selection textures. This is arithmetic, not observed GPU memory. Current Host/UI scene caches can retain previously selected heroes; measured residency/eviction work is an explicit next performance gate, not concealed as mobile certification.
