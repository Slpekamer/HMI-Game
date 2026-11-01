# ADR-0011: Read-only combat observation v1 (proposal; implementation pending)

Date: 2026-11-01. Baseline: published UX0004 f4aa1e45. Owner: isolated feedback-metadata study. Parent authorized observational metadata, not gameplay changes. Related UX-0007 VFX contract is in the sibling VFX study; this ADR is not a claim of completed implementation or visual acceptance.

## Decision proposed

Preserve the existing 2,048-entry `GameEvent` channel, all original enum values/fields/order/drop behavior and existing constructors. Add an explicit `FeedbackMechanism` field with `Unknown=0` to the immutable event value, using a new constructor overload while retaining the old constructor. `GameSimulation.Emit` and internal `Damage` accept an optional mechanism (old callers remain Unknown). Actual systems name their mechanism at the point of emission/application: BasicMelee, Orbit, Ricochet, Honey, CompanionShot, Impact, ImpactBlast, EnemyContact. Existing attack events also carry the label. DamageResolution forwards it without changing arithmetic or call order. Unknown stays generic; adapters never infer a mechanism from Value or geometry. Death remains generic unless its existing caller explicitly forwards a known cause later; no inference from reward Value.

The added field is observational only. No new calls are inserted into the old event channel, so adding Support/WallBounce cannot displace Damage/Death/legacy events. Existing adapters keep reading original fields unchanged.

Add a separate fixed 512-entry `CombatObservation` buffer for notifications that have no legacy event. Proposed API:

```
public const int CombatObservationSchemaVersion = 1;
public int ObservationCount { get; }
public long DroppedObservations { get; } // saturating diagnostic counter only
public CombatObservation GetObservation(int index);
public bool TryGetEnemyMotionObservation(EntityId id, out EnemyMotionObservation value);
```

`CombatObservation` is a readonly value with kind, full Source/Target IDs, From/To, Value, Tick/RunToken/Wave. Its index in the current buffer completes the dedup key (run,wave,tick,index). Kinds and units are versioned:
- `SupportHealApplied`: emitted immediately after the existing clamp assignment; source support companion, target player, Value = actual HP delta, including zero when full. Zero means a real support attempt consumed its ordinary cooldown but **does not authorize a healing number or HP increase cue**. A modest source cast cue is allowed. No cooldown change.
- `SupportBuffApplied`: emitted immediately after the existing recipient BuffTime=1.5 assignment; source support companion, target recipient, Value=1.5 seconds. A refresh is an actual assignment, not an invented new stack; recipients remain invulnerable companions.
- `WallBounce`: emitted only inside the existing hit!=0 && WallCooldown<=0 branch, after reflection/count/cap logic; source=target=full enemy ID, From=To=actual resulting position, Value=existing bounce ordinal (1..3). Final third collision may stop knock motion; this is still a real wall contact/reflection branch, not a new impulse or damage event. Optional separate vector/flags fields should only be added if the VFX adapter truly needs the reflected velocity/axis; v1 does not expose mutable rule state.

Buffer overflow is drop-newest **only in this independent channel**, increments its own saturating diagnostic counter, does not return a result to rules and cannot affect legacy event capacity, damage, cooldown, RNG or progression. No callbacks, queues, heap allocation per notification, reentrant adapter code or render-dependent capture switches. Initial capacity is a bounded observation choice, not a mobile performance claim. An adapter may discard further notifications under its own visual budget; it never writes back to Core.

### Effective honey slow is a stamped observation, not per-tick event spam

Keep a separate capacity-sized array of immutable `EnemyMotionObservation` samples, with full ID, run/wave/tick, `HoneyMultiplier`, and `UsedKnockMovement`. Set it from the actual MovementSystem branch: knock branch records multiplier 1 and UsedKnockMovement=true; chase branch records the exact `slow` local used in speed multiplication. Do not recalculate proximity later in a getter: player/enemy positions and knock state can change later in the same tick. This describes **the last committed movement pass**, not a promise about the next tick. It cannot supply a mutable EnemyComponent.

The getter validates full ID, generation, current run/wave/tick, phase and live entity. No valid sample exists before the first movement step, after scope reset or after entity destruction/reuse. Pause retains the last committed sample (and authority Tick), permitting static visual state; every Step clears discrete observation events even if paused, so no effects replay. On resume the next movement pass replaces the sample. Scope reset/prepare/begin/restore clears observation storage explicitly, including same-tick-zero cases.

