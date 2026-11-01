# M2 reconstructed playable candidate — 2026-10-31

Status: scoped desktop prototype gates and clean source-snapshot rerun passed; remote publication is separately verified. Real-device and full native victory tests are not claimed.

## Fresh automated checks

From repository root with isolated Godot 4.6.3.NET / SDK 10.0.403: `scripts/verify.sh` passed dependency boundary, core Release build,538761 freshly executed assertions, Godot host build, actual headless import and managed120-frame smoke. Build: 0 warnings/errors; import/smoke no errors. Exact output adjacent. Warm 300 ticks measured honey32/128/512 at3.83/12.37/45.66ms; mixed12819.56ms. All zero warm allocations; mixed2,895 bounded cosmetic drops remain disclosed. These are CPU fixtures, not full graphical frame times.

`python3 tools/art/validate_assets.py content /tmp/hmi-recovered-asset-validation.json` passed10 GLB +20 SVG hashes, dimensions, ground pivots, identity roots, topology and opaque materials. Recreated original Blender assets are not byte-identical copies of lost files. Font is a renamed OFL Noto subset with included license; generators are build-time only.

## Fresh native interaction before final presentation fixes

Independent reviewer ran the actual .NET game on cloud Linux llvmpipe (software renderer, not hardware GPU). Honey completed wave 1 with12 coins. Shop could open before card selection, but Next was disabled. Bought shield2 + tonic3 + shoes5 once each; balance2=earned12−spent10; sold-out items and unaffordable gloves disabled. Free honey-radius choice enabled Next. Wave2 began125/125 HP and2 coins; Escape pause worked.

These interactions were real native UI input, not console fixtures. However card descriptions overflowed and portraits were too dark in that candidate. Both were blocking presentation failures, not ignored green checks. Fixes initialize wrap before sizing and restore preview-only0.8/0.4 lighting. Arena remains linear/direct 0.25/ambient 0.12; actual internal-wall GLBs replace placeholder boxes without changing collision extents.

Independent read-only host review found mouse shade alone did not prevent keyboard access to underlying buttons. The final candidate disables/restores background focus/buttons and synchronizes codex close on page refresh. Fresh native repeated-open/Tab/Enter/focus-loss verification is required below before publication.

## Final native and export evidence

The final wrapped-label/modal candidate passed fresh native upgrade/shop readability, Tab/ShiftTab/Enter focus isolation, rapid buy→codex redraw, focus loss→close remaining paused, next-wave 125 HP/ledger continuity, and restart100 HP/0 coins/wave 1. See M2-independent-review.md. Three runtime screenshots are retained as JPEG bytes with correct extensions. The final corrected portrait view is `final-selection-palette.jpg`; bright yellow honey and blue summon were visually verified after rotating the isolated key light. The earlier selection image is historical evidence, not the final palette.

A newly rebuilt Linux standalone export ran headlessly and printed HMI_READY/HMI_SMOKE. Fresh first and repeated Linux exports both passed actual standalone smoke. A packed-resource engine probe confirmed gameplay JSON present and deliberate builds/*.json + builds/*.tscn sentinels, global.json and source manifests absent. See M2-linux-export.md; the live runtime source matches that export manifest exactly. Old lost APK/Linux exports are not reused. Fresh Android reconstruction/export is not attempted in this milestone; real-device tests remain unrun.

Fresh native mouse joystick right/up movement and recenter passed. Clicking dodge while moving showed its disabled 1.7-second cooldown; stationary activation is intentionally a no-op. Fresh native defeat showed 5 kills/5 coins, then restart returned 100 HP/0 coins/wave 1. The actual defeat and cooldown screenshots are retained. Native full six-wave victory, window resizing, touch hardware and audio are explicitly unrun; core automated progression fixtures do not replace those native/device tests.

## Limits

One chaser archetype; no full six-enemy roster, audio, saves, meta progression, multiplayer, production signing or final balance. No Android device/emulator, hardware GPU, Unity/团结/AOT or cross-platform deterministic-float certification. Event drops and quadratic impact scans remain bounded known limits. Six24-second waves are prototype tuning, not a complete campaign.

The actual failed text-layout screenshot is retained as `pre-fix-upgrades.jpg` beside the corrected `final-upgrades.jpg`; this documents the observed defect and correction rather than reporting only successful frames.


## Final clean source snapshot

A new `HMI-Game-m2-final` source copy excluded all caches, generated import sidecars and build outputs. Running `scripts/verify.sh` there passed the dependency check, core build, 538,761 assertions, host build, 10 GLB/20 SVG validation, clean Godot import and actual managed smoke; both builds had 0 warnings/errors. The actual output is `M2-clean-snapshot-output.txt`. All 61 authored runtime files match the independently exported source manifest; all five adapter source hashes match the final native-tested assembly source. Generated default `.import`/`.uid` files are intentionally regenerated rather than published.

Publication files and full-source SHA256 manifest are in the release archive's `publication-manifest.json`. This is a verified source artifact, not a claim that GitHub already contains it. Main is not merged.
