# C preparation: deeper skill graph contract

Status: design only, not loaded by the current game. Legacy data is frozen at `content/legacy-gameplay.json`; audit output is `evidence/legacy-catalog-inventory.json`.

## Compatibility and compilation

Accept schema 1 unchanged. Schema 2 adds optional requirements but preserves every legacy ID, rank cap, stat operation and old behavior. Normalize legacy `parent` / `parentLevel` to one all-of prerequisite. Reject a card specifying both legacy and new prerequisite syntax rather than silently choose precedence. Compile string references and effect types once at catalog load into index-based immutable structures. Runtime eligibility cannot allocate lists/dictionaries each tick.

Proposed schema-2 fields:
- `requiresAll`: array of `{id, rank}`; all conditions required.
- `requiresAny`: array of `{id, rank}`; empty means no alternative requirement, otherwise at least one required.
- `exclusiveGroup`: optional stable group identifier. First acquired member locks other members for the run. Ranking the selected member remains legal up to its cap. Legacy cards have no group.
- `cap`: bounded positive rank limit; never infer unlimited stacks from a missing cap.
- `effects`: registered typed stat/effect definitions only. New trigger behavior requires a separately tested system; never a switch on card ID.

Groups are symmetric by definition, not directed “excludes” lists. Do not put mutually exclusive prerequisite alternatives inside all-of. Validate unknown/self references, duplicate requirements, impossible ranks, cycles (conservatively reject any prerequisite dependency cycle, including alternatives), group collisions, and unreachable card branches. Conservative rejection may disallow sophisticated cyclic OR graphs; that is deliberate scope control for this version.

Graph validity alone is insufficient: evaluate effective stat domains for reachable legal combinations and known extreme loadouts, including negative tradeoffs and ordering. Multipliers remain explicit; an additive baseline effect is not relabeled as multiplicative. Show prerequisites, mutually exclusive choice and exact downside in the UI before acquisition.

## First content slice

Add few differentiated tier-2 decisions before filling a numerical target. Start with existing registered domains and clear tradeoffs, e.g. orbit coverage versus close-range density, ricochet chain versus per-hop reach, honey area versus control, companion specialization without exceeding four, impact range versus collision strength. A proposal must specify what play behavior changes; two differently named unconditional damage increments do not count as meaningful branches.

Fusion nodes may use all-of requirements across two trait roots. They must not accidentally require exclusive alternatives or duplicate the same authoritative hit. New synergy trigger mechanics remain separately budgeted and tested; they are not authorized to bypass damage cooldowns because they are called a fusion.

Keep legacy 21 cards/4 items behavior tests intact. New names for food characters belong to presentation/localization, not stable skill IDs. No new dodge or damage from animation callbacks.

## Saturation and supply

Read-only legacy inventory confirms 23 remaining trait acquisitions per starting hero, 15 base and 5 risk acquisitions. These are pool capacities, not promised times to saturation: the player chooses only one slot each intermission. Random selection and branch locks will change availability, so measure across seeds and policies rather than extrapolate card count alone.

When all permanent traits are unavailable, offer an explicitly named next-wave trait supply with an actually useful bounded effect for that build. Maxed base/risk slots remain visible and disabled with explanation. Do not classify this degraded fallback as a rich endless loop. Measure valid-slot counts over representative progression; avoid silently forcing one choice in ordinary runs. Supply effectiveness, expiry and repeat scope need their own Core tests before a concrete supply definition ships.

## Quality gates

- Schema-1 digest/behavior parity; all old cards/items remain reachable with unchanged effects.
- Negative schema fixtures for every invalid graph/domain case above.
- Multi-seed, all-five starting heroes, alternate branch policies; deterministic offer sequence and no softlock.
- Duplicate select, wrong wave/run, ranked exclusive member and sibling rejection, new-run reset.
- No per-frame eligibility allocations; catalog-load allocations reported separately.
- Actual menu screenshot proves full prerequisite, tradeoff and disabled reason readable.
- High legal attack rate video proves presentation does not permanently restart windup. Cosmetic trigger coalescing may change only gesture presentation, never authoritative attack frequency.
