# Launch-only pose-key feasibility prototype

Baseline c890cd50 preserves the approved turning-run and the separately rejected moving-aim experiments. This branch prototypes a different visual language: an outward opening gesture followed by a front/side/rear energy-release accent. It is not a gun-barrel tracking solver, a hand-origin projectile, or production integration.

Only accepted real ricochet launches trigger this layer. Generic/near-melee gestures retain the original animation graph and cancel the launch cue with a short cosmetic crossfade. Core, projectile origin/path/timing, graph clocks and approved root/lower gait remain unchanged. Missing direction evidence resets the cue instead of borrowing an earlier shot.

Key poses are authored once from the actual rig's fixed bone lengths and cached as local rotations. A shoulder-outward opening waypoint precedes the sector accent; the gesture retracts through the same waypoint. It does not solve a moving endpoint against the ballistic direction every frame. Chest yaw is bounded to0/25/30 degrees. The arm points outward and forward/sideward/backward as a readable release gesture, explicitly not a muzzle proxy. Left and right geometry must be independently tested; mirroring is not proof.

Scope is initially only front/side/rear keys and their short interpolation. The actual launch-relative sector is latched for one gesture; continuous retargeting during arbitrary body reversals is not certified. `LAUNCH_POSE_FORCE` is a fixture-only key/side override for geometry tests, never a claim that an artificial pose direction matches the actual projectile. Natural review must leave it unset.

First WIP implementation and key generation require build, real mesh/morph, lifecycle and native validation. Earlier aim-IK footage is not evidence for these new keys. No main/integration merge is included.

## Initial scoped evidence and remaining counterexamples

Runtime `ebcd53c5` builds with zero warnings/errors. Six forced key fixtures and four natural routes (0, +90, -90, 180 degrees) complete their 180-frame numerical comparisons. Authority, camera, soles, boot geometry, motion and locomotion traces are byte-identical to each paired baseline; unrelated local bones and morphs are unchanged. The actual-launch/lifecycle probe passes 97 checks, including duplicate rejection, cancellation, pause, focus and scope reset. These are not appearance passes.

Independent skin reconstruction applies the actual morph weights. Six keys at opening/accent/return samples still contain new distal-arm side-torso intersections: front-left opening/return has six triangle pairs, front-right accent has twelve, side-right has nine, rear-right four to nine. Some side/rear chest rotations also increase the untouched opposite shoulder-region intersections. Right-hand/head intersections improve at the checked samples and cross-arm intersections are zero there. Counts are not penetration depths or a visible-severity verdict. Full interpolation geometry has not been certified.

Native review tools record the unforced real straight and reversal routes, both with identical approved lower gait and baseline/candidate variants. They preserve original camera, all unique 60 Hz frames and normal speed; only a 32 px label strip is added. Recordings are an internal shape/readability review of this small prototype, not an acceptance claim. Earlier IK recordings do not represent this implementation.
