# Portable simulation boundary

This source was reconstructed after the cloud workspace reset on 2026-10-31. It is not a byte-for-byte restoration. Earlier test totals are historical and do not certify this reconstruction. Use the new evidence recorded by the project lead.

## Authority and schedule

`GameSimulation` composes the systems and exposes value snapshots. Data-only component arrays live in `World`; data-only per-run resources are `RunProgressionResource`, `WalletResource` and `CardLoadoutResource`. No engine object or engine vector enters the core.

Each `Step` clears the cosmetic event buffer, consumes the queued commands in FIFO order, and advances one 1/60-second combat tick only when unpaused. The explicit schedule is:

1. Progression commands: scoped input, pause, selection, atomic shopping and transitions.
2. Tick and generalized timed-item expiry; temporary timer decay.
3. Player/enemy movement and bounded wall reflection.
4. Independent companion movement/attack/support.
5. Shared basic melee, orbit, ricochet, honey and impact systems.
6. Enemy-to-player contact, pickup refresh, deferred entity cleanup/spawning.
7. Defeat before wave-end progression.

Only the command/lifecycle phase and end-of-tick cleanup create or remove entities. Damage marks a dead life before awarding its one reward; later attacks cannot reward it again. Entity slots are generation-checked. Companions have no health component and enemies only target the player.

## Commands and presentation events

Commands are consumed on the next `Step`, including while combat is paused. The last valid movement command wins. Unscoped convenience commands bind to the current run/wave at enqueue time. Deferred UI callbacks **must capture explicit run and wave tokens when rendered**, so an old button cannot act on a later shop. A new run discards queued commands. Purchase stock and free-choice guards make repeated same-tick requests idempotent.

Events carry run, wave, tick, source/target generation handles, positions in metres and a kind-specific value. They exist only until the next `Step`; adapters must read after every tick during catch-up. `Ray`/`Melee` values describe pre-power nominal damage, `Damage` is applied damage, `Death` is the computed coin reward, `Impact`/`Blast` values are range/radius, `Pickup` is its type, and `WaveEnded` is the wave index. These are cosmetic notifications, **not** commands to reapply damage or rewards.

The fixed buffer keeps the first 2,048 events per tick and drops newest excess events; `DroppedEvents` records the diagnostic. No gameplay system consumes this buffer. Dense impact stress can exceed it, so zero cosmetic loss is not promised. Collision-pair cooldown storage instead fails closed when saturated, rather than evicting live cooldowns.

## Data and bounds

Host/tests deserialize JSON into `GameContent`; core copies and validates that DTO, including schema, IDs, prerequisites, cycles, five-choice pool capacity and conservative effective stat bounds. Definitions are stable IDs plus typed effects, not content-specific combat switches. Supported orbit, ricochet-bounce and companion counts are at most four. All additions combine before multipliers; player equipment speed is multiplied by the separate risk-speed domain.

World coordinates are X/Y in the core, mapped to Godot X/Z; metres, arena X −14..14 and Y −8..8. The two AABB walls leave passages. Movement substeps and LOS are pure geometry, not authoritative engine physics. Enemy iteration and equal-distance ties use slot order. Xorshift32 drives spawns; FNV-style deterministic offer hashing uses seed, character and prior picks. No cross-platform floating-point bit identity is promised.

Prototype decisions: six 24-second waves, one chaser archetype, full health at wave start, real defeat, no dodge mechanic (removed by explicit user request; legacy command value1 rejected, no speed burst/cooldown/invulnerability). No save schema, networking, Unity/AOT verification or real-device performance certification is included. Impact pair scans are quadratic; representative warm allocation/time measurements are required rather than assuming ECS guarantees performance.
