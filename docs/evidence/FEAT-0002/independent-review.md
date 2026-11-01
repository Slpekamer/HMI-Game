# FEAT-0002 independent visual acceptance review

2026-10-31. Latest user amendment: **remove dodge entirely**; this supersedes the old concept’s dodge button. Require higher-fidelity models as well as motion.

Scope: user rejected M2's visual fidelity and motion. M2 remains frozen. Earlier functional/ECS validation does NOT imply approved visual fidelity. This review explicitly fails M2 against the four user-approved concepts.

## Inputs inspected
Actual pixels of docs/art-reference/{character-select,combat-open-v03,wave-upgrades,wave-shop}.png (1672×941). Actual M2 selection-palette, combat, upgrades and shop screenshots under /workspace/shared/hmi-review-recovered. Compare normalized content regions, excluding runtime letterboxing. Concepts are layout/style targets, not literal mechanics or a requirement to reproduce generated lettering/artifacts.

## Prioritized gaps and concrete acceptance gates

### P0 combat framing/readability
M2 shows a small beige rectangle within a lavender dashboard, approximately72% of width and58% of contentheight. Reference world fills the frame under HUD, uses oblique depth, multiple irregular wall arrangements/gaps and readable soft shadows. M2's two straight pale walls and uniform perimeter do not provide the same candy-world composition.
- New real runtime must show continuous cream world beneath overlay HUD, no lavender picture-frame around the active arena; preserve large nongrid movement space.
- Hero must read as the chosen character, not a tiny side-on colored sliver. M2 roughly1.5–2% screenwidth vs concept approximately4%; use a comparable relative scale/readability while keeping distant high-angle view and enemy counts legible. Exact percentages are reviewer estimates, not user-mandated balance changes.
- Clockwork gears/key/feet must be recognizable, not near-black dots. Improve front/fill lighting/material contrast without washing floor white.
- Rounded pastel walls, clear openings, contact shadows and a visible player grounding marker. Decoration must not imply collisions absent in core; actual collision dimensions remain authoritative.

### P0 motion
M2's live observed translation rotates/slides a rigid full model; no readable locomotion/action poses. A screenshot cannot pass this gate.
- Idle: modest breathing/ear/accessory motion visible on a stationary character.
- Run: alternating limbs or convincing articulated hop/stride, body lean/settle, animation speed responds to movement; release returns to idle without continued sliding.
- Attack: distinct anticipation/contact/recovery pose or limb/weapon gesture synchronized to existing core melee/event; VFX reinforces contact, not just an unrelated line.
- No dodge: no button, Space/controller binding, cooldown, speed burst or hidden invulnerability. Historical concept button is explicitly superseded.
- Capture real runtime short video covering idle/run/attack states and transitions; inspect several frames and continuity. No generated animation mockup substituted for runtime. Test paused animation freeze and restart clearing visual state.

### P1 menu visual system
All three M2 menus replace the concept's candyworld backdrop with empty lavender and the live arena peeking between panels. Thin type and small generic badges flatten the hierarchy.
- Dedicated contextual candyworld background/soft blur or layered scene, not visible paused combat clutter.
- Heavy rounded plum headline and strong title/subtitle hierarchy; cream/plum/pastel framing and coral primary actions. No white-on-light unreadability.
- Selection: portraits occupy most of card visual area, clear chosen check/badge and luminous blue border, colored nameplate; bottom selected-character summary and common-melee indicator; start CTA prominent. Avoid giant blank card interiors.
- Upgrade: distinct purple/blue/coral frames and category tabs, large mechanism illustration, prominent effect numbers with downside emphasis. Keep complete truthful descriptions accessible; do not trade functional text clarity for prettier but misleading art.
- Shop: large actual item illustration, clear price capsule, separate earnings/spend ledger, distinct purchased vs insufficient states (not one generic unavailable label); visible nextwave CTA.

### P1 combat HUD
Reference has portrait-health medallion, separated wave/timer pills, companion portrait tray and a historical circular dodge control (now removed by user). M2 has plain panels/text buttons.
- Restore these visual anchors while retaining touch hit areas and pause/codex/control semantics.
- Companion tray must reflect owned companions, not claim absent allies; no companion health bars or enemy targeting change.

## Non-negotiable regression gates
Core remains pure C#, Godot animation/UI consume views/events only. No rule-authoritative animation callbacks. No frame-rate-dependent gameplay, collider-scale changes or hidden balance changes. Rerun core suite/build/import/smoke plus actual input release, absence of all dodge behavior, pause/focus/codex, purchase exact-once/stock/ledger, freepick requirement and nextwave. If camera coordinate mapping changes, verify keyboard/touch direction and walls. Rendered labels can be restyled but must retain prerequisites, costs and negative effects.

## Current verdict
M2: functional prototype passed; concept-faithful visual acceptance FAILED. New increment: pending source/runtime/video inspection. Do not label pending animation/visual/device gates green from screenshots or tests alone. Remaining production polish must be stated, not hidden under 'placeholder visible'.

## Model fidelity addition
Inspect close-up front, three-quarter and side views plus game-camera view of each revised hero. Require recognizable approved silhouette (ears/wings/fox muzzle/honey gear/boxing mitts), facial expression/eye proportions, clean rounded form, richer material differentiation and coherent accessories. Color identity alone is insufficient. No claim of fidelity from tiny gameplay thumbnails; inspect actual exported model pixels. Animation must deform/articulate meaningful parts rather than merely move the entire rigid root.

