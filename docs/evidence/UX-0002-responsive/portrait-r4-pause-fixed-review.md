# Portrait pause/scope correction: independent red → green and actual input

Specific ordinary-pause camera drift is cleared in this scoped desktop check. Overall Android/long-session/exit-stability gates remain separate.

## Exact evidence
- Pre-fix Host+Layout with the SAME new test builds/imports, then fails exactly `ordinary pause freezes camera immediately, not after settling` (host-portrait-pause-controlled-red.log).
- Fixed source test PASS290, actual authority Move ticks produce nonzero camera lag before Pause. Camera, actor and tick stay fixed from first paused presentation frame; genuine layout change explicitly reframes once; same layout does not; ExitSelection→Resume and Close→newStart reset camera scope to restored/new actor. Controlled corner setup now explicitly frames test poses rather than relying on paused interpolation.
- Final main DLL f9f2800c5b0d2ec39e26a748591e576be64e53ca3f08e5fdb28e383ce1ed07b3. Fresh XDG, normal Kaotuan1030 gameplay, actual WM sizes538x878 and1180x812.
- D1.8s→Esc gives100HP/1coin/00:22. Early/late paused-world crop(0,580,538,730) is exact decoded RGB equal; pre-fix corresponding wall shifted about5px. Entire screenshots differ in HUD portrait/control regions, so no full-screen freeze claim.
- Actual paused aspect rotation retains HUD and modal; return portrait→resumeA reversal; Restart→chooser→newStart shows centered hero100HP/0coin/00:24. These are normal input, no forced survival or cheats.
- Native process exit0 and subsequent desktop inventory confirm Godot gone. Driver unsupportedVSync warning retained.

This proves the targeted pause/scope correction, not every animated bound, all five physical-device inputs, long-play comfort, or intermittent Mono shutdown stability. Exact540/720portrait static results remain separately controlled, not these WM sizes. No continuous portrait movie obtained; first-frame transform guarantee comes from the red/green engine fixture, and actual screenshots independently corroborate world stability.
