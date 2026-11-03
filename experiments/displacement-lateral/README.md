# Bilateral synchronized side-hop — implementation WIP

The rightward synchronized side-hop style was accepted together with the independent shadow sample. This batch extends only the fixed-heading side-hop diagnostic to leftward movement. It does not integrate production movement, aiming, direction transitions, shadows, or other heroes.

The component accepts an explicit direction of +1 or -1. The rightward calculation path is preserved; leftward movement negates the travel axis while retaining body-facing, shoe basis and anatomical knee pole. The fixture keeps the same fixed body heading and reverses movement input for StrafeL. Both variants use the original displacement-based paired contact/flight curve; no movement speed, collision, static proportions or Core rules change.

Pending fresh validation: build; actual CPU skin stance/ground/reach; boot separating plane and knee/foot orientation; steady, short-tap, repeated start/stop, pause/reset; byte-for-byte accepted rightward, forward and backward regressions; native normal-speed video inspection. Mathematical symmetry alone is not acceptance. No tests are claimed run in this first WIP checkpoint.

First fresh build passed. The first leftward runtime exposed the missing StrafeL asset-clip lookup; the fixture now explicitly reuses frozen StrafeR as its base comparison clip, while candidate lower poses are owned by the signed displacement component. The failed attempt is retained; corrected runtime validation remains pending.
