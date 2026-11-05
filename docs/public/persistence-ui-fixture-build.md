# Persistence UI fixture dependency repair, 2026-11-05

## Requirement and scope

Restore the isolated persistence UI regression's compile dependency closure without changing production behavior or weakening its checks. Baseline `d25f776` failed before import: `AssetVisuals.cs:21` referenced `ControlledGaitDrill` and `NormalPresentationCandidate`, neither included in the generated test project's explicit compile list (CS0103, two errors).

Only `scripts/test_persistence_ui.py` and this report change. The production project, source, assets and probe assertions are unchanged. Other independently maintained fixture repairs are outside this report.

## Decision and isolation

Include the two existing presentation helper files, then reference the existing `src/Core/Hmi.Core.csproj`: `NormalPresentationCandidate.AppliesTo` takes the real `EntityView` and uses `EntityKind`. This builds the canonical Core library, not a replacement contract or a simulated selector. It does **not** mean the test executes Core simulation or validates durable storage.

The generated Godot project still disables default compile items, includes all `GameUi` partials plus its explicit UI/presentation dependencies (16 source files at this checkpoint), and instantiates only the copied `PersistenceUiProbe` entry scene. No GameHost, persistence adapter, coordinator or gameplay scene is compiled/instantiated. Core has no engine dependency and is not a Godot scene host. The fixture does not set candidate flags or rewrite selector behavior.

Rejected alternatives: compiling every Godot source would broaden the fixture into the game assembly; stubbing selectors would stop exercising the real dependency; copying selected Core source files would duplicate the library's compilation policy and require additional transitive source dependencies. Referencing the canonical Core project preserves its .NET Standard 2.1/C# 8/nullable/warnings-as-errors configuration while the UI fixture retains Godot 4.6.3 and net10.0.

## Verification

On Godot 4.6.3 Mono and .NET SDK 10.0.403, run `GODOT_NET=/path/to/godot python scripts/test_persistence_ui.py` with `dotnet` on PATH and a writable temporary directory:

- Fixed strict run: build exit 0, headless editor import exit 0, runtime exit 0; `PERSISTENCE_UI_PROBE PASS 100`.
- Original assertion template is unchanged, including stale callbacks, repeated actions, cancellation, blocking error states, focus, button bounds and unsaved/recovery wording.
- Nonzero exit, `ERROR:`, `Exception` and missing runtime pass marker remain fatal. No diagnostic filtering or warning suppression was added.
- Build is **not warning-free**: 19 CS0649 warnings, zero errors. Twelve are pre-existing unassigned UI callback fields in this isolated harness; seven are newly included `ControlledGaitDrill` fields retaining their defaults because the drill controller is intentionally absent. The original failed build had 12 warnings and two errors.
- Generated manifest inspection confirmed the explicit 16-source compile list, only the Core project reference, and the probe entry scene. Python syntax compilation and `git diff --check` passed.

### Counterevidence retained

After the successful run, two separate clean copied projects were built without changing the fixed copy or baseline evidence:

1. Remove both helper compile entries and the new Core reference, restoring the original generated project: build exit 1, the same two CS0103 errors at `AssetVisuals.cs:21`, 12 warnings.
2. Keep both helpers but omit only Core reference: build exit 1, CS0234 for `Hmi.Core` and CS0246 for `EntityView`, two errors. This confirms that adding the helper names alone is insufficient.

The harness retains `compile-files.txt`, build/import/run logs and generated project in its printed evidence directory. Separate negative-control build logs and the original failure were retained outside source control; private machine paths and full operational logs are not published.

## Limits

This is a headless UI semantics and dependency regression, not native appearance approval, full-game/strict-suite acceptance, durable-write or migration validation, old-reader compatibility, Android/controller certification or performance evidence. A scoped clean import here does not erase earlier strict-import failures elsewhere. No production compatibility contract or gameplay/save rule changed.
