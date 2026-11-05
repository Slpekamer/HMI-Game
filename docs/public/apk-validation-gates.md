# APK packaging validation gates

## Requirement and scope

Prevent a failed signature or alignment check from becoming a successful APK
packaging report. This is a tooling-only addition based on integration
`4030b40540f893985bd655369ba4460fb968e71d`; no gameplay, save, visual,
export-preset or signing-key change is included.

A historical export-evidence script outside this repository inspected a saved
certificate string and recorded ELF alignment values, then unconditionally wrote
signature and alignment PASS fields. A synthetic invalid APK with matching
certificate text, an explicit v2 verification failure, a failed alignment log and
4 KiB ELF segments still produced exit 0 and PASS. That reproduces a reporting
bug. It does **not** invalidate the separately executed official-tool results for
the original historical artifact.

The original evidence is preserved. A reusable replacement is now tracked in
`scripts/validate_apk.py`, rather than editing an old evidence archive.

## Usage

Use official Android SDK Build-Tools executables, and obtain the expected public
certificate fingerprint from a trusted prior build or signing record. Do not
learn the expected signer from the candidate being tested.

```sh
python3 scripts/validate_apk.py /path/to/candidate.apk \
  --apksigner /path/to/android-sdk/build-tools/36.1.0/apksigner \
  --zipalign /path/to/android-sdk/build-tools/36.1.0/zipalign \
  --expected-certificate EXPECTED_CERTIFICATE_SHA256

python3 scripts/test_validate_apk.py
```

The validator runs `apksigner verify --verbose --print-certs` and
`zipalign -c -P 16 -v 4` against the supplied APK in the current invocation.
It accepts no saved tool logs. Every check must pass before it writes a success
JSON report to stdout. Failure prints a diagnostic to stderr and exits nonzero.
The caller must check the exit code and keep failed attempts identifiable.

Checks cover:

- ZIP integrity and duplicate entries; required manifest/DEX entries.
- A single expected signer, successful APK v2 verification, explicit official
  success output and zero tool exit codes. Missing tools, timeouts, absent
  markers and contradictory failure output fail closed.
- Official ZIP 16 KiB alignment validation plus independent ELF64/AArch64 shared
  library headers, bounded program-header tables, PT_LOAD file/memory ranges,
  power-of-two alignment of at least 16 KiB and offset/address congruence.
- ARM64-only native library entries with exact `libgodot_android.so` and
  `libmonosgen-2.0.so` runtime names, and exactly one
  nonempty asset entry for each HmiGame, Hmi.Core and Hmi.Persistence assembly.
- No `.jks` or `.keystore` entries; unchanged APK SHA256 before/after checking.
- Checks remain active under Python optimization; no safety gate uses `assert`.

The alternative of merely parsing saved logs was rejected because those logs may
be stale or belong to different bytes. The command does not sign, realign, modify,
install or upload the APK. Use it as a separate packaging gate, not as a bypass
for failed build/import/export diagnostics.

## Verification, 2026-11-05

- 43 synthetic Python tests pass, including failed v2/zipalign results, wrong or
  duplicate signers, nonzero exits with success text, missing tools, timeouts,
  4 KiB/non-power-of-two ELF alignment, malformed headers, virtual-address
  overflow, truncated segments, duplicate ZIP names, CRC corruption, missing
  assemblies, mutation during verification and Python `-O`.
- A later review reproduced another false positive: the initial runtime-name
  substring check accepted a Mono component without the main Mono VM, or a
  Godot-named lookalike without the engine library. Required library paths now
  match the pinned export exactly, including case. Three added regressions
  reject a component-only Mono payload, a Godot lookalike and a wrong-case
  runtime name. Optional Mono components are not required.
- Architecture boundary and all nine notice/inclusion checks pass.
- Independent code review found missing ELF header-version/type/size validation
  and virtual-memory overflow checks in the first implementation. Both were
  fixed and covered before the final verification.
- Actual official Android Build-Tools 36.1.0 verified the preserved historical
  UX0004 APK again through this new command, on the cloud Linux environment.
  Size: 119,290,693 bytes. SHA256:
  `a3920a509088de1bfa3c668b11855a884f26547e2a03cdf28c4b1afd0124bc60`.
  Public certificate SHA256:
  `5159469c18953c52991d0deeb362f3c6416960acd5ad3fa44f2798b843738386`.
  Actual v2 signature, ZIP alignment, CRC, eleven ARM64 ELF libraries with
  16,384-byte PT_LOAD alignment and three required assembly entries pass.
- Two real-tool negative checks also pass: a wrong expected signer is rejected;
  a separate modified copy of the APK is rejected by actual `apksigner`.
  Both exit 1 and emit no success JSON. The original APK remains unchanged.

These historical bytes are **not** the later `08d4510` APK and are not a newly
built game. Synthetic positive fixtures use stubbed tool responses and are not
cryptographic validation; the real-artifact check is separate evidence.

## Remaining limits and compatibility

This is an ARM64 Godot/Mono test-package contract, not a generic APK validator.
A future ABI, runtime, signing scheme or official output-format change requires
an explicit contract/test update. Unknown tool output is rejected, not guessed.
The executables supplied by the caller must be trusted official tools. The hash
check detects persistent changes, not a hostile concurrent swap-and-restore
attack; validate a quiescent artifact in an isolated build workspace.

Assembly presence is not managed-code execution or source/publish-byte identity.
The checker does not parse package/version/permission metadata, validate every
runtime resource, prove full ELF loader behavior, or certify installation,
upgrade/data migration, touch, GPU, animation, performance or gameplay. It cannot
establish compatibility with an installed package signed by a different key.
No device or emulator was used. Core/adapter game tests were not rerun for this
Python-only change; no full-suite pass or merge/release approval is claimed.

## Official references

- [Android apksigner](https://developer.android.com/tools/apksigner)
- [Android zipalign](https://developer.android.com/tools/zipalign)
- [Android 16 KiB page-size checks](https://developer.android.com/guide/practices/page-sizes)
- [Godot 4.6 Android export requirements](https://docs.godotengine.org/en/4.6/tutorials/export/exporting_for_android.html)
