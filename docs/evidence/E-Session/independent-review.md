# E session/codec independent review — 2026-10-31 20:19 UTC

Exact isolated copy /tmp/hmi-session-independent.SHFTis, build Release -m:1:0 warnings/errors; full copied worker suite plus independent cases terminal PASS77. Production files unmodified by reviewer. Shared IndependentCases source is test-only; worker may integrate after reconciliation.

Independent additions reject duplicate nested Count/null card/external catalog mismatch; re-read advertised recovery after another authority closes it; stale epoch rejection; failed new-run transaction preserves prior hero/token while blocking exit/resume; retry starts intended hero once; first-snapshot unsaved mode advances later waves with no writes/no false saved flag; process restart cannot recover that unsaved run; Close failure blocks unsaved/exit bypass and retry reaches Closed.

Earlier read-only finding: store acknowledgement assigned new sequence before Core prepare/initialize failure, leaving retry using a different request. Worker fixed with explicit Prepare/Initialize stages; principal tests exercise postcommit rejection without rewriting authority. UnauthorizedAccessException was also added to expected failure handling after review. These were real pre-acceptance gaps, not hidden by initial passing counts.

Scope is pure session/Core/store integration with injected ports plus worker actual filesystem cases, not Godot host UI/crash proof. Native first EUI screenshots revealed misleading unsaved wording (no previous safe point exists; whole run stops saving), now assigned to UI correction. Host focus, terminal close gating, real Resume/new-run and loss-of-ack integration remain separate gates.

Final refreshed source: worker added canonical UUID rejection and finalized successful unsaved recovery refresh, plus extended pending-contract/shield restore cases. Exact independent /tmp/hmi-session-final.drV1tH rerun terminal PASS144, build0 warnings/errors. Current production Persistence .cs files compare byte-identical to snapshot. Final log/manifest are e-session-final-independent.log and e-session-final-source-sha256.txt; prior77 is earlier scope, not final count. Native host gate still pending.
