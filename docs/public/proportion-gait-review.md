# Juanjuan proportion and animation trial — user-rejected skating archive

The user accepted the static proportions but rejected this animation sample because the feet visibly skate and the stride does not match movement speed. This branch is an archival record of the rejected animation, not an approved review candidate or production replacement. Further work must match foot travel and gait timing to actual locomotion; passing parity checks does not satisfy that requirement.

This isolated WIP preserves the accepted face/body/shorts shapes while raising the upper body and pelvis 35 mm, the knee landmark 20 mm, and compressing the rounded boot crown by 10 mm. Sole and footprint are preserved. The accepted static shape is not a production replacement.

The rig is rebound to the accepted static mesh. Idle, Run, Attack, StrafeR and Backpedal are rebuilt for the new rest skeleton. Foot targets are retained; new lower-leg animation does not deliberately stretch bone scale. Run alone uses one derived 3.814588 mm downward body correction to maintain 1 mm extension clearance. Thus its upper-body world offset is 31.185412 mm, versus 35 mm for the other clips. Rest geometry is unchanged by this animation correction.

Functional Blink, Smile and Surprise shapes and materials are retained. Attack keeps its original source timing and non-looping contract; the host continues to drive facial values from actual events. No blendshape animation tracks compete with that host. The visible ring is part of the skinned mesh, with 462 vertices rigidly weighted to Hand_R; checking an empty attachment is not a substitute for that geometry check.

## Scoped checks

- Rig bind versus accepted static shape: maximum vertex error below 0.1 micrometre; rebuilt Idle frame 1 below 1.6 micrometres.
- Skin/inverse binds, three functional morphs and three embedded texture images independently validated. Final geometry, morphs, materials and binds match the validated bind asset.
- Five source clips retain their intended periods/loop contracts; the lower-bone scale deviation is floating-point sized, not intentional stretching.
- Visible ring rigid-transform residual below 0.2 micrometre across the five clips. Rendered Attack keyframes show continued grip.
- Copied Core is byte-identical. Existing stationary-facing regressions: 225 pure and 231 host checks passed.
- Headless controlled A/B: forward, side and backward routes each complete 180 updates. Authority, events/projectiles, camera, actor motion, phase, Attack timing and host facial values match. Old/new bone poses and world geometry are intentionally not equality requirements after rebinding.

Native controlled A/B completed all six 180-update runs. All authoritative/headless traces matched; evaluated BoneAttachment position and basis residuals were zero in every one of the 180 post-draw samples per run. Each 181-frame capture was decoded: simulation frames 0–179 plus one repeated final frame, aligned exactly between A and B. The subsequent user visual review rejected the locomotion because of skating. The controlled drill fixes cosmetic heading and is **not** a completed moving-fire heading controller. It does not change Core movement speed, damage, trajectory or event timing.

## Limitations

Local linear-skin deformation remains, especially inside the shorts. Large ratios on short hidden edges are recorded rather than presented as a new visible failure; close rendered calf views do not show detached or spiked limbs. This is not a claim of all-angle deformation quality.

Sole diagnostics include small below-plane excursions during existing animation blend boundaries (approximately 1.3–4.4 mm in the scoped headless routes). No exact foot lock or zero ground penetration is claimed. The actual native comparison was reviewed and rejected for skating. The stride and timing do not adequately match actual movement speed; close-ups and parity checks do not override this failure.

No production replacement, main/integration merge, all-character rollout, mobile performance claim or APK is included.

## Reproduce the isolated check

Use the repository-pinned .NET/Godot versions. With the Godot executable available, run:

```sh
GODOT_BIN=godot bash experiments/proportion-gait/run-check.sh headless
# Explicit desktop movie capture; requires a working display:
GODOT_BIN=godot bash experiments/proportion-gait/run-check.sh native
```

Each command requires a fresh output directory (`PROPORTION_OUTPUT` can override it). Outputs are local artifacts, not source files. The scene is `scenes/controlled_gait_movie.tscn`; the candidate is selected only by this opt-in fixture. The archived torso-weight class remains a compile dependency of the reused presentation code, but the proportion scene explicitly disables that experiment. Editable, packed, sanitized source and re-export checks are under `experiments/proportion-gait/art/`; Blender 4.3.2 re-export reproduced the frozen candidate bytes.
