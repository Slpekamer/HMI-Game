# UI-MOTION-0001 — restrained interruptible UI motion study

## Status and source
Independent off published UX0004 f4aa1e45, same 949-file source as local e54a88c. Current helper/sample only; no GameUi hook, mobile input ownership or native visual acceptance. User requested UI motion; parent approved short quiet transitions, button visual recovery, selection/card cues and HP/wallet emphasis with unchanged interaction/authority.

## Contract
- Plain C# Godot-adapter helper, **not a new Node script**, no Tween/Theme/StyleBox/resources, no gameplay callbacks/commands. Core and Persistence unchanged. Manual Advance consumes host render delta only.
- 32 slots; repeated target cues replace, never queue. Overflow drops cosmetic requests. Epoch from BeginScope rejects stale page callbacks.
- Page/card/selection enter 140–180 ms with initial 82% alpha (never invisible while interactive); value emphasis 90%→100% RGB. Default no pulsing loop, flash, bounce of whole button or extra decoration.
- SelfModulate changes only the target’s own drawing, not descendants. Empty layout containers are not valid visible page-fade targets; select real labels/panels or other drawn nodes within the fixed budget.
- Whole controls: SelfModulate only, no transform/rect/focus/disabled/mouse-filter modification. Native button action is never delayed or emitted by animation.
- Optional press visual squash is **only** an explicitly passive descendant of a stable BaseButton. Native passive types only, no interactive descendants, MouseFilter Ignore/Focus None. Scale 97%→100%; parent hitbox untouched. This is the only branch called visual recovery/squash, not a claimed whole-button bounce.
- Scope/page/newrun changes: BeginScope cancels/restores before rebuilding controls. Focus loss or pause/modal: SetSuspended(true) restores, advances epoch and blocks new cues. Resume SetSuspended(false) advances again, never replays a queue. Owner must refresh motion binding tokens for new valid events. Resize/input-provider cancellation: BeginScope invalidates queued tokens before owner rebind/layout; CancelAll alone only restores and is not a lifecycle-token barrier. Owner Dispose is idempotent. Detached/freed targets are evicted safely.
- ReducedMotion(user,system) cancels existing effects and omits new effects. System preference is explicitly injected; no automatic OS support is claimed. When platform adapter cannot query a reliable API, provide the user option and keep weak default.
- Helper has exclusive ownership of SelfModulate during a cue (or Scale for decorative proxy); caller cancels before changing that property for layout/state. No styles or shared imported assets mutated.

## Proposed integration (lead owns later)
GameUi may own one controller and pass render delta. SetPage calls BeginScope before Clear. Selected page/card drawn nodes play alpha cues after layout; empty containers do not fade their descendants; actual button callbacks and one-shot guards remain unchanged. ButtonDown/Up may route to SetPressed using current epoch; focus/pause/page cancel do not play a delayed release. Selection change triggers visual cue on new preview only after ready; it must not delay ready or hide loading errors. UpdateHud sets authoritative strings/bar values first, then optional short value emphasis; animation never tweens numeric truth. Mobile input provider token is separate from motion epoch.

## Files / ownership
UiMotionController.cs; UiMotionProbe.cs.template; scripts/test_ui_motion.py; this document. No GameUi/VirtualJoystick changes while mobile worker owns input integration. No Core/config/export/package changes or remote publication.

## Verification plan
Actual-engine helper probe: every cue ends at exact baseline; old scope rejected; held input steady; invalid dt; 32 slots; repeated 100-trigger drift; reduced user/system policy; stale/removed/freed targets; double Dispose; whole button hitbox/input properties unchanged; child visual-only scaling; interactive descendant rejected; no actions emitted.

After lead integration, actual native captures/input must cover Home→selection→game→cards/shop, rapid press/release/drag-out/keyboard focus, interrupted page changes, pause/focusloss/resize/newrun, reduced-motion toggle, narrow/tall viewport and HP/wallet legibility. Headless helper pass is not that UI/user/device gate. Known engine shutdown diagnostics remain strict failures and must not be hidden.

## Current measured result (2026-11-01)
Final independent compiled fixture: **174 checks, runtime exit 0**, no runtime ERROR/FATAL/resource warning. Exact helper SHA `5a549277f19afb579efa70aa2931c60c8a0a514ea047f475056b24a036cb4756`; source copy parity checked. Fresh build: 0 warnings/errors. Strict runner **failed** after editor import returned 0 with `EditorSettings shutdown_adb_on_exit` ERROR. The same compiled isolated fixture was then run separately, establishing only scoped helper behavior, not a clean pipeline. Logs and parity are `/workspace/shared/hmi-ui-motion-review/`.
No GameUi hooks, visual animation sample, real input animation gate, device validation or automatic OS preference lookup is complete. No baseline authored file changed. Do not label this as shipped UI animation.

