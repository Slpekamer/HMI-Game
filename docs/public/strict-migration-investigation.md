# Strict import and migration diagnosis — WIP

Baseline: `4030b40540f893985bd655369ba4460fb968e71d`. No Core or Persistence behavior has changed.

## Historical migration failure
The unchanged RuleMigrationTests build succeeds, then PreserveHappy dereferences a null Recovery at line 27. The current Core TryConvertLegacy deliberately returns RulesMismatch, while the historical suite expects the retired rule1 direct-conversion contract. Current RuleUpgradeTests explicitly assert that converter stays closed, and the supported rule1 behavior is preserve-old/start-new through InspectUpgrade/ConfirmUpgrade. Reopening the old converter to make the historical suite green would violate that boundary.

A test-only precondition now reports the inspection kind/detail instead of an unexplained NullReference. It must still fail; no historical assertion, strict check or test invocation is removed. The synthetic DTOs in these suites are not exact historical-writer evidence. An exact rule2 source snapshot was subsequently found (see the verified reader section below); the rule1 dev snapshot remains an invalid substitute.

## Editor shutdown investigation
The official Godot 4.6.3 Android export poll thread reads export/android/shutdown_adb_on_exit when a runnable Android preset is present. The observed error means EditorSettings no longer exists at that read. The exact final reference-holder/timing is not yet proven by a native stack. A separate A/B copy tests making the Android preset non-runnable. This would disable Android one-click deployment, not merely suppress a log; explicit command-line export is a separate feature. After explicit acceptance of the one-click deployment tradeoff, this candidate sets only the Android preset to non-runnable. No engine upgrade is assumed and no ERROR filter is relaxed. This is a project-level workaround, not an upstream lifetime fix.

## A/B and export evidence
Three imports of the original runnable preset each returned zero but emitted the target EditorSettings ERROR. Three otherwise matching imports with only Android runnable=false returned zero without ERROR. A separate command-line Android debug export still entered the Android exporter, but failed because Android source/build templates, Java SDK configuration and Android platform/build tools are absent. That failure is retained; no APK or installation pass is claimed. One-click deployment is disabled; command-line packaging still needs its normal dependencies.

## Exact prior reader verified
The immutable historical snapshot `e4e7e6351e8de39c3376594968022f661e189d12` retains real rule2 Core/Persistence source and the same gameplay catalog bytes. Its rejected melee-presentation experiment is not being adopted. The new explicit-commit runner extracts source blobs from Git, verifies the version/catalog, records source SHA256 and reader DLL identity, then calls the real historical coordinator Inspect. It neither rewrites source constants nor substitutes a mock reader.

The default current RuleUpgrade run passes 579 checks but reports old reader not run. With the exact compiled prior reader it passes 653 checks: the stage-0 original selection positive control is Selected; all six actual interrupted-upgrade stages are Uncertain to the old reader; original saved bytes and artifact counts stay unchanged. The tests still use synthetic protocol fixtures, not saves from every historical installation. This closes this specific optional prior-reader gate, not the unrelated older schema2-to-schema3 reader gate or all migration coverage.

The historical RuleMigration suite remains failed with an explicit incompatibility assertion instead of a null dereference. Current Core/Persistence behavior is unchanged. Strict presentation reruns under the workaround are recorded below; no aggregate pass is implied.

## Strict presentation reruns
The unmodified strict runners for HostLifecycle, ResponsiveUi (893 checks) and the explicitly legacy FoodAnimationMixer contract (17,590 assertions) all returned zero through build, editor import and runtime. Their existing ERROR/Exception rejection logic remains intact. This is materially different from the earlier standalone diagnostic runtime passes after a failed import. It does not cover every historical test, native rendering, hardware devices or an aggregate release gate.

## APK dependency status
Official Godot 4.6.3 mono Android source/debug/release templates and JDK 21 are now available in the execution environment. Android SDK setup remains blocked by a connection-refused download; no workaround through alternate versions or endpoints was attempted. The initial actual APK export failure is retained, and no successful APK export or installation is claimed. Restoring SDK/build dependencies and rerunning packaging remains separate work.

## Upstream references and interpretation
- [Android exporter thread exit](https://github.com/godotengine/godot/blob/4.6.3-stable/platform/android/export/export_plugin.cpp#L418-L457) gates the settings read on a runnable preset.
- [Exporter lifecycle](https://github.com/godotengine/godot/blob/4.6.3-stable/platform/android/export/export_plugin.cpp#L4053-L4087) starts/stops the poll thread.
- [EditorSettings destruction](https://github.com/godotengine/godot/blob/4.6.3-stable/editor/settings/editor_settings.cpp#L1297-L1303) and [EditorNode teardown](https://github.com/godotengine/godot/blob/4.6.3-stable/editor/editor_node.cpp#L8621-L8651) explain the lifetime boundary; the precise last reference-holder remains unproven. Mono reference retention is a hypothesis, not an observed stack trace.
- [Runnable one-click deployment](https://docs.godotengine.org/en/4.6/tutorials/export/one-click_deploy.html) documents the disabled feature. It is distinct from APK packaging and manual installation.

Current scoped result: import workaround verified, specific prior-reader compatibility gate verified, historical direct-migration suite still intentionally incompatible/failed, APK verification blocked. Production Core/Persistence and save policy remain unchanged.

## APK continuation after dependency restoration
The SDK download blocker above was resolved using the official stable command-line tools and ordinary permitted installation. The actual template requirements are Android platform 36 and build-tools 36.1.0, now installed alongside JDK 21 and the verified 4.6.3 mono Android templates. An isolated build of source `56db1dd9ed3291a7eed8dee904e8f513ecb2546a` passes build/import and the Godot-driven Android-arm64 .NET publish step. A local test-only debug signing key was created with authorization; no signing material is published, and compatibility with an older APK signature is not promised.

The first Gradle wrapper download failed with network unreachable. The same official Gradle 8.11.1 distribution was then downloaded through the existing permitted download tool and verified against its official SHA256, allowing the wrapper to start. The next real export still failed while resolving Android Gradle Plugin 8.6.1. A diagnostic Gradle invocation reports network-unreachable errors on its configured repository requests. No proxy settings, server, security permissions or plugin version were changed to evade the failure. No APK was produced, signed-package validation was therefore not run, and no device installation is claimed. Packaging remains blocked at Gradle dependency resolution rather than at the reviewed presentation code or the runnable-preset setting.
