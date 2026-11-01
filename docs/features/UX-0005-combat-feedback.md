# UX-0005 — Bounded combat feedback (implementation in progress)

## Authority and baseline
Latest owner request: enhance enemies' and hero's attack and received-hit feedback while retaining quiet readable UI. Independent branch from published UX0004 remote f4aa1e45 (same tree as local e54a88c); no mutation of the publishing snapshot, no remote writes. Core/persistence/content/model gameplay bytes must remain unchanged. No new damage, stun, invulnerability, knockback or altered attack timing.

## Actual event audit
- Melee/Ray/Impact are authoritative attack emissions; Damage is the actual damage notification. Do not infer hit from animation or nearest distance. Orbit and Honey may emit Damage without an attack emission.
- Enemy ContactSystem directly deals7HP at squared distance<1.44 and cooldown0, sets .7s cooldown and moves that enemy away .6m. There is **no wind-up**. A first proximity-only foot-mark trial was rejected by actual dense-scene review as clutter/ambiguous footprints. It is removed. No enemy wind-up or countdown is fabricated; adding a genuine wind-up would require a separately authorized gameplay change.
- Enemy Damage may be followed by Death and same-tick destruction. Death cues use immutable event position + full generation ID, not current slot occupant/alive requirement. Death.Value is reward data, not damage.
- Player death has no enemy Death event. Its existing authoritative Defeat page remains the distinct terminal signal; combat accents clear when that page opens. No fake enemy Death/reward event or delayed terminal animation is introduced.

## Proposed presentation contract
One instance-owned feedback adapter consumes original run/wave/tick/event-index tuples before the next simulation Step; dedup within that scope, never re-stamp stale events. It never writes World or schedules GameCommands. Actor tint is instance-local material overlay with previous resources restored on unbind; actor transforms/colliders and the existing Attack/Run mixer remain untouched.

A single projected canvas renders bounded short traces/arcs for actual attacks, small hit accents, separate expanding death accents and a localized player-damage cue. No fullscreen flash, camera shake, vibration or hit-stop. Accessibility scope: weak default, not a new settings screen. Default soft tints, single smooth pulses at most one per actor/.35s; HP still updates for every hit (cosmetic cooldown is not invulnerability). Player damage has reserved priority and aggregates bursts. Damage lacking an attack cue remains visible (Honey/Orbit).

Budget:48 pooled effects,12 ordinary new effects per render plus reserved player cue; no queued catch-up playback. Prioritize player damage, then deaths, then attack/hit accents; drop low-priority visuals deterministically when full. Diagnostics distinguish accepted/dropped/coalesced, peak slots and rate, not phone-performance claims. Scope changes, pause, restart, page transitions and node disposal clear/freeze appropriately.

Audio was studied separately with four original deterministic synthesized cues. Actual native and headless resource leaks reproduced even in independent minimal programs; a native GDScript ownership bridge showed no reliable benefit. Per parent decision, **no new audio player, stream, bridge, WAV or audio initialization is included in this visual milestone**. Sound work and red evidence remain a separate study, not delivered audio. No auditory approval is claimed.

## Stages and evidence
1. Exact restored source/actual baseline footage and event audit.
2. Adapter/unit-engine lifecycle, event dedup, catch-up, death-generation, budgets, instance isolation and authority-byte parity.
3. Same-source controlled single-enemy/high-density representative mechanisms A/B, plus ordinary actual input, pause/restart and readable injury/death differences. Label injected setups explicitly.
4. Final aggregate and scoped actual review before local commit/Library. Parent controls subsequent remote or binary delivery.

Remaining limits: no enemy wind-up is implied; audio is excluded, not silently muted. Device performance/shadow/short-stride issues remain separate.

## Actual first candidate feedback (2026-11-01)
Correct single-step controlled A/B authority trace matched exactly (Damage133/Ray20/Melee11/Death4, 60 paused frames). Reviewer saw localized hero injury/soft enemy tint improvements, but cream-on-cream death accents and rays were too faint. Narrow followup adds dark thin under-strokes and holds initial contrast, without more particles or fullscreen effects; six steady proximity marks removed. First mistaken double-process fixture is retained as superseded, not substituted for the corrected trace. Native candidate with old audio had real resource-leak ERROR despite exit0, so audio remains blocked.

