# HMI-Game recovered original art

2026-10-31 recovery: original local production was lost in cloud reset. Sources were reconstructed from active conversation, preserving numeric geometry/material parameters and v01.1 chaser key connection. These are newly generated actual Blender assets, not recovered original binaries. Do not reuse old SHA256 values. Original archives and validation sheets are retained in the durable art Library bundle; fresh runtime checks are recorded separately.

Original rounded-toy adaptation of approved character-select/combat/wave UI references, re-inspected after recovery. Simplified opaque soft-vinyl heroes (not fur replicas), deep plum enamel/brass enemy, blue summon and bright yellow honey identity preserved. No external model/texture downloads. Original project assets, no third-party asset dependency. Code and sources may be retained with project.

## Contract
GLB2, metres, Y-up, forward -Z, feet root, applied transforms. Blender sources Z-up/+Y. Hero heights1.2m, chaser.9, companions.7, wall.8,pickup.3. One joined mesh, multiple material surfaces; opaque PBR color/roughness/metalness; no custom shader/alpha/rig/animation. Collision and gameplay remain Core-owned. Adapter may bob/squash only as presentation.

## Rebuild
Set `HMI_ART_OUTPUT` to output root (default ../v01 relative to scripts).

```
blender -b -t 2 --python tools/art/build_assets.py
python tools/art/build_ui.py
blender -b -t 4 --python tools/art/render_validate.py
python3 tools/art/validate_assets.py content /tmp/hmi-art-validation.json
godot --headless --script tools/art/render_ui.gd -- /absolute/path/v01/ui
```

`HMI_ART_IDS=enemy_chaser` can restrict render validation. Blender4.3.2 CPU Cycles32 samples/no denoiser (this build lacks OpenImageDenoise). No claims about hardware GPU, mobile performance, Unity imports or rig animation. Blender BEAUTY triangulation can change equivalent triangle index/diagonal ordering between runs; byte-identical GLB rebuilding not promised. Release manifest locks actual delivered hashes. All20 SVGs are original text-free128px paths; panel/button nine-patch margins32px. Chinese remains engine text.

Runtime destinations: content/models/*.glb and content/ui/*.svg. Generator sources: tools/art/*.py / render_ui.gd. Keep .blend sources in durable art archive, not necessarily repo; avoid committing .blend1 backups and cache.
