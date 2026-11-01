# UX-0006 mobile input — WIP, not release acceptance

The isolated implementation is based on UX0004 remote `f4aa1e45`; it does not alter Core, Persistence, gameplay content, model bytes, damage, speed, checkpoints, or skill rules.

## Functional evidence

- Engine-independent movement/menu router: 232 assertions passed.
- Floating joystick actual engine: 130 assertions passed across five aspect combinations, including Viewport.PushInput dispatch, including fresh-center/negative-pointer cases (see bundled terminal log).
- Complete menu wiring actual engine: latest 157 assertions passed through Viewport.PushInput, including actual production Start/safe-point exit/Continue; menu card/shop/ending data are controlled fixtures, not earned gameplay. Device selection, second-device rejection, neutral rearm, provider unregister/re-register, modal safety, held-confirm gating, scroll-to-focus and five-size hint fit are covered.
- New UI strings were checked against the existing font; no font bytes changed. Default positional button labels do not claim a particular controller brand.

## Explicit unresolved exit / pipeline gate

The strict runner stops on existing editor shutdown `EditorSettings ... shutdown_adb_on_exit` ERROR even when import exits 0. No ERROR filter or weakened gate was added.

A menu run printed all 154 behavioral assertions, then **aborted at native teardown (`!rc_owner`, unsafe references)**. Earlier clean 151 runs do not erase this counterexample. A narrow test-only ownership comparison used `using` for each new InputEvent after synchronous PushInput returns; the same 154 checks then exited cleanly. No production event/getter/shared resource was disposed, no GC or artificial teardown delay added. This is limited counter-evidence, not an engine root-cause fix or a stability guarantee. The original red log remains bundled.

Subsequent scoped functional runs remain useful but do not lift this release blocker. The final candidate is WIP pending independent real input/screens, overall regression/exit acceptance, actual controller hardware, and Android export/device validation. No new APK was built or distributed. Fullscreen flags are configuration changes only; the user's phone report is not claimed fixed from desktop results.

## Neutral-navigation regression (after stage 2 WIP)

A concrete additional negative control held D-pad Right while opening the overwrite confirmation. The old router moved focus off the default Cancel onto destructive Confirm despite no new direction press. The same actual-engine probe fails specifically at that assertion with the old router. Scope/reset now gates menu direction until neutral, as well as gating buttons until release; the same 157 assertions pass with the new router. Pure tests now total232. This fixes a focus-safety boundary, not the separate native teardown blocker.

## R3 safe-area extension

Mobile-only OS safe-area/transform polling with an injectable reader passed 22 actual-engine assertions. It observes inset changes without a SizeChanged event, cancels old input only on change, preserves unchanged gestures, retains last valid layout through invalid samples while cancelling/disabling touch until validity returns, preserves keyboard/gamepad tokens and intent during unknown touch geometry, and stops after unload. Focus-in forces a fresh read. Pause tick/run/wave/camera remain unchanged. Desktop does not poll. This is a deterministic OS-boundary simulation, not Android device proof. The preset's edge-to-edge/immersive flags still require a new actual APK audit.

## R2 actual native finding, R3 minimal hint response

Independent R2 real mouse play reached wave4 in a normal Buding run, then paused/restarted and closed normally. Injected Joypad native capture also exited0. Neither is physical gamepad/mobile proof, nor does it erase historical teardown failures. The actual captures found the external controller hint covering the player's HUD and remaining outside the failure modal. That is a concrete visual blocker despite text-fit assertions.

R3 removes external hints entirely during combat and all modals, and while a menu is scrolled. Disconnect cause is retained inside the existing pause description (no additional combat banner). Menu hints use measured text bounds at the safe top, avoiding the chooser's Back button; controller operation details remain discoverable in the existing Home instructions. The updated 206-assertion engine UI probe checks these context and non-overlap boundaries; final native images are still required. Native controlled cards now use real catalog descriptions rather than short invented labels, while remaining explicitly controlled availability, not natural unlock history.

## R3 actual native and R4 unknown geometry boundary

R3 actual injected native capture passed 216 assertions, ten images and exit 0 without ERROR/leak. Independent review found the prior hint/HUD collision removed: disconnect cause is inside the pause description, modal hints absent, narrow chooser Back separate, long catalog cards and scrolled CTA reachable. Controlled floating-stick held/unregistered images visibly show appearance/removal. These are injected engine operations, not physical controller/touchscreen evidence. Earlier strict-import and teardown counterexamples remain unresolved.

R4 changes only the safe-area observer and its test. The old R3 with the new negative test fails specifically when a positive OS rectangle maps wholly outside the viewport: it previously reopened full-viewport touch. The new observer rejects that sample, retains the previous layout, and stops only touch. It starts mobile touch closed before the first validated sample; valid recovery needs a new touch-down. Keyboard/gamepad tokens and intent stay valid. Final actual-engine probe passes 28 assertions, exit 0. No Android OS/device claim, and R3 images are reused only for unchanged hint/layout paths, not relabelled as R4 native execution.
