# Moving-facing Run experiment — WIP

Requirement: turn the body toward actual travel instead of maintaining crab-like side locomotion. This isolated branch is not integration or release acceptance.

The frozen `DisplacementRunPresentation.cs` stays unchanged, including its fixed-heading contract. A separate opt-in `TurningRunPresentation` retains the accepted straight stride, flight and stop curves, and adds explicit world-anchor release and airborne braking when travel changes direction. The body follows actual rendered XZ movement with bounded exponential smoothing; finite turn speed means a transient heading/travel difference is expected. Planted anchors are not rotated with the root. Limb reach, ground and teleport guards remain active.

The first implementation is unvalidated WIP. Abrupt left/right/reverse turns, short presses, restart, collision slides, full skin/sole checks and native normal-speed footage remain to run. Rephasing at a direction change is an experimental flight bridge, not a claim of anatomically natural turning. Straight-path regression has not yet run.

Only the controlled fixture enables this mode (`DISPLACEMENT_TURNING_RUN=1`, `DISPLACEMENT_GAIT=Run`). `DISPLACEMENT_TURN_DEGREES` selects -180..180; turn starts at frame60 unless `DISPLACEMENT_TURN_FRAME` is set. A and B share body rotation and real simulation inputs; B alone applies displacement lower-body poses. Gameplay Core, movement speed, projectile targeting/path, damage and attack timing are unchanged. Independent moving attack aim is explicitly a later batch, not implemented here.

No main/integration merge, mobile or performance certification. Earlier sidehop/reversal experiments remain archived and are not advanced by this branch.
