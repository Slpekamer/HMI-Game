# ADR-0010 — Quiet responsive presentation and bounded portrait follow

Status: accepted scoped implementation after final strict aggregate and same-candidate native input gate, 2026-11-01 07:31 UTC. Authoritative Core, persistence and all model assets unchanged.

## Context
The owner reported crowded cards, poor display adaptation, awkward chooser presses and difficult-to-read motion on an actual Android build. Stage1 UX-0001 added Home/actual animated chooser/enemy bars/layered animation. This increment only combines independently reviewed layout and presentation work.

## Decision
- Replace crowded decision-page backgrounds with opaque warm low-contrast panels; retain every card requirement, cost, duration, risk, stock and confirmation intent.
- Use actual viewport `CanvasItems`/`Expand`, landscape1280×720 and portrait720×1280 design bases, transformed OS safe area on mobile, desktop client area otherwise. Reflow existing controls rather than rebuilding active modal callbacks or random seeds.
- Portrait cards/shop scroll vertically; keep all legal choices and bottom actions reachable. Keep enemy health rectangles in their actual canvas-local projection space and reject offscreen full rectangles.
- Convert joystick input through its inverse global canvas transform. Only a genuine layout/safe-area change cancels held input; identical repeated metrics do not.
- Keep landscape camera behavior. Portrait follows the rendered player with a16.8m short-axis view; this sacrifices distant arena visibility rather than shrinking the player until unreadable. It does not follow UI gaps. Pause freezes immediately; real resize may reframe once without advancing authority. Run/entity scope changes snap to the new player.
- Android orientation explicitly uses Sensor Landscape4, not automatic portrait Sensor6. Desktop portrait compatibility does not prove Android rotation/touch behavior. Export-manifest and device checks remain separate.

## Rejected alternatives / retained evidence
- Full-world-width portrait framing produced tiny heroes and exposed finite-floor edges; rejected despite functional bounds assertions passing.
- Initial camera continued easing after ordinary pause; independent old-source RED/new-source GREEN290 and actual early/late world-pixel equality establish the narrow correction.
- Global old-theme reversion did not reliably fix shutdown failures and was not adopted. Stage1's exclusive primitive-wrapper mitigation is retained unchanged; no shared-resource Dispose or production forced GC.
- Orthogonal shadow change lost visible shadows; rejected. Existing shadow parameters and Run asset payload remain unchanged. No Android flicker-root-cause claim.
- Raising petals to+.015 with ground0 makes petals and honey top surfaces coplanar; actual diagnostic artifacts confirmed. Neither that old floor variant nor its corrected separate study is in this increment.

## Verification
The coherent r7 strict aggregate passed including Core624665, primitive256, native-five-hero mixer17590, Codex26, temporary/endless UI150, responsive893, actual host responsive/pause/scope290, persistence/store/session and lifecycle gates. Old absolute globalY530 test assumptions were replaced by same-card actual global rectangles, retaining full text/expiry and adding column alignment checks. First red logs are preserved; one EditorSettings shutdown ERROR was strictly rejected, then same-source fresh-config final full run passed. No error whitelist.

Same-candidate final normal-input/rotation/restart/Home/normal-exit review passed on exact DLL6934d14d…. Immediate paused world crop is decoded-RGB identical; no ERROR/FATAL on that normal Alt+F4 exit. Continue entry was shown, not clicked in this short gate. Prior independent static and input gates are not misrepresented as final merged-version evidence. No phone-performance, all-resolution/device, natural gait or perfect HUD visibility claim.

## Reversibility
Presentation files and project orientation can be reverted independently of Core/catalog/checkpoint schema. Do not roll back or alter a player's persisted run to undo a layout choice.
