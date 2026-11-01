# ADR-0001 — Portable deterministic simulation and thin Godot host

Status: accepted for prototype; reconstructed 2026-10-31 before implementation. User requested ECS, decoupling and eventual Unity migration. This is not a migration certification.

## Decision
Use a dependency-free netstandard2.1 / C# 8 core, with data-only entity components and run/wallet/loadout resources. Stable runtime handles contain slot and generation. Ordered systems own progression, stat effects, movement, targeting, damage and lifecycle; GameSimulation is the orchestration boundary, not a second host rules engine. Godot.NET 4.6.3 / net10.0 handles only input, presentation, resource loading and platform lifecycle. No Godot or Unity types enter core.

Simulation runs at 60 fixed ticks/sec. Coordinates are meters on X/Y ground, converted to Godot X/Z with Y-up; arena X[-14,14], Y[-8,8]. Host catches up at most eight ticks per frame and drops excess time rather than an unbounded spiral. Pause does not advance gameplay timers. Explicit seeded RNG and stable iteration order support same-runtime replay, not cross-platform bitwise floating-point guarantees.

Commands carry run/wave context. Stale UI callbacks capture their original scope; they must not silently bind to a new run. Repeated purchase/selection is validated atomically. Events are transient per tick and consumed after EACH simulation step, including catch-up steps. Bounded cosmetic event loss must be diagnosed, never used to drive authoritative damage or economy. Snapshots are copied/read-only. Structural creation/removal is performed at documented safe lifecycle stages; no collection mutation during active queries.

Versioned JSON contains characters, tree prerequisites, typed stat effects and items. New content composes registered behavior rather than branches on content IDs. Stat domains isolate basic melee from traits. Validation rejects unknown IDs, cycles, exhausted offer pools and invalid effective domains. No third-party ECS framework, reflection plugins, persistence or networking is added.

## Alternatives and tradeoffs
Godot-only gameplay is quicker but defeats migration/testing boundaries. Full DOTS adds unsupported engine/runtime dependencies. A custom minimal array-backed ECS is easier to audit, but is not automatically fast: representative 32/128/512 entity workloads must measure time and allocations. Impact pair queries remain a bounded quadratic cost to profile before raising capacity.

## Validation strategy
Build core independently; test deterministic input replay, generation reuse, duplicate/stale commands, wallet conservation, trees and all effect domains, duration boundaries, independent companions, limited ricochet, bounded impact chains, pause/reset and full progression. Build/import/run Godot headlessly, then inspect actual graphics and interactive selection/movement/pause/codex/wave/card/shop/restart paths. Export builds are separate gates; Android devices and Unity remain unverified unless actually executed.

## Recovery evidence boundary
The cloud workspace reset destroyed the original unpublished source, tests, exports and screenshots. Remote staging retained only configuration and documentation. Historical passes do not certify reconstructed files. Every reconstructed milestone requires fresh tests and a durable source backup before publication. Main is not merged.

## Prototype performance acceptance

The warm 300-tick regression harness enforces at most128 managed allocation bytes per measured loop; current observed result is0, not a promise of allocation-free content loading or rendering. Timing is reported rather than hard-failed on a shared cloud CPU, where scheduler noise would create false negatives. A provisional investigation threshold is2ms mean simulation time per tick at512 representative enemies (well below the16.67ms whole-frame interval); this is not a mobile budget. Rendering/native input/device profiling remains separate, and the2048-event cosmetic capacity is a known visual-density ceiling. Raising entity/event capacity requires fresh time/allocation and saturation checks.
