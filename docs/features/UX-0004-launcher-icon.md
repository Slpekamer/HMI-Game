# UX-0004 — Explicit Kaotuan launcher icon

## Request and minimal scope
Parent authorized correcting the actual Android export error `No project icon specified` using the existing new Kaotuan identity, without redesigning the roster. The generated foreground is the unaltered original 1254×1254 RGBA image; official Godot export creates platform sizes. A separate code-native cream background is explicit, avoiding template branding. Source provenance and the rejected first generation remain in `content/ui/launcher/PROVENANCE.md`.

Only project/export configuration, icon assets, an icon test and its verify entry change. No C#, Core, persistence, model, animation, gameplay or camera changes. Public Android preset is `.foodpreview`, versionCode 6; no signing credentials. Packaging exclusions explicitly omit source manifests and Core/Persistence bin/obj caches. Both platform presets match the tested isolated exclusion policy.

## Verification and limitations
- Exact original PNG SHA, dimensions, alpha, conservative opaque safe circle, background and config references pass Python checks. Actual imported Texture2D sizes and Android fields pass a Godot probe; build and Linux smoke pass.
- Actual offline Android code6 export exits 0, Gradle exits 0; the previous **No project icon ERROR is gone**. Signature, alignment, assemblies, licenses/JSON, actual imported hero clips/morph resources and adaptive XML/mipmap structure pass independent packaging checks.
- The separate known `EditorSettings` / `shutdown_adb_on_exit` error still occurs **after export DONE**. One bounded fresh-config control did not fix it. This is a labeled test candidate, not a claim that every diagnostic is green. Source gameplay aggregate evidence remains UX-0003; byte parity establishes that this increment does not alter those production files, rather than inventing a new full aggregate run.
- Godot orientation setting 4 exports Android `userLandscape` (11), not literal `sensorLandscape` (6). Landscape variants respect the user's rotation preference. Desktop portrait compatibility does not promise Android portrait play.
- No Android device/emulator execution was available. Phone touch, launcher behavior, shadow flicker and short-stride residuals remain unverified/unresolved as previously recorded.
- Actual exported mdpi/xxxhdpi WebP and circle/rounded mask simulations passed the scoped visual review recorded in the evidence directory. Raw foreground alpha1 specks were not edited or called pixel-perfect.

## Source-to-package boundary
The frozen r9 candidate was exported before adding repository documentation/evidence. Final public preset incorporates the exact exclusions already used in that export. Final application-input hashes must be reconciled against the tested copy; docs/evidence are not silently described as having been compiled. New UX binary/evidence Library identities preserve the previously delivered BUG4 APK and download history. Remote publication remains separate authorization.
