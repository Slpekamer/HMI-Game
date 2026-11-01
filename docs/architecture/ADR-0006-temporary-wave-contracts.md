# ADR-0006 — D2 temporary one-wave contracts

Status: parent-approved before original implementation on 2026-10-31; recovered after the 19:27 cloud reset. Supersedes D1's disabled capped base/risk slots after 14340/15000 synthetic menus had a single valid choice.

Production schema2 adds exactly six compiled data definitions, two per trait/base/risk category. Permanent cards remain preferred while eligible. Saturated categories rotate among valid one-wave contracts; only one of the three offers may be chosen. No new trigger system or per-content switch is introduced.

| ID | Permanent-based delta |
|---|---|
| temp.trait.protection | MaxHealth +10% |
| temp.trait.scouting | Enabled RicochetRange +15%; enabled HoneyRadius/ImpactRange +10% |
| temp.base.rate | BasicRate +10% |
| temp.base.reach | BasicRange +10% |
| temp.risk.slowreach | Actual PlayerSpeed −10%, BasicRange +15% |
| temp.risk.fragile | MaxHealth −15%, BasicDamage +15% |

Let P include permanent ranks and run-lifetime shop items only. Apply ordinary wave-shop effects W(P), then add the contract's fraction of P. PlayerSpeed P already includes PlayerRiskSpeed; its delta is added after that composition exactly once. Wave items and previous contracts never enter P. Initialization applies one pending contract; expiration removes it and wave items, recomputes permanent maximum, and clamps current HP without healing. D1 supply and D2 contracts cannot remain active together.

Compile only declared typed domains and finite nonzero fractions (absolute value <= .5), with nonempty descriptive metadata, unique IDs/stats, exactly two definitions per slot, positive benefit, and a real negative risk effect. Mechanism-specific ranges require matching enabled guards. Runtime eligibility verifies actual next-wave domain safety and nonzero benefit. A pre-choice purchase refreshes offers; a post-choice purchase cannot invalidate the pending contract. Selection and initialization revalidate.

Custom catalogs at extreme stat bounds may explicitly expose ContractLimit rather than fake an ineffective bonus. Shipped saturated builds must have three effective categories. Legacy catalogs without temporary definitions retain D1 behavior and remain separately tested.

The first rotation's low FNV bits correlated with fixed slot selection and severely skewed candidates. Use a deterministic uint avalanche mix of seed/history/wave/slot, without consuming combat RNG; assert multi-seed candidate distribution and identical replay. This is a bounded fallback, not proof of gameplay depth.

Checkpoint identity is stable contract ID plus catalog/rules identity, not pre-buffed stat values. E will capture pending choices before initialization and use one Begin path; no file persistence is included here.

Recovery gate: historical PASS624431 is not transferable to recovered source. Re-run baseline, new production regression, independent checks and allocation probes before a new commit; distinguish changed test count and unavailable original logs. UI/device/Unity/play-balance remain separate gates.
