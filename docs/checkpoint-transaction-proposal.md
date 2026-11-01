# E checkpoint transaction protocol

Status: proposal reconstructed from reviewed task text after the 2026-10-31 19:28 cloud reset. Not an implementation or passing crash test. Supplements the approved checkpoint-contract.

## Sole authority and files

One synchronous serialized adapter writer owns an exclusive directory writer lock. No background serializer may commit independently. Core owns pure DTO validation and canonical wave initialization, not file paths or JSON APIs.

`control.json` is the sole checksummed authority: schema/rules/catalog hash, monotonic operation sequence, persistent run UUID, status PendingStart/Active/Closed, and at most two explicitly committed immutable snapshot descriptors (same UUID, sequence, generated basename, hash, length). A previous control copy is diagnostic only, never a recovery authority. Invalid/missing control with historical snapshot data means fail-closed uncertainty; do not resurrect an old active backup.

Snapshots use `snapshot-<UUID>-<sequence>-<store nonce>.json`. Nonce prevents an orphan from a failed control commit colliding with a later different payload at the same logical sequence. Never overwrite an immutable payload. A retry of the same prepared operation may reuse bytes only after exact hash/length validation.

Recovery first validates current control, then only descriptors listed there. Every candidate must match active UUID/catalog/rules, have sequence <= control sequence, and pass hash/schema/full DTO validation. Select the newest eligible descriptor. If latest is corrupted, only the explicitly listed prior same-run safe point may be used, with rollback banner. Newer orphan files or arbitrary older snapshots never win by filename/time. Unknown schema/catalog incompatibility is an explicit failure, not guessed migration.

## Atomic file operation and limits

Write a same-directory temp, Flush(true), close, readback/validate, then same-filesystem atomic replace/rename. Snapshot commits before control. These steps target tested process-interruption consistency; filesystem directory-entry/power-loss guarantees and Android behavior remain separate gates. Checksums are accidental-corruption detection, not anti-cheat.

Bound snapshot to256KiB, control to16KiB, total store to2MiB including pending temp. Keep at most two bounded diagnostics. After valid control commit, or startup establishing valid control, remove unreferenced store-owned snapshots/temps but never either listed descriptor. Cleanup failure must not invalidate recovery; if quota cannot be met, refuse a new save with visible failure rather than delete eligible candidates or grow forever. Invalid control does not authorize deleting potential historical data. Ignore unrelated files. Lock/temp-only directory is NoSave, not proof of a corrupt previous run. Test1000 commits/repeated interrupted writes/oversize DTO/quota failures.

## State transitions

**New Run:** validate request, atomically commit PendingStart with new UUID and no descriptors; this also makes all previous identities ineligible. If this first commit fails, do not begin a new playable run or expose old hero Continue as the new run. Show Retry/Cancel. Then prepare wave1, capture pre-init DTO, write snapshot and commit Active control. If this second save fails, new run can proceed only after explicit in-memory-only warning; PendingStart has no Continue. Begin canonical initialization exactly once after the save outcome is acknowledged.

**Next wave/Continue Endless:** completed selection/purchases are already applied once. Enter Prepared state (target wave, no live world, pre-init RNG, pending wave items/contract not consumed); capture immutable DTO. Write snapshot, commit Active control listing new and previously current same-run descriptors, then BeginPreparedWave once. Save failure leaves the older committed safe point authoritative and warns that current progress is unsaved; wallet/RNG roll back together if resumed. No duplicate wave6 rewards when opening the endless intermission.

**Defeat/Finish:** atomically commit Closed control with new sequence and no descriptors before claiming durable termination. Old snapshot files do not revive Closed. Failure shows closure uncertainty/Retry and no ordinary Continue in this process. Death occurring before its durable close acknowledgement is fundamentally indistinguishable on disk from an earlier crash; sudden failure in that window may recover the old active state. Do not claim strict anti-revival or anti-cheat across this unacknowledged window. Durable tick/input logging or fail-closed all interrupted sessions would be different scope.

## Crash matrix

| Interruption point | Recovery |
|---|---|
| Before PendingStart commit | Previous committed identity unchanged; new run never began |
| PendingStart committed, no snapshot | New pending UUID, no Continue, no old hero resurrection |
| Partial temporary snapshot | Ignore temp; only current control references count |
| Snapshot renamed, control not committed | Orphan ignored; older committed safe point wins |
| Active control committed, before/during first tick | Restore exact pre-init DTO and initialize once |
| Latest committed snapshot damaged | Only listed same-UUID previous descriptor, explicit rollback |
| Control invalid with old snapshots | Fail closed; no old control backup fallback |
| Closed committed while files remain | No Continue |
| Close failed | Explicit uncertainty/Retry, not success |
| Stale operation UUID/expected sequence | Reject before write; no late asynchronous commit |
| Logical sequence reused after orphan | Fresh nonce; never overwrite different immutable bytes |

