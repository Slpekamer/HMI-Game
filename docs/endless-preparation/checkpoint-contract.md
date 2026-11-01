# C/E preparation: deterministic wave-start checkpoint

Status: proposed implementation contract, 2026-10-31. No save implementation or gameplay change is claimed. Prepared separately from the frozen character integration milestone.

## NDEC-006 — One canonical save phase

Save only a `ReadyToStartWave` snapshot after the free choice and all purchases have been committed, immediately before the first simulation tick of the next wave. The UI calls this the latest wave-start safe point. Starting a new run also creates the wave-1 safe point. Never persist an incomplete reward/shop menu in schema 1.

The stored `nextWave` is the wave to replay, not the wave just completed. A mid-combat voluntary exit or crash followed by Continue restores that wave's starting state: uncommitted kills, coins and damage are rolled back together. This is an explicit rollback-on-resume policy, not a claim of exact mid-combat resume. It cannot accumulate coins by repeatedly replaying because the entire wallet and RNG are restored, not merged. Formal defeat does not gain a retry-safe-point button by this contract. Coordinator confirmed 2026-10-31 17:22 UTC: defeat and Finish persist a closed-run record; a future retry mode needs its own gameplay decision.

At wave 6 completion, record the finite-win milestone without issuing a second wallet reward. Choosing Continue Endless constructs the wave-7 safe point with the same build and ledger. Choosing Finish closes the active run; historical scores are separate from the active checkpoint.

Tradeoff: if the app closes in the reward menu, it returns to the preceding wave's start, not that menu. This is disclosed before exit and on recovery. Saving partial menus would require preserving generated offers, prices, stock and selected flags; it is intentionally outside schema 1. Do not silently re-roll menus on load.

## DTO and determinism

Envelope: schema version, exact catalog content SHA-256, rules version, save sequence, canonical payload hash, payload. Payload: hero stable ID, mode, nextWave, seed and current RNG state, loadout ranks, permanent choices, equipment stacks, next-wave temporary effects and their explicit remaining lifetime, wallet earned/spent/balance, finite-completed flag and run statistics as of this safe point. No Godot types, nodes, paths, entity handles, wall-clock timestamps affecting RNG, input queues or presentation events.

Restoration validates the entire DTO into temporary data before replacing any live state. Validate unique known IDs, legal ranks and prerequisite/exclusivity consistency, finite effect values and known domains, bounded wave/tier/count values, arithmetic-safe ledger equality, allowed mode/phase combinations, exact catalog/rules compatibility. Reject unknown versions or incompatible content; do not guess migrations. Allocate a fresh run token and clear all command/event/presentation queues. Build the wave deterministically from the saved RNG state; tests compare against uninterrupted start-of-wave state.

## Adapter persistence transaction

The pure core exposes capture/validate/restore data operations; the Godot adapter owns serialization and file I/O. Write a temporary file in the destination directory, flush and close, verify it can be fully parsed/validated, then replace the primary with an atomic same-filesystem operation where supported, retaining the previous valid backup. Do not delete the only good save before replacement. Report storage failure and keep playing in memory; never silently announce Saved.

Do not select by filename priority. First resolve the validated committed active identity/closure record and its operation sequence; then consider independently validated primary and backup snapshots that are eligible for that identity and committed sequence. Select the newest eligible committed snapshot, never an uncommitted temporary file or a closed/previous identity. Preserve corrupt bytes for diagnosis without exposing personal paths. A fallback banner identifies that an earlier eligible safe point was recovered. If neither is valid, show the reason and offer an explicit New Run; never partially import currency or loadout. Save sequence is for choosing validated versions, not an anti-cheat guarantee. Local checksums detect accidental corruption, not hostile editing.

## Required implementation tests

