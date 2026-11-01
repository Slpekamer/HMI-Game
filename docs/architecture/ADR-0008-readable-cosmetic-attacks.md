# ADR-0008 — Readable overlapping cosmetic attacks

Status: accepted scoped presentation fix after fresh recovery tests and independent native sampled movie review, 2026-10-31 19:42 UTC.

Every valid authoritative attack previously sought Attack to zero. Events closer than clip duration can hold the character in anticipation. Core damage cadence must not become an animation clock.

Keep source-generation/run/wave/tick validation. On Advance, consume the bounded cosmetic flag. If Attack is already active at the **start** of this render step, absorb the trigger into that gesture without seeking/restarting or retaining a delayed tail, even when this step reaches its end. After completion, only a new event starts another gesture. Smile may refresh; damage expression retains priority. Pause consumes/advances nothing. Wave/run reset and disposal clear all transient state. Authority hits, skills, coins and rules are untouched.

Alternatives: speeding clips can be unreadable; restarting causes starvation; unbounded queueing creates delayed fake activity. Coalescing is reversible and bounded. Full-body attacks can still mask locomotion at extreme rates; this change alone is not natural-animation or commercial-quality approval.

Gates: red-before-fix; several render deltas/high event rates; stop returns latest Idle/Run without backlog; pause/lifecycle; five final imported assets >=95% actual Attack duration; fresh aggregate; actual fixed-clock native capture and independent review. The former cloud's 1450/1855 counts and video were historical; repeat on reconstructed files.

## Fresh recovery evidence

Full scripts/verify.sh passed after recovery: Core baseline, build/import/smoke, host lifecycle, presentation 1450, actual five-asset cadence 1855, cache lifecycle 366, and selection portrait gates. Initial isolated editor shutdown produced an Android EditorSettings ERROR; retained red log and fresh entire aggregate rerun clean. Restoring restart-on-every-event only in isolated test copy reproduces the expected presentation assertion failure. Fixed source unchanged during final full run.

Independent actual 288-frame/24-fps movie and full decode completed; sampled poses show high-trigger gestures progressing, pause frames 150/180 exactly equal, stop returns Idle and movement returns Run. This is a production-presenter fixture, not actual combat balance, phone performance, or full-speed human animation approval. See docs/evidence/B4-attack-cadence.

Integration note: this decision was ADR-0006 on the independent B4 branch; renumbered ADR-0008 here to avoid colliding with the temporary-contract ADR. Historical evidence retains its original branch labels.
