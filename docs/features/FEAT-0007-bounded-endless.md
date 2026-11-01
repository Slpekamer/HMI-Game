# FEAT-0007 — D1 bounded endless structure

Status: local core stage tested; **not accepted as a complete late-game loop**. Parent superseded the disabled-slot fallback design with D2 temporary contracts after the saturation evidence below. D1 is retained as a focused, reversible foundation and regression checkpoint.

## Implemented scope

ADR-0005 defines an explicit wave6 Victory decision: ContinueEndless enters the wave6 intermission without paying new rewards; normal NextWave initializes7. FinishRun is terminal. FiniteCompleted and EndlessWavesCompleted are separate. Scoped duplicate/stale commands reject. Immutable SimulationLimits default to512 entities,256 enemies,4 companions,12x enemyHP and1.5x enemy-speed factors. Finite default legacy values remain unchanged; finite replay digest still matches.

Difficulty tiers clamp before arithmetic; enemy spawn refuses capacity pressure before consuming RNG. Ledger additions/purchases and wave advancement reject overflow atomically with a terminal reason; exhausted run tokens require a fresh simulation object. Endless pick history is bounded, not an unbounded string list. Risk cards that cannot apply their full next-wave enemy downside are ineligible, with typed RiskLimit and command-time revalidation.

D1's temporary protection increases initialized maximum and current HP by10% of permanent maximum, excludes wave items/supply from its basis, applies once, expires at wave end, and clamps without healing. Duplicate NextWave does not reinitialize it. The pending choice is separate from active bonus; no file restore exists yet.

## Fresh evidence

- Own and independent snapshot: **PASS591345**; logs in `docs/evidence/D/`.
- Fifteen synthetic1000-wave selection/initialization sequences, five heroes ×three seeds:15000 menus,14340 singleton menus,14279 supply selections. **95.6% single-option late-game menus are an observed design deficiency**, not a successful rich endless experience. This evidence triggered D2's replacement design; no1000-wave combat-survival claim.
- Actual default high-wave initialization:256 enemies, at most4 companions and512 total slots. All-trait warm300 ticks:31.30ms,0 allocated bytes in this run. This spawn-layout stress has0 dropped cosmetic events; the separate packed mixed stress still has2895. Neither event-loss number is universal.
- Independent boundary tests include supply with permanent120 HP plus a50 HP wave item, duplicate continue/finish, duplicate supply/init, no compounding/no expiry heal/no death revival, risk consumption under cap, wave/run-token/ledger extremes, capacity and RNG invariants.
- All prior schema1/C1/C2 tests remain; no new enemy archetype, engine dependency or trigger system.

Not run: Godot endless UI/visual integration, device/Unity/export, real long-session balance, save-file durability. Only the existing chaser receives bounded difficulty/density. D2 and E remain separate stages.
