# BUG-0001 — Disposed animation presenter after entity-slot reuse

Status: corrected with red/green actual-engine regression; full aggregate passed538343 assertions plus engine gates; independent normal native replay passed (blue1030, expanded companions, wave2 continuing, pause and reset). Visual model acceptance remains separately FAILED by owner.

## Reproduction
Native blue hero seed1030: clear wave1, buy gloves once, pick companion expansion, enter wave2. Timer remained24 and pause stopped responding while ObjectDisposedException flooded from CharacterPresentation.Snapshot. The pure-core, clip sampling, camera and UI layout tests had passed; none exercised engine presentation-object lifecycle across entity-kind replacement.

## Root cause and fix
GameHost replaced the visual node when entity generation changed, but assigned a new CharacterPresentation only for non-pickups. If an animated actor slot became a pickup, the old presenter still held the queued-for-deletion Node. Once the engine freed it, the next render accessed the disposed object. The fix clears the old presenter on EVERY generation replacement, before binding the new visual. Pending attacks from a different generation are discarded; matching new-generation events remain eligible. This repairs ownership rather than hiding the exception with null/disposed checks.

## Regression evidence
`scripts/test_host_lifecycle.py` copies the project to an isolated temporary directory, injects the test template and internal test access ONLY into that copy, then builds/imports/runs the actual Godot engine. No test hooks, altered health or internal access are shipped in the production host.

The same test FAILED before the fix with the native ObjectDisposedException, then PASSED after the fix:
- animated actor→pickup in the same slot, with actual queued deletion flushed across engine frames;
- pickup→animated actor and animated generation replacement;
- stale generation attack cleanup;
- explicit expanded-companion test fixture through real wave1 fixed ticks→wave2, movement→idle, pause and restart.

The isolated fixture deliberately removes enemies/keeps the player alive and assigns expansion for deterministic lifecycle coverage. It is NOT normal gameplay evidence. An independent normal native replay is a separate required gate. The whole aggregate now invokes this regression.

Evidence: docs/evidence/BUG-0001/engine-{red-before-fix,green-after-fix}.txt. Core rules/config unchanged by this bug fix. Rejected r3 candidate APK remains held and must not be delivered.

Validation environment note: the first full rerun failed on Godot's editor shutdown Android-setting diagnostic after another export session. Isolated XDG_CONFIG_HOME per validation removed the cross-session state; the next complete aggregate passed without filtering out errors. Both logs are retained.

Independent normal native retest closed the concrete blocker: earned8−gloves5=3, repeat charge guard, companion expansion, timer24→22→16, moving/idle/Space-no-dodge/pause and restart100HP/0coins/wave1. No managed exceptions. New exports remain unrun and model aesthetics remain rejected.