## Initial revision actual native iteration (02:01–02:03 UTC)
Actual images saved initial-selection.jpg, initial-combat.jpg, initial-upgrades.jpg, initial-shop.jpg. Oldv01 models pending replacement; not an animation/fidelity final gate.
Improvements: portraits now fill cards, colored frames/selectedbadge/summary, village background, cream titleplates; combat world fills viewport and hero larger. No dodge control visible.
BLOCKING: textured short panels force minimum-size/ornament overlap. Combat bottom Station/Recall/Codex and right Pause partially clipped. Selection frame strokes cross name/trait text. Upgrade category labels exceed tabs. Shop price decoration crosses effect text; lower effect lines hidden behind purchase buttons, bottom Back/Next clipped. UI worker received exact screenshots and owns fixes. Typography still too thin and generic; vector village and generic badges do not yet reproduce dimensional candyworld/large illustrated mechanisms.
Arena still mostly straight-on overhead, long pale straight walls and dark rear-facing clockwork dots; closer camera improves hero readability but not yet approved composition. No claim final accepted.

## Second revision actual native iteration (02:06–02:08 UTC)
Saved second-{selection,combat,upgrades,shop}.jpg. Stronger contextual raster candycity background and colored frames/large illustrations substantially improve concept direction. Combat clipping fixed; no dodge control and pressing Space produced no visible dodge. Oldv01models remain pending.
BLOCKING shop details still hidden behind buttons (tonic expiry, shoe/glove finalscope). Button skins squashed vertically into thin almond/slit shapes, whiteborder crossing label. Same-ink outline+fontembolden produces doubled/heavy bodytext. Worker owns fixes; source/build released. Do not accept screenshot decoration as fidelity while primary text/actions are compromised.

## Refined v02 offline model inspection
Inspected actual refined-multiview.png and run-pose-review.png. Improved bent blue ear, bow/tufts/paw toes, layered expressive eyes/mouth, honey dipper/barrel, wing edging/beads, mitt stars and alternating articulated limbs. Still visibly smooth plastic vs approved plush/fabric-like form/material; repeated surprised face across heroes, fox muzzle/tail silhouette weak, wings loop-like instead of filled butterfly structure. These are remaining fidelity gaps, not a motion failure. Runtime camera/material/animation acceptance pending. Offline pose renders do not substitute for gameplay video.

## No-dodge implementation regression
Reviewer-owned narrow Core/tests change only in visual-revision, not frozen M2. Removed Dodge command/API/event/timers/speed multiplier; reserved numeric1 rejects so legacy inputs cannot alias Pause. Added invalid command, normal-speed, no contact-immunity tests. Added glove original-baseline arithmetic tests1.5→1.62→1.74 and boosted2→2.12→2.24, nextwave activation/cap2. Fresh full suite PASS538343, no-dodge-gloves-core-tests.log. Lead owns independent aggregate rerun.

## 2026-10-31 02:26 UTC — r3 independent native gate FAILED
Latest user explicitly rejected v02 models again. No visual acceptance or final polished video claim. Future art work is single-blue-character static shape approval only.

Fresh r3 native evidence: final-selection.jpg, final-combat.jpg, final-upgrades.jpg, final-shop-before-pick.jpg. Blue HUD face clipping fixed; companion controls fit; complete risk and all four shop scope/expiry texts visible; pill buttons no longer cross text. No Dodge button; Space produced no visible dodge. This is technical/layout evidence, not aesthetic approval.

Normal blue seed1030 cleared wave1 with 7 earned coins. Shop before free pick showed disabled next-wave CTA. Bought gloves (5 coins), repeated click did not charge again, selected companion expansion; wave2 began with 2 coins and 3 companions.

BLOCKER: early wave2 timer stuck at24, Escape pause failed, stale attack-beam accumulation. Native log repeatedly throws ObjectDisposedException Godot.Node3D at CharacterPresentation.Snapshot:43 -> GameHost.RenderEntities:210 -> _Process:161. Process stopped. Likely GameHost198-204 generation replacement from animated entity to Pickup leaves old presentation referencing queued/freed root; clear presentation/pending attacks on every replacement. Lead notified; must regression-test actual slot reuse and rerun native wave transition before stable claim. Core PASS538343 remains valid but cannot certify adapter lifecycle.

Final wave2 animation/input/modal regression not passed because runtime exception invalidated it. No hardware/mobile performance certification. r2 runtime-motion-short.mp4 is pre-HUD-fix genuine input motion evidence only, not accepted final art.

## 2026-10-31 02:35 UTC — lifecycle fix independently retested
Lead added isolated actual-engine host lifecycle probe. Reviewer read genuine red-before-fix ObjectDisposedException log and green-after-fix log; fixture explicitly isolated from shipped runtime. Full aggregate reported PASS538343.
Fresh normal native blue1030 repro (no cheats): wave1 earned8, gloves cost5, repeated click not charged twice (balance3), companion expansion free pick, nextwave. Wave2 timer progressed24→22→16, continued kills/coins and movement, no stale-beam accumulation, Escape pause opened correctly. Mouse joystick drag released to center; Space caused no Dodge. Restart returned selection and new run wave1 HP100/100, coins0. Native log contains no managed runtime exception. Evidence lifecycle-fixed-wave2.jpg and lifecycle-fixed-native.log. Earlier r3 failure remains preserved for provenance.
This closes the specific adapter lifecycle regression, not model acceptance. Latest user rejection remains controlling: v02 aesthetic gate FAILED; only single-blue static silhouette/face/proportion approval work should proceed. No new final polished video/APK or all-five art approval implied. Wave2 animation state is covered by engine probe plus observed movement; not a frame-by-frame final animation/video acceptance.
