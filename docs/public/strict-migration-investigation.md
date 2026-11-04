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

The historical RuleMigration suite remains failed with an explicit incompatibility assertion instead of a null dereference. Current Core/Persistence behavior is unchanged. Strict presentation reruns under the workaround remain pending.
