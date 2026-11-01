# Core work recovery after cloud reset (2026-10-31 19:27 UTC)

Original worktree `/workspace/scratch/80f5bc988ef7/HMI-Game-endless-core` is absent in the replacement environment. Final D2 commit attempt failed before process creation due to exec-server transport disconnection. No D2 commit is confirmed. Historical test results below are evidence from the previous environment, not a claim that reconstructed sources have passed.

## Confirmed local commits before reset

- C1: d330c6b1de25011ab696e34286d3f037d084fa79 — compiled schema2 graph, PASS 538393 own/independent.
- C2: 75b293f9b0ea3206cd7627a598a15b9cd92e2263 — eight branch/fusion nodes, PASS 546285 own/independent.
- D1: 60d7762909d2910b867da8b19ae0a15dda2614c8 — bounded endless, PASS 591345 own/independent. Superseded saturated-disabled-slot target after 95.6% singleton-menu evidence.

Last known checkpoint archives (parent may have persisted Library identities):
- C1 portable tar SHA256 84aad99baafb6f3441aeeeb4a690aa5f09dedcc5762954535d56458dce28e954.
- C2 portable tar SHA256 655e54b52deca06ce0a97cf5cbd2fd9fc5810aba8c143417aebc31f3a4e59bac.
- D1 tar was created at `/workspace/shared/hmi-core-checkpoints/D1-60d7762-portable.tar.gz`; hash was not printed. Do not invent it.

## D2 frozen candidate before reset

Pure core/content/tests independently byte-matched and PASS 624431. Independent reviewer `/root/review_game_architecture` had `d2-independent-final.log`, review and source-hash manifest. No UI/device/Unity/save-file validation was claimed.

Six production temporary contracts, each one wave, permanent-based additive fractions after wave-item composition:
- temp.trait.protection: MaxHealth +.10P.
- temp.trait.scouting: RicochetRange +.15P guarded by RicochetEnabled; HoneyRadius +.10P guarded by HoneyEnabled; ImpactRange +.10P guarded by ImpactEnabled.
- temp.base.rate: BasicRate +.10P.
- temp.base.reach: BasicRange +.10P.
- temp.risk.slowreach: actual PlayerSpeed -.10P, BasicRange +.15P.
- temp.risk.fragile: MaxHealth -.15P, BasicDamage +.15P.

PlayerSpeed permanent basis includes PlayerRiskSpeed exactly once; contract delta is added after that composition. Wave shield/tonic do not compound the basis. Expiry removes contract and wave items, recomputes permanent maximum and clamps current HP without healing. Pending and active contract indices are distinct. Duplicate choice/initialization and stale commands reject. Legacy no-contract catalogs retain D1 behavior.

Catalog adds TemporaryContracts DTO, exactly six or zero; two per category; nonempty ID/name/description, no duplicate card/reserved IDs, only eight permitted typed stat domains, nonzero finite fraction <=.5 absolute, unique affected stat, positive benefit required; risk requires a negative effect. Mechanism range domains require correct enabled guard. Runtime checks actual next-wave domain and nonzero benefit. Shop before selection refreshes offers; shop after selection rejects any invalidating purchase. Next-wave rechecks pending choice.

Rotation initially had severe low-bit correlation (4998:2), documented and fixed with uint avalanche: xor>>16, multiply0x7feb352d, xor>>15, multiply0x846ca68b, xor>>16. Input combines bounded history, seed, wave*0x9e3779b9 and slot<<24; no combat RNG consumed.

Final production saturated 15×1000 synthetic menus over five heroes: all 15000 had three enabled categories, zero singleton; six choices2530/2470/2525/2475/2485/2515. Seed1 twin replay compared every slot over1000 menus. These simulate selection/init, not combat survival. Actual256-enemy active-contract300 ticks:37.88ms, zero warm allocation. Packed mixed legacy stress retains2895 cosmetic drops.

New files: src/Core/TemporaryContractSystem.cs; tests/CoreTests/TemporaryContractTests.cs; TemporaryContractIndependentTests.cs; ADR-0006-temporary-wave-contracts; FEAT-0008-temporary-contracts; D2 evidence. Modified Core Catalog/Contracts/EndlessRules/GameSimulation/RunProgressionSystem/StatSystems/World; production JSON adds six definitions and balanceVersion prototype-3-d2-wave-contracts. Existing D1 tests were pinned to frozen legacy-gameplay.json; independent D1 lifetime/overflow assertions retained.

## Next approved E Core scope (not yet implemented)

Parent owns file transactions at its checkpoint-transaction-proposal.md (6f10086), authoritative control plus two committed same-UUID immutable snapshots; PendingStart invalidates old identity, Closed never falls back, corrupt control fails closed, bounded store. Do not replace with a competing single-envelope/file implementation.

Core shall add Prepared phase and PrepareRun/PrepareNextWave/BeginPreparedWave(token). Capture only pre-initialization: RNG unchanged, pending shop wave items/contracts not applied. Restore validates a full temporary DTO before transactionally replacing existing instance state and increments RunToken (avoids fresh-instance token1 alias); returns Prepared and uses the same Begin-once path. Old APIs wrap prepare+begin to preserve legacy behavior. Prepared rejects movement/pause/shop/selection. Failed restore must leave exact digest unchanged. Compare restored semantic state/RNG separately from transient EntityId/generation; old presenters/events must be cleared and old run callbacks reject. Parent approved this API direction but ADR/test plan must precede implementation.

Potential pre-existing custom-duration MaxHealth item sync defect is a separate follow-up, not silently folded into frozen D2; shipped shield is whole-wave and tonic BasicRate only.
