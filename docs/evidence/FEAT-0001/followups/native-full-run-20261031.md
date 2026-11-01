# Post-M2 native full-run check — 2026-10-31

This evidence extends, rather than rewrites, the immutable M2 release's earlier test boundary. No gameplay source, content or frozen artifact was changed.

## Actual normal-control victory

Independent reviewer used the native Godot .NET game on cloud Linux software rendering, not a simulated console fixture. Character `honey`, seed `1030`. No debug wave skip, free coins or direct state editing.

Five actual choices: `honey.wide` → `summon.root` → `honey.wide` → `crit.root` → `summon.team`.

Purchases: shield after wave1; shield + tonic + shoes + gloves after waves2 and3; shield + tonic after waves4 and5. Total spent: 2 +15 +15 +5 +5 =42. Ordinary movement and pauses used to navigate the arena and menus.

The actual victory screen shows all six waves complete, 136 enemies defeated, 136 coins earned, 42 spent and 94 remaining. The ledger invariant is preserved:136−42=94. Screenshot: `full-run-victory.jpg`.

This closes the native full-six-wave path gap for this representative character, seed and choice path. It does not certify every hero/build, final balance, touch hardware, audio, Android devices, hardware GPU performance or Unity compatibility. The original M2 archive correctly reported the narrower checks available at its freeze time.


## Desktop resizing followup

The reviewer also observed the victory/selection pages at 1180×812, 1280×720, 1320×720 and 1359×750. They remained readable, with aspect-preserving letterboxing and no clipping. See `resized-selection-1359x750.jpg` and the independent `full-run-review.md`. Smaller phone/portrait sizes were not tested; no responsive mobile certification is inferred.
