# ADR-0007 — Portable prepared-wave checkpoint boundary

Status: parent-approved API implemented as a scoped core candidate; fresh final regression and adapter integration are separate gates. No file I/O or JSON dependency in Core. Adapter protocol remains the parent's `checkpoint-transaction-proposal.md` at historical6f10086, not an alternative design.

## API and one initialization path

- `PrepareRun(characterId, seed)`: reset a new scoped run and enter `RunPhase.Prepared` for wave1 without spawning entities or consuming combat RNG.
- `PrepareNextWave()`: only Between+CardPicked; validate committed choice/shop state and numeric limits, increment target Wave, clear prior transient world, enter Prepared. Purchases/selection/movement/pause are ignored while Prepared.
- `PreparedWaveToken` contains RunToken, target Wave, monotonic preparation ordinal.
- `TryCapturePrepared(CheckpointIdentity identity, out WaveCheckpointDto dto, out CheckpointError error)`: query only in Prepared. DTO includes pre-init RNG and pending, not already applied, wave items/contracts.
- `BeginPreparedWave(token)`: exact current token required; applies pending wave items/contract once and initializes canonical world exactly once; does not advance tick implicitly. Repeated/stale calls reject.
- Existing `StartRun` and NextWave command wrap prepare+begin and retain existing timing (NextWave consumed in Step still proceeds to tick1). The one Begin implementation is shared by direct play and restored play.
- `TryRestorePrepared(dto, expectedIdentity, out error)`: validate complete copied temporary resources first, then replace existing simulation resources transactionally, incrementing its RunToken. Return Prepared; caller uses the same Begin path after adapter persistence acknowledgement. Never reset a fresh object's token to1 and claim stale callbacks are safe.

## DTO contents

Versioned identity: snapshot schema, catalog content hash, rules version/limits, persistent run UUID and committed sequence (adapter owns UUID/sequence transaction). Simulation: hero ID, initial seed, pre-init RNG, target wave, finite/endless flags and completed count, stationary companion mode, wallet coins/earned/spent/kills, card ID/rank pairs, owned item ID/count pairs (pending wave items not consumed), finite pick history bounded to5 or bounded endless history hash, pending temporary contract stable ID (or explicit legacy supply flag). No active entity arrays, events, timers, current wave-earned coins or previously buffed stat values.

Initial wave capture is allowed after PrepareRun. Later captures are only after complete selection/purchases and before the next wave's initialization. Restoration discards an interrupted wave's kills/coins/movement/RNG together and rebuilds its canonical start; UI must disclose this rollback.

## Validation and atomicity

Before mutation: validate all nullable/length/range fields, duplicate/unknown IDs, ranks/caps/all-any/group legality, initial hero root ownership, item caps, wallet conservation with wide checked arithmetic, progression/finite-result consistency, nonzero RNG, limits/catalog identity, finite-history bounds, temporary choice eligibility and exclusive pending state. Validate resource capacity and all indices needed by initialization. A failed restore must leave exact prior authority digest unchanged.

Build the new resource graph in temporary memory, with no writes to current World/Run/Wallet/Loadout. Only after all checks and allocations succeed perform non-throwing reference swaps/reset operations on the simulation's single writer thread. Atomic means no exposed partially mutated gameplay state, not lock-free cross-thread mutation. Check RunToken/preparation overflow before changing anything.

## Handles and replay evidence

EntityId/generation and RunToken are transient session handles, not persisted business IDs. Preserve/invalidate old handles deliberately when clearing world; host destroys all presenters and queued events on successful restore. Tests separately prove old callbacks reject and old EntityId cannot act on newly initialized actors. Compare authoritative semantic snapshots (kind/slot/position/health/stats/wallet/RNG) after canonical initialization, explicitly excluding freshly changed session tokens/generations. Do not label that normalized comparison a bitwise whole-object match.

## Tests before implementation acceptance

Initial and next-wave prepared capture; no RNG/entity/tick changes while waiting; duplicate/stale Begin; exactly one item/contract application; prepared input rejection; current instance token increments; malformed each DTO field, complete validation before mutation; catalog/rules mismatch; unknown IDs/rank conflicts; negative/overflow wallet; bad progression/RNG; old and new pending temporary states; same checkpoint restored repeatedly yields equal normalized initialization/RNG; failed restore exact digest nonmutation; old handle rejection; all legacy golden/tests unchanged. Device/file crash tests belong to parent adapter suite, not Core assertions.

## Persistence limit

No Core DTO can guarantee an unacknowledged death survives power loss. Adapter Closed/PendingStart control semantics and uncertainty UI remain authoritative. The parent's documented before-durable-close crash window is retained; no anti-cheat or arbitrary-filesystem durability claim.

## Final API decisions before implementation

The host injects the exact loaded catalog SHA256 through a new constructor overload; old constructors remain playable but checkpoint APIs reject missing identity. Capture receives immutable store-provided UUID/sequence metadata and never generates it. Capture is a read-only idempotent query; repeated capture of the same prepared state and identity neither consumes RNG nor changes pending effects.

The wire DTO is deliberately a detached, mutable plain-data transport object, suitable for engine-side serializers and negative fixtures, not a handle into Core. Every capture allocates independent arrays. Restore validates copied temporary data and never retains caller arrays. Thus mutable wire data is untrusted; Core authority is not mutable through it. The single-writer adapter serializes its owned capture and computes its payload hash before committing. No JSON attributes or serializer dependency enter Core.

Restore targets the existing simulation instance and increments its RunToken. It builds a temporary prepared simulation/resource graph using the same immutable catalog and limits, validates all arithmetic/array indices and allocates before swapping references. World generation baselines are copied from the old instance; preflight rejects generation exhaustion before swap. No callback/allocation/validation executes between the resource swaps. BeginPreparedWave uses the same initializer for restored and ordinary runs, and rejects duplicate or stale tokens.

DTO validation also enforces finite pick-history consistency and total acquisition bounds; bounded endless history is opaque deterministic state, not a proof against deliberate client edits. Snapshot metadata must match the descriptor identity passed by the adapter, not merely agree with itself. File transaction semantics and acknowledged-close failure windows remain entirely under the parent's existing protocol.

Finite history is validated in acquisition order against compiled prerequisites/exclusivity/caps, not merely final counts. It is not replayed as an anti-cheat proof of lottery picks. Owned run items and current pending wave items establish a wide-arithmetic minimum Spent; historical consumed wave items may increase actual Spent but are not reconstructed. Each item's owned count is also bounded by completed intermissions. Initial wave1 requires the one hero root only, zero wallet/items/picks, initial RNG state and default companion mode.

Pure `TryValidateCheckpoint` shares full DTO validation but does not depend on the live simulation phase or session-handle exhaustion and never swaps resources. It remains safe for store scans and acknowledgement retries after Begin. Actual restore additionally rejects terminal live runs and preflights session/generation limits. Store permission still comes from valid Active control; DTO validity alone cannot authorize recovery from Closed control.

The validated staging graph includes system-owned storage as well as resources. Restore adopts that already allocated schedule storage without callbacks or allocation, preventing future stateful systems from accidentally inheriting an old session cache. Existing impact pair cooldowns were already scoped by run/wave; this makes the restore boundary explicit rather than relying on that implementation detail alone. The public rules/ordering do not change.
