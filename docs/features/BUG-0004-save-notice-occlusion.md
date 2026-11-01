# BUG-0004 — Reduce save-notice obstruction without moving the camera

## Scope and authority
Parent-authorized isolated presentation trial following actual player/HUD occlusion reports. Baseline is frozen PERF-0006. Parent approved adoption after final aggregate; final aggregate and scoped native gates now passed. Local commit/export remain parent-owned. The rejected stronger camera and left-below-health variants are not included.

Only runtime change: the existing persistent SaveStatusNotice request rectangle moves from (390,145,500,28) to (748,28,272,28), using the existing empty gap between wave and wallet panels. The existing style actually imposes 40px minimum height on both variants. Font16, complete status wording, visibility rules, pause/failure modal, companion tray, Core, persistence, models, camera and input all remain identical.

## Acceptance gates and evidence
- All three actual host states fit the real Label without clipping/trimming; longest text218px, styled minimum242px<272px. Empty state still hidden; state updates retained.
- Real logical viewport remains1280×720 across four requested physical sizes; do not imply portrait-responsive redesign or device readability certification.
- Actual UpdateHud extreme endless wave and wallet inputs do not overlap the relocated notice. These are contract stress values, not naturally earned money. Existing large-wallet text touches pause independently of this change; recorded, not silently repaired here.
- Controlled5-hero/9-point/four-size180-pair fixture: notice overlap20→0 and conservative HUD raster union135388→52988. These are sampled rest/all-yaw bounds, not alpha silhouettes or continuous animation.
- Independent0.5m grid uses authoritative Core.Geometry.Blocked with actual player radius.7845 legal hero-point samples;1560 excluded. Exact HUD union7551187.05→3226412.43, no worsening/new-overlap samples. One conservative residual Cuilan(-4,-7.5),4.80px², remains; inspect actual pixels separately.
- Native13-pair actual-engine screenshots and independent report support the sampled improvement; long text remains complete. Residual native pair passed its scoped check: current visible Cuilan is not touched by the relocated notice, but existing timer still covers head detail. Final full aggregate exit0; normal main saved-success/movement/pause/resume/restart/second-start passed on matching DLL.

## Rejected alternative and fixture history
Left-below-health placement(20,108,345,28) transferred obstruction:20 removed/20 added samples, total HUD raster135388→135548; actual pictures cover head detail. It remains rejected in its separate overlay. First left fixture accidentally retained a pause overlay; raw pictures retained as superseded test evidence. Corrected test clears only the modal while keeping Core paused, not light/material changes.

## Limits and reversibility
This improves one persistent strip only, not all HUD occlusion, continuous movement comfort, all animation envelopes, or mobile performance. It leaves camera overview and gameplay unchanged. Revert the one rectangle expression to restore baseline. Do not alter font/text or camera to disguise a failing layout gate.
