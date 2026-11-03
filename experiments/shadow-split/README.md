# Directional first-split study — bounded validation, user-approved visual style

Visual status: shadow B was user-approved on 2026-11-03. The harder-shadow
appearance tradeoff is accepted for this reviewed candidate; bounded coverage
and software-renderer/device limits remain. No production merge is authorized
or performed by this documentation update. Runtime code remains frozen.

Base: approved Run snapshot `5c9251e34507a9292112a2a1ed835d8b1c2ee85f`.
Requirement: reduce visible moving shadow-edge aliasing without changing camera,
gameplay, accepted locomotion, lighting energy, or disabling shadows.

The only rendering candidate is `DirectionalShadowSplit1 = 0.015`, enabled by
`SHADOW_SPLIT_CANDIDATE=1` only during the controlled gait fixture. With the
existing near/far defaults, its first cascade ends near 60 m instead of 400 m.
Godot 4.6.3 renderer_scene_cull.cpp excludes orthographic cameras from the
ShadowMaxDistance clamp; changing that property alone is not this experiment.
All other shadow settings, camera fields, Core and character resources stay fixed.
Removing the conditional restores baseline behavior. Normal gameplay is unchanged.

A and B must both use `DISPLACEMENT_CANDIDATE=1` (the approved Run), identical
seed/input, full 1280x720 source frames, normal 60 fps, and decoded frame stamps.
Require byte-identical authority, camera and pose traces. Examine static scenery
shadow stability, contact, cascade transitions and acne; test arena edges and
aspect ratios before broader acceptance. Headless/default/source checks do not
prove visual improvement. If only sharpness changes and instability persists,
reject this candidate rather than sweep parameters.

No native A/B or performance/device result is claimed by this initial checkpoint.
Hero-only blob shadows cannot address the observed static-environment issue and
would sacrifice silhouette/directional/occlusion fidelity; not implemented here.

## Bounded coverage fixture
`run-native-coverage.sh` launches a separate scene, never normal gameplay. It
freezes a genuinely paused Combat world and captures west/east/north/south
camera views, with five 8-cm neighboring positions each, at 1280x720 and 720x1280.
The transforms use the existing landscape follow-clamp and portrait edge focus
positions. These are **static diagnostic views, not played edge routes**; they
do not claim that the player traversed the arena. Camera basis, original aspect
size policy, projection and near/far are retained. Only diagnostic UI is hidden.
Each view requires three identical raw framebuffer hashes. A/B manifests require
identical camera/paused tick data and light settings except the candidate split.
No native coverage result is claimed by this fixture checkpoint.

See [validation.md](validation.md) for completed scoped native A/B and paused side-view results, explicit harder-shadow tradeoff and remaining limits. No production merge.
