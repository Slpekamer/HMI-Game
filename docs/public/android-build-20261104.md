# Android debug APK export

Android debug build — 2026-11-04

Source: Slpekamer/HMI-Game 08d4510e62c1dac602b99adef37924bd51bbe6de.
The source contains the approved rule3 integration branch. No gameplay changes.
Godot 4.6.3 stable .NET; .NET SDK 10.0.403; Android API 36 revision 2;
build-tools 36.1.0; Gradle 8.11.1; Android Gradle Plugin 8.6.1; JDK 21.
Official Godot, Microsoft, Google and Maven dependencies were used.
No proxy or network security settings were changed.

Result: HmiFoodPreview-08d4510-arm64-debug-complete.apk (128834886 bytes).
SHA256: 47941f296bd1012ccf727b0d32f2163781327ed6e746e7cb342b5ebff1b7340d
Package: org.example.hmim2probe.foodpreview
Label: HMI Food Preview
Version code: 6; version name: UX0004-foodpreview-debug
ABI: arm64-v8a only. Minimum Android API 24; target/compile API 36.
Signing: local Android debug RSA-2048, APK v2 verified.
Certificate SHA256: aac839a08cf53015b3037bbbad1cf2ac0fbfac52ba0a63f354b83de6266b5a94
This test signature cannot replace an existing installation signed by another key.
No private signing material is included or uploaded.

Validation passed: actual Godot-driven .NET publish and Gradle APK export;
APK ZIP CRC, 16 KiB native-library alignment, package metadata, ARM64-only native
libraries, HmiGame/Hmi.Core/Hmi.Persistence assemblies, runtime library and signing.
Core regression: 646403 assertions. Save upgrade: 579 checks with a real temporary
directory; historical old-reader check not run because baseline reader was absent.

Device validation: NOT RUN. adb reported no attached devices; no phone installation,
launch, touch-input, GPU or performance success is claimed.
One-click editor deployment remains disabled; command-line APK export succeeds.

Recovery details: Godot's macOS .NET finder prefers the system installation even
when PATH points at the isolated SDK. A temporary sdk.paths entry in global.json
selected isolated SDK 10.0.403 without changing the pinned version. The machine-only
entry was removed after export. The first export's managed publish failed and its
incomplete APK was discarded; only the complete validated APK is delivered.
The default macOS temporary path contains a symbolic link rejected by storage
checks; tests used a real workspace temporary directory. No storage checks changed.
Gradle wrapper reused the Mac's existing 8.11.1 distribution, while an isolated
Gradle cache resolved official dependencies. Build artifacts and keys stay out of Git.
