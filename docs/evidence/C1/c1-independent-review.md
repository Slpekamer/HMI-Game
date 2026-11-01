# C1 compiled skill graph — independent review

2026-10-31 19:01 UTC. Isolated source: HMI-Game-endless-core. Reviewer owns only SchemaV2IndependentTests.cs; no Core/product adapter edits.

Fresh separate-output snapshot executed **538393 assertions PASS**, log c1-independent-final.log; exact source hashes c1-independent-source-sha256.txt. This includes unchanged legacy suite, worker golden digest comparisons and worker schema tests, plus 37 independently authored assertions. Shipped content remains schema 1; this is infrastructure, not delivery of deep content/endless/save.

Independent tests cover joint ALL/ANY contradictions under exclusive assignments, transitive conflicts, a legal OR escape, rank/cap and member upgrades, new-run group reset, stale offered sibling rejected at command consumption, duplicate selection and prior-run callback rejection, mutable input detachment, malformed/missing/self/duplicate/cross-slot/null requirements, invalid groups, syntax/version boundaries, budget rejection, and zero-allocation warmed eligibility.

Review found legacy empty-Parent acceptance regression; it was corrected and explicitly tested before this pass. Review also required branch-conditioned finite-pool validation rather than global summed caps. Current terminal-assignment algorithm skips illegal selected states and nonterminal none states; worker added nested-group positive/negative fixtures. This is valid under current acyclic, monotone rank/positive prerequisite rules; future respec/dynamic prerequisites require re-review.

Compilation is bounded to 256 cards / 16 requirements / 65536 exclusive assignments. Independent synthetic maximum load measured 859.05 ms, 103616 allocated bytes on this cloud run; not a phone startup guarantee. Warm eligibility allocated zero bytes. Existing mixed core stress still reports 2895 dropped cosmetic events, not universally zero drops. Runtime state remains pure C# and authoritative Choose revalidates graph eligibility; no animation damage callbacks or Dodge restored.

No new blocking C1 defect found in reviewed scope. Conservative cycle/stat-envelope rejection is documented, not arbitrary graph completeness. Actual graph UI, new content balance, high-rate presentation, endless 1000-wave tests, persistence transactions and physical-device checks are NOT passed by this result. Source released to worker for its final aggregate and scoped commit; any later Core/test mutation requires another verification.
