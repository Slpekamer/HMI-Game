# ADR-0005 — D bounded endless progression and one-wave supply

Status: proposed implementation plan, authorized by parent 2026-10-31. Values are reversible prototype tuning.

## Authority and transitions

Run progression/resource remains sole authority. New commands `ContinueEndless` and `FinishRun` are appended without reusing retired command value 1. Existing finite `Victory` after wave6 remains the decision state. `FiniteCompleted` becomes true once when that wave ends. `ContinueEndless` transitions the same wave6 to `Between` and builds one menu; no kill/wallet rewards are issued by either transition. The ordinary pick/shop/NextWave enters7. `FinishRun` enters terminal `Finished`; restart creates a new scoped run. Duplicate commands and old run/wave callbacks reject. Endless wave completions have a separate count, not a rewritten finite result.

The core adds typed `GetOfferSlots()` (slot category, kind, optional card, enabled/disabled reason) while retaining `GetOffers()` for existing finite adapters. Trait/base/risk categories do not change. Empty base/risk slots are visibly capped/disabled. A saturated trait slot offers the named one-wave supply below. A valid trait choice is always available at intermission. The old list-of-picks hashing must be bounded for indefinite runs; use an incremental history hash while preserving finite legacy offer golden behavior, or explicitly retain legacy algorithm for finite mode and switch only on entering endless.

## Explicit health supply decision

Parent explicitly selected: next-wave temporary protection grants **10% of permanent maximum health** as extra maximum and current health once at next-wave initialization. Permanent maximum excludes wave-lifetime items and supply itself. It does not stack permanently. At wave end expire wave items and supply, recompute permanent maximum and clamp current health to that permanent maximum; no heal is performed by expiration. A duplicate choice cannot add another pending supply. A checkpoint stores the committed pending choice, not an instruction to add the reward again.

Supply is displayed honestly as survival support in the trait slot, not as a new damaging trait. At least one valid option is a correctness floor, not a claim of rich late-game choice diversity; measure saturation distributions.

## Bounds and overflow

Configurable immutable limits default to512 total entity slots,256 enemies,4 companions,12x enemy-health and1.5x enemy-speed factors relative to archetype base. Validate before creating simulation state. Preserve constructor compatibility for existing finite tests. Clamp difficulty tier before multiplying it: base progression formulas never use arbitrary wave numbers directly. Default endless HP grows0.10 per wave past6; speed0.01; final factors include purchased risk stats then clamp. Finite behavior is unchanged. Enemy creation checks capacity/count before consuming RNG or creating entities. Cosmetic event pressure remains independent of authority.

Check wave increment, run-token increment and wallet ledger arithmetic before mutation. On a ledger/wave numeric limit, terminate explicitly with a typed reason, no wrap/free coins/partial transaction. Wrong/duplicate requests remain no-ops. Kills/earned/spent remain conservative and consistent.

## Tests and scope

All old schema1 digest/tests and C2 production tests remain. Add synthetic1000-wave progression with fixed seeds, explicit actual-wave labels, saturated pools, useful supply expiry, sixth-wave choice/duplicate protection, wallet invariant, int.MaxValue boundaries, actual entity caps and all-trait warm allocations/perf. Add separate independent adversarial tests. Synthetic progression does not establish1000 waves of combat survival, balance or mobile performance.

No new enemy rule variant in this stage: the existing one chaser archetype receives bounded stat/density progression. Enemy rule variants require a later separately validated change. No file save implementation. E will define checkpoint DTO validation and transaction/crash contract separately.

### Risk clamp review decision

Independent review identified that clamping enemy difficulty could erase a risk card's advertised downside while retaining its coin/mobility benefit. In endless mode only, reject risk acquisition when the full next-wave enemy-health/speed increment would exceed its configured bound. Report typed `RiskLimit`, retain the risk slot, and recheck in command consumption. Finite semantics remain unchanged. Existing acquired risk bonuses are retained as difficulty saturates, but no new free-risk acquisition is allowed; UI must describe the capped environment honestly.

### Superseding decision after measured saturation

D1 implementation and regression checks pass, but15 synthetic1000-wave paths yielded14340 singleton menus out of15000. Parent therefore explicitly superseded the disabled base/risk-slot policy on2026-10-31: D2 will add a small seed-rotated, repeatable but non-stacking one-wave contract pool, two candidates per trait/base/risk category. D2 requires its own exact effects, domain/effectiveness guards, lifecycle tests and ADR before implementation. This D1 decision remains history, not the final late-game UX target.