## Independent visible hook study (not merged)
`GameUi.Motion.cs` now owns the controller only in this copy. `GameUi.cs` has explicit small hooks (patch saved beside review evidence); no Focus/Layout/mobile provider file is edited. Home title, subtitle and primary Button draw at 82%→100% alpha; chooser summary and newly chosen button use the same brief cue; card title/detail labels enter, and a true already-picked status line gets 90%→100% emphasis. There is no world/HUD/HP/wallet change, no artificial wait before action, and no whole-button scale. ButtonDown/Up use a quiet color recovery; MouseExited/FocusExited cancel rather than force a delayed release. Page token prevents retired button signals from affecting a new page; lifecycle controller epochs are independent of input routing.

The public `SetMotionPreferences(userReduced, systemReduced)` controls these **real hooks**, but no user settings widget or persistent setting is shipped in this study. System preference remains injected. Parent owns the eventual accessible setting location and lifecycle integration. Focus and layout notifications cancel synchronously; modal/non-sample page suppression is checked at render/event entry. No automatic page re-entry animation after focus regain.

### Native plan (requires scheduled slot; not started)
Run `UiMotionVisibleProbe` under actual renderer with `--motion-capture=<existing-directory>/sample` to capture Home enter/mid/settled/press, chooser changed/settled, cards enter/settled/picked, reduced Home and narrow Home. This is a controlled UI fixture with synthetic OfferUi and emitted button signals, not a natural reward history or real input gate. Then run ordinary main for real pointer down/hold/drag-out and keyboard focus, Home→chooser selection and return, quick interruption/resize/focus loss; inspect side by side against exact UX4 source. Reduced preference must visibly suppress the samples while keeping text/status fully present. Do not mark screenshots or gestures passed before those runs.

## Accessibility closeout — actual user control (independent candidate)
The earlier API-only limitation is now addressed in this copy: Home → 玩法说明 contains a keyboard-focusable `减少界面动效：开启/关闭` button. The state description and persistence outcome appear **first** in the existing scrollable modal; no new settings page or decorative UI. This is interface animation only, not combat-effects accessibility policy. The modal uses the existing generation/one-shot action protection; each toggle immediately cancels motion and rebuilds the modal, invalidating the old button callback. Existing page/keyboard/input navigation files are untouched.

- `user://presentation.cfg`, section `ui`, boolean `reduced_motion`; independent of checkpoints. Loading happens before the first Home cue. Missing file uses the weak default. Load failure/invalid typed value is explicitly reported in the settings text, without a false saved claim.
- Save success says `设置已保存在本机。`; failure still applies the preference this UI session but says `本次生效，保存失败。重开后可能恢复原设置。`. It does not claim durability or overwrite/reset gameplay progress. No OS preference is queried; the separate system bool API remains injected.
- On save, load the same file first and preserve unknown keys/sections; update only ui/reduced_motion. Only a successful load or explicit FileNotFound permits writing. Other read/parse failures leave the existing file untouched and report session-only status. Each ConfigFile is a new unescaped native wrapper in a using scope. No shared loaded resources are disposed.
- New text initially exposed missing 幅/微 glyphs; wording was simplified to `短时动效`, preserving exact released font bytes. Final runtime glyph scan has zero missing codepoints; no desktop fallback assumed.
- Actual engine preference fixture: **25 checks / exit 0**, including real file readback and UI recreation, false/true behavior, stale modal and page callbacks, invalid bool and deterministic filesystem failure. Build 0 warnings/errors. Strict import still fails on the separate known editor shutdown ERROR. Native user toggling/capture not yet run at this checkpoint.

Preference-only diffs versus the prior native-approved motion sample are `shared/hmi-ui-motion-review/preference-study/GameUi.cs.patch` and `GameUi.Motion.cs.patch`. Only GameUi ShowInstructions is delegated; all setting behavior lives in the existing Motion partial. No changes to GameUi.Focus, layout/input providers, Core/Persistence, hit rectangles or fonts.

### Final preservation-aware preference gate
Supersedes the initial25-only candidate:29 normal UI/filesystem assertions pass; unknown keys and other sections survive a toggle. Separate malformed INI safety run passes4 assertions with two expected ConfigFile parse ERROR diagnostics retained, so that run is not a clean-log claim. Original malformed bytes remain unchanged. Main fresh-test-XDG smoke exit0. Native/prebuilt scripts and exact hashes are now under `shared/hmi-ui-motion-review/preference-study-r2/`; earlier preference-study is historical and must not be launched as final.
