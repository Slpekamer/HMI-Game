# Integrated damage fixture consent repair

Independent test-only branch based on `8054667027b9260e03fd85772a3c47dbb2909df2`.
No runtime C#, Core/Persistence, asset, scene, gameplay or rendering settings change.

## Requirement and diagnosis

The two older damage fixtures called private `GameHost.Start` once and assumed
combat had begun. The current production flow first asks for explicit rule-space
preparation, returns to selection with a nonresumable `PendingStart`, and requires
a second start intent. The fixtures skipped that contract.

Before changing the fixtures, a fresh baseline build and strict headless editor
import passed. `EnemyDamageSemanticProbe` passed 41 checks. The unchanged
`IntegratedDamageHostProbe` failed at `actual durable new run started with original
stats`; `IntegratedDamageFirstDrawProbe` failed at `initial reference differs`.
These failures are retained as baseline counterevidence, not asset regressions or
evidence that production consent should be weakened.

## Change and safety boundaries

Both probes now use one test-only setup helper, injected only into the isolated
test project. It follows production `StartRequested`, presses the real visible
preparation button, and sends the explicit second `StartRequested`. Reflection
reads state for assertions; it does not set `_ruleSelected`, inject a session,
seed a save, or change simulation state. The direct private `Start` remains only
as a negative check that the old assumption cannot bypass consent.

Eleven setup checks cover fresh isolated storage, missing confirmation, repeated
start, cancellation, stale confirmation, a fresh confirmation after cancellation,
durable `PendingStart`, repeated confirmation/no automatic combat, and the explicit
second start's hero, tick and durable `Active` state. The Host restart also uses
the production start callback after closing the old run.

All original damage, imported-resource ownership, pause/focus/restart/disposal,
Core reference equality, natural first-hit and first-post-draw assertions remain.
No expected value, diagnostic gate, damage cadence, HP, position or event was
weakened or injected. Skipping consent, weakening the old assertions, and changing
production to satisfy an obsolete fixture were rejected alternatives.

## Scoped results

Godot 4.6.3 Mono and .NET SDK 10.0.403:

- Two independent candidate copies with separate fresh data directories passed
  build, strict headless editor import and all three behavioral probes without
  the runner's error, exception or resource-leak diagnostics.
- Enemy damage semantics: **41 checks passed**, unchanged.
- Integrated Host: **42 checks passed**, including the 11 setup checks.
- First-draw headless preflight: **tick 80, one natural enemy damage event**.
  Every sampled Host state equals the independent Core reference. New feedback
  is active with age zero before rendering.
- Portable Core: **646,403 assertions passed**.
- Actual-engine Checkpoint Host: **36 checks passed**, including clean build and
  strict import. Rule Upgrade: **579 checks passed**. Its exact old-reader stage
  reported **NOT_RUN**; this does not establish historical reader compatibility.
- Architecture and all nine preserved notice checks passed.

Two deliberate negative controls were compiled in separate test-project copies:
omitting the preparation button press, and omitting the explicit second start.
Both probes rejected both mutations (four expected exit-1 results), respectively
at `confirmation selects a rule slot but does not auto-start combat` and
`explicit second start begins the requested hero at a durable initial safe point`.
No success marker was emitted. These mutated copies are counterevidence only;
the authored fixture and production sources were unchanged by the controls.

Reproduce using an empty work directory for each independent run:

```sh
export GODOT_NET=/path/to/Godot_v4.6.3-stable_mono
python3 scripts/test_integrated_damage.py prepare --work /tmp/damage-consent-run
python3 scripts/test_integrated_damage.py behavior --work /tmp/damage-consent-run
```

`prepare` still fails closed on strict import errors. `behavior` remains separately
labelled and does not clear a failed import. A previously selected save directory
fails the new freshness precondition rather than silently bypassing confirmation.

## Open gates

This is a scoped headless behavioral result, not a native first-post-draw image or
visual acceptance. Native frame stamps, hurt-tint contrast, brass readability,
crowd/wall-shadow behavior, performance, physical devices, export/APK and owner
review remain unverified here. No full-suite, migration, release or merge approval
is implied. The enemy material study's visual acceptance remains open.
