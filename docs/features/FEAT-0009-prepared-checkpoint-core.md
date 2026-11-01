# FEAT-0009 — Portable prepared-wave checkpoint boundary

Status: scoped Core implementation independently verified. This does not certify adapter file transactions, Godot integration, platform durability or end-to-end recovery. See ADR-0007 and the parent's separate control/snapshot protocol.

## Public boundary

Construct with `GameSimulation(content, limits, exactCatalogSha256)` to enable persistence identity. Legacy constructors remain playable but reject checkpoint APIs rather than invent a hash.

- `PrepareRun(hero, seed)` and `PrepareNextWave()` enter `Prepared`: target wave is known, no active world exists, RNG has not initialized the wave, and pending shop items/contracts are not consumed.
- `PreparedToken` identifies the current run/wave/preparation ordinal. `BeginPreparedWave(token)` initializes once without an implicit tick; duplicate/foreign/old tokens reject. Existing StartRun/NextWave wrap prepare+begin and retain legacy timing.
- `TryCapturePrepared(identity, out dto, out error)` is read-only and repeatable. Identity UUID/sequence comes from the store. DTO arrays are detached transport data, never authority references.
- `TryValidateCheckpoint(dto, expectedIdentity, out error)` is pure full validation, independent of live phase/session-handle exhaustion, usable during store scans or lost-ack retry.
- `TryRestorePrepared(dto, expectedIdentity, out error)` validates a temporary resource/system graph, then transactionally replaces the existing instance and increments RunToken. It returns Prepared and uses the same Begin path. Terminal live runs cannot be revived by this method.

The adapter must pass the chosen descriptor's actual sequence, which may be an older same-run fallback, and must authorize restoration through valid Active control. Core does not generate UUIDs, file sequences, JSON or filesystem operations.

## Safety and deterministic reconstruction

Validation covers catalog/rules/limits and descriptor identity, numeric/progression/RNG bounds, known and unique IDs, card rank/all/any/exclusive legality, finite acquisition order, initial root-only state, item ownership/caps, wallet conservation and minimum purchase cost, bounded histories and eligible pending temporary state. All validation/allocations precede authority swaps. Failure leaves exact authority and system-storage identity unchanged.

Successful restore adopts already-built system storage, preserves old per-slot generation baselines and increments session scope. Exhausted/negative generations fail closed; retired slots are never wrapped/reused, and failed spawns do not consume RNG. The host must clear old presenters/events on restore. Runtime handles are not persisted business IDs.

Canonical comparison includes world component values, stats, wallet, owned/active items, loadout, RNG and timers, excluding changed session tokens/generations and inactive storage. The tests separately prove old handles/callbacks are rejected. This is not a cross-platform floating-point bit identity claim.

## Fresh evidence

Own and final exact-source independent suites both pass **624665 assertions**. Evidence is in `docs/evidence/E-Core/`, including independent source hashes. Old schema1 golden behavior and all prior C/D/health-lifetime tests remain green.

New checks cover repeated capture without RNG changes, prepared input rejection, duplicate/stale Begin, pure validation after Begin, all-five heroes at maximum seed, invalid-field atomic rejection, ordered prerequisite history, impossible ownership/spend, terminal/run-token/generation/ordinal boundaries, and all six pending contracts with purchased wave items. Restored and uninterrupted initialization plus command replay match normalized authority; repeated restore never stacks effects.

The first reconstruction comparison correctly failed because old inactive offer indices remained in the continuous path but not the restored path. Prepared now clears all offer indices to−1 on both paths. The failure and diagnostic are retained; the final full suite passes.

Fifty pure validations at capacity512 took6.50ms and allocated9,440,216 bytes in this cloud run (roughly189KiB per validation): intentional staging allocation at safe points, **not** zero-allocation combat work. Combat warm-allocation gates remain zero. Timings are machine/load observations, not device certification.

## Integration limits

The core cannot prove a save was flushed; Begin requires the host's explicit decision after successful acknowledgement or an explicit unsaved-mode warning. Capture only at Prepared prevents accidental mid-wave snapshots. Restoring rolls back the interrupted wave's wallet and RNG together and rebuilds full starting health under the saved contract. UI must disclose this rollback.

Closed-control handling, immutable snapshot names, quota, writer lock, acknowledgement retry, actual process interruption and the documented unacknowledged-death durability window belong to the parent's adapter protocol. No file-format migration or anti-cheat guarantee is implied here.
