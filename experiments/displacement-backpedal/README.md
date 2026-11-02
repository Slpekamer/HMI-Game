# Backpedal displacement pilot — WIP

The forward Run and settle motion/style was accepted by the user. Preserve its component SHA256 `59a33f326c3c4344566243499c7af03703625be853a03d8a66bd12062f69fdfa` and the approved static rig. Base: `5c9251e34507a9292112a2a1ed835d8b1c2ee85f`. Acceptance is not permission to merge production.

This next isolated experiment covers only backward locomotion for the same character. Sideways motion and moving-fire heading are later work; no other hero is changed.

## One bounded candidate
- Phase follows actual rendered displacement. Declared stance anchors remain fixed in world space.
- Travel axis points backward relative to unchanged body facing. Shoe orientation and the anatomical knee pole stay body-relative; rotating the character/shoes to disguise forward Run as backward motion is not acceptable.
- Start from the accepted four-fall/s stride/contact/flight parameters and real leg/boot geometry. Check true 3D reach, actual skinned sole clearance and boot separation. No magnitude sweep, hidden IK target clamp, root correction, bone scale or Core-speed change.
- Airborne foot paths may be explicitly replanned inside the reachable volume. Preserve genuine Attack/morph/event timing and assess evaluated attachments.
- Controlled A/B keep the same cosmetic heading; this is not the full moving-fire controller.

The approved Run file will stay byte-identical. The new adapter is an explicitly isolated derived copy for this pilot, with provenance and a reviewable direction-only diff; do not silently refactor the approved behavior while testing a new direction.

## Gate
Implementation, build and native results are pending in this checkpoint. Require backward A/B authority parity, actual CPU-skinned stance/ground checks, 3D reach/boot geometry, input tap/restart/pause/reset checks, and byte-identical Run behavior regression. Then inspect actual full-speed native footage at unchanged camera/scale. Numerical locking alone is not visual acceptance. Keep failures identifiable and checkpoint every implemented batch before reporting.

The first wired Backpedal A/B build and four steady headless runs passed the inherited sole/reach/parity checker. An additional fixture-only CPU readback now evaluates every rigid boot vertex and reports separation along the body-right axis. This is a real separating-axis bound for the boots, not a claim about trousers, skin folding or GPU appearance. The new readback is checkpointed before testing; Run sole-output format and the approved Run component remain unchanged.
