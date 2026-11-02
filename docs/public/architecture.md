# Architecture

## Boundaries
`src/Core` is the engine-neutral fixed-tick gameplay authority. It owns entities, components, combat, wave timing, economy and seeded randomness. `src/Adapters/Godot` maps input to commands, snapshots to visuals and events to animation/feedback. `src/Adapters/Persistence` owns durable storage and migration coordination. Tests are kept outside runtime compilation.

The core targets .NET Standard 2.1 with C# 8 compatibility constraints; the current Godot host uses its separately pinned SDK. This separation is intended to permit a future Unity adapter, not a claim that Unity integration already exists. No DOTS dependency is assumed.

## Current rules and presentation
Combat waves run for 60 simulation seconds, excluding pause and selection/shop phases. Pending spawns reserve positions and activate after one second. Pending entities cannot attack, collide or award currency. Phase changes cancel pending work. Rule 3 replaces instantaneous ranged rays with independently stored projectiles; melee, orbit and honey retain their existing rules.

Visuals are bounded, instance-owned and cleared on appropriate scope changes. Hurt feedback uses rate limits and real dark intervals: not every damage event produces a new flash. Projectile appearance does not enlarge collision radius. Gamepad selection uses a selected device and safe modal focus; injected desktop tests do not certify real controllers.

## Extension decisions
Use stable IDs and small explicit command/event contracts. New effects must state timing, source/target identity, stacking, expiry and interruption behavior. Record save/schema or gameplay changes before integrating them. No cosmetic experiment may silently replace authoritative collision or save selection.

Current unresolved work includes moving attack orientation, natural side/back movement presentation, shadow flicker and dense-scene readability. Rejected experimental fixes are not in this checkpoint.
