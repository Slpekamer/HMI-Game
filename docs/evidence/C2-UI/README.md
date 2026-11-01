# C2 UI reconstructed after workspace reset

This source was reconstructed after the 19:27 UTC workspace reset, on the C2 base restored by the lead from Library. The old local commit ancestry and old uncommitted CUI files were lost. See `docs/CUI-recovery.md` for historical observations; they are not fresh pass evidence for this reconstruction.

## Scope

OpenCodex keeps existing input/pause/modal lifecycle and delegates text only to CodexFormatter. Each card uses authoritative GetCardAvailability and GetLevel. Display includes current rank/cap, named legacy or ALL/ANY required/current ranks, named exclusive siblings and complete original effects. UI never recomputes eligibility. “满足树条件” explicitly does not mean the card was offered or can be claimed directly. Core and gameplay JSON remain unchanged.

Approved supporting corrections:
- Card illustration120→94, title up26px, body124→150; font20, buttons and complete effects unchanged.
- Wrapped explicitly uses line_spacing0, matching the text gate. Shop's−3 override stays. The historical actual-control probe exposed162px when the old helper inherited3; separate Label measurements alone were insufficient.
- Rebuilt renamed Candy Sans from the existing official installed Noto source, retaining1321 old codepoints and adding10 missing runtime characters (✓〔〕叮望烤瞄绫绯驳). The new font maps1331 codepoints and covers all493 currently required adapter/catalog characters. Exact source/output/coverage data is in content/fonts/glyph-coverage.json. OFL text is unchanged. Desktop fallback is not accepted as shipped font coverage.

## Reproduction and evidence

Source the isolated Godot/.NET environment, then `bash scripts/verify_codex.sh`. It runs font coverage, independent formatter tests, the existing full aggregate and an actual-engine CodexProbe compiled only in a copied test project. The probe reads real GameUi-created controls, checks modal/repeat/focus behavior and full fusion/base/risk text above buttons. Optional native capture is explicitly a controlled display fixture, not random acquisition or survival proof.

Fresh results and native screenshots will be appended before commit. The initial reconstructed WIP archive was created before running tests and clearly marked unvalidated; it is not a release artifact.

## Limits

Existing descriptions mix older demonstration units with new meters; correcting that authoritative copy is a separate content stage, not silent formatter math. Fusion cards lack dedicated illustration assets, but full text must remain visible. D's future RiskLimit/typed slots/endless result screen are not part of this C2 increment. No APK, device, Unity or final art acceptance is claimed.

## Fresh reconstruction verification (19:36 UTC)

`recovered-aggregate.log` completed exit0: formatter594, core546285, all existing build/asset/animation/lifecycle/selection/smoke checks, all33 catalog descriptions and actual CodexProbe23. The actual fusion control is278×150 with minimum150 and line_spacing0. Source hashes and empty Core/catalog diff are retained here. This is newly executed evidence on reconstructed source, not a reused historical count. Native reconstruction review remains pending until recorded below.

## Fresh native reconstruction review (19:37–19:38 UTC)

Independent reviewer ran the restored source in the real cloud Godot Compatibility/llvmpipe window. The controlled fusion/base/risk fixture passed24 and its actual screenshot contains complete descriptions above buttons. Production codex top, crit exclusivity and fusion ALL/ANY/rank-two text were inspected with the rebuilt font. Tab/Enter close, Escape pause, real OS focus loss followed by close preserving pause, and restart to selection passed. This pass samples those nodes; historical all-eight-node browsing is not presented as a new exhaustive native audit. The process was stopped and its window disappearance confirmed. Source/font hashes match the fresh aggregate snapshot.

See `recovered-cui-native-review.md`, `recovered-cui-source-sha256.txt`, actual screenshots and logs. The integration fixture is not random gameplay acquisition evidence. Audio fell back to Dummy, and mobile/device/export/Unity remain unrun. Dedicated fusion illustration and content unit consistency remain explicit limitations, not hidden successful gates.
