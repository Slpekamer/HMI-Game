# Work-in-progress development archive — NOT A RELEASE

Branch: `wip/mobile-ui-20261101`

Application source commit: `dab91dc3f62724286d2ac54d74053edac7043620`. Published `dev` baseline remains `f4aa1e452ecfab70bd235d635ced0bbfd3b1a629`; this archive must not move `dev` or `main`.

## Scope
Floating joystick providers, generic gamepad navigation/hotplug, safe-area handling, accessible UI motion, and lighter joystick drawing. No new combat VFX, wave rule change, APK or remote release is represented.

## Scoped evidence
Main build 0 warnings/errors; router 232, floating 130, safe-area 28; earlier joint input/UI 30+4 and controlled native/normal mouse checks. Actual B/C joystick traces are identical; A/C fixed-to-floating routes differ as expected.

## Failed, untested and unresolved
Strict runner remains blocked by retained EditorSettings shutdown_adb_on_exit ERROR. Historical intermittent ResponsiveUiProbe script_bindings teardown fatal is unresolved, not washed away by later clean samples. No physical gamepad, phone fullscreen, touch hardware or Android validation. Attack animation readability remains inadequate and is being addressed in a separate asset pilot.

## Durable evidence
Library `libfile_3afb88c8626c81919f68157ff95c37f2` version 1. Its outer manifest identifies the exact saved source; historical manifests inside source directories remain historical.

## Publication-only differences
Only this status document and a README warning are added; no application code/content changes. Raw AVI exclusions: 0. Full omitted-file hashes are recorded in the publisher handoff manifest. This is authorized development archiving despite known problems, not completion approval, CI success, or APK delivery.
