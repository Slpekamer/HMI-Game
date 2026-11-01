# FEAT-0002 — Reference fidelity and animated character presentation

Status: MODEL VISUAL GATE FAILED — owner explicitly rejected the revised model screenshot again on 2026-10-31. UI/core technical validation is separate. Source: owner APK feedback on 2026-10-31: UI does not reproduce approved designs, static models slide, model detail is insufficient. Follow-up explicitly withdraws dodge until its design is decided.

## Baseline and scope
M1–M3 publication snapshots remain immutable. Their functional QA does not constitute owner visual acceptance. This increment starts from M3 in a separate workroot. Gameplay/economy stays unchanged except removing active dodge. Reference images are concepts, not runtime evidence.

## Design before implementation
- Restore reference composition: immersive continuous candy arena, readable actors and walls, compact portrait/health HUD, split wave/time pills and companion tray. Camera and cosmetic scales must not alter authoritative collisions.
- Rebuild menus with a candy-world backdrop, cream/plum title hierarchy, dimensional color-coded card frames, dominant portraits/illustrations, selected badge and summary. Keep actual effect/downside text and ledger; never reproduce illustrative concept values as gameplay facts.
- Refine model silhouette, face, limbs and materials as well as adding actual multipart Idle/Run/Attack clips. No root-motion authority. Animation completion never deals damage or changes ECS state.
- Thin presentation driver consumes fixed-tick position snapshots and scoped combat events. Entity generation replacement removes presentation state. Idle/Run loop; Attack is a bounded cosmetic one-shot. Pause freezes playback, restart discards state. Events are consumed each fixed step, not just once per render.
- Newest owner request overrides the older reference dodge button: no button, Space binding, active command effect, cooldown, speed boost or invulnerability. Historical artifacts are retained as historical only.

## Acceptance gates
Independent clean core/build/import and regression aggregate; no-dodge command regression; menu/modal/focus and ledger regressions; actual native screenshots of all four screens compared with references; actual movement/idle/attack video showing articulated poses, not root translation alone; lifecycle/pause/restart checks. Android changes need fresh build and owner/device review; desktop software rendering is not hardware certification.

## Evidence and decisions
Pending. Do not call this complete based on a headless build or prettier asset render. Keep user review outstanding until actual runtime output is delivered and assessed.

### Presentation iteration notes
First native revision exposed real minimum-size overflow: texture nine-slice margins were reused as content margins. These were not accepted as merely polish. Second iteration improved world coverage and menu hierarchy but retained shop overflow and distorted short buttons; fixes require another native pass. Refined eight-model candidate is integrated pending final QA. glTF does not encode loop intent, so adapter explicitly applies Idle/Run linear loops and Attack non-loop; import probe verifies sampled non-root articulation after that policy. New-wave tick rollback resets presentation speed and one-shot state, preventing stale run state across waves. Camera now trials a fixed diagonal offset; screen input maps through its horizontal right/back vectors, with no collision or core movement change.

## Owner rejection and new gate (2026-10-31 02:24 UTC)
The owner said “这模型好丑啊” after viewing the revised actual selection screenshot. This is a failed visual gate, not an optional polish note. Stop expanding the current model batch and stop new APK delivery. Preserve technical test evidence without presenting it as visual completion. The next art step is ONE blue 棉队 character, rebuilding silhouette/face/proportions and presenting front/side/three-quarter static views for explicit owner approval BEFORE animation and other heroes. Current model files remain a rejected historical technical candidate; no automatic integration of the next art iteration.
