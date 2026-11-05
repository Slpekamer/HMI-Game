# Candidate strict import and migration gates

Baseline: `7425c3d1268480be1144141b32de71b2b8726d4e`, the independent input/APK/candy candidate. This branch does not change main or the existing integration branch. The prior investigation at `08d4510e62c1dac602b99adef37924bd51bbe6de` is a reference, not substituted test evidence.

## Android editor-import workaround

The owner requires an installable APK, not editor one-click deployment. Only the Android preset's `runnable` flag changes to `false`. This disables one-click device polling/deployment; it does not remove the Android preset or disable explicit command-line APK export. Linux remains runnable. Core, Persistence, content and gameplay/presentation code are unchanged.

Fresh cloud A/B checks used Godot 4.6.3 Mono, .NET SDK 10.0.403, serial builds and separate writable editor/data directories. The build passed with zero warnings/errors. Three original imports each exited zero but emitted `EditorSettings not instantiated yet when getting setting "export/android/shutdown_adb_on_exit"`. Three otherwise matching imports with Android `runnable=false` each exited zero without `ERROR:`, `Exception` or `SCRIPT ERROR`. No diagnostic filter was changed. This is a project-level workaround for the Android exporter shutdown path, not an upstream engine lifetime fix or a claim that all shutdown errors are impossible.

An actual `--export-debug "Android Food Preview"` invocation still reached the Android exporter. It exited 1 because this isolated environment lacks configured Android source/build templates, Java SDK and Android SDK paths. No APK was produced. This proves export dispatch remains available, not successful packaging, installation or update compatibility. The earlier reference branch's separate Gradle dependency failure remains historical evidence, not a dependency test of this cloud environment.

## Migration boundary retained

The unchanged baseline `RuleMigrationTests` reproduced its `PreserveHappy` null dereference (exit 134). Its rule1 fixtures expect the retired direct-conversion contract. Rule1 direct conversion remains closed; current supported preserve-old/start-new behavior belongs to `InspectUpgrade`/`ConfirmUpgrade`. The current default `RuleUpgradeTests` passed 579 checks and explicitly reported `OLD_READER NOT_RUN`. These are different contracts; one cannot be claimed as the other passing.

The test-only precondition now reports `inspection=Uncertain` and the contract incompatibility rather than dereferencing null. The historical suite still exits 134. No original assertions, historical test invocation, error filter or production conversion behavior was removed or relaxed.

## Fresh exact-prior reader result

The immutable rule2 commit `e4e7e6351e8de39c3376594968022f661e189d12` was fetched and its actual Core/Persistence source compiled. Its gameplay catalog exactly matches the candidate (SHA256 `30981d16c5c608f70e750bbaf809eb1e811aef34d71f24258fcac852eba0e2de`), and both SDK pins resolve to 10.0.403. No historical source constants are rewritten or experimental presentation code adopted.

The wrapper and reader are adapted from `08d4510`, with stronger Active checkpoint/run/sequence positive-control checks, exact six-write transaction coverage, effective child-process timeouts and complete historical/current source and build-output hashes. The real old coordinator accepts the original selected Active rule2 checkpoint at stage 0, then returns Uncertain without checkpoint authority at all six actual after-write upgrade stages. Original saved bytes and artifact counts remain unchanged. The upgraded suite passes 656 checks; reader and current builds have zero warnings/errors. A current-rule3 source is rejected (exit 1), and a shortened commit identity is rejected (exit 2).

These are current-generated synthetic fixtures inspected by an actual historical decoder. They are not old-writer-created saves, certification of every real installation, or evidence for the unrelated schema2-to-schema3 reader gate. The default no-reader run remains 579 checks with `OLD_READER NOT_RUN`.

## Fresh strict runner results

The original strict build/import/runtime runners now pass without bypassing their diagnostic checks: keyboard lifecycle 6,460; floating input 130; corrected gamepad UI 209; HostLifecycle; responsive UI 893; and the explicitly legacy food-animation mixer 17,590. These are scoped engine runs, not physical controllers, phone performance or new visual approval.

Aggregate and additional boundary checks remain separately classified. Neither this checkpoint nor clean imports imply a fully green aggregate, fresh APK, hardware/controller performance or real user-save certification.
