# RuleUpgradeTests

All files are generated memory stores or fresh temporary directories. Never pass a
real user save directory to these tests. No Godot process is required.

```sh
dotnet build tests/RuleUpgradeTests/RuleUpgradeTests.csproj -m:1 -t:Rebuild
dotnet tests/RuleUpgradeTests/bin/Debug/net10.0/RuleUpgradeTests.dll
```

The default run includes Core conversion, independent adversarial tests, each-write
before/after injected faults, actual physical files, and six real child process
kills. It explicitly prints OLD_READER NOT_RUN without the additional argument:

```sh
dotnet tests/RuleUpgradeTests/bin/Debug/net10.0/RuleUpgradeTests.dll \
  --old-reader /path/to/exact-old-reader/Reader/bin/Debug/net10.0/Reader.dll
```

The optional legacy-reader gate requires a separately supplied executable built from the exact prior Core/Persistence version under test. Record its source commit, source hashes and DLL hash in your own test evidence. Its entry point must call the real coordinator Inspect; a mock expected-version check is not equivalent. Positive control must read the original rule2 selection before subsequent upgrade stages are tested. This minimal source snapshot does not bundle that historical executable.

For the rule2-to-rule3 contract, an immutable historical source is available at
`e4e7e6351e8de39c3376594968022f661e189d12`. Fetch it into an accessible repository,
then run the source-building wrapper from any working directory:

```sh
git fetch origin e4e7e6351e8de39c3376594968022f661e189d12
python3 scripts/test_exact_prior_upgrade_reader.py --baseline . \
  --expected-sha e4e7e6351e8de39c3376594968022f661e189d12
```

The wrapper extracts immutable Core/Persistence/catalog/SDK-pin blobs without
changing historical constants, rejects a non-rule2 source or catalog mismatch,
and records both source trees and all reader/current output-file hashes. Its
reader calls the real historical coordinator. Stage 0 must recover the original
Active rule2 run/checkpoint; all six after-write upgrade states must be Uncertain
without exposing checkpoint authority. The current suite checks the complete
six-write sequence through selector publication, enforces a bounded child-process
timeout, and verifies the old reader did not modify saved bytes or add artifacts.
The printed temporary evidence directory contains commands, SDK versions,
working-tree status, provenance and build/runtime logs.

These fixtures are created by the current test, not actual old writers or user
installations. They do not certify the separate schema2-to-schema3 compatibility
contract below, every historical save, or the retired rule1 direct-conversion
suite. That historical suite remains failed, with its incompatibility now
reported explicitly instead of as a null dereference. Production conversion and
consent rules are not changed.

The separate `scripts/test_rule_registry_old_reader.py` compatibility runner likewise requires explicit `--baseline /path/to/legacy-checkout --expected-sha <full-commit>` arguments. It rejects missing arguments, invalid identities and dirty source before building the reader. Supply the correct legacy version for the compatibility contract being tested; an arbitrary commit is not evidence of historical compatibility.

`--independent-only` runs reviewer-owned cases alone. `--upgrade-child` is an internal
process-kill fixture mode; the parent test always gives it a newly generated temp
root. Synthetic wave20 DTO fixtures validate pending effects and preserved state;
they are not a record of playing 20 waves.
