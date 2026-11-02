# 卷卷 proportion rig — isolated animation-review candidate

**Static proportions accepted; animation rejected by the user for visible skating and stride/speed mismatch. Archival experiment only, not a production replacement.**

## Design
Upper body, face, arms, hips and intact shorts translate35mm upward; knee landmark rises20mm, ankle/sole stay fixed. Rounded boot upper dome/cuff are10mm lower; lower boot and footprint stay unchanged. Accepted static height is1.235m rather than1.200m. No face redesign or added ornaments.

The new bind mesh preserves accepted static appearance (max neutral error8.63e−8m). Rig/rest was rebuilt from the original ready Idle pose; original named skin weights and functional Blink/Smile/Surprise were preserved. All embedded textures and UVs retain their identity.

Five actions: Idle, Run, Attack, StrafeR, Backpedal. New fixed-length legs use source Foot trajectories through two-bone IK; no target clamping or intentional limb scale. **Run alone** requires a derived constant3.814588mm pelvis/upper lowering for1mm extension margin. Its upper world offset is31.185412mm, versus35mm for the other clips; static rest stays unchanged.

## Verified, and limits
- 1skin/17joints; nonzero named morph targets; three embedded textures.
- Actual source Attack timing1/30→13/30s, loopfalse; other original periods retained. No fabricated morph-weight tracks or attack-event markers: application presentation still owns event-driven expressions.
- Idle1 matches new bind within1.514e−6m. Foot sole trajectory difference≤3.54e−7m; visible ring's462 vertices remain rigid Hand_R-owned (error≤1.80e−7m across sampled actions).
- Actual keyframe renders showed connected calves/rounded boots and the held ring. Local inherited LBS stretch remains, including a high-ratio short edge hidden inside shorts. This is not a claim of flawless skinning.
- Diagnostic Idle1↔Run1 pose interpolation reaches−0.824mm sole depth at midpoint. Native transitions and gameplay-scale readability are documented by the companion controlled A/B review; this interpolation diagnostic alone is not that evidence.
- **Not world-space foot locking.** Retaining an in-place Foot trajectory does not solve skating at arbitrary movement speed.

## Contents / provenance
- `source/juanjuan-proportion-motion-r1.blend`: final packed editable source; includes all5actions and3morphs.
- `../runtime/candidate.glb`: isolated runtime review export.
- `tools/export_runtime.py`: parameterized re-export from that source, tested with Blender4.3.2.
- `tools/check_runtime.py`: read-only structure/timing check; not a visual test.

The published source is a sanitized standalone copy: character geometry, weights, rest, actions, morphs and packed texture bytes verified unchanged. Machine-local paths/UI state removed. Re-export with Blender4.3.2 reproduced the bundled GLB byte-for-byte in the final check.

Derived from this project's original ready-attack r2 character and directional gait authoring sources, plus its accepted static proportion trial. No external art, textures, or new licensing claims are introduced; retain the project's existing asset rights/provenance.

```sh
blender -b -t 2 --python tools/export_runtime.py -- --source source/juanjuan-proportion-motion-r1.blend --output build/candidate.glb
python tools/check_runtime.py build/candidate.glb
```

Export only to an isolated path; these tools do not install or replace production content.