- Fresh wave-1 save, rewards/purchases then wave-2 save, wave-6 finite completion then wave-7 endless save.
- Repeated Next/Continue, old menu callbacks, save failure and retry cannot duplicate purchase, rank or currency.
- Kill/earn during combat, restore twice: both restores reproduce the original safe-point balance and RNG, never accumulated rewards.
- Primary truncated or wrong hash, backup valid; both invalid; unknown schema/rules/catalog; interrupted temporary write and failed replacement.
- Every collection duplicate/unknown ID, illegal prerequisite combination, NaN/infinity, negative counts, int.MaxValue-near wave/rank/ledger values; checked arithmetic before application.
- No live simulation mutation on any failed restore. New run token rejects prior callbacks/events, and presentation instances reset.
- Same saved payload and commands yield identical core digest; engine integration proves exit/continue messaging and save errors are visible.

No mobile suspend/filesystem behavior, migration, encryption, cloud sync or commercial anti-cheat is certified by these planned tests.

- Crash injection before write, after durable replace, and before/after the first wave tick: no duplicate spending/rewards; restoration selects exactly one committed safe point.

## NDEC-007 — Run identity and closure (confirmed 17:22 UTC)

Use a persistent run UUID plus monotonic operation sequence, distinct from the ephemeral in-memory run token. A closed-run record survives removal/corruption of the primary snapshot; backups of the same closed identity must not revive it. Finish, formal defeat and New Run are explicit transitions, never implemented by deleting only the primary file. Store closure/active identity as a validated transactional journal or equivalent robust record with defined recovery ordering; implementation must select and test a concrete atomic protocol, not claim multi-file rename is automatically atomic.

New Run becomes the durable active identity only when its initial safe point is committed. If that fails, clearly label the current new run as not saved and do not silently present an older character as its Continue. If closure cannot persist, show an error; recovery must explicitly disclose uncertainty rather than promise anti-revival across power loss.

Add crash/failure tests for finish → restart → corrupt-primary/valid-old-backup, defeat → restart, new-run save failure, closure before/after primary replacement, and stale backup from a different identity. No score double-counting when re-reading a closed run.

## Exact RNG boundary

The saved RNG is **before next-wave initialization/spawning consumes randomness**. Capture after applying completed purchases/cards but before calling InitializeWave. Restore recreates the canonical empty wave state, applies the DTO, and calls InitializeWave exactly once using that pre-init RNG. The uninterrupted path takes the same initialization call. Compare full post-init world digest and RNG state, not just seed or entity count. A mid-init crash restores the saved pre-init state and starts once; never store a post-init RNG then replay initialization.

Coordinator explicitly preserves roguelike failure cost: only voluntary mid-run exit or abnormal interruption may resume; formal death and Finish close the run. Local files/checksums cannot prevent deliberate client tampering; this is crash consistency, not anti-cheat.

## NDEC-008 — Serialized writer and committed-sequence precedence

The first implementation uses one serialized persistence writer, not concurrent snapshot writes. Every requested operation carries the persistent run UUID, expected active generation and a monotonic sequence assigned by that writer. A queued snapshot becomes ineligible as soon as a later close/new-run transition is accepted; recheck eligibility immediately before its durable commit. A background serialization completion may not directly replace files. Cancellation alone is insufficient because an already-running I/O call can finish late.

A valid but older primary does not outrank a newer committed backup or closure record. Recovery must first establish the authoritative committed operation and its identity, then choose compatible snapshots. If the control/journal state cannot establish whether an identity is closed, fail closed with an explicit recovery-uncertainty message rather than revive a potentially defeated run. Checksums do not resolve ambiguous transaction order.

The exact single-filesystem journal/control-record layout and crash points remain an implementation ADR gate; these precedence rules are fixed for that gate. No multi-file transaction is claimed implemented here. If invalidating the previous identity fails, New Run must not expose a Continue button pointing to that previous hero. UI error/retry behavior must distinguish not-yet-started, in-memory-only and durably active runs.

Required additional fixtures: valid-old primary/newer committed backup; newer uncommitted snapshot; old snapshot completion after Close; old snapshot completion after New Run; close at each commit boundary; corrupted control record with otherwise valid old snapshot; save failure with previous hero still on disk. Exercise deterministic fake I/O completion orders without sleeping, then actual adapter crash/failure injection. These are planned tests, not passing results.
