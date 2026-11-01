# Food heroes integration — independent staged review
2026-10-31, reviewer /root/review_game_architecture.

## Scope
New approved food roster: summon=Kaotuan, orbit=Feiling, honey=Buding, crit=Juanjuan, bounce=Cuilan. Existing pure C# gameplay and no-Dodge rules preserved. Actual assets/animations and Godot integration required; no replacement of visual quality with geometry counts.

## Contract review
Read recovered AGENTS, design scope, ADR-0002 and runtime contract. Agreed stable IDs, meter/Y-up/-Z/feet-center identity root,1.2m neutral height, Idle/Run/Attack without root motion/gameplay callbacks, per-instance Blink/Smile/Surprise default0. 15–30k triangles,<=4 surfaces including props,<=32 bones and1K atlas set are candidate budgets, not device performance passes. First Kaotuan imported pilot precedes bulk replacement. High-resolution sources preserved; runtime simplifications must be shown/disclosed.

## Fresh independent evidence (17:02 UTC)
- Core source and gameplay.json compared byte-for-byte with recovered baseline: unchanged. Isolated Release test rerun PASS538343. Hot loop300ticks allocations0 for tested32/128/512 enemy cases; mixed-trait stress dropped2895 bounded cosmetic events. Not rendering/mobile performance proof.
- New synthetic actual-engine CharacterPresentation probe PASS482, clean build/import/run. Tests manual animation clock, locomotion/attack interruption and repeat, pause queue/clip/morph freeze, damage expression priority, shared Mesh instance isolation, wave tick rollback/run reset, wrong generation/scope/future/out-of-order event rejection, valid catch-up event handling, invalid delta guard, blink bounds, inactive visibility, idempotent disposal and real queued-free safety.
- Existing HostLifecycle probe extended and freshly PASS: actor→pickup after actual queued free, inverse and stale generation, intake scope/generation rejection, original catch-up tick rendering, old run/wave/future pending events not re-stamped, companion expansion wave1→2, movement/pause/restart, actual host scene exit disposes retained presenters.
- Found host pending queue originally retained only EntityId and stamped current scope/tick during rendering. Lead fixed to preserve GameEvent context and reject invalid intake; above regressions pass on corrected code.
- Initial fixture build failure was reviewer test literal double→float, corrected; not a production defect. Historical failed log retained in /tmp.

## Remaining gates
Actual final five runtime assets, morph default/instance isolation per imported hero, real clip articulation/pose/material fidelity, native selection/start/switch/pause/restart/UI, actual motion samples, load/resource counters and device limitations. Synthetic five-instance loop is NOT five-production-asset proof. No visual/GPU/mobile/Unity compatibility pass inferred from headless tests.

Static GLB validator review requested counting active-scene mesh instances (not unique mesh resources), nonzero/default/duplicate morph validation, and actual-engine root/foot pose checks rather than counting animation channels alone. These are contract-gap findings, not proof of a defect in as-yet-unproduced assets.

No publication or business-rule edits by reviewer. Test files released to lead for aggregate; early-adapter-sha256.txt pins this stage, later source changes need affected reruns.
