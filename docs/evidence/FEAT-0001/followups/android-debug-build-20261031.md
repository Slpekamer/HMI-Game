# Android debug package followup — 2026-10-31

Status: actual frozen-M2-source ARM64 APK exported and statically verified after fresh explicit network-adapter approval. Android device/emulator execution is **not run**. Library storage and successful user attachment delivery are separate facts; the large original APK exceeded the attachment limit, so delivery packaging is handled separately.

## Source and build boundary

C# sources, gameplay JSON and project.godot are unchanged from frozen M2. Only an Android export preset was added in a separate build copy. The exact optional preset is preserved at `docs/android-debug-export-presets.cfg`; it is not automatically loaded by the default project, whose Linux checks remain unchanged.

Versions: Godot 4.6.3 .NET and matching official templates; .NET SDK 10.0.403 / Android workload 36.1.2; JDK 17.0.21+9; Android SDK 36 revision 2, Build Tools 36.1.0, NDK 29.0.14206865, CMake 3.10.2, platform-tools 37.0.1, CLI 23. Formal SDK licenses were explicitly authorized; preview licenses were not accepted.

The default Gradle download first failed with `java.net.SocketException: Network is unreachable`. The dependent retry was paused. After new explicit owner approval, only child-process configuration reused the existing unauthenticated proxy and read the existing Java truststore. No global certificate/configuration changes or TLS disabling occurred. The approved export exited0. Historical failed evidence was retained, not represented as a pass.

## Original verified artifact

- File: `HmiGame-M2-arm64-debug.apk`, 98,134,940 bytes.
- SHA256: `ae1e961a511c7e7c12afe18555da44e99d468f02a2d11695fb37d2e28c54c1ff`.
- Package: `org.example.hmim2probe`; label: `HMI M2 Debug`.
- Version name `m2-debug`, code1; ARM64-only; minSDK24 / targetSDK36.
- APK v2 signature verified; `zipalign -c -P 16 4` passed.
- Gameplay JSON, HmiGame.dll, Hmi.Core.dll and Godot/Mono native libraries verified in the actual package; no keystore files embedded.
- `aapt dump permissions` listed the package and no requested application permissions.

ZIP alignment does not prove device startup or complete16KiB native runtime compatibility. Debug signing is not production signing. Official AGP8.6.1/SDK36 compatibility warnings, fallback icon and the known Godot editor shutdown diagnostic remain disclosed. No store upload or production readiness claim.

## Reproduction outline

Use a fresh source working copy and install the matching official dependencies. Configure the Android SDK/JDK paths in the Godot .NET editor for your environment; do not copy cloud-specific network settings or signing material. Copy `docs/android-debug-export-presets.cfg` over that working copy's `export_presets.cfg`, then create `builds/` and run:

```sh
dotnet build HmiGame.csproj
/path/to/godot-mono --headless --editor --path . --import
/path/to/godot-mono --headless --path . --install-android-build-template --export-debug Android builds/HmiGame.apk
```

The local Android template, SDK, signing keys and exported APK stay outside version control. The optional preset intentionally uses a clearly temporary debug package identity; choose a reviewed production identity/signing plan separately. Any repackaged or differently compressed APK requires its own manifest/signature/alignment checks; it must not borrow the original artifact's verification result.
