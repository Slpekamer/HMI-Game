# Strict import and migration diagnosis — WIP

Baseline: `4030b40540f893985bd655369ba4460fb968e71d`. No Core or Persistence behavior has changed.

## Historical migration failure
The unchanged RuleMigrationTests build succeeds, then PreserveHappy dereferences a null Recovery at line 27. The current Core TryConvertLegacy deliberately returns RulesMismatch, while the historical suite expects the retired rule1 direct-conversion contract. Current RuleUpgradeTests explicitly assert that converter stays closed, and the supported rule1 behavior is preserve-old/start-new through InspectUpgrade/ConfirmUpgrade. Reopening the old converter to make the historical suite green would violate that boundary.

A test-only precondition now reports the inspection kind/detail instead of an unexplained NullReference. It must still fail; no historical assertion, strict check or test invocation is removed. The synthetic DTOs in these suites are not exact historical-writer evidence. The optional exact rule2 reader remains unavailable; the rule1 dev snapshot is not an interchangeable rule2 baseline.

## Editor shutdown investigation
The official Godot 4.6.3 Android export poll thread reads export/android/shutdown_adb_on_exit when a runnable Android preset is present. The observed error means EditorSettings no longer exists at that read. The exact final reference-holder/timing is not yet proven by a native stack. A separate A/B copy tests making the Android preset non-runnable. This would disable Android one-click deployment, not merely suppress a log; explicit command-line export is a separate feature. No export preset has been changed in this source checkpoint, no engine upgrade is assumed and no ERROR filter is relaxed. A/B results are pending.
