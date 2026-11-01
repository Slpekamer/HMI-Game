# BUG-0002 — Timed health-item expiry leaves a stale health component

Requirement: portable stat effects and ECS health authority must agree; expiry cannot heal or retain a removed maximum-health benefit.

Independent reproduction on the D2 frozen candidate: set the otherwise shipped shield's Duration to1 second, buy it through ordinary intermission and start the next wave. Initially Stats.MaxHealth and Health.Maximum are125. After65 ticks Stats.MaxHealth becomes100, but Health.Maximum remains125. This is a generic supported-content lifetime defect; shipped shield has duration0 and tonic only affects attack rate, so the six D2 contracts' recorded lifecycle checks were not invalidated.

Root cause: timed StatEffectSystem recalculation updated the stat resource but did not synchronize the player's Health component maximum. Fix: after a timed expiry recalculation, set component maximum to the final recomputed stat (including any active one-wave contract), and clamp current HP downward only. Restore of a larger maximum never grants health. Reuse the same synchronization helper for contract expiry rather than maintaining divergent constraints.

Tests before commit: positive timed buff expiry clamps high current HP; expiry preserves injured HP; expiration of a negative max-health item raises maximum without healing; paused ticks do not expire it; active contract plus timed shield uses the exact remaining final maximum; complete existing suite and zero-allocation probes. No persistence, UI or unrelated balance change.

Fresh result: own and exact-source independent full suites both PASS624451, including20 new assertions. Ordinary shield125→100 clamps current120→100; injured current is unchanged; negative health-item expiry80→100 does not heal; pause does not advance expiry; active protection plus timed shield135→110, then wave-end100/current9 remains unhealed. All prior compatibility, progression and strict warm-allocation gates pass. Evidence includes reproduction, full runs and independent source hashes. This is a core rule correction; no UI/device verification is implied.
