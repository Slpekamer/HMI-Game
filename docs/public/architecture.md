# Experimental snapshot architecture

Core owns fixed-tick gameplay, generation-safe entities, seeded randomness and real projectile impact damage. Godot adapters own input, animation and rendering. Persistence is isolated from the Core. This snapshot predates the newer integrated rule-3 save-upgrade UI and must only be evaluated with fresh isolated data.

The added resolver is purely presentation: it approximates a visual reaction location on the actual victim's rigid mesh. It does not change physical collision coordinates, projectile radius, damage, movement, target selection or timing. It is not a reconstructed collision contact point.

The original portable ECS and Unity-adapter boundary remains required. See [engineering rules](../../AGENTS.md) and [experiment result](impact-surface-rejected.md).
