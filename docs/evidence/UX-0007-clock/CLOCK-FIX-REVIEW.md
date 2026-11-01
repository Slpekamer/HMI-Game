# Birth-render clock candidate — isolated, not merged

Production delta is ONE file, CombatFeedbackVisuals.cs. CombatSkillBodies and
all17 Core .cs plus Core csproj remain byte-identical to frozen context; Host
is not edited. Exact proof is production-diff-proof.json and clock-fix.patch.
No TTL, material alpha function, shape, attack animation, audio, gameplay or
metadata changes. The actual recording/context root remains untouched.

## Semantics
BeginFrame increments a render epoch exactly once. SetState (including8 Core
catch-up calls) and Consume never increment the epoch. Newly allocated cue
slots record BornFrame. Actual accepted player hurt and tint pulse record
their own birth epoch; coalesced/duplicate/rejected hits do not reset it.
Advance does not age a cue born in the current epoch; next render advances
existing cues normally. Repeated Advance in the same epoch cannot erase a
newborn. Paused Advance changes no cue age, and scope/reset/clear discard all
birth flags with their original slots. Budget12/48 unchanged; no queue replay.

A pause render can show a birth without aging it; upon resume, the subsequent
render treats it as existing and consumes only the actual unpaused delta.
This does not promise a GPU/display acknowledgment for minimized/offscreen
windows. Tint's original envelope still starts at alpha0; we do not call a
retained tint resource an opaque first-frame flash. Hit/player arc/body provide
the birth-frame visual while the existing tint envelope stays unchanged.

## Results (actual engine headless, not strict full-tree or native pixels)
- Same final ClockProbe source: old7619f04 controller exit1 at line45, explicit
  newborn render-opportunity assertion; fixed d9fcf09 controller exit0 with977
  assertions. Initial974 result extended by3 enemy merged-tint assertions.
- Deltas.016/.1/.25: new Melee/Ray body remains age0/visible at first opportunity;
  old pre-aged cue still expires at.1/.25. Next render.25 expires newborn too.
- Real8-step catch-up: two accepted attacks active2→2 instead of2→0; HP100/100,
  original events/coins/ticks match control. Eight-step stress still exactly12
  accepted body slots; subsequent render expiry works, no immortal effects.
- Pause/resume, scope/reset, repeated same-render Advance, player/tint expiry
  and separate enemy coalescing all tested. Merged hits do not reset old tint
  envelope or player-cue age.
- Original feedback probe54 passes unmodified.
- Body probe1009 passes with exactly TWO BeginFrame additions at previously
  implicit separate renders (old cue expiry and Ray expiry). All assertions
  remain intact; no BeginFrame added inside catch-up or event loops. Exact
  old source and body-probe-render-boundaries.patch retained. Native storage
  assertions still explicitly pending in this rebuilt copy.

No editor import was run for this small diagnostic project, so it cannot
close the full project's known strict import/exit WIP blocker. No actual
context movie on this candidate yet. The low-FPS first-frame loss is fixed in
this isolated state test; it does not explain/solve the60fps arc's weak shape
or authorize a longer TTL. A .24s melee experiment remains a separate future
variable after native/context review.
