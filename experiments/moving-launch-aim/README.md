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

## Thick forearm clearance candidate

The outward elbow plane still intersects the torso:180 frame116 has32 distal crossing pairs versus baseline0, and−90 frames126/127 also fail. Actual joint positions show the hand is outside the torso; the thick forearm surface extends inward/downward from the deeply bent elbow. These are real surface failures, not a bad aim-axis metric.

A separate-directory candidate now unfolds the selected arm with two-bone IK toward90% of its unchanged total length, nearly horizontal with a small downward slope. Its blend retains the authored reach direction and distance at entry and restores them on exit. The existing outward pole controls elbow placement. Only rotations of Chest and both arm/forearm/hand chains are owned; every local bone translation and scale, root and lower gait remain unchanged. Unreachable/degenerate targets throw instead of stretching limbs or silently clamping. Hand orientation is retained from the preceding aim stage. Geometry, lifecycle and appearance must be rerun; the prior native recording remains the rejected earlier runtime, not evidence for this new candidate.

## Elbow elevation follow-up

The90%-reach candidate passed the three standard numeric routes, six short-stop/repeated-tap A/B pairs and85 host checks, but actual distal intersections remain:180 frame116 has12 pairs versus baseline0;−90 frames126/127 have20/23 versus baseline12/12. Its measured right elbow is42mm below the shoulder while thick lower forearm surfaces still overlap the chest side. The next candidate changes the elbow pole's small vertical component upward rather than downward, keeping the same shoulder, hand target,90% reach and bone lengths. This is an explicit raised-elbow pose experiment, not an increase in limb length or a claim that pole alignment ensures clearance. Recheck the exact surface counterexamples and normal-speed silhouette before acceptance.

`run-check.sh smoke|taps` creates fresh A/B headless fixtures, with the approved turning gait enabled in both. `MOVING_AIM_OUTPUT` and `GODOT_BIN` must be provided. The verifier supports complete180/195-frame streams and checks unchanged authority/root/feet/attack clocks and all non-owned bone poses; it reports rather than hides reach-heading proxy limits. The strict historical character-presentation harness still stops on the known EditorSettings import shutdown diagnostic; its whole suite is not reported green. Scoped candidate runtime runs are separately clean, and a fresh Core run passed646,403 assertions.

## Failed-direction replacement semantics

Code review identified a capture-failure edge: a newer launch without a recoverable direction could otherwise reuse an older held sample. The host now pairs the captured direction with the exact accepted projectile identity/tick. An accepted launch lacking that evidence clears the previous visual aim; duplicate/stale attack triggers do not change it. A selected non-projectile gesture also clears the unrelated held direction. This changes no event scheduling or attack graph request. Natural-route and lifecycle checks are rerun; no claim of exhaustive event-overflow coverage is added.

## Horizontal elbow-plane comparison

Raising the elbow cleared180 frame116's torso intersection, but−90 frames126/127 became worse and showed more head overlap. Exact morph-aware head comparison also found existing original-animation head intersections (180 baseline25, raised22), so not every head crossing is newly introduced. The next focused comparison removes only the elbow pole's vertical bias, seeking an outward horizontal fold between chest and head; all other reach/aim settings stay fixed. This is a bounded pose comparison, not a claim of full-body mesh clearance. The probe now explicitly checks matching accepted launch binding, duplicate-no-sample preservation and missing-evidence replacement clearing after a presentation reset.
