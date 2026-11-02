# Rule 3: real projectiles

## Gameplay contract
Ranged attacks launch straight, non-homing projectiles. Damage is applied only on a swept enemy collision, not when the projectile is created. Sweeps account for relative enemy motion, use stable tie-breaking and stop at walls. The simulation processes projectile identities in a stable order rather than claiming globally sorted times of impact.

Initial tunable values are speed 18 m/s, collision radius 0.08 m, independent capacity 32 and a 720-tick lifetime for the complete chain. Total path length is finite and derived from allowed leg ranges and hop count. Existing attack cadence, damage and range formulas are otherwise preserved; delayed hits nevertheless change balance and future gameplay outcomes.

Ricochet selects a valid next target only after a real impact. The next leg starts on the next tick; no entire chain is pre-drawn. Full identities and visited targets prevent stale-generation references and prohibited repeated targets. There is no wall bounce. Previously launched projectiles survive source removal, but wave termination, defeat and restart clear them. Pause freezes their simulation.

A full pool does not consume the attack cooldown: a bounded retry is made on a later tick using a stable fair lane order. This consumes no extra random samples. Transition-event overflow may reduce presentation but cannot erase authoritative projectile motion or damage.

## Presentation
A filled faceted cyan body represents player ricochet; a saturated caramel body represents companion shots. Dark sides, restrained highlights and short historical tails improve contrast without white bloom or long fake laser lines. Display size is independent of collision radius. Source attack animation is triggered at launch, not delayed until impact. Existing target hurt coloration follows real damage.

The first tiny-body version was rejected as insufficiently visible and is superseded by the filled-body candidate. A later target-surface micro-burst experiment was also rejected: visual benefit did not justify complexity. It is not included. Contact and very short continuation legs can still be hidden by models under normal depth testing.
