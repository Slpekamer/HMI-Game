# CUI work-in-progress recovery, 2026-10-31 19:28 UTC

The execution workspace reset during final evidence/commit preparation. The original uncommitted CUI worktree, final font binary and fresh logs are absent. These files are reconstructed from the implementation agent's active task context; they are **not byte-identical frozen recovery and had not been retested at the initial WIP checkpoint**.

Required base: C2 commit75b293f, originally /workspace/scratch/80f5bc988ef7/HMI-Game-deep-tree-ui, branchdev/deep-tree-ui. Parent owns recovering that base/toolchain. Do not apply to D without separately adding its new RiskLimit presentation. Do not touch main/frozen B2.

Historical observations before reset (not reusable as fresh pass): formatter594 assertions; core546285; full host aggregate; actual CodexProbe23; native controlled fusion-card capture24; native scrolling through all eight new nodes/items, close/reopen/modal/focus-loss pause; final font top screenshot and full rare fusion/risk card actual pixels observed. The very last screenshot save and process exit were interrupted and unconfirmed.

Historical issues/fixes to preserve:
- Initial text gate: fusion.sugaring needed150px, oldbox124. Approved actual layout illustration120→94, nameY351→325, detailY398→372,height124→150, button unchanged.
- Actual control then exposed162px because Wrapped inheritedline_spacing3 while standalonegate used0. Approved actual Wrapped override0; shop−3 remains. Never merely relax assertion.
- Font original1321codepoints lacked✓〔〕叮望烤瞄绫绯驳. Approved preservealloldglyphs+allcurrentadapter/catalogchars using existingofficialNoto/OFL. Final pre-reset font had1331codepoints,493required,SHAc11791214854f21b5961e51f8b93472bd2b5fe9203c2693c1d062b4a1c88b2db; that binary is now unavailable. Rebuild and retest; do not claim samehash.
- Existing effects mix olddemo units103 etc withnewmeters; separate content-consistency issue, do not silently rewrite authoritativeEffect here.
- Fusionillustrationprefix has noasset; blankartarea is a disclosed limit, complete text is required.

OpenCodex patch: keep pause/input/lifecycle code, replace only its StringBuilder construction/loop/return with `return CodexFormatter.Build(_content, _sim.GetLevel, _sim.GetCardAvailability);`.

Reconstruction must receive fresh full tests, actual controls, native glyph/card/codex checks and a durable backup before a completion/commit claim. No APK/export/phone/Unity claim.

## Recovery closure, 19:40 UTC

The lead restored the C2 source from Library into a new Git baseline `3c96388931607ef633226fd2842c0774faab0ecf`; this is not the original Git ancestry. Reconstructed CUI then passed fresh formatter594, core546285, full existing aggregate, actual-control23 and independent native fixture24 plus sampled codex/modal/focus checks. Current font SHA256 is `17750bad1351e67b2334f247ad7211c77185e6c2ca5180d4540c1699096cb1cc`, not the historical hash above. Current evidence is in docs/evidence/C2-UI. No Core or gameplay JSON changes were made by this presentation increment.
