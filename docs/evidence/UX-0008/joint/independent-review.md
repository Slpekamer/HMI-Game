# Mobile input + UI motion joint scope

Final exact production source copy: `HMI-mobile-motion-joint-probe`; source hashes in `final-hashes.json`, production diff empty against integration root. New test files only were released to the integration tree; this worker made no production edits.

## Actual results
- Build: zero warnings/errors. Fresh editor import exits 0 but records the known `EditorSettings ... shutdown_adb_on_exit` ERROR. This remains a strict pipeline failure.
- Final headless actual-engine run: **30 assertions, exit 0**, no runtime ERROR/FATAL/leak (`run-final-focus.log`). Isolated temporary XDG data/config, no player save touched. No native desktop or physical controller claim.
- Real Viewport.PushInput Joypad opens Home instructions, toggles reduced-motion preference, confirms a file readback, rejects held confirmations across modal reconstruction and old detached callback replay, rejects device 8, closes and restores a visible Home focus. Held close cannot activate Home until release.
- A valid SpinBox LineEdit focus chosen before the new-scope gamepad poll survives that poll and eight repeated samples; focus is not forcibly reclaimed every frame.
- Motion press and cancellation preserve button rect/scale, restore original visual, retire active scope on modal/combat; actual injected touch cancels at focus loss and authoritative pause. UI advancement leaves paused authority unchanged. QueueFree completes.

## Concrete failure and fix
The original integration closed the modal but left GuiGetFocusOwner null. `run-second.log` is the first precise failure. A separate copy with only old GameUi.Focus.cs restored fails at the same `close returns valid visible Home focus` assertion (`run-controlled-red.log`, exit 1). The parent added once-per-navigation-generation restoration, preserving existing valid focus in the current scope. Final 30 checks pass with that exact source. The final probe's later SpinBox test was strengthened after the controlled red; code preceding the failing close-focus assertion is identical, and the initial red does not depend on that later test.

## Test corrections retained
`run-first.log`: test substring "关闭" accidentally selected the reduced-motion-off toggle rather than exact Close; changed test to exact text. `run-diagnostic-not-acceptance.log`: focus assertion deliberately reported only to inspect later tests; its last assertion incorrectly assumed Pause command always increments tick. Fixed test captures tick after authoritative Pause Step and compares only subsequent cosmetic calls. Neither diagnostic is acceptance or a product defect.

Original historical mobile teardown abort and strict import error are not erased by this scoped green. Android fullscreen, actual OS insets, physical gamepad/touch and full integration native visual checks remain outside this result.
