# UX-0006 — Floating touch input and replaceable input providers (independent candidate)

## Baseline / ownership
Exact published UX0004 `f4aa1e452ecfab70bd235d635ced0bbfd3b1a629`, tree `bc8e6c5e453917a58963d2f1eb3c97ad149c653a`. This work is isolated in HMI-Game-mobile-input-r1. It does not include UX0005 feedback or exit experiments. Core/skills/save rules/character assets stay byte-identical.

Owned implementation files: `TouchJoystick.cs`, new pure-C# `Input/MoveInputRouter.cs`, new `GameHost.Input.cs`, narrow host wiring in `GameHost.cs`, `GameUi.cs`, `GameUi.Layout.cs`; new input unit/engine probes and updates to old fixed-stick assertions in ResponsiveUiProbe/HostResponsiveProbe. No parallel worker edits these files in this tree. UI-motion worker owns another tree/helper.

## User contract
- Press in the legal left combat region establishes a fixed center for this gesture. Drag produces clamped movement. Release hides the complete stick, clears movement and allows a different next center. It is not a continuously chasing joystick.
- One active pointer/device; another touch cannot steal it. Release/cancel, focus loss, pause/modal/page change, safe-area/viewport change, new run and provider disable/unregister reset capture and old vector.
- Starts occur only after GUI handling and additionally exclude actual visible HUD control rectangles and OS safe edges. Current gestures continue outside their start region, then clamp, so sliding does not drop motion at a boundary.
- Godot pointer positions are viewport coordinates; use the inverse canvas transform exactly once. OS screen safe-area mapping remains in the existing layout adapter. Do not double-apply physical scale.
- Weak optional visual only; keyboard movement and Core camera-to-world mapping remain unchanged.

## Provider API / reversible defaults
`MoveInputRouter` is ordinary C#, no Godot/Unity/Core reference. It exposes Register, Enable, Disable, Unregister, Token, Submit, Resolve and ResetAll. Tokens include provider identity and monotonically increasing generation. Old generation input is rejected after cancellation/source switch/removal/re-registration. Registry is bounded, priorities explicit, tie order stable. The router never touches UI or dispatches ECS commands.

Sources: keyboard(priority30), gamepad(20), touch(10). Resolve selects the highest-priority **nonzero** valid sample, never sums vectors. Releasing to zero cannot preserve stale movement. A source switch clears and invalidates the old source; polled keyboard/gamepad can submit a fresh sample next frame, while touch must begin a new gesture. Connected gamepad disables touch availability (and visual capture); disconnect optionally restores touch (default true). Axis noise below the configured radial deadzone cannot steal keyboard control. Device connect alone does not inject movement.

Defaults are centralized: touch deadzone0.08, gamepad deadzone0.20, keyboard0. The old touch implementation had **no explicit deadzone**, so0.08 is a deliberate reversible input-feel change, not a byte-identical behavior claim. Rescale magnitude beyond deadzone to retain full-scale movement; Core speed and acceleration rules do not change.

## Verification plan
1. Pure router console tests: register/duplicates/bounds; disable/remove/re-register and stale tokens; switch clearing; no summation; radial bounds/NaN; paused reset; priority/noise; no zero/active oscillation under held polling.
2. Actual-engine controlled input: press/drag/release at two centers; all cancel paths; active ID; unrelated finger; mouse; UI rejection; left/right/OS safe edges; transformed canvas at six sizes; invisible when idle; hotplug device notifications injected only for deterministic tests.
3. Host integration: same accepted move intent follows unchanged camera basis into ECS; pause/new-run clears; actual device connection/hide/unregister paths. Old fixed-stick expectations become floating-start+drag expectations, not deleted assertions.
4. Actual desktop mouse/resize/focus/pause/restart review when lead provides a desktop slot. Real touchscreen/gamepad and Android fullscreen remain separate, unverified until target evidence. Editor shutdown diagnostics remain failures, not filtered.

## Android fullscreen scope
Existing project uses canvas_items and runtime Expand, Android orientation4. This does not prove phone surface/cutout/system-bar coverage. Installer owns package/manifest audit; actual user model/version/screenshot is pending. No orientation change, SDK change or APK export in this input step.

## Migration
Unity replaces touch/device/canvas drivers and rendering. Pure router/provider tokens/MoveIntent policy and its tests remain reusable. Core remains the sole movement authority; input providers do not know damage, cooldown, inventory or save state.

## Approved follow-on: complete gamepad UI (separate stage)

