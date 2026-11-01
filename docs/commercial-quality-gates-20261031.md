# Commercial-quality gap register — independent review, 2026-10-31 20:05 UTC

This is a development candidate, not a store-ready product. Counts below are scoped evidence, not quality scores.

## P0 — integration correctness before accepting persistent progression
- E1 store crash/identity transactions and E-Core prepared-state replay have separately passed. Host integration is not yet demonstrated: acknowledge durable control before unique Begin; reject late callbacks; close on death/finish; fail-closed UI on uncertain control/close failure; new-run failure must not resurrect the old hero.
- Actual resume must disclose whole-wave-start rollback of wallet, RNG and purchases. Test process termination around actual host save/ack/Begin, not just store wrappers. No malicious-client anti-cheat or universal power-loss guarantee.

## P1 — gameplay and readability before commercial claims
- Real normal Buding1030 run cleared five waves and died late wave6 (115 earned,33 spent,82 left). Natural endless10–30-wave loop remains unvalidated. A second distinct damage-oriented build is useful, without changing balance to make the test pass.
- Hero can enter areas obscured by top HUD and bottom companion controls. Demonstrate enemy/contact readability and steering at all arena edges; improve framing or safe-area layout in a separate tested increment, not by silently shrinking collision space.
- D1 long-run single-choice degradation was real; D2 fixes synthetic slot availability with six one-wave alternatives.15,000 synthetic menus are not evidence that capped builds remain enjoyable. Observe actual late-wave decisions, time-to-kill, unavoidable damage, income usefulness and fatigue.
- New skill/contract cards currently lack bespoke illustrations. Mixed illustrative distance units versus meters need a consistent explanatory convention. Keep costs, prerequisites, exclusivity and one-wave expiry readable at minimum supported viewport.
- High-rate coalescing reaches readable clip phases in actual fixed-FPS fixture, but is not full real-player combat readability certification. Metallic foil/bell can appear dark at side angles; Feiling thin silhouette and cropped wide loops remain art/layout residuals.

## P1 — resource and device envelope
- llvmpipe desktop tests do not establish Android/GPU performance. Retained atlas memory was132.8MB texture after hero cycling; clearing managed cache references alone did not lower ordinary no-GC residency. Rejected Ignore-template experiment worsened buffers/loading and must stay rejected.
- Measure real target-device frame-time distribution, shader/import stutter, memory pressure after repeated hero switches, thermals and background/resume. No production forced GC as a substitute for ownership design.

## P2 — product completeness
- Verify touchscreen joystick/control sizing, focus/input release, audio on actual devices, save-error recovery wording, accessibility/contrast/localization and distribution/export flow.
- Do not claim content breadth from eight new nodes alone. Record which branches naturally appear and meaningful tradeoffs rather than chase a fixed card count.

Evidence is in adjacent native/Core/store review reports. Preserve failed diagnostics and the exact accepted source hashes; do not reuse pre-reset logs for reconstructed sources.
