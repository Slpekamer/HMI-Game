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

## Native prototype capture

The two unforced natural routes now complete native A/B capture. Thirty-two runtime trace files exactly match their corresponding headless runs, including the owned pose and launch-cue traces. All 720 actual FramePostDraw attachment samples have zero position/basis error. The candidate-only review sequence contains 360 unique frames at 60 fps (six seconds), full original camera content plus the label strip; decoding and tick completeness pass.

Continuous frame inspection shows the opening/accent/retraction gesture without forcing a rear shot into a gun-pointing pose. At the original small character scale it reads as a brief outward hand gesture; this is an internal art/readability observation, not user approval or proof that the known distal geometry intersections are invisible. Natural routes cover real front, side and rear launches but not every forced left/right key or arbitrary retargeting. The limited key geometry counterexamples above remain unresolved, full interpolation geometry remains uncertified, and no production integration is claimed.

## Approved direction, timing refinement WIP

The user approved the first native energy-gesture direction. This approves the stylistic direction, not the unresolved mesh counterexamples or production integration. Preserve the first prototype as a reference.

The next small change lengthens only the cosmetic cue from 360 to 500 ms: 80 ms opening, 140 ms approach to accent, 120 ms readable accent hold, 70 ms return to outward waypoint and 90 ms fade to the current original animation. Actual launch, damage, cooldown and original graph clocks remain unchanged. Near-melee cancellation retains its 60 ms cosmetic handover rather than delaying an attack. Geometry keys are unchanged in this timing batch; all previous cross-intersection counterexamples still apply until independently retested. New timing/interpolation and mixed-event verification remain pending.

## Fixed forearm roll candidate

A narrow offline skin diagnostic rotated the actual Forearm local basis about its Hand-child translation axis, with the Hand hierarchy following normally. At the original front-right accent sample, +30 degrees removes the twelve distal torso intersections without head/cross-arm intersections; the front-left opening sample clears its six at -30 degrees. Fifteen degrees did not clear them. These are only two sampled points, not a general acceptance claim.

The next candidate caches those independently signed fixed rolls in its keys, preserving the skeleton chain endpoints and wrist rest relation. This addresses thick forearm surface orientation rather than blindly moving a ballistic target. Other sectors, entry/retraction interpolation and actual runtime skin remain to be verified. A new read-only simulation probe observes real melee and projectile events in an unmodified encounter; no synthetic attacks or health/range changes are used. Its results are still pending.

Mixed-event probe first run failed before producing evidence because its JSON serializer followed Vec2.Normalized recursively. The test now uses the established explicit two-number Vec2 converter from the controlled movie fixture. This is a test-only correction, not a game/runtime change; mixed-event evidence remains pending until a fresh run completes.

## Extended-cue runtime evidence, still WIP

The 500 ms / 30-degree-roll candidate builds without warnings/errors. All six forced key cases and three natural turning routes pass numerical authority/lower-gait/unowned-bone comparisons; the lifecycle probe again passes 97 checks. A separate unmodified encounter produces 1,065 A/B frames, eight real player melee events and twenty real ricochet launches with byte-identical authoritative traces. Five melee events interrupt an active cue; their held launch samples clear immediately and their cosmetic cancellation settles within the checked five-frame windows. This does not certify same-tick coincidence or multi-tick catch-up ordering.

Independent actual-skin checks at four samples per key find remaining distal-side-torso intersections: front-left near opening has three pairs, front-right near opening five, rear-right accent two. These are the same thick near-elbow forearm surface against Chest/Pelvis, not newly diagnosed head/cross-arm contact. The complete entry/exit interpolation is not yet cleared. The 30-degree candidate is an improvement but not a geometry pass; further bounded fixed-roll clearance diagnosis is separate from approved stylistic direction.

## Remove the unnecessary front waypoint, limited candidate

A bounded follow-up diagnostic at cumulative 40/45 degrees clears the front-left opening and rear-right accent samples, but still leaves three distal-torso pairs at front-right opening. The angle search stops there; neither angle is a general clearance solution.

Both front accents already have zero selected distal torso/head intersections at the tested 73/79 samples. The next candidate therefore uses that same fixed accent for front opening and return, removing the problematic lateral detour instead of continuing to increase roll. Its 80 ms entry and final 90 ms exit remain smooth, within the same 500 ms envelope. Right-rear alone uses the independently tested 40-degree fixed roll; all other keys retain 30 degrees. The changed entry/exit paths and every rear phase still require fresh actual-skin interpolation checks. No previous discrete result certifies this new candidate.
