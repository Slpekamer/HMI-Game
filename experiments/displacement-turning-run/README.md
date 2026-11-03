# Moving-facing Run experiment — WIP

Requirement: turn the body toward actual travel instead of maintaining crab-like side locomotion. This isolated branch is not integration or release acceptance.

The frozen `DisplacementRunPresentation.cs` stays unchanged, including its fixed-heading contract. A separate opt-in `TurningRunPresentation` retains the accepted straight stride, flight and stop curves, and adds explicit world-anchor release and airborne braking when travel changes direction. The body follows actual rendered XZ movement with bounded exponential smoothing; finite turn speed means a transient heading/travel difference is expected. Planted anchors are not rotated with the root. Limb reach, ground and teleport guards remain active.

The first implementation is unvalidated WIP. Abrupt left/right/reverse turns, short presses, restart, collision slides, full skin/sole checks and native normal-speed footage remain to run. Rephasing at a direction change is an experimental flight bridge, not a claim of anatomically natural turning. Straight-path regression has not yet run.

Only the controlled fixture enables this mode (`DISPLACEMENT_TURNING_RUN=1`, `DISPLACEMENT_GAIT=Run`). `DISPLACEMENT_TURN_DEGREES` selects -180..180; turn starts at frame60 unless `DISPLACEMENT_TURN_FRAME` is set. A and B share body rotation and real simulation inputs; B alone applies displacement lower-body poses. Gameplay Core, movement speed, projectile targeting/path, damage and attack timing are unchanged. Independent moving attack aim is explicitly a later batch, not implemented here.

No main/integration merge, mobile or performance certification. Earlier sidehop/reversal experiments remain archived and are not advanced by this branch.

## First retained run and correction

First WIP built with zero compiler warnings/errors. Four directions (0, +90, -90 and180 degrees) each produced180 A/B frames with identical real paths, but shutdown emitted a Godot ERROR: Reset sampled skeleton GlobalBasis after tree removal. This is not a passing runtime result. Reset now guards that optional basis refresh with validity/tree checks. The original logs remain counterevidence.

The next candidate also bridges pelvis height/velocity across flight rephasing and blends airborne shoe yaw rather than snapping it to body yaw. Support feet still retain the complete world anchor until liftoff. Numerical frame/contact gates and narrow mesh reviews remain separate from normal-speed visual acceptance. In the first run, body-axis boot AABBs overlap on several +/-90-degree frames; this is an unresolved mesh-review requirement, not a claim that the meshes penetrate or are clear.

## Wider turning step candidate (not yet validated)

The corrected five-direction smoke run completed10 A/B runs without runtime errors and passed actual distance, floor, reach, anchor and authority gates. Independent triangle review nevertheless found real boot intersections at -90 frames62/63 and +90 frame74, plus a left/right leg crossing at -90 frame63. Conversely ±180 frames62–64 had no boot/leg crossings despite negative body-axis AABB gaps. These are retained, distinct results; AABB overlap is not a collision verdict.

The expanded tests then failed at +90 turn-frame64 (previous hip incorrectly reconstructed using the already-rotated body) and repeated short presses (a reachable-envelope violation). A third candidate stores the actual preceding skeleton world transform, subtracts full hip travel when braking, and authors a short wider airborne turning step with15mm extra crouch. The40mm per-side swing target offset eases in/out; planted world anchors are never widened or rotated. All reach/floor guards remain active. Full revalidation is pending, and independent moving attack aim remains absent.

## Expanded-phase mesh failure retained

The first wider-step candidate cleared the original smoke mesh frames, but -90 degrees beginning at frame61 still produced57/56 boot-triangle crossings at frames62/63. The width target was accidentally eased twice (its own50ms ramp and the100ms forward-flight bridge), so early clearance was insufficient. The next WIP applies that same bounded width ramp directly to already-airborne feet, while the support liftoff branch keeps its original world anchor. No maximum width increase or reach-guard relaxation. The revised phase and mesh gates are pending; no native delivery is approved by these intermediate results.

## Idle-to-reverse edge case

The early-width correction cleared the four focused turn cases, including the prior boot failures, with real LBS boot/leg checks at the failed frames. Additional direct-from-idle tests then found an unreachable first frame for180-degree startup: the turn bridge previously required the gait to have already started. This candidate lets a changed initial direction enter the bridge immediately, treating both actual idle-grounded feet as support liftoffs. Their world anchors lift vertically before horizontal movement. Forward startup still uses the frozen Run-derived path. The interrupted phase run is partial, not a full pass; new startup and complete phase validation remain pending.

## Short-press turn settling (next WIP)

The immediate idle liftoff removed the full-start -90 crossings, but its two-tick short press still crossed boots/legs during frames35–39. The180 short-press run also missed the strict1mm reach reserve late after landing (0.913mm, despite no runtime exception). Both findings remain failures. The next candidate adds a bounded outward airborne stop arc with10mm temporary extra crouch, ending at zero lateral offset before touchdown; planted anchors never move. Stop feet and shoe yaw now target the last real travel direction, rather than the body's still-interpolating heading. The body completes that previously requested turn after positional motion becomes subpixel, avoiding a residual mismatch that consumed the reach reserve. It does not choose an attack aim or change authoritative movement. All new startup/stop/phase checks remain pending.
