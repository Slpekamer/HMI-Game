# FEAT-0008 — D2 temporary contract fallback

Status: recovered core candidate freshly tested after cloud reset; independent final review and commit recorded separately. Godot UI, persistence and play-balance are not covered by this core milestone. See ADR-0006.

## Scope and authority

Six production one-wave definitions replace unavailable saturated categories: trait protection/scouting, base melee rate/range, and risk movement-for-range/HP-for-damage. Their exact IDs, fractions, prerequisites and full temporary descriptions are in `content/gameplay.json`. All use existing typed stat domains; no new trigger system or per-content ID dispatch.

Permanent baseline P includes ranks and run items, never wave items or prior contracts. Apply ordinary wave item effects, then add the contract's fraction of P. Actual PlayerSpeed includes PlayerRiskSpeed once. Protection adds max/current HP at initialization; a negative HP contract constrains current HP. At expiry restore maximum and clamp downward without healing. Pending and active identities are separate, and only one selection is consumed once.

Eligibility requires nonzero benefit, a real risk downside, enabled mechanism guards and safe actual next-wave stat values. A pre-choice purchase refreshes offers; a post-choice purchase cannot invalidate the pending contract. Selection and initialization revalidate. Custom boundary catalogs fail closed with ContractLimit instead of showing an ineffective buff. Legacy catalogs with no contracts retain the D1 fallback and its original lifecycle regressions.

## Fresh recovery evidence

The cloud environment reset at 19:27 UTC before the original D2 commit could be confirmed. Parent restored D1 from its verified Library archive into new local baseline `2bdab6c`. Fresh restored-D1 tests pass 591345 (`restored-d1-baseline.log`). D2 code/tests were then reconstructed, not assumed to retain old validation.

Recovered full suite freshly passes **624431 assertions**, `docs/evidence/D2-recovery/full-tests.log`. The numerical count happens to match historical D2; the new log is the evidence. Original independent source bytes/logs were lost, so this is not a claim of bit-for-bit original-source recovery. Legacy replay digest and production-vs-legacy card/item checks still run.

Fifteen synthetic saturated 1000-wave menu/initialization runs cover all five heroes. All 15000 menus had three enabled categories and zero singleton menus. Contract selection counts: protection2530, scouting2470, rate2525, reach2475, slowreach2485, fragile2515. Seed1's three-slot sequence matches a second simulation for1000 menus. These do not simulate1000-wave combat survival or player preference.

Before reset, a low-bit FNV rotation was observed to collapse to approximately4998:2 choices under a cyclic policy. The recovered final implementation uses the reviewed deterministic uint avalanche mix, not that version; the fresh distribution regression above passes. Historical skew is retained as a narrative finding, not falsely presented as an available original log.

Actual256-enemy all-trait active-contract300-tick probe:46.34ms and zero warm allocated bytes on the replacement cloud machine. Separate honey32/128/512 probes:8.94/16.04/61.90ms; packed mixed128:31.82ms, zero allocation and2895 cosmetic drops. Timings vary by machine/load; neither event-loss figure nor cloud latency is a mobile performance guarantee.

Independent arithmetic gates cover rate1.5 + wave tonic0.3 + contract0.15 =1.95, composed movement `(1+.06+.16)*1.4*.9`, and glass-shell HP65 with shield81.25 minus9.75 =71.5. Recalculation/repeat noncompounding, expiry/no healing, restart, mechanism guards, malformed definitions and domain-invalid shop transactions are covered.

## Remaining limits

This improves late-game choice availability but does not prove a fun or balanced endless loop. Ten-to-thirty-wave real-play experience, UI text/readability/interruption flow, device/Unity/export and checkpoint recovery remain separate gates. No new enemy archetype or file persistence is added.

A pre-existing custom duration-based MaxHealth item synchronization concern is a separate follow-up; shipped shield lasts the whole wave and tonic changes only attack rate. It is not hidden as a solved framework-wide lifetime guarantee.

### Allocation-gate stability observation

A final independent pre-instrumentation run failed the combined maximum-catalog zero-allocation/10000-eligible assertion; that old assertion did not print which value failed. The cause remains unlocalized, and no JIT/runtime explanation is asserted as fact. Its red log is retained. The test now records measured bytes and eligible count **after** the measurement, without changing warmup, loop count or the strict zero threshold. Three consecutive fresh processes and a final exact-source independent snapshot all pass624431 with `allocated_bytes=0, available=10000`. These instrumented results are the current acceptance evidence; the unexplained earlier observation remains a stability follow-up rather than being erased.
