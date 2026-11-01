# Birth-frame comparison: genuine native continuous movie, controlled 4 Hz update

This is separate from the frozen 12-image first-FramePostDraw fixture. Both sides use the same seed1030 crit Core run to collect genuine BasicMelee and Ricochet event records and verify each authoritative step against a twin simulation. Capture subsequently replays those unchanged records at prescribed presentation times; it does NOT deal damage again. No event position, TTL, alpha, geometry, Core source or attack animation changes. The only production A/B difference is original CombatFeedbackVisuals vs the isolated render-epoch birth guard.

40 updates, each `BeginFrame → optional unchanged real event Consume → Advance(.25)`, with MovieWriter `--fixed-fps 4` producing actual continuous engine frames. No screenshot concatenation. Expected duration about10s; final actual frame count/duration must be read from AVI/ffprobe, not guessed from updates. 4 Hz is an intentionally controlled presentation rate, NOT measured game/device performance. Core collection input is identically no movement, and no new Core step occurs during replay. Fixed orthographic16.8 camera and cream floor are identical between builds.

Zero-index schedule:4 Melee;12 Ricochet;20 Melee;28 Melee then29–32 pause;33 resume/expiry. All other frames gaps/expiry. Reset before each independent replay makes reuse of an original event explicit and avoids falsely claiming another authoritative attack. One event maximum per birth frame, within original12-per-render/48slot budget. Original should never show body at .25; fixed should show its birth frame and retain the last cue during pause, then expire. No damage trace/HP/coins move while this diagnostic plays.

Prebuilt native entry (only with coordinator desktop slot):
`bash /workspace/scratch/80f5bc988ef7/HMI-vfx-birth-movie-r1/run-native.sh`

Runs old then fixed, fresh XDG per process, explicit Dummy audio, output raw AVI and native log under each variant/evidence. Final trace cmp requires both authoritative collection and presentation input/camera TSV byte-identical. Process exit0 is not a clean resource gate: inspect logs for ERROR/leak. Independently decode each AVI, compare frame timing, and inspect actual body pixels before accepting. A side-by-side video may subsequently crop/scale the two original movies but must preserve provenance and may not masquerade as a new render.

Retained residual: tint starts alpha0 and can expire at a later low-FPS sample without a visible tint intermediate. This narrowly protects the body's first rendering opportunity, not all low-FPS perceptual issues. Native status remains PENDING until review. No .24s melee lifetime experiment is included.
