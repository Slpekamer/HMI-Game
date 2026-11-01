# PERF-0006 — Skip hidden main-world rendering on opaque menus

## Scope and implementation

Exactly one runtime expression changes in `GameUi.SetPage`: main viewport 3D is disabled for the existing opaque `selection`, `between` and `ending` pages. Between includes shop; ending includes finite/endless decisions and results. Selection was already disabled. Combat, pause, combat codex and combat save-failure modals keep the world rendering. Unknown pages fail open. Existing exit behavior restores the viewport's captured prior flag.

No Core, data, persistence, camera, collision, actor, animation authority or input changes. The experimental camera is **not** included. This optimization relies on the pinned project's opaque RGB backdrop covering logical 1280×720 and letterboxing; future transparent backgrounds, Expand/aspect/layout changes or new pages require renewed validation. It does not introduce a general coverage framework.

## Fresh verification

- Existing full aggregate passed after the one-line change; the new structural probe passes 144 checks and is now included in `scripts/verify.sh` so future changes cannot silently bypass the contract.
- Actual host/UI structural checks preserve actor and AnimationPlayer IDs, paused/resumed clock, three combat preview viewports, same-frame/repeated transitions, unknown-page fallback and both prior viewport flags on UI exit. All test user data are isolated.
- Actual native A/B: four changed pages × four physical sizes, eight postdraw warm frames each variant. All 16 full RGBA image pairs match exactly; independent decoder verification agrees. Requested size, actual window size and content framebuffer size are separately recorded. Portrait physical resize is letterboxed landscape, not responsive portrait support.
- Renderer work in this fixed scene decreases by 637 draw calls and 188644–197754 submitted primitives per pair. These are renderer counters, not authored triangle totals or claims of frame-time, phone performance, battery, thermal or memory benefit. Combat rendering cost is unchanged.
- Separate normal production play: Buding seed 1030 naturally clears wave 1, earns 12, selects honey.wide, buys gloves for 5, starts wave 2 with 7 coins and intact world/actors/HUD, pauses/resumes/moves and restarts to selection. This is not a six-wave or endless survival claim.

The first probe failure was a fixture omission: directly calling ShowCombat without the normal Host.RefreshUi/UpdateHud omitted the hero preview. The corrected fixture uses the real refresh path; product code was not altered to satisfy it. Native logs retain the environment VSync warning and contain no gameplay/render error.

## Reproduce and evidence

`source <toolchain>/env.sh`, then `bash scripts/verify.sh`; independently run `python scripts/test_opaque_pages.py` for the copied-project structural gate. The printed copied project can be launched with `-- --capture-opaque=<absolute prefix>` for controlled native comparisons; use fresh absolute XDG data/config directories. Main-scene normal-play verification is separate from this direct-page fixture.

Repository evidence keeps logs, full results JSON, hashes of all 32 raw screenshots, independent report, four actual-play JPEGs and one 1280×720 between-page A/B pair. The complete 32-PNG set is packaged for the companion Library evidence archive rather than bloating Git. See `docs/evidence/PERF-0006` and `docs/experiments/PERF-0006-opaque-pages.md`.

Final aggregate including the newly wired opaque-page probe completed with exit 0 (`/tmp/hmi-verify.eUAhhJ`). Its main assembly SHA256 matches the independently exercised production-native assembly. Runtime source stayed unchanged during A/B and normal play; the follow-up aggregate wiring and documentation do not require a new visual recording.
