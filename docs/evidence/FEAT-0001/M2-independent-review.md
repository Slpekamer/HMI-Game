# FEAT-0001 independent reconstruction review

2026-10-31 UTC. This evidence is fresh after workspace reset; historical evidence is not substituted.

## Core
Independent isolated source-copy run: `source godot-toolchain-verification/env.sh; dotnet run --project /tmp/hmi-review-final/tests/CoreTests -c Release`.
PASS 538761 assertions. Log: final-independent-core-tests.log.
300-tick warmed simulation measurements: honey32 3.82ms,128 12.06ms,512 46.60ms; mixed128 19.42ms. Zero measured managed allocations in each timed loop. Mixed workload dropped2895 bounded cosmetic events; gameplay authority does not consume cosmetic events. These are cloud CPU simulation measurements, not rendering or mobile benchmarks.
Pure netstandard2.1/C#8 core, generation entity lifecycle, explicit resources/systems, fixed60Hz seed, bounded scoped commands/events, validated neutral data definitions and no Godot types. Unity/IL2CPP compatibility remains unverified.

## Fresh native pass before final presentation fixes
Linux Godot .NET4.6.3 Compatibility, software Mesa llvmpipe. No hardware GPU/audio/device certification. Honey selection and keyboard movement observed; first24-second wave completed,12coins earned. Entered shop before free card: Next disabled. Bought shield2,tonic3,shoes5; correct ledger earned12/spent10/balance2; stock and insufficient funds controls disabled. Chose honey radius card, Next enabled. Wave2 started with125/125health and2coins. Escape paused.
Blocking detail text overflow found on upgrade/shop; screenshot native-upgrades-overflow.png. Lead changed wrapped-label initialization order and bounds; final native retest pending. Core worker found modal background keyboard focus leak; lead added background disable/focus lock; final native retest pending. Preview lighting improved in source; final native retest pending.

Do not mark final native milestone green from this intermediate report. Reconstructed final host build/import/smoke is lead-reported green, and final core source is independently green as above.

## Final fixed native run (00:38–00:42 UTC)
Final source rebuilt by lead before launch. PASS scoped native regression: selection previews readable; honey movement/combat; integrated GLB internal walls and unclipped arena; full upgrade/risk and four shop descriptions readable; free card→shop, shield purchase5earned/2spent/3balance; next wave125HP. Codex Tab/ShiftTab/Enter in combat/shop did not activate underlying buy/next. Buy→immediate codex, Escape/reopen/Escape left flags synchronized; nextwave+pause still worked. Actual desktop AltTab verified game focuslost; return then Escape closed codex into paused21second state, not autoresume. Restart returned selection; fresh run100HP/0coins/wave1. Blue hero and independent companions visually present in final-combat.png.

Evidence: final-selection.png, final-combat.png, final-upgrades.png, final-shop.png, native-fixed.log. No blocking native defect observed after final fixes. Nonblocking presentation: models remain small and relatively flat vs polished concept; long shop descriptions fit but have tight bottom spacing. Native full six-wave victory, defeat ending, resize/mobile/touch hardware/audio were not tested in this final reconstruction run. Core automated runs cover six-wave/death/trait mechanics; that does not replace device QA. Final scope is maintainable playable prototype, not production completion.

Fresh mouse/touch joystick and dodge-button actuation were not tested; keyboard movement was tested. Final native log contains environment-only unsupported Vsync and ALSA dummy-audio fallback; no gameplay exception observed. Adapter source fingerprints saved in final-native-source-sha256.txt; isolated core copy compared identical to final core source excluding build caches.

## Final palette/pointer/defeat follow-up (00:55–00:57 UTC)
Preview directional-light yaw changed to152 degrees; fresh native portrait rendering PASS: honey bright yellow, blue cotton and remaining pastel identities clearly readable. `final-selection-palette.jpg` supersedes prior selection screenshot only. Mouse joystick drag plus held directional click moved blue hero from center to right wall; separate upward held click moved hero upward, release visually recenters joystick. Dodge button clicked with D movement held triggered visible disabled1.7s cooldown (paused for capture): `final-dodge-cooldown.jpg`. Stationary dodge is intentionally a no-op, not a failure. Real touch/multitouch hardware still untested.
Actual native defeat observed with5kills/5earned/0spent/5balance; `final-defeat.jpg`. Defeat-screen restart then newrun verified100HP/0coins/wave1; no prior shield/card carried over. No final native six-wave victory run. No further blocking defect observed. Source SHA256 in final-palette-input-source-sha256.txt; fresh log native-palette-input.log. New screenshots correctly stored asJPEG .jpg (older shared .png files contain JPEG bytes; repository copy renamed correctly).
