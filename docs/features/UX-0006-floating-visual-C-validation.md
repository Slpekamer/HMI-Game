# Floating joystick C: narrow visual subtraction

Integrated 2026-11-01 onto `eda890a`, as a local WIP increment; no remote publication.

## Exact scope

Only `TouchJoystick._Draw` changes: disk fill/shadow are lighter; a thin white rim with a subtle dark under-rim and translucent thumb remain visible. Two bounded arc draw commands are added. No performance improvement is claimed. Diameter, travel radius, legal touch region, capture, routing, deadzone, coordinate conversion, pause/reset and Core rules are unchanged. All bytes before `_Draw` match the integration base.

The accepted C file SHA256 is `16d2ba54f4a75c0e60a571c91dffa3db238570cd9c75f381e27f887fb2a660d7`.

## Actual visual evidence (isolated C before integration)

- Native C: 420 frames, 30 fps, 14 seconds, normal exit 0 with no ERROR/FATAL/ObjectDB/resource diagnostics. This is offline MovieWriter evidence, not a performance benchmark.
- Actual B/C 18 pointer events and complete 420-row authority/camera trace are byte-identical. Only appearance changed. A/C compares old fixed versus floating input, so authority routes intentionally differ.
- Independent review accepted less obstruction at frames 31 and 211, identifiable rim/thumb, and removal after release at frame 60. The cream-floor rim is weaker than B; strong sunlight, small phones and real finger occlusion remain untested.
- Fixed test viewport 1280×720 inside a recorded 1180×812 desktop window; not Android fullscreen or real-touch hardware verification.
- Original B obstruction evidence is retained. No new HUD/companion-panel change is included.

Evidence: `/workspace/shared/hmi-mobile-input-ab/C-visual/native/independent-review.md`, `native-proof.json`, A/C and B/C full continuous comparison videos. Compact evidence ZIP SHA256: `69fd8f0e4120733ad77fec054ddf9f59f09727002da68c28009cdf6fd9392025` (raw AVI remains local and is hash-recorded, not inside this ZIP).

## Post-integration checks

- Main `dotnet build HmiGame.csproj -m:1`: exit 0, zero warnings/errors. DLL SHA256 `789b51ad9be2e197897b52e7c26f86ff4b484456815d8f9edd4c508fec524abd`.
- Pure input router: PASS 232, exit 0.
- Floating input: strict runner stopped at editor import `EditorSettings ... export/android/shutdown_adb_on_exit` ERROR despite import exit 0. The exact compiled isolated copy was separately run: PASS 130, exit 0, no runtime ERROR/leak.
- Safe area: the same strict import blocker was retained. Exact compiled isolated copy separately ran PASS 28, exit 0, no runtime ERROR/leak.

These are scoped build/behavior checks, **not a green strict aggregate**. No runner was weakened, no warning filtered, and no new native integration run or device claim is inferred. Earlier teardown failures remain historical evidence.

Post-integration logs, strict import diagnostics, hashes and `results.json`: `/workspace/shared/hmi-mobile-input-ab/C-visual/integration/`.

Retained local evidence: `docs/evidence/UX-0008/floating-C/`. Full controlled videos and compact source evidence are preserved as exact Library identities in its `library-deliverables.json`.