- Keep one selected device; a second connection cannot steal it. Active disconnect clears input and requests authoritative Pause without changing checkpoint state. It does not hand movement to another already-connected pad. Reconnection/selection requires neutral axes before gameplay resumes.
- Stage 2 owns `GameHost.GamepadUi.cs`, `GameUi.Focus.cs`, engine-free `Input/GamepadMenuRouter.cs`, and their probes. Left stick remains movement; D-pad/stick navigation has deadzone, hysteresis, initial delay and repeat interval. South confirm / east cancel labels are configurable; Start pause, shoulder codex paging/scrolling only when that UI is active. No dodge/gameplay actions are invented.
- Consume every raw Joypad event before Godot default `ui_accept` / navigation handlers, including unselected devices. Explicit selected-device routing emits at most one semantic action. Confirmation requires release after page/modal generation change; no held-confirm repeated shopping/rewards.
- Persistence modals retain their existing cancelability and one-shot generation. Default focus is non-destructive Cancel where available; cancel never bypasses fail-closed save failure. Focus navigation stays within current page/modal, scrolls focused card/button into view, and permits codex scrolling.
- Injected device events and actual engine page actions are required; physical Bluetooth/USB gamepad behavior remains unverified without hardware.

## Mobile fullscreen configuration scope

This independent candidate explicitly sets Android `screen/immersive_mode=true`, `screen/edge_to_edge=true`, and startup `window/stretch/aspect="expand"`. Existing orientation 4 remains (export maps it to Android userLandscape). Background may extend into display edges; input/controls use the safe rectangle and additional joystick-radius inset. These settings are not proof of the reported phone issue being fixed: actual new APK flags/manifest and device screenshots are pending separate verification.

## Centralized input thresholds

Touch radial deadzone 0.08 is a deliberate new sensitivity policy (old touch had none). Gamepad center is 0.20 with 0.02 hysteresis: engage above 0.22, release at or below 0.18, normalized magnitude remapped from 0.18. All limits preserve Core speed. Keyboard has no deadzone. Gamepad is neutral-gated after scope/reset; held keyboard may submit a new sampled intent without one-frame oscillation. Touch must always make a fresh down after a reset or another source wins.

## Stage 2 concrete controls

Selected device only: left stick moves during combat, D-pad/left stick moves menu focus (0.55 engage / 0.35 release, 0.35 s initial delay / 0.12 s repeat, one cardinal move per sample); south/A confirms, east/B safely backs out, Start pauses, north/Y opens/closes the build codex. Right stick scrolls codex/long content. These are Godot standard semantic button identifiers, not a guarantee of every controller's printed labels. Confirm/cancel bindings and hint labels are adapter-configurable. No raw Joypad event reaches Godot's default GUI actions, so a second pad cannot bypass selected-device ownership. Keyboard GUI actions remain unchanged.

Stage 1 evidence: pure router 120 assertions and standalone actual-engine floating joystick 100 assertions passed. Strict import remained red due the existing EditorSettings shutdown diagnostic; the same compiled fixture's behavior was run independently, not represented as a clean full pipeline. Actual native/touch hardware still pending. Stage 1 archive is commit `0f2d047d7412cfe54f325952d36dac95a2332a6e`, a WIP rather than release.

The current configuration facility is a **code-level adapter API**, `ConfigureGamepadConfirmCancel(confirm, cancel, confirmLabel, cancelLabel)`, not a player settings/remapping screen. It changes semantic bindings and displayed labels together and resets the held-button gate. Default labels describe face-button positions (下方键 / 右侧键; 上方键 for codex), not a claim that all controllers print Xbox A/B/Y. `Start` is the default pause semantic. Hardware-specific mappings and printed legends require actual target-controller validation.

After the active device disconnects, another already-connected pad is not automatically promoted. It can explicitly claim single-player ownership by pressing Start; that first press only selects it, never unpauses or confirms. A release and neutral stick are required before subsequent actions. The movement-provider API supports removing touch entirely and registering it again without stale capture or sampler exceptions. Runtime UI/hitbox data and input-routing generations remain separate from UI-motion animation generations.

## R3 OS inset observation boundary

`GameHost.SafeArea.cs` reads a readonly snapshot (physical safe rectangle + viewport screen transform) at most once per 0.25 seconds during mobile frames, and immediately on focus-in. Desktop has no mobile polling. `ConfigureSafeAreaReader` is a test injection seam, not a simulated-device success claim. Only changed valid snapshots trigger layout. The snapshot is a struct, with no per-read subscription/list/task allocation. Transient invalid rectangles/transforms retain the last valid layout but invalidate capture and disable new touch acquisition until a valid sample returns; this transition resets input only once and does not change camera or Core. Changed physical mapping also cancels capture even when the resulting logical rectangle happens to match; unchanged input mapping and safe rectangle preserve a held gesture. Unload disables polling and releases the reader delegate. Existing camera framing is unchanged, and paused layout updates do not step Core.

An actual-engine injected-OS probe passed 18 assertions: OS inset changes without a viewport size event, same sample/no reset, physical transform change, forced focus-in, bounded reader calls, paused authority/camera preservation, invalid transient sample, desktop no-op, and unload. Phone system bars/cutout behavior still requires real hardware verification.

Unknown mobile safe geometry is fail-closed for touch, including the interval before the first sample, non-invertible transforms, and positive rectangles with an empty viewport intersection. Retain the last valid layout; do not invalidate keyboard/gamepad state. Restored validity permits a new down, never resumes an old capture. OS-specific safe area values and edge-to-edge behavior still require Android validation.
