# Opaque menu render suppression — independent native review
2026-10-31 22:01 UTC. Isolated HP-based trial; fixed gameplay camera retained.

## Source scope
Read the actual health-final→menu-render-trial diff: the sole production change expands SetPage Disable3D from selection to selection OR between OR ending. Unknown pages remain rendering-enabled. Combat pause/codex/persistence modal do not turn off the world. No Core rules, resources, lighting, camera or layout edits.
Matched fixture GameUi SHA256 01b5d3d295cab557af53da744926e4ef7a950e50cf6c642c6b05d6db4bf8fdbd.

## Actual native result
Launched the frozen hmi-opaque-pages-1zah7t1g project through actual GODOT_NET, after sourcing env then overriding fresh XDG data/config. No existing player save used. Godot 4.6.3 Compatibility / Mesa llvmpipe.
Native terminal PASS192, 16 pairs, process exit0. Actual desktop inventory confirmed the window exited. Log includes environment VSync warning, no ERROR.

Four pages (free card, shop, ending, finite-complete/endless decision) × four requested physical sizes. JSON reports actual window sizes exactly 1280×720, 1600×720, 720×1280, 1024×768. Logical viewport stays1280×720. Actual captured content framebuffers respectively1280×720,1280×720,720×405,1024×576; portrait request is letterboxed landscape content, not responsive portrait UI or a phone approval.

All16 pairs have0 differing RGBA channels. Independently decoded all32 PNGs and compared complete raw RGBA byte arrays and dimensions:16/16 exact equality. Visually inspected representative between1280, shop720×1280, ending1600, endless1024×768 candidate pixels. Opaque background and text remain intact.

Renderer observations after8 FramePostDraw warm frames/variant: each pair removed637 draw calls; removed188644–197754 submitted primitives depending size. This is real renderer-work reduction in this fixed scene, NOT frame-time, mobile speed, battery, memory or thermal evidence. No time benchmark conducted alongside concurrent aggregate work.

Fixture also executes repeated same-frame known pages→combat, checks retained actor/AnimationPlayer IDs, paused world clock, resume clock progression, HUD viewport recreation count, unknown-page fail-open, UI exit restoration for both previous flags. These are controlled engine checks. Direct ShowBetween/ShowEnding presentation calls while core is paused Combat are NOT natural run transitions, unlocked cards or legal six-wave completion. Separate gameplay transition/interactive restoration gate remains to be recorded if requested before final adoption.

Evidence: opaque-native.log; opaque-page-results.json;32 opaque-page-*-{baseline,candidate}.png; opaque-menu-evidence-sha256.txt. No production source changed during review.

## Additional normal production interaction — 22:03 UTC
The requested actual gameplay seam is now covered independently of the direct-page fixture. Launched frozen HMI-Game-menu-render-trial main with another fresh XDG directory, normal Buding seed1030. With ordinary WASD movement and automatic combat, naturally survived wave1 (12 earned,0 spent,12 balance). Picked honey.wide / 蜜圈扩展, entered actual shop, bought one glove for5 (balance7), clicked Next. Wave2 actual arena, actor, enemies, honey zone and HUD rendered again, showing100/100HP, wave2/6,7coins. Paused, resumed and moved again, paused and restarted to selection. No synthetic completion, cheats, load/revival or direct core-state edits in this run. This is a one-wave transition test, not a six-wave/endless balance claim.

Saved actual evidence: menu-play-natural-wave1.jpg, menu-play-shop-purchase.jpg, menu-play-wave2-paused.jpg, menu-play-restart-selection.jpg, menu-play-native.log, menu-play-source-sha256.txt. One immediate post-Escape tool screenshot captured a partial redraw; a fresh settled state was explicitly re-observed and saved, showing the complete pause modal over the intact world. Native log has only environment VSync warning. Terminal Ctrl+C and subsequent inventory confirm no remaining Godot window.

Conclusion: narrow unchanged-pixel/menu-render-work and actual between→shop→combat restoration gates pass. No blocker to this one-line known-opaque-page optimization found; final aggregate/source freeze and release remain lead-owned. Future transparent backgrounds, viewport Expand/layout changes or new pages require renewed coverage validation.