## Core and adapter boundary

Core proposes PrepareRun/PrepareNextWave -> Prepared, immutable DTO capture, BeginPreparedWave with run/wave/preparation token. Old StartRun/NextWave may wrap prepare+begin to preserve legacy nonpersistent behavior. Restore fully validates temporary state before replacing existing simulation resources and incrementing run token; failed restore leaves state unchanged. Do not create a new simulation with token1 and accidentally accept old UI callbacks also scoped run1/wave1. Clear host presenters/commands/events on successful restore. Initialization applies pending contract/shop effects once and consumes pre-init RNG once. Compare canonical gameplay state/RNG across uninterrupted and repeated restoration; distinguish ephemeral entity handles from semantic determinism while separately testing stale-generation rejection.

## Required gates

Fake I/O failure at every flush/rename/control boundary; fresh-store reopen after each. Wrong/staleUUID/seq; truncated/hash-invalid files; control corrupt with good old snapshots; new-run save failure with prior hero; close failure; lock contention; duplicate/path-traversal descriptors; exact catalog/rules mismatch; bounded cleanup; sequence exhaustion; whole-DTO atomicity; complete wallet/rank/temporary-effect/RNG replay without reward duplication. Then actual filesystem fault injection and native save/Continue/rollback/error UI. Mobile suspend, arbitrary power loss, cloud sync and hostile-client tampering are unverified.

## Idempotent acknowledgement and query semantics

Each persistence request carries a stable operation ID, expected committed sequence and payload hash. If control already records the same operation and identical payload, a retry returns AlreadyCommitted with the same sequence; it must not advance sequence or apply rewards again. Same operation ID with a different payload is rejected. This covers successful rename followed by lost acknowledgement. Core Capture is a pure idempotent query while Prepared, not an RNG-consuming command; Begin rejects repeated/stale preparation tokens. A duplicate store acknowledgement for an already begun token is ignored by the host rather than initializing twice.

## Initial implementation review findings (2026-10-31 19:45 UTC)

The standalone adapter prototype is not wired into gameplay. Independent review found and corrected retaining a corrupt newest descriptor after rollback (which would discard the last valid backup), unbounded read-before-limit checks, and a UUID uppercase/cleanup-domain mismatch. Newly added checksummed malformed-control tests then caught an exception-category mistake: InvalidDataException is not caught by IOException on this runtime, so recovery needed its own explicit branch. These reds are retained in evidence; earlier partial passes are not substituted for the final rerun.

Current implementation uses bounded reads and file lengths for quota accounting, canonical lowercase UUIDs, exact owned filename syntax, at most 1024 enumerated directory entries, and only the latest validated previous descriptor as backup. Unknown/unrelated files are not deleted. Filesystem path basenames and existing symbolic links are rejected. None of these checks provides hostile-client anti-cheat or power-loss durability certification.

## E1 process termination supplement

The combined109-check runner also starts four actual child processes and terminates each at one deterministic boundary before/after the real immutable snapshot write or real control replacement. Reopening selects the prior committed payload before control rename and new payload after it; retry identifies lost acknowledgement without sequence duplication, and OS-released exclusive writer locks permit recovery. The earlier in-memory model additionally covers temp/flush/before-rename boundaries. This is not arbitrary power-loss, storage-controller durability, or Android lifecycle certification. A fixture variable-shadowing build failure was corrected without production changes and retained.

Default all-zero UUIDs are rejected for both run and operation identities, in addition to canonical lowercase formatting. The combined suite is now111 checks. Resetting an unreadable authority is not implicitly authorized by BeginRun; recovery remains fail-closed. A future explicitly confirmed destructive reset needs a new store epoch or equivalent stale-operation isolation and its own tests, rather than resetting a sequence counter silently.

### Owner decision: corrupt-authority reset

2026-10-31 19:52 UTC: parent confirmed fail-closed without automatic deletion/overwrite. A future explicit double-confirmed reset must first move this game's entire store into a timestamp/epoch quarantine backup, then initialize a new epoch and reject old-epoch callbacks. It must not affect unrelated user files. Until that operation has its own safe tests, the initial UI exposes error/retry only and no reset button. This choice prioritizes preserving uncertain progress over silently restarting.
