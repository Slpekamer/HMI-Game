# E-Session: synchronous Core / store bridge

Status: implemented scoped adapter; aggregate suite currently 144 checks (including 22 independent assertions). Exact-source independent rerun also passed 144 checks with zero build warnings/errors. Godot end-to-end acceptance remains a separate gate.

## Authority and API

`CheckpointSession(GameSimulation, CheckpointStore, CheckpointCodec)` is a synchronous, single-owner-thread adapter. The host must serialize every method call on its main thread and must not advance Core while a persistence action is pending. No tasks, asynchronous completions or background writes are created. Store locking independently excludes simultaneous filesystem writers. The session does not contain combat, economy or progression rules.

The host captures `IntentEpoch` when building every callback and additionally captures Core run/wave for gameplay callbacks. Successful or failed accepted intents invalidate older callbacks. Rejected stale epochs do not mutate simulation or persistence. `RefreshRecovery` rescans authority but cannot discard a pending operation. `CurrentWaveSaved` describes the current initialized/prepared wave, not a claim that every combat tick is durable.

- `StartNew(hero, seed, epoch)` commits a new UUID PendingStart before preparing Core. A failed/uncertain acknowledgement preserves the same operation and expected sequence.
- `PrepareNext(epoch)` asks Core to prepare only from Between. Pending choice/items remain unapplied until acknowledged Save, then one canonical Begin. Save failure leaves Prepared frozen.
- `Retry(epoch)` preserves exact operation, payload and expected sequence. After acknowledgement, internal Prepare/Initialize stages are distinct from I/O stages; Core rejection cannot accidentally reuse an incremented sequence as a different request.
- `Resume(epoch)` rechecks current durable authority, selects only a checksummed listed payload using its descriptor sequence (which can differ from control sequence after fallback), validates/restores Prepared, then initializes once without another Save or charge.
- `ContinueUnsaved(epoch)` is deliberately restricted to the first snapshot failure after known PendingStart. It rereads authority and refuses Uncertain or already Active results; a lost Save acknowledgement must be retried instead. Later-wave failures cannot use this escape. HUD must continuously disclose that an unsaved run has no resumable safe point.
- `Close(epoch)` handles Defeat/Finished and explicit user abandonment from Combat/Between/Victory. A failure blocks other actions until retry. On successful explicit abandonment the host must immediately stop/pause simulation; this adapter never invents a gameplay terminal state.
- `ExitSelection(epoch)` invalidates callbacks and refreshes recovery without closing the run. Host pauses/stops ticks and clears presentation. Resume rolls back the interrupted wave's RNG, wallet and build together.

The UI translates `LastError`, `HasPendingFailure`, `IsCloseFailure`, `CanContinueUnsaved` and recovery kind. Core errors such as invalid character or exhausted handles are fail-closed programming/state errors, not promises that repeating Retry repairs them. The host supplies catalog-validated hero IDs. No reset/delete action is offered for Uncertain authority.

## Codec

`CheckpointCodec` uses bounded strict JSON, no comments/trailing commas, exact root field coverage, no unknown or duplicate root/count-object properties, canonical UUID identity, exact catalog/rules identity and full pure Core validation. DTO defaults cannot silently repair omitted root fields. Encoding is presentation/I/O-layer work; Core remains engine/JSON/filesystem independent. The same pure validator can inspect snapshots after initialization without altering authority.

## Evidence and limits

`tests/CheckpointSessionTests` references the real Core and persistence project. It uses a deterministic in-memory I/O fault port plus actual temporary filesystem roundtrips. The test executable uses the existing friend assembly name solely to arrange synthetic wave boundaries and inspect RNG; no Core source change or production diagnostic API was added.

Own tests cover first start/next/resume, RNG/wallet rollback, no duplicate rewards, stale callbacks, latest corruption fallback with older descriptor sequence, before-write/lost-ack faults at PendingStart/Save/Close, terminal Close retry, explicit unsaved restrictions, strict JSON negatives, a synthetic legal multiwave run with actual kill rewards and purchased shield plus a temporary contract restored without compounding, permission-denied port failures, actual nondirectory parent, postcommit Core rejection without I/O sequence drift and changed recovery authority. Build command uses `-m:1` because this cloud environment's default multi-node MSBuild named-pipe transport was independently observed to fail; this is not a product runtime workaround.

Initial compile mistakes (BetweenWaves enum and OfferSlotView.Id) and a fixture using a nonexistent hero ID were corrected before green. Initial multi-node build failed without compiler errors; serial build exposed actual compiler errors. A subsequent synthetic contract fixture reached its contract but had no kills and therefore no coins for its requested shield; the test was red. It was corrected to spawn and kill enemies through the existing damage/reward system before purchasing, without altering product logic. These were development failures, not successful tests. The final own log is `docs/evidence/E-Session/own-tests.log`. Native host, actual process termination and phone/power-loss behavior require their separate gates; these adapter tests do not establish them.
