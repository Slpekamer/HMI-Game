# Directional gait feasibility study — returned for readability work

This isolated one-hero study adds a controlled StrafeR / Backpedal asset drill. It does not enable a moving-fire controller in the normal game and is not merged into the approved integration branch.

- Both comparison sides use the same preserved mesh, rig and original three clips. The candidate appends two clips. A uses original Run; B replaces exactly seven lower-body bones in a private Run copy, retaining the existing upper Attack layer.
- The controlled scene fixes cosmetic heading identically in A/B. It uses ordinary full-speed movement input, unchanged camera/scale, seed, enemies, health and projectile/damage authority. It is not a normal targeting or aiming demonstration.
- Four native desktop runs completed 180 simulation updates. Both directions cover two moving clip wraps at approximately 4.8 m/s and natural attack overlap. A/B authority, camera/actor transforms, non-lower local poses and morphs match exactly. Local upper-pose parity does not imply unchanged upper world positions when Pelvis moves.
- The original stationary-facing regression passes 225 pure and 231 Host checks with the fixture disabled. Editor-import shutdown diagnostics remain separately recorded. No mobile/performance or complete moving-gait lifecycle validation is claimed.
- Foot sliding remains substantial. CPU-readback sole material points drift approximately 2.08 m over a sampled 0.45 s stance; blending can place them roughly 4.6 mm below the reference plane. These are pose diagnostics, not GPU-contact certification.
- Review found no clear broken boot/trouser silhouette at the game scale. The important failure is readability: right-side steps give only a small visible benefit, while backward feet overlap beneath the body. The study is returned for improvement, not accepted as the moving-combat solution.

Next step requires choosing a meaningful visual change rather than repeatedly tuning tiny foot amplitudes: either a bounded lower-leg clearance revision or a more readable whole-body weight-transfer pilot. Preserve the character's face/body identity and gameplay speed. No full controller or other-hero batch follows from this study.
