# OBS-0001 independent review
2026-11-01. Read-only ADR/source review plus isolated full-suite execution.

Copied src/Core, content and CoreTests to HMI-metadata-independent-review excluding bin/obj; exact Core hashes stored in source-core-hashes.json. After execution all source Core hashes still match the worker's frozen source. No worker production edits.

Added independently owned CombatObservationIndependentTests.cs with21 checks. Fresh isolated build0warnings/errors; full run exit0 PASS624734 (worker baseline624713 +21). Key negatives: pending death invalidates stamped motion before Destroy; same slot new generation cannot inherit it; malformed IDs safe; post-pass position cannot retroactively change observed slow; drop-newest retains first512; optional overflow cannot suppress actual damage or original event; old index invalid after Step reset; counter resets only scope; failed restore does not clear existing transient outputs; successful restore clears and does not replay; Begin token remains one-shot.

ADR permits ephemeral output only, not rule/version/checkpoint changes. Source review confirms separate buffer and actual applied-value capture. Worker-owned5850tick authority trace and5old-checkpoint parity are additional evidence, not rerun under this independent test. Existing full regression suite was freshly rerun here.

Concurrent work active: timing values are environment observations, not mobile performance acceptance. No Godot presentation integration, Android hardware or VFX fidelity is implied by this Core gate.