## Version, checkpoint and migration compatibility

- `CombatObservationSchemaVersion=1` is a new **ephemeral output contract**, not gameplay catalog schema2 or checkpoint schema1. `GameEvent` original enum and constructor remain intact; the new mechanism field's Unknown default is explicit.
- No observation arrays, counters, mechanism fields or stamped samples enter WaveCheckpointDto, codec, catalog hash, RulesVersion or serialized data. Existing JSON source and rules constants remain byte-identical. Old checkpoint schema1/rules hmi-core-wave-start-1 must continue to load; prepared capture bytes must be unchanged.
- Restore never replays old observation notifications, never heals/reapplies a buff and never performs a bounce. It begins with empty transient outputs; first real simulation tick generates only its new observations.
- Consumers handle unknown mechanism/kind as generic/no optional cue. A future change to units/meaning requires a new observation version; no silent reinterpretation.
- All types remain plain C# netstandard2.1, no engine references. Unity's adapter reads this same output, replacing only rendering/input glue.

## Validation plan required before integration

1. Fresh unchanged full Core regression suite on baseline and candidate; do not reuse historical assertion totals.
2. Identical deterministic harness compiled separately against baseline and candidate. Hash every tick's authority fields: World masks/generations/transforms/HP/enemy and companion cooldowns/knock states, progression/timers/RNG/wallet/loadout/items/stats, commands and stateful Impact pair reservations. Compare legacy GameEvent original fields and dropped count separately, ignoring only newly added observation fields. Five heroes, all-mechanism combination, wall impact/collision blast, support full/partial HP and buffs, dense capacity, pause/resume, prepare/begin, death and endless path. Same setups, seeds and command schedule; no candidate-side changes to expected snapshots.
3. Explicit mechanism tags for each real Damage/attack path; no invalid/dead target emission; Power multiplier unchanged. Overlapping Honey/Orbit in one tick must remain distinctly tagged without guessing Value.
4. Support heal actual delta including zero, capped HP, correct recipient buff refresh, no Support signal when original used=false. No extra RNG or cooldown work.
5. WallBounce only existing qualified hit branch, not ordinary chase wall movement, and no notification during wall cooldown; original three-bounce cap/velocity exact.
6. Slow sample reflects actual chase multiplier and knock bypass; no proximity-only false positive; stale generation/run/wave/restore and paused behavior.
7. Force overflow of new 512 buffer, prove old event count/content and all authority remain identical, even with zero visual consumer; no per-Step allocation after warmup. Test stale indices and diagnostic saturation.
8. Exact old checkpoint captures/restore, new transient state reset, repeated capture/ack guards unaffected. Core+Persistence regression gates still run.

## Alternatives rejected

Adding support/bounce kinds into the existing bounded event channel risks evicting legacy feedback and changes legacy ordering. Guessing mechanism from amount/range is incorrect when builds overlap. Recomputing slow from current distance misreports knock branch. Introducing new cooldown/flight/damage timing to improve VFX would be a gameplay change and is out of scope.

## State

2026-11-01: lead and independent reviewer accepted the narrow design direction. Implementation proceeds only in the isolated study; integration remains gated on actual tests.

## Reviewer clarifications before implementation

- Consume both event channels after **each Step**. An eight-step catchup requires eight reads; the next Step resets observation indices even when paused. Do not read only the last buffer or key by index alone.
- DroppedObservations is cumulative within the current prepared/begun wave scope and resets to zero on explicit Prepare/Begin/Restore resets. It is diagnostic, excluded from authority hashes and DTO. It saturates at long.MaxValue.
- Slow samples report the multiplier actually used by Movement earlier in this tick. Later Impact may start knockback or later damage may destroy the enemy; no recomputation is done. Dead/reused IDs reject the sample; alive same-tick targets retain the honest historical movement sample.
- HP delta is exactly existing clamp-after minus original-before, no new clamp/rounding. Zero is a full-health support action, not healing.
- Existing constructors preserve source compatibility on recompilation, not arbitrary struct ABI. Project-wide source inspection found no GameEvent usage in Persistence/checkpoint code and no known raw Marshal/binary transport. The test gate must confirm no raw event serialization/Marshal caller; separately distributed precompiled/native consumers are not certified.
