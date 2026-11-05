# Candidate strict import and migration gates

Baseline: `7425c3d1268480be1144141b32de71b2b8726d4e`, the independent input/APK/candy candidate. This branch does not change main or the existing integration branch. The prior investigation at `08d4510e62c1dac602b99adef37924bd51bbe6de` is a reference, not substituted test evidence.

## Android editor-import workaround

The owner requires an installable APK, not editor one-click deployment. Only the Android preset's `runnable` flag changes to `false`. This disables one-click device polling/deployment; it does not remove the Android preset or disable explicit command-line APK export. Linux remains runnable. Core, Persistence, content and gameplay/presentation code are unchanged.

Fresh cloud A/B checks used Godot 4.6.3 Mono, .NET SDK 10.0.403, serial builds and separate writable editor/data directories. The build passed with zero warnings/errors. Three original imports each exited zero but emitted `EditorSettings not instantiated yet when getting setting "export/android/shutdown_adb_on_exit"`. Three otherwise matching imports with Android `runnable=false` each exited zero without `ERROR:`, `Exception` or `SCRIPT ERROR`. No diagnostic filter was changed. This is a project-level workaround for the Android exporter shutdown path, not an upstream engine lifetime fix or a claim that all shutdown errors are impossible.

An actual `--export-debug "Android Food Preview"` invocation still reached the Android exporter. It exited 1 because this isolated environment lacks configured Android source/build templates, Java SDK and Android SDK paths. No APK was produced. This proves export dispatch remains available, not successful packaging, installation or update compatibility. The earlier reference branch's separate Gradle dependency failure remains historical evidence, not a dependency test of this cloud environment.

## Migration boundary retained

The unchanged baseline `RuleMigrationTests` reproduced its `PreserveHappy` null dereference (exit 134). Its rule1 fixtures expect the retired direct-conversion contract. Rule1 direct conversion remains closed; current supported preserve-old/start-new behavior belongs to `InspectUpgrade`/`ConfirmUpgrade`. The current default `RuleUpgradeTests` passed 579 checks and explicitly reported `OLD_READER NOT_RUN`. These are different contracts; one cannot be claimed as the other passing.

Further strict runner and immutable historical-reader results will be recorded separately. Neither this checkpoint nor clean imports imply a fully green aggregate, fresh APK, hardware/controller performance or real user-save certification.
