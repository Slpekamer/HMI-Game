# Moving launch aim experiment

Baseline: turning-run evidence20ddedb6, runtimebbd726b6. The owner approved the normal-speed turning-run review on2026-11-03. Preserve that root, lower gait and contact behavior. Approval does not mean this next aiming layer is accepted or integrated.

Scope: a separate opt-in presentation layer driven only by each accepted real ricochet launch direction. No target reacquisition, ballistic redirection, attack rescheduling or Core edits. Capture direction while the launch buffer is current; same-tick impacts provide the first real leg when the projectile no longer exposes its original direction. Invalid, stale or unavailable directions fail closed rather than guessing. Upper-body articulation must not rotate the lower body to conceal its limitations.

Initial batch only wires and traces that immutable cosmetic input. Bone alignment, adversarial lifecycle checks and native review remain pending. Behind-the-body shots require special pose review; a180-degree chest twist is not an acceptable shortcut. No main/integration merge is included.

## Upper-body yaw candidate

Capture-only runs completed three real routes with21 original authority/camera/pose/contact traces byte-identical to the approved turning baseline. Natural launches include a137-degree rearward shot, so front-only examples would be insufficient.

The next opt-in `MOVING_LAUNCH_AIM=1` candidate restores its private previous-frame rotations before animation evaluation, then applies a bounded±60-degree chest assist and rotates the selected near-side upper-arm chain around its shoulder. Bone positions, scales, lower gait, root heading and attack graph clocks remain untouched. The existing elbow bend and gesture are retained, with80ms blend and bounded/smoothed yaw. The shoulder-to-hand horizontal vector is only a diagnostic reach-heading proxy: this rig has no authored muzzle axis or authority socket, so it is not claimed to be a muzzle alignment proof. Focus loss clears the held sample and restores the base. First compile exposed a local variable shadowing error, corrected before running. Rear-shoulder range, mesh intersections, lifecycle and normal-speed appearance are still unvalidated; no claim of all-angle success follows from this initial pose candidate.

## First pose validation (83af66c3)

Three180-frame routes completed without runtime errors. Authority, camera/root, attack timing, actual foot/contact and lower-gait diagnostics are byte-identical to capture-only baselines. Every local bone pose except Chest/UpperArm_L/UpperArm_R is also byte-identical; the three edited bones retain exact local translations. The actual host probe passed85 checks, including real moving launch capture, duplicate/context/disposed rejection, paused/invalid-delta freeze and focus-loss reset/no replay. This does not cover all catch-up, event-overflow or zero-length-impact cases.

The horizontal shoulder-to-hand proxy is deliberately not certified as precise aim: full-weight error reached39.6 degrees on a−90 route and32.1 degrees on the rearward route (frontward route7.5 degrees), while entering/leaving the layer can be farther off. These measured limitations remain visible rather than being removed by an overly loose pass threshold. Upper-arm/body mesh and native review remain pending. The new review scripts retain both the approved turning gait in A and B; only B enables this aim layer. They do not compare against the earlier in-place gait.

## World-target smoothing correction (pending validation)

The initial arm layer smoothed the inverse correction to a moving authored arm, causing persistent horizontal reach-heading lag even after blend-in. The next candidate instead slews the immutable launch's world-space target at a bounded720 degrees/second, then computes the current animation compensation without filtering it a second time. Chest assistance, shoulder angle cap,80ms blend, gesture clock and source launch direction remain unchanged. This does not anticipate a shot or guarantee alignment at its first frame. New range/mesh/native checks are required; earlier proxy errors remain counterevidence.

## Distal arm/torso counterexample and elbow-plane candidate

The world-target run preserves the numerical invariants and removes most steady proxy lag, but independent LBS review found a real right-forearm/torso intersection at180-route frame116 (baseline none). This is not dismissed as a shoulder seam, and better aim alone does not pass geometry. The next candidate reorients the selected arm's existing elbow-bend plane outward by rotating the whole chain around its actual shoulder-to-hand axis. This leaves the hand endpoint, elbow bend amount and bone lengths unchanged. Each arm's plane blend enters/exits over80ms; shoulders and lower body remain untouched. New torso/arm and cross-arm checks are required before native delivery.
