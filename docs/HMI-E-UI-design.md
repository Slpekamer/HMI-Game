# E persistence UI contract proposal

2026-10-31. Design only, authorized by implementation lead; not part of frozen DUI source. Await unified integration tree and coordinator API before implementation.

## Ownership

The persistence/host coordinator owns durable state, file validation, safe-point choice, operation tokens, retry identity, state transitions and whether play can proceed. UI receives immutable presentation data and emits intents. It never reads save files, predicts a successful write, chooses fallback snapshots or revives a closed run.

Suggested presentation state contains: CanContinue, Summary, RollbackWarning, SaveStatus, ErrorText, CanRetry. Actual names can match the frozen coordinator API. Do not infer eligibility from localized text or file presence.

## Selection and confirmation

Show “继续最近安全点” only when coordinator marks it eligible, with hero/wave summary and explicit rollback scope supplied by the coordinator. If recovery fell back from damaged data, show the reason and actual older point; never imply the latest wave was restored.

Starting a new run while a resumable run exists requires a replace confirmation. Confirm emits the captured new-run intent; cancel preserves current character/seed selection and existing save. UI confirmation is not itself a write or proof of success.

## Pause / voluntary exit

Add “退出至选人 · 保留最近安全点”, separate from Restart. Explain that current-wave progress is not saved and resuming returns to the last safe point. The coordinator controls pause and exit; UI must not directly restart or delete anything.

## Save failures and explicit degraded operation

`ShowSaveFailure(message, canContinueUnsaved)` is a blocking UI surface until the coordinator chooses a transition. Suggested events: RetrySaveRequested and ContinueUnsavedRequested. The host retains a stable operation/token so repeated clicks cannot create a second transaction or write a different snapshot.

- Prepared next-wave save failed: combat must not begin silently. Offer Retry, or only when authorized by coordinator, “继续本波（未保存；恢复时回到上一安全点）”. The warning remains visible during unsaved play.
- PendingStart first write failed: no new playable run may begin; canContinueUnsaved must be false.
- PendingStart succeeded but first snapshot failed: coordinator may permit explicit unsaved continuation with the full warning; not a silent fallback.
- Closed write failed after death/completion: no ordinary Continue or ContinueUnsaved and no resurrection. Show terminal-state save warning, Retry, and coordinator-approved return-to-menu warning. Retrying persists the same terminal intent, not a new active run.

## Modal / lifecycle rules

All new dialogs trap keyboard focus, prevent click-through and preserve existing codex/pause focus-loss protection. Capture callback intent/token for the displayed operation. Repeated open/close/retry and stale callbacks after a different run must be harmless. Back/Cancel cannot convert a failed save into implicit permission to play.

## Acceptance gates

Actual controls at 1280×720 and the established viewport: full rollback/error text visible, buttons within safe bounds, all source strings covered by shipped font. Tests must cover each branch above with coordinator-provided states, repeated/stale button signals, focus loss, modal keyboard navigation and no UI file I/O. Native screenshots must clearly distinguish safe Continue, unsaved-warning continuation, damaged fallback, replacement confirmation and closed failure. Host integration tests must prove combat remains blocked at Prepared failure until an explicit allowed action and that PendingStart-first-write failure cannot start gameplay. Export/device durability remains a separate verification claim.
