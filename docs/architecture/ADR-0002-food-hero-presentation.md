# ADR-0002 — Food heroes: generation-scoped presentation and independent facial morphs

Status: implementation proposal accepted for this authorized increment; runtime assets and device performance remain unverified. 2026-10-31.

## Scope and authority
The owner requested the five new food heroes modeled and integrated. Retain the tested 2D selection mechanism, no Dodge, and unchanged pure C# ECS gameplay. The recovered immutable source is Library `libfile_5e023a57be188191af847cf3d1b0fdc5` v1, ZIP SHA256 `d62019d783ac50d7e03624a3ac6d527b40c192737afe208abd2f9e7d3bc3f65d`. Earlier art and code test passes do not certify the new runtime assets.

## Decision
Body clips and facial morph weights are a Godot-only presentation adapter. It reads immutable entity snapshots and scoped cosmetic events, never schedules damage, currency, collision or movement. Preserve Core and gameplay.json bytes. Stable IDs are orbit=绯绫, crit=卷卷 (ricochet), honey=布叮, summon=烤团, bounce=脆蓝 (impact); display names do not become gameplay keys.

An entity presenter owns one model instance and its entity generation. The host disposes it before every slot replacement, pickup transition, run restart or scene exit. Run/wave context and stale event checks prevent previous-run gestures from leaking into a new actor. No asynchronous resource loading or actual object pool is introduced: the existing PackedScene cache creates independent instances. Any future pooling must call the same explicit reset/disposal contract.

AnimationPlayer advances manually from a single presentation delta, so pause freezes both clips and morphs. Attack interrupts locomotion, repeated authoritative attacks restart only the visual gesture; completion returns to current movement/idle state. Wave/tick rollback clears transient animation and expressions. Morphs use per-MeshInstance3D blend-shape values, never mutate shared Mesh resources. Blink is a deterministic cosmetic cycle; damage snapshot changes can show brief Surprise, attack can show brief Smile. These are feedback, not new abilities. Model body clips must not also animate these facial channels.

## Alternatives
Embedding face tracks in every body clip duplicates authoring and couples expression to locomotion. Driving damage from animation callbacks violates the authoritative ECS boundary. Exporting the offline high-density procedural model directly ignores mobile feasibility. All are rejected.

## Gates
Fresh baseline core/build/import tests; actual-engine presenter pause/repeat/interruption/reset/shared-resource tests; existing slot-generation/pickup/wave2 regressions; runtime asset contract validation including skeleton poses/morphs; native all-five selection/start/movement/attack/pause/restart and actual video; representative render counters and allocation measurements. Software rendering is not mobile GPU certification. Android build and device execution are distinct gates. Unity remains unverified.
