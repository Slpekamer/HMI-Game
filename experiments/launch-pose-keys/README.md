# Launch-only pose-key feasibility prototype

Baseline c890cd50 preserves the approved turning-run and the separately rejected moving-aim experiments. This branch prototypes a different visual language: an outward opening gesture followed by a front/side/rear energy-release accent. It is not a gun-barrel tracking solver, a hand-origin projectile, or production integration.

Only accepted real ricochet launches trigger this layer. Generic/near-melee gestures retain the original animation graph and cancel the launch cue with a short cosmetic crossfade. Core, projectile origin/path/timing, graph clocks and approved root/lower gait remain unchanged. Missing direction evidence resets the cue instead of borrowing an earlier shot.

Key poses are authored once from the actual rig's fixed bone lengths and cached as local rotations. A shoulder-outward opening waypoint precedes the sector accent; the gesture retracts through the same waypoint. It does not solve a moving endpoint against the ballistic direction every frame. Chest yaw is bounded to0/25/30 degrees. The arm points outward and forward/sideward/backward as a readable release gesture, explicitly not a muzzle proxy. Left and right geometry must be independently tested; mirroring is not proof.

Scope is initially only front/side/rear keys and their short interpolation. The actual launch-relative sector is latched for one gesture; continuous retargeting during arbitrary body reversals is not certified. `LAUNCH_POSE_FORCE` is a fixture-only key/side override for geometry tests, never a claim that an artificial pose direction matches the actual projectile. Natural review must leave it unset.

First WIP implementation and key generation require build, real mesh/morph, lifecycle and native validation. Earlier aim-IK footage is not evidence for these new keys. No main/integration merge is included.
