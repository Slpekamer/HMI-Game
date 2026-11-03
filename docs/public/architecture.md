# Architecture

## Boundaries
`src/Core` is the engine-neutral fixed-tick gameplay authority. It owns entities, components, combat, wave timing, economy and seeded randomness. `src/Adapters/Godot` maps input to commands, snapshots to visuals and events to animation/feedback. `src/Adapters/Persistence` owns durable storage and migration coordination. Standalone test projects and injected test templates are outside runtime compilation. Dedicated Godot diagnostic adapters in the adapter source tree are compiled, but run only through their diagnostic scenes/explicit fixture entry points; the normal main scene remains GameHost. This candidate is not a minimal release export.

The core targets .NET Standard 2.1 with C# 8 compatibility constraints; the current Godot host uses its separately pinned SDK. This separation is intended to permit a future Unity adapter, not a claim that Unity integration already exists. No DOTS dependency is assumed.

## Current rules and presentation
Combat waves run for 60 simulation seconds, excluding pause and selection/shop phases. Pending spawns reserve positions and activate after one second. Pending entities cannot attack, collide or award currency. Phase changes cancel pending work. Rule 3 replaces instantaneous ranged rays with independently stored projectiles; melee, orbit and honey retain their existing rules.

Visuals are bounded, instance-owned and cleared on appropriate scope changes. Hurt feedback uses rate limits and real dark intervals: not every damage event produces a new flash. Projectile appearance does not enlarge collision radius. Gamepad selection uses a selected device and safe modal focus; injected desktop tests do not certify real controllers.

## Extension decisions
Use stable IDs and small explicit command/event contracts. New effects must state timing, source/target identity, stacking, expiry and interruption behavior. Record save/schema or gameplay changes before integrating them. No cosmetic experiment may silently replace authoritative collision or save selection.

The normal-game combination has now passed user video review: the Crit player turns toward actual movement and uses a real-launch energy gesture rather than a gun-muzzle constraint. The reviewed shadow split is included. Original contact regions, harder shadow edges, dense-scene readability and device certification remain limitations. Historical rejected paths remain isolated experiments, not normal gameplay selections. See [pre-integration status](combined-preintegration.md).
