# E — Safe-point host integration: scoped tested milestone

## Baselines and ownership

This isolated branch starts from tested DUI `7f9ee251dbe3282cb959bed41e321475c952dfb0`. The frozen DUI, B4, Core and E1 roots remain unchanged. B3 reference hygiene and B4 readable cosmetic attack coalescing are selectively combined here, without the rejected exclusive-cache experiment or environment B/D prototype. Core E adds the separately tested timed-health fix through its baseline. All combined-source checks must run again.

The pure Core owns Prepared state, checkpoint DTO validation and deterministic wave initialization. The net10 persistence assembly owns bounded file operations, immutable snapshots and the authoritative control record. The session coordinator owns their synchronous transaction ordering and stable retry operation IDs. Godot owns presentation, local user-data path, input intentions and error dialogs, never wallet/skill rules.

## Required end-to-end gates

- New run: durable PendingStart before preparation; snapshot acknowledgement before the only Begin. A failed initial authority write never starts a playable replacement run.
- Next wave: full choices/purchases captured at Prepared, before spawn RNG consumption. Retry is same payload/operation, not another purchase/reward or initialization.
- Resume: only listed, validated same-run descriptor; previous snapshot recovery uses its own sequence for DTO identity, while the next commit uses control sequence + 1.
- Defeat/Finish: Close must be acknowledged. Failure blocks Continue and exposes Retry. Acknowledged Closed authority rejects both snapshots. If all close writes fail and the process is forcibly killed, an older Active record may remain; the failure dialog states this limitation and blocks normal navigation.
- Exit to selection preserves the safe point and explicitly explains loss of current-wave progress. Restart/new run are distinct intentions.
- Corrupt/unknown authority: preserve data, error and Retry only. Quarantine-and-reset remains deferred, not silently implemented.
- Pause, repeated clicks, old scope callbacks, modal Escape/Tab, focus loss, scene exit and process interruption must be tested on final source.

## Experience evidence, not acceptance

The first normal Buding seed 1030 run reached wave 6 and died (115 earned, 33 spent, 82 balance). A second normal Kaotuan run completed wave 1, selected companion expansion, skipped shopping and died in wave 2 (22 earned, 0 spent). The second run did not form a meaningful contrasting multi-node build. Both used coarse native input batches; neither proves balance failure, nor establishes the required 10–30 wave endless experience. HUD/companion tray edge occlusion is an observed presentation risk, not grounds to alter collisions or grant invulnerability.

Status as of 2026-10-31 20:40 UTC: implementation is complete; layered Core/store/session gates and real three-process host success-path review have passed. The revised close-failure warning screenshot and production-hash negative-control regression also passed (actual engine 33 checks; headless 31; removing the normalization fails the first production initialization assertion). The final unified aggregate remains pending, including adapting the legacy controlled endless fixture to legal checkpoint history. The first aggregate exposed and fixed uppercase catalog-hash initialization; a later aggregate stopped on an intermittent Godot editor shutdown diagnostic, retained as failed evidence rather than waived. These scoped results are not Android/device verification or commercial readiness.

## Final aggregate — 2026-10-31 20:58 UTC

The complete `scripts/verify.sh` finished exit0 on the unified source. Core624665; Codex formatter655; actual-engine presenter1450, attack1855, cache366, codex23, lawful-history endless144, persistenceUI115, checkpointHost31; physical store112 and session144 passed. Architecture, notices, glyph coverage, all five asset contracts/imports, camera mapping, selection, smoke and host lifetime checks also passed. Counts identify suites, not a claim that every gameplay/visual scenario is covered. Complete final logs and source hashes are in `docs/evidence/E-Integration`.

Independent native evidence includes three production processes: interrupted Buding run restores the committed wave start; cancelling replacement leaves authority unchanged; confirmed replacement then actual death commits Closed; the third process has no Resume. Fault UI was separately captured using injected I/O failure, with33 actual-engine checks. Negative control removing catalog lowercase normalization fails the exact first production initialization assertion; the unchanged positive source passes.

Historical reds remain preserved: uppercase catalog identity, editor shutdown diagnostic, and the sparse-input test assuming eager directory creation. The latter test now explicitly creates its own corrupt-input directory and asserts lazy constructor behavior; production bounds were not weakened. The final aggregate retained strict ERROR checks. The editor race was not reproduced in eight separate fresh imports, not proven fixed.

Not covered: Android/real-device performance, audio, arbitrary hardware power loss, all platforms, catalog migration, anti-cheat, every hero completing a run, or10–30 waves of representative real endless play. Environment B/D and HUD camera experiments are excluded. Natural-GC texture residency is not claimed optimized. Linux export is a subsequent independent gate. Remote publication requires a separately verified commit/ref.
