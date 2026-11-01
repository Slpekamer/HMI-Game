# ADR-0003 — Compiled skill graph and staged endless progression

Date: 2026-10-31. Status: C1 accepted for implementation; C2/D/E remain separate gates. Baseline local reconstruction commit `f470a3e`, not a remote commit. Owner: portable Core worker; Godot adapters and frozen food-hero integration are outside this change.

## C1 API before implementation

`GameContent.SchemaVersion` accepts 1 and 2. Schema 1 behavior, legacy IDs, effects, seed offer ordering, six-wave lifecycle and public command numbers remain unchanged. The shipped JSON stays schema 1 in C1.

Schema 2 adds nullable `CardDefinition.RequiresAll` and `RequiresAny`, arrays of `RequirementDefinition { Id, Rank }`, plus nullable `ExclusiveGroup`. Omitted/null means absent; an explicitly supplied array (including empty) cannot coexist with legacy `Parent`. Legacy parent normalizes to one all-of condition. Schema 1 rejects use of new fields rather than silently ignoring them.

All requirements compile once to immutable internal `(cardIndex, rank)` arrays. Group names compile to integer indices. `Catalog.Graph.IsEligible(index, levels)` and `GameSimulation.GetCardAvailability(id)` perform allocation-free integer/array checks. Availability distinguishes unknown ID, capped, missing prerequisite, exclusive sibling lock and available. Existing Choose validation remains authoritative; UI availability does not grant cards.

## Graph validation and limits

Reject unknown/self/duplicate references (also duplicates across all/any), nonpositive or excessive rank, mixed syntax, any dependency cycle including OR edges, invalid group identifier, groups spanning non-trait domains, and single-member groups. Character start roots must have no prerequisites or exclusive group. This avoids a starting hero permanently closing another hero's legacy acquisition.

Joint reachability is evaluated under complete exclusive-group assignments (none or one member), not by unioning individually reachable prerequisites. Within each assignment, a topological traversal computes attainable cards/ranks. A card is loadable only if at least one assignment admits it; thus an all-of fusion of mutually exclusive ancestors is rejected even if each ancestor is separately reachable. This also catches OR prerequisites whose other mandatory ancestors make every alternative impossible.

Compilation is intentionally bounded: at most 256 cards, 16 requirements per card, and 65,536 group-assignment combinations. Exceeding the validation budget is an explicit catalog error, not an assumption of validity. These are reversible prototype content authoring limits, not commercial scaling claims.

Effective stat bounds are checked per assignment with conservative interval envelopes over attainable ranks and item stacks; mutually exclusive effects are never summed together. The envelope may reject a sophisticated prerequisite-coupled tradeoff that exact optimization would permit. That conservative restriction is explicit; do not silently weaken safety or add an unbounded solver. C2 content must fit these declared bounds. New trigger rules require a separate registered ECS system and authority budget.

Legacy parity is tested against a fixed baseline behavior digest plus schema1↔schema2 normalization equivalence and the entire existing regression suite. Schema2 authoring fixtures are separate from production content. New groups must not be applied to legacy production cards; the C2 content audit compares all old definitions and verifies their reachability.

## Staged next interfaces (not implemented by C1)

C2 adds a few differentiated branches/fusions only after C1 review. D adds explicit `ContinueEndless` and `FinishRun` commands at finite victory, with finite-completed flag separated from endless score. Disabled base/risk slots preserve category; only an actually useful next-wave trait supply may replace a saturated trait pool. Current schema1 six-wave behavior remains a supported path.

D uses validated configurable caps (initial total entities512/enemies256/companions4, enemy HP12x/speed1.5x), clamps tier before arithmetic, and checked wave/ledger transitions with explicit terminal reasons. Synthetic 1,000-wave tests are not claimed as player sessions. D's API and balance definitions will be recorded before implementation.

E is DTO/API design only in this tranche: checkpoint at completed selection/purchases before exactly one next-wave initialization; pre-init RNG, whole wallet/loadout, content hash and rules version. Restore validates temporary data before any mutation. Finish/defeat close persistent run identity. Adapter file I/O must use a serialized writer with UUID/generation/sequence guards and a concrete crash matrix; no multi-file atomicity or revival prevention is claimed by a DTO.

## Gates and ownership

C1: independent netstandard2.1 build; unchanged legacy suite and baseline digest; executable invalid-graph matrix including joint all/any impossibility; ranked exclusive selection/reset/stale requests; data clone isolation; measured eligibility allocations and catalog-load cost. Test logs and exact changed-file manifest accompany a local-only commit and persistent checkpoint. UI/render/device/Unity checks are not implied by pure-core tests.

Files: implementation worker owns Core and `SchemaV2Tests.cs`; independent reviewer owns `SchemaV2IndependentTests.cs`. No concurrent edits to the same test file. Parent integrates adapters later. Each next stage has its own green commit and checkpoint; no main merge or remote push from this worktree.

### C1 finite pool and legacy acceptance clarification

The graph also checks every legal maximal assignment: selected group members must be reachable; an unselected group is terminal only when no member can satisfy its prerequisites under that assignment. Every terminal reachable trait pool must contain at least six total ranks (one starting root plus five selections). This prevents a large alternate branch from hiding a branch-specific finite-run dead end. Nested unavailable groups correctly remain unselected. This proof relies on the declared DAG, positive prerequisite ranks and monotonic acquisition; respec/removal would require a new proof.

Empty legacy `parent` strings normalize to null, preserving the previously accepted schema1 input. Schema2 is now supported, so the old unsupported-schema regression is retained using schema3 instead. The shipped schema1 catalog is byte-for-byte unchanged by C1.
