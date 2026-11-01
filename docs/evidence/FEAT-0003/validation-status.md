# FEAT-0003 validation status — 2026-10-31

Fresh aggregate in isolated `HMI-Game-2d-selection`: PASS. Actual command: source pinned toolchain env, then `bash scripts/verify.sh`. Full log: aggregate.log.

Passed:
- Architecture and preserved-notice checks; core and Godot host builds with zero warnings/errors.
- 538343 freshly executed core assertions, including removed-Dodge and gloves regression. Warm core benchmarks allocate zero bytes; mixed128 stress drops2895 bounded cosmetic events as previously documented, not a lossless-event claim.
- All existing static/animated asset validation, UI assets, engine import, animation/camera probes, catalog text and layout probes, headless playable smoke.
- Existing actual-engine actor/pickup generation/lifecycle regression.
- New isolated actual-engine selection probe: all five portrait IDs and summaries, repeated selections, captured hero/seed, keyboard focus API, fresh combat start, repeated codex close/back, restart, queued-free HUD viewport cleanup, root-menu re-entry, main3D rendering restored in combat and inherited viewport state restored after UI deletion.
- Five true-alpha PNGs decode, hashes and significant silhouette bounds verified. Source RGBA pixels total31,463,732bytes (~30MiB); this is a decoded source-size estimate, not measured GPU memory.
- Pure-core source hashes are identical to prior visual revision.

Passed native graphical/input review: all five identities/aspect/fit, bright-yellow honey, click badge/summary, keyboard Tab/Enter, restored real3D combat/movement, pause/restart, repeated second hero/newrun, codex close/back and pause. Actual screenshots included; known environment warnings are software llvmpipe VSync unsupported and dummy audio fallback, not gameplay exceptions. Owner visual acceptance remains pending. Existing pink in-combat HUD model portrait crops its head; this pre-existing issue is outside the 2D-selection change and not called fixed. Exports, real mobile hardware and Unity were not rerun for this increment. Prior exported APKs do not contain this UI revision. No remote commit/push verified. In-game3D models remain the prior rejected batch pending separate natural-standing art work; these PNGs do not imply new mechanics.
