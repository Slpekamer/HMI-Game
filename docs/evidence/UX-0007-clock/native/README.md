# Controlled birth-render native comparison — pending actual pixels

Two isolated builds differ only in CombatFeedbackVisuals production file: original vs render-epoch birth guard. Both collect identical genuine BasicMelee and Ricochet events from a normal crit/1030 Core run and compare a second authoritative simulation while collecting. No event positions, damage, TTL, alpha or meshes are modified. After collection, the original event is replayed through the real controller, with Host-equivalent BeginFrame → Consume → Advance(.25) → FramePostDraw. No automatic controller or Core stepping runs during captures.

Each effect gets empty / first actual draw / next render after Advance(.25) captures. Expected: original first draw equals empty, fixed first draw contains body, both next draws equal empty. Old and fixed camera position is computed once from the same two event endpoints; fixed orthographic size16.8, cream plane. This is a controlled diagnostic, not real low-FPS player play or a performance benchmark. No GUI labels obscure image comparisons.

Native only after allocated desktop slot: `bash /workspace/scratch/80f5bc988ef7/HMI-vfx-birth-native-r1/run-native.sh`. It uses already-built projects, fresh per-process XDG after env, Dummy audio, and refuses headless rendering. No network build on desktop. Native logs still must be audited for ERROR/leak even if process exit0. Exact RGB test is verify-native-pixels.py; screenshots must also be independently visually inspected. Native remains PENDING until actual results.

Tint's original alpha envelope starts at0; at very low FPS it can reach expiry before a later visible tint sample. This fix does not alter tint opacity or claim to cure that residual. Body and player hurt arc have distinct envelopes. No lifetime change is included.

Exact existing body regression test adjustment: HMI-vfx-clock-fix-r1/body-probe-render-boundaries.patch, two BeginFrame calls at explicitly separate old-effect expiry renders. Original test preserved as CombatSkillBodiesProbe.before-render-boundaries.cs.template; production renderer unchanged.
