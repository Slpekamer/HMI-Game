# FEAT-0005 — Compiled skill graph, stage C1

## Requirement and scope

The user's authorized overnight extension requires deeper, extensible skill trees without moving authority out of the portable ECS. Parent's C/D task and the supplied `docs/endless-preparation/` are design provenance; their JSON case lists are specifications, not executed runners. C1 implements graph schema only. Production content, Godot presentation, finite victory behavior and persistence are unchanged. C2 content, D endless progression and E checkpoint design remain separate stages.

## Architecture and acceptance

See ADR-0003. Pure core owns immutable copied configuration, compiled rank/all/any/exclusion indices and runtime eligibility. UI receives `GetCardAvailability`; no per-frame graph assignment search occurs. Card choice still goes through existing scoped command/progression authority. Graph compilation has explicit 256-card/16-requirement/65,536-assignment limits, joint reachability, conservative effect intervals and legal terminal branch pool validation.

## Fresh evidence

- Baseline local `f470a3e`: 538343 assertions; `docs/evidence/C1/baseline-tests.log`.
- Final C1: **538393 assertions**, `full-tests.log`. The suite includes independent adversarial tests and all legacy tests, with unsupported schema moved from 2 to 3 because 2 is intentionally supported.
- Frozen baseline DLL golden digest: `668C7C237D35F542`, `legacy-baseline-digest.log`. Four new runs match: schema1, schema2 legacy parent, schema2 normalized all-of, schema1 empty-parent acceptance. Digest covers every hero's 900-tick state/events, five intermissions/offers/choices/stats; it is a regression fingerprint, not a cross-platform determinism promise.
- Worst supported synthetic load: 256 cards × 65,536 assignments, 795.62 ms / 103616 allocated bytes in this cloud run. Warm eligibility has zero allocation. This is not a mobile startup benchmark.
- Combat 300-tick honey 32/128/512: 4.87/8.45/30.82 ms, zero warm allocation. Mixed 128 all-trait: 13.13 ms, zero warm allocation, 2895 bounded cosmetic event drops. Dropped cosmetic events never drive damage authority.
- Graph negatives: unknown/self/duplicate/cycle/rank/mixed syntax, contradictory all/any under exclusivity, budgets, nested terminal dead ends and unsafe effective stats. Positives include OR escape, selected branch rank-up, reset, copied DTO isolation and incompatible effects not summed.

## Limits and verification status

Passed: netstandard2.1 compilation through the net10 console harness, fresh core suite, deterministic regression fingerprint, synthetic graph-load and warm allocation measurements. Independent final snapshot also passed 538393 assertions; review and exact source manifest are included in evidence. Not run for C1: Godot graphical integration, Unity/团结, mobile/device/IL2CPP/export, long real player sessions. C1 does not claim new playable content, endless gameplay or save files.

Rollback: revert this focused stage to schema1-only code; no shipped schema or gameplay data were migrated. Future schema2 content must be reverted separately before reverting schema support.
