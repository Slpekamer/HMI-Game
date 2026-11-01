# FEAT-0008 — D2 Godot presentation integration

Status: scoped UI integration tested and independently reviewed. Natural long-session balance remains unverified; one normal attempt died late in wave six.

## Frozen inputs

CUI presentation commit 0298212 plus exact D2 core/content/core-tests commit 05c96916. Do not import subsequent timed-health or persistence changes. Core is read-only in this stage.

## Minimal presentation design

- At finite wave-six Victory, show completion separately from endless progress. Offer explicit ContinueEndless and FinishRun intents, scoped to the current run/wave. No UI reward arithmetic or new payout. Restart remains a separate reset action.
- HUD shows finite `wave / 6` or endless absolute wave plus endless completed count. Finished/Defeat shows finite completion, endless completion, ledger and core terminal reason without calling every terminal state victory.
- Build all three offer slots from GetOfferSlots, including disabled slots. Keep authoritative Card.Effect verbatim. Distinguish permanent, next-wave supply, one-wave contract and unavailable kinds; honor Enabled and typed unavailability. UI does not infer eligibility or simulate stat deltas.
- Show pending versus active contract by identity/name, with full description available in codex; mark one-wave duration. Preserve free-pick/shop order and command scope. Repeated Continue/Finish clicks remain core-validated.
- Codex adds RiskLimit wording and an explicitly temporary contract catalog, distinct from acquired permanent ranks. No persistence/checkpoint UI in this stage.

## Gates

Fresh core regressions; formatter tests; actual control bounds for all six contract descriptions and disabled slots; real command wiring/duplicate/stale UI lifecycle; full existing host aggregate; native finite completion decision, contract/disabled fixtures and text inspection. Controlled fixture is not a claim of long-session survival or balanced endless play. Refresh font coverage from actual new text, unchanged OFL. No device/export/Unity claim.

## Rollback and limits

Presentation can revert independently to frozen CUI only together with its finite-core baseline. D2 remains unchanged. Existing illustrative-unit inconsistency, missing fusion art and unproven late-game balance remain explicit. No new art or gameplay rules.

## Final scoped result (20:01 UTC)

Fresh automated aggregate and native controlled UI gates pass; see docs/evidence/DUI. Real production Buding1030 cleared five waves then died in wave six, earned115 minus spent33 equals balance82. This is retained as experience evidence, not erased or called a10–30-wave pass. Manual Continue→wave7 is verified only from a controlled finite-complete fixture; manual Between Finish remains unrun, while actual callback/duplicate/stale tests pass. No gameplay balance was changed to obtain a pass.
