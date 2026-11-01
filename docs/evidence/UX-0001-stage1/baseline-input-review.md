# Independent experience baseline — 2026-11-01

## Scope and provenance
Actual production main from immutable `godot-toolchain-verification/experience-audit/baseline`, not the concurrently edited experience-r1. Exact four hashes in baseline-source-sha256.txt. Normal keyboard/pointer input, seed1030, fresh isolated XDG; no simulation hooks or forced wins. Desktop X11 / Godot4.6.3 Compatibility / llvmpipe. This is NOT an Android run or a performance measurement.

Recording baseline-input.mp4 is a transcode of actual Movie Maker AVI: 1280×720, 24fps, 1706 frames, 71.083333s. Full MP4 decode exited0; game PROCESS_EXIT=0; window inventory confirmed removed. Actual app window1180×812, logical16:9 letterboxed. Fixed-frame capture wall time142s differs from encoded71s. First earlier recording auto-finished at1200 frames before input and is retained separately as baseline-selection-idle.avi; not claimed as interaction.
AVI SHA 1a1c088c407ee5a719ea8c2ebaa14791f1aa35ee20720462d9a6155a12ec84c3
MP4 SHA c4e06c36f6b0ad5c64b5d8c01a931efc8b707ac60bae9314e8f826a1d61bb09d
Transcode: ffmpeg -i baseline-input.avi -c:v libx264 -preset fast -crf20 -pix_fmt yuv420p -an baseline-input.mp4 (actual CLI uses separate `-crf 20`).

## Reproduced / observed
- Cold start goes directly to chooser, no Home. Existing five portraits are static2D and background is visually dense. Confirms missing-Home complaint and old contract, not a new implementation failure.
- Held selected Kaotuan card changes decorative blue/cream frame to generic gray-beige flat rounded rectangle (encoded frames430 before /460 held). After release its focus/hover appearance can remain generic yellow until focus changes; baseline-held-release.jpg records this. This is a concrete press/focus-style mismatch, not just an inferred source defect. Same sequence also held Buding.
- Normal Kaotuan and Buding combat shows enemies with no visible health indicators. Player/companions remain distinguishable but damage-to-enemy feedback is absent.
- Real WASD movement, reversal, automatic attacks, Esc pause/resume and ending→selection→Buding Start occurred. Kaotuan died in wave1, six earned/zero spent; not a survival or balance verdict. Buding was paused before graceful window close.
- Game-scale sampled frames show changing body orientation/hand pose but very weak leg-step readability. This does NOT prove the Android animations are absent. Source full-body Attack priority and supplied bone evidence support a targeted moving-Attack layering investigation; keep independent actual same-route motion comparison.
- Existing HUD, companion panel, joystick and decorative walls occupy substantial attention/space. Selection has many simultaneous borders, ribbons, badges and highly detailed scenery; simplification has an actual visual basis rather than font-fit alone.

## Temporal limits and next gate
Sampled contacts kaotuan-motion-samples.jpg use frames700..760/10; buding-motion-samples.jpg use1420..1480/10. Seven chronological crops + one unused black tile, no interpolated frames. These samples and source data are NOT full-speed comfort certification. Shadows remain a user-reported temporal blocker: current images do not independently identify a flicker cause. Preserve this original clip for same-camera single-variable shadow/contact comparison; do not claim a still proves stable shadows. Floor top-.05 vs foot0 is a separate geometric gap reported by lead, to compare independently from lighting/bias/Run.

APK payload inspection by installer rules out absent animation resources, not Android clock/render execution. Device model/OS/GPU/touch/insets and actual Android playback remain unverified. Candidate Home/3D preview, native layers and floor changes must each pass their own actual input/pixel gates before final combined regression. Acceptance-matrix-20261101.md is the controlling matrix.