## Audio exclusion decision
2026-11-01: independent93-case diagnostic found15 resource/Playback failures despite every process exit0, across C#/GDScript and headless/native. Earlier clean-sample progress statements were corrected in preserved reports. Neither GC, shared-resource Dispose, arbitrary delay nor a GDScript bridge is accepted as a product fix. Runtime audio code/assets removed; visual implementation and its complete gates continue separately.

## Pure-visual checkpoint (2026-11-01 12:29 UTC)
- Fresh full Core suite:624665 assertions. Core, Persistence and model parity preserved.
- Independent actual-engine visual behavior:51 checks, including zero audio child/player allocation at initialization and after dense events. Strict runner import remains blocked by the editor shutdown diagnostic; the separately executed compiled behavior result is not called a strict pipeline pass.
- Exact pre-generation-fix DLL f8e203113190ab4674e599ab261875b7d6f0c64c0d835743d16b20069d95fe5b: normal Buding1030 survived wave1 (79HP/16coins), chose Honey expansion, entered wave2, paused/resumed/restarted and switched to Cuilan100HP/0coins. After real injury, normal system window close exited0 without resource errors. No new audio objects exist. Final controlled contrast review is pending.
- Full verify: build0 warnings/errors and actual smoke succeeded; pipeline correctly failed on editor `shutdown_adb_on_exit` ERROR. Remaining regressions are separately enumerated, not hidden by filtering or retries.
- Catch-up identity correction under test: a newer-generation Damage may arrive before Render binds the new actor. Permit its immutable-position accent, never tint the old occupant. Reject older-generation Damage, and reject companion/pickup only on exact identity. World never reuses generation int.MaxValue; no wrap-order assumption is introduced.
- Rejected audio study: Library libfile_ee4fab10ce50819195c9d94580d6e709 v0. Separate WIP source identity: libfile_1445be7bb5bc819198a75d50b5b65955. UX0004 source/releases remain unchanged.

## Final desktop visual scope
Independent reviewer cleared the revised narrow visual gate: actual event-aligned Ray54/Death109/Hurt197 samples are distinguishable on the cream arena, with permanent proximity arcs absent in dense537. The corrected720-frame movie exactly matches baseline authority trace. Its exit explicitly had PlayerCueActive=True and no resource diagnostics. Normal main completed wave1→trait→wave2, then reset to another hero and closed normally.

Rendered controller bd23b643 and normal DLL f8e20311 precede only the generation predicate correction b872f794; no renderer/material/geometry changes followed. That edge is separately old-source RED/new-source PASS54. Final main build d7f06cfe03b525629bdb6387cd426beb0d3b74ce3a045a74b989b71661307ac5 and fresh-XDG smoke exit0. No claim that the movie exercised slot replacement. Existing crowd occlusion remains, and the controlled movie has a top-HUD capture crop; normal input screenshots retain the actual window.

## Final regression result — WIP, not accepted release
Post-aggregate enumeration completed21 commands: two store/session builds and their runs passed (Store112, Session144);17 strict engine runners each rejected editor shutdown diagnostics. Separately executing those exact built/imported copies produced16 clean runtime passes, but ResponsiveUiProbe printed893 checks then terminated with native fatal `csharp_lang && !csharp_lang->script_bindings.is_empty()` and leaked Theme/ViewportTexture/Environment/StyleBox references (exit-6). This is an additional real runtime red, not merely the editor shutdown error. Host responsive290 passed. No diagnostic is filtered and no repeated run replaces this failure.

ResponsiveUiProbe constructs GameUi only; it does not construct GameHost or CombatFeedbackVisuals. Its source and all production UI remain byte-identical to UX0004. One exact-baseline same-probe control is being recorded to characterize scope, not prove a root cause or erase this red. Parent permits only a clearly labeled WIP local commit/Library update; no remote release or new APK.

One-shot baseline control completed: exact UX0004 source with the identical ResponsiveUiProbe built clean; editor import retained its ERROR; separate runtime passed893/exit0 without diagnostic. The candidate runtime red is therefore **unexplained**, not dismissed as a proven unrelated baseline bug. UI-only construction and unchanged UI bytes narrow investigation but do not establish causality. No further retry was substituted for the red.
