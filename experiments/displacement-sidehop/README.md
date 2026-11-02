# Synchronized side-hop pilot — WIP, headless checks in progress

One isolated rightward **two-foot synchronized side-hop**, not an alternating side-step or moving-fire controller. Forward Run and Backpedal motion/style are user-approved and preserved byte-for-byte in their existing adapters. Static proportions, Core movement/speed/events/collision and other characters are unchanged. No production integration is authorized here.

## Single candidate
Actual rendered rightward displacement drives a common two-foot phase. At Core speed 4.8, each 1.2-world-unit cycle means four paired landings/second: 0.24 distance / 50 ms simultaneous stance, then 0.96 distance / 200 ms flight. This is not the old 2.4-unit individual-foot alternating cycle. Body, shoes and anatomical knee pole remain forward-facing.

Both feet retain the measured 236.967 mm hip separation. Identical flat-boot translations preserve approximately 17.288 mm lateral boot separation. Each stance moves the hip over a fixed actual sole anchor, with relative ankle travel +120 to -120 mm. The air path connects those endpoints with zero world horizontal velocity at lift/landing: 10 ms quadratic endpoint transitions and a displacement-conserving central relative velocity 1.515789 m/s (world 6.315789 m/s). Body arc is 50 mm and sole lift 40 mm, with the explicitly short 10 ms lift transition. Nominal hip-to-ankle height is 180 mm. Sampled steady reach maximum is 218.054 mm versus 220.155 mm total bone length; this is a design estimate, not runtime validation.

Real Idle foot offsets, synchronous dual-support restart, airborne 3D reach and stop/tap bridges require separate checks. Do not snap away the initial fore/aft ankle offset or move a declared stance anchor. No bone stretching, root correction or hidden target clamp is permitted. There is one candidate, not an amplitude sweep.

## Gates and limits
- Build and actual skin ground/reach/stance checks; steady, short taps, repeated taps, restarts, pause/reset.
- Body-relative rightward displacement, shoe basis and anatomical knee pole; both boot envelopes.
- Actual evaluated leg/boot/shorts deformation versus existing static seam overlaps, plus close and unchanged-camera native pixels. Rigid boot bounds alone cannot certify trouser clearance.
- Approved Run and Backpedal exact trace regressions through the updated host.
- Normal-speed native A/B and a separate user style decision. Synchronized double-foot lift may look conspicuously bouncy; stop this candidate if distracting rather than changing amplitudes repeatedly.

Fresh build passed with 0 warnings/errors. Four steady A/B runs and 16 tap scenarios passed independent actual CPU-skinned sole, authority/camera/upper-local/morph/event checks. The tap minimum reach reserve was 1.836 mm and maximum actual stance drift 1.222 micrometres. The selected-gait steady verifier explicitly expects two simultaneous stance feet for StrafeR and retains the single-foot assertion for Run/Backpedal. All 28 restart timings (26 real restarts plus two uninterrupted controls) and two pause/reset probes also passed. Approved Run/Backpedal four-steady groups each match their previous references exactly across all 24 trace files per gait; their wider tap/restart regressions are running. Native appearance and style are unreviewed.

Offline actual LBS reconstruction matched measured sole geometry within 0.417 micrometres across ten sampled frames. Sampled left/right leg surface intersection count was zero. Leg/pelvis and some boot/upper-lowerbody surface intersections exist, including intrinsic asset and previously approved backward-action overlaps; the regions are bone/height-derived, not semantic trouser labels. These counts locate close-view inspection points, not a visible-collision verdict. In particular, stop/settle frames need native visual inspection. Coplanar contacts and continuous-time absence of collision are not certified.

`run-native-review.sh` records full-width Chinese sequential A/B chapters (continuous taps and two-tick short tap) through an existing display. Set `GODOT_BIN` to the Godot .NET executable and `DISPLACEMENT_OUTPUT` to a fresh directory. The script does not create a display. Private logs and recordings are not public source payloads.
