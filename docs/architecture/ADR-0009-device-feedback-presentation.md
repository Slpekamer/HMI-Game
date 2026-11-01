# ADR-0009: Device-feedback presentation without authority changes

Status: accepted for scoped UX-0001 stage1 after isolated experiments and final regression, 2026-11-01. The earlier full-flat candidate's shutdown failures remain recorded; engine-wide root cause is not established.

## Decisions
- The latest owner request supersedes 2D selection. Home is a presentation page; only existing captured intents start/restore runs. A single live chooser SubViewport instantiates one selected hero with copied animation libraries/manual Idle. Leaving the page releases that preview. Never preload/render five simultaneous preview worlds.
- Enemy HP is a single bounded canvas draw list sourced from EntityView. Enemy kind, generation/slot, active and positive health checks exclude companions/pickups/dead-pending entities. It never writes health or adds a second gameplay system.
- Actual food skeletons use an instance-owned manual AnimationTree. Idle/Run locomotion blends separately from a filtered native OneShot. Exact upper-body bones are Chest/Head/Scarf/arms/forearms/hands; Root/Pelvis/legs and Feiling tails retain locomotion. Attack events remain cosmetic and retain run/wave/tick/generation validation. Legacy assets retain the existing player path.
- High-rate events coalesce while the native OneShot reports active, including its final active frame; there is no gesture backlog and no reduction of authoritative attack rate. The former timer-only gate conflicted with native end-frame activity; independent red/green samples were1220→0 conflicts. Pause advances neither clips nor morphs. Detached mixers do not evaluate targets outside the tree.
- Box/Disc factories deterministically dispose only their newly constructed temporary managed mesh/material wrappers after MeshInstance3D has taken native Ref ownership. Shared GD.Load/PackedScene resources and provider caches are not disposed. Native identity/property/two-instance regression256 plus actual rendering protect this ownership boundary. No production GC is added.

## Alternatives rejected / limits
Two AnimationPlayers writing the same skeleton were not adopted. Global-flat Theme withdrawal, repeated-Home removal, five-StyleBox disposal and HUD texture unlink alone did not eliminate observed shutdown failures; none is described as the fix. The deterministic primitive boundary passed same-time project repeats and two full aggregate runs, plus three actual normal window closes. It is a verified local mitigation/ownership improvement, not proof of universal Godot shutdown correctness.

No Core/collision/skill/stat/save mutation. Original GLBs and Run motion amplitude remain unchanged; visible stepping improves with layering but is not physically locked to4.8m/s. This stage does not include responsive framing, floor placement or shadow settings. Android animation payload presence is verified separately; actual playback, device performance and reported shadow flicker remain unverified.
