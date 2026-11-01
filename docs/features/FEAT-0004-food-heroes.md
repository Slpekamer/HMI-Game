# FEAT-0004 — Five food heroes in the existing game

Owner authorization: 按照这种标准，建模其他四个角色，然后接入游戏. The new food concepts replace the rejected old character models. Current work is isolated in `HMI-Game-food-heroes`; old frozen deliveries remain intact.

## Deliverables and non-goals
- Five runtime GLBs with Idle, Run, Attack and separate facial morphs; 2D portrait selection retained.
- Current stable character/trait IDs and gameplay rules unchanged. No Dodge, no new weapons/mechanics, independent companions with no HP bars and never enemy targets.
- Explicit source-to-runtime manifests, licenses, artist source backup and actual engine screenshots/video.
- No main merge, no fabricated remote commit or device-performance claim.

## Stages
1. Recover source and pinned toolchain; document contract; fresh baseline validation.
2. Improve presentation lifecycle/morph driver and isolated engine regression fixtures; test before local commit.
3. Import first Kaotuan runtime sample and verify orientation, skinning, clips, morphs and budgets before five-character batch.
4. Integrate all final assets/2D portraits and display mapping; full regression/native visuals/performance and optional export.
5. Save immutable tested source and evidence; coordinate authorized publisher and verify remote commit separately.

## Acceptance / current limits
All applicable tests must pass on final files. A missing required hero asset/clip/morph is an integration failure, not a silent fallback sphere or old-model success. Pre-asset adapter tests may use explicit fixtures and must be labeled as such. High-density offline .blend/procedural shader quality does not prove runtime fidelity. Actual new-hero device tests, exports and visual approval are pending.

See [ADR-0002](../architecture/ADR-0002-food-hero-presentation.md) and [runtime asset contract](../food-hero-runtime-contract.md).

## Increment status — 2026-10-31 17:41 UTC
- A presentation infrastructure: tested, locally committed and durably saved; remote confirmation separate.
- B1 approved food 2D portraits/display identity and resource-failure gates: tested, locally committed and durably saved. Runtime 3D still old, clearly disclosed.
- B2 five food runtime assets: first Kaotuan is in isolated import/visual diagnosis. r10 fixes the long Smile crease, but strict full-weight frames still show a forehead artifact. The earlier r9 tangent-fix conclusion was withdrawn because the compared weights differed. Four other runtime assets are pending. No five-model acceptance, Android delivery or device performance claim.
- C/D/E deeper trees/endless/checkpoint: isolated design and content-audit preparation only, no active Core change.

## Staged B2 update — 18:32 UTC
Kaotuan r23 and Buding r3 are the first production resource pair. The other three runtime heroes remain legacy until their individual visual/engine gates clear. All five selection illustrations already use the food identities.

The strict equal-weight facial artifact was removed by exact local-morph index partition, not by limiting weights or changing lights. Kaotuan's later outer-edge seam needed clamp sampling and correction of 211 coverage-premultiplied base-color texels. r23 only compacts the proven r22 effective data. Buding's whole portable derivative is byte-reproducible; Kaotuan's JSON values and BIN bytes match while JSON serialization order differs. See asset provenance and independent reproduction evidence.

Normal Buding gameplay passed two waves, choices, a single five-coin gloves purchase with repeat prevention, wave-three start, and restart into Kaotuan with clean state. Full five-hero, mobile GPU/audio, six-wave balance and final commercial quality remain separate gates. Final-pair main aggregate and the final r23 host screenshot/movement check have now passed; evidence is in `docs/evidence/FEAT-0004/two-hero`. Remote publication remains separately verified.

## B2 final five-hero scoped gate — 18:44 UTC
All five runtime food assets are now present: Kaotuan r23, Buding r3, Juanjuan r9, Feiling r2, Cuilan r2. Stable IDs and the entire Core/test/catalog gameplay tree are byte-identical to tested B1. The 2D selection screen remains texture-based.

- Final main aggregate passes Core 538343, presentation 482, structural contract 17, actual import/root/clip/morph/instance isolation for all five, lifecycle, selection, text/layout, build and smoke.
- Independent actual engine five-state and sampled 240-frame clip fixtures cover each candidate. These are scheduled fixed-FPS captures, not a real-time performance benchmark or a claim that every frame/intersection was inspected.
- One final native host cycled all five: select → start → short movement → pause → restart. Models, HUD identity and fresh 100 HP/0 coins matched; no old honey zone/companions leaked across heroes. Four adapter files and all five GLB hashes exactly match the reviewed candidate.
- Normal multi-wave/card/shop behavior remains supported by the recorded Buding wave1→wave2→wave3 run and subsequent Kaotuan reset, not five separate full victories.

Known remaining quality work: Feiling is thin from the side and its large loop crops in the bust HUD; Cuilan's foil has dark side reflections and its lateral accessory crops in that portrait. Faces remain readable, but character-specific framing/readability deserve another polish pass. Scene-cache residency after rotating all heroes needs actual memory measurement. Audio, Android hardware, thermal/battery behavior, full-catalog balance, deeper trees/endless and commercial readiness are not certified by this increment.
