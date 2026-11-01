# ADR-0004 — Presentation resource residency (B3)

Status: measured baseline; minimal reference-release experiment proposed, not yet accepted as an optimization.

## Evidence and boundary

On 2026-10-31, Godot 4.6.3 .NET / Compatibility / X11 / llvmpipe ran two normal Start/Restart cycles through all five heroes. The isolated fixture calls UI intents; it is not a combat benchmark. Initial selection reported 48,543,231 texture bytes and 56,862,295 total renderer bytes. After five heroes and return to selection, both cycles plateaued at 132,822,507 texture bytes / 150,437,793 total bytes, including after diagnostic forced GC. Host and UI caches held 10 and 7 IDs, with overlapping resources; these are not 17 independent texture sets. Destroying the host and diagnostic GC reduced texture bytes to 8,998,911 and total to 15,404,471. This supports live-host retention, not an unbounded or post-teardown leak. Headless zero renderer counters are unsupported evidence, not zero usage.

## First controlled change

Add an explicit method that clears only managed PackedScene cache references. Invoke after the host schedules actor destruction and the UI clears HUD model previews on returning to the 2D selection page; also clear on owner exit. Do not call Dispose on GD.Load-shared resources. Live instances retain their native dependencies, including until queued deletion completes. Keep the same loading path and fail-fast missing-model behavior.

This is deliberately smaller than introducing a shared asset service, leases, or deep-copy resources. The hypothesis is that removing strong cache roots allows ordinary GC/refcount cleanup instead of retaining every visited hero for the full host lifetime. It does not promise immediate eviction or frame-time improvement. Production must not call GC.Collect. The observation fixture separately reports pre-GC and diagnostic post-GC results. A later deterministic ownership design is justified only if this experiment or load-latency evidence requires it.

## Gates before accepting change

- Same two-cycle native residency fixture; all five start/restart, cache counts and renderer counters before/after diagnostic GC.
- Actual live instances survive cache-reference release, including shared HUD/host resources; subsequent reload and owner teardown remain safe.
- Existing presentation lifecycle, scope/generation, morph isolation, selection, all-five asset import and core regression tests.
- Native selection/start/pause/restart confirms rendering, with first-load/reload latency reported as observation rather than phone performance.
- Frozen B2 source and publication manifest remain unchanged. No gameplay, ECS, material or model changes.

## Controlled experiment result (19:04 UTC)

Clearing references passed the independent actual-engine live-resource gate: 366 checks across five heroes, two caches and three live instances, shared-resource safety, instance morph isolation, body clips after diagnostic collection, reload and queued teardown. The native two-cycle experiment reported 48,936,447 texture bytes / 57,398,247 total after diagnostic full collection, versus the baseline 132,822,507 / 150,437,793.

Crucially, a separate ten-cycle (50 starts) run with **no forced collection while the host lived** performed one generation-0 collection and no generation-1/2 collection. Final selection remained at 132,822,507 texture bytes. Therefore accept the small change as **cache-reference lifetime hygiene / reclaimability**, not proven routine memory reduction or deterministic eviction. Do not add production full collections to manufacture a success. This result motivates a later explicitly owned resource-lifetime experiment, with its own loading/instance/renderer gates.

The same natural run timed only synchronous StartRequested callbacks: first five heroes 80.24 / 38.39 / 37.96 / 59.38 / 44.07 ms; following 45 calls median 3.59 ms, max 9.82 ms. These are software-cloud process observations, not total frame time, GPU timing, cold disk measurements, or device budgets. No performance threshold is declared passed.
