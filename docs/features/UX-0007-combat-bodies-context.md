# UX-0007 first real-context body candidate — WIP

Baseline: approved observer-only Core commit e1b4cdb (parent f4aa), plus exact
UX0005 bea56bb pure-visual feedback overlay. This candidate is NOT released;
UX0005 strict editor/exit diagnostics remain open. No audio, mobile/joystick,
UI-motion, gameplay/balance, save schema or Core source changes.

## Mapping and budget
Only `Melee + BasicMelee` and `Ray + Ricochet` use new bodies. Other mechanisms,
including CompanionShot and Unknown, keep honest existing generic cues.
Impact/Blast/hit/death/player hurt remain unchanged. One original `_pool` of
48 effect records and original per-render12 budget, including eight catch-up
steps; death replacement hides the same body slot rather than appending.
HasBody suppresses only that slot's old 2D attack line/arc, not Damage accents.

CombatSkillBodies is a plain C# owner, not a registered Godot Node script.
At Initialize only: three adapter-owned meshes (arc, thin connector, crystal),
48 exclusive Alpha materials, 48 MultiMesh resources with exactly3 instances,
96 built-in draw nodes directly owned by the host. No second effect-slot pool,
new lights, shadows, colliders, audio or per-event nodes/materials/meshes.
Every spawn merely changes these preallocated resources' properties. The two
nodes per slot are reused: Main switches arc/link; Crystals is hidden for melee.
Material cull/alpha and both nodes' transform/visibility are explicitly reset.

Ray crystal positions update with true endpoint length while dimensions stay
fixed; all three nodes and full connector are present at first accepted event.
No travelling head, late endpoint, fake dodge window or deferred damage.
Melee arc scales its radius to event distance but is only a single-target cue,
not an AoE fill. Parent/actor transforms never change. The pooled construction
adapts r4 to shared geometry: source crystal uses a warm instance tint, not a
new per-source mesh; radius scaling also scales arc width. These small visual
construction differences still need actual context review. Default movement
is conservative static body + true material-alpha fade, without contraction.

## Ownership and teardown
The optional Initialize worldOwner enables3D bodies; existing standalone
Control tests without a worldOwner retain generic cues. Actual GameHost passes
itself. Reset/wave/phase/run changes hide bodies and reuse all resources.
Exit detaches ONLY this owner's new Mesh/Material/MultiMesh references, queues
its96 nodes, then disposes owned wrappers. No GD.Load/getter resource is
Disposed. Either world-first or feedback-first exit is explicitly tested;
freed nodes/camera are guarded, no RemoveChild during busy exit, no GC/delay.

## Verification boundaries
New probe tests source-tag mapping/fallback, reuse state, fixed resource IDs,
48/12 saturation/player priority/death replacement, pause, scope and both exit
orders. It also drives two normal Core runs at seed1030 for900 steps, consuming
only the first run's real events with the adapter and comparing authority/event
fields. It is controlled automation, not natural player survival or VFX pixels.

First new headless run produced real Melee5/Ricochet31 and passed other checks,
but two native MultiMesh transform readback assertions failed: Dummy returned
identity immediately after SetInstanceTransform. Independent pure-GDScript
Set→Get reproduced identity without game/controller. Those failures remain;
new probe labels endpoint/width assertions NATIVE_STORAGE_PENDING unless run
with `--native-body-storage`, which requires a real display and FramePostDraw.
This is an explicit renderer capability split, not acceptance of bad endpoints.
Both assertions remain mandatory for native acceptance. Initial strict import
also failed on the existing EditorSettings shutdown ERROR; separate behavior
runs cannot substitute for a clean strict aggregate.

Current local scope results: 1009 headless checks passed, explicitly excluding
2 native storage assertions; all54 prior feedback behavior checks passed on
exact candidate adapters. Both strict import attempts retained the existing
EditorSettings ERROR and therefore are not strict PASS. Expected native mode
adds real renderer/FramePostDraw checks and actual2m/8m PNGs. Evidence includes
initial failing endpoint getters and independent plain-GDScript reproduction.
The real Core loop observed5 BasicMelee and31 Ricochet events, with no altered
original event fields in its comparison; this is not an exhaustive all-build
balance/parity campaign.17 Core source files remain byte-identical to HEAD.

Native storage update (2026-11-01): independent actual llvmpipe Compatibility
run completed PASS1015 native_storage=True / exit0, no ERROR/FATAL/leak.
2m/8m endpoint and fixed crystal width assertions ran after FramePostDraw;
actual PNGs retained. This closes the two renderer-storage pending checks,
not combat readability. Eight-meter connector remains very thin in the empty
gray scene. Actual actor/UI/dense-context review remains pending.

Exact main build:0 warnings/errors. Main import:exit0 but retained existing
EditorSettings shutdown ERROR (strict red). Fresh-XDG actual main headless
smoke exit0, HMI_READY then Combat wave1/entities16/tick63, no smoke errors.
No normal input/actual-context movie yet. No local formal commit or remote
publication; this is an independently preserved, blocked-validation WIP.
