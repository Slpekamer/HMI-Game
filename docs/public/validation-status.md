# Validation status — WIP

This public checkpoint contains completed code with known limitations; it is not release acceptance. Runtime code matches the frozen integrated rule 3 candidate. Publication only narrows documentation/provenance and parameterizes environment-specific scripts; it does not add experimental effects.

## Passed in scoped development checks
- Portable Core regression: 646,403 assertions.
- Integrated targeted checks: codex 655, input 232, checkpoint store 112, session 174 and rule-upgrade 579.
- Integrated engine runs covered save-consent flows, codex focus, injected gamepad navigation, material feedback and projectile launch/impact behavior. These were scoped runs, not every historical test in one fully green aggregate.
- Real desktop main checks covered explicit start/confirmation, normal movement and camera changes, pause/resume, codex open/close, natural defeat, restart and normal window close.
- Controlled native footage demonstrated launch, flight, impact damage and subsequent ricochet travel. Filled cyan/caramel bodies were more visible than the rejected tiny bodies.
- Save validation used synthetic fixtures; cancel preserved old bytes and confirmed upgrades selected a distinct rule 3 slot.

## Failed, limited or unverified
- Shadow flicker remains. Tested distance and filtering alternatives produced no visual improvement and were not adopted. Other shadow experiments had costs or artifacts and are not production fixes.
- Moving attacks can still face the movement direction rather than the struck enemy. Natural side/back gait and full skill readability remain unfinished.
- Contact/short hops and crowded actors can be occluded. Hurt cues intentionally gate repeated events; they are not a confirmation for every hit.
- Active-projectile pause was not reliably captured in the latest manual run; automatic semantics and ordinary manual pause are distinct evidence.
- A strict editor-import shutdown diagnostic remains recorded. Historical intermittent native exit failures have not been proved eliminated by subsequent clean scoped runs.
- No current Android hardware, physical controller, Bluetooth/USB compatibility or mobile performance certification. Desktop software rendering is not a phone benchmark. No APK is delivered by this checkpoint.
- No claim of full-wave survival/balance, all-angle projectile tracking, real-time comfort or exhaustive historical migration coverage.

## Public verification guidance
Build and run affected test projects using the README. Preserve failures and distinguish passed, failed, blocked and unrun checks. Historical diagnostics may require extra fixture inputs not included in this minimal public snapshot. The complete operational recovery/debug archive remains separately retained; it is not necessary to reproduce ordinary runtime behavior and is not published here.
