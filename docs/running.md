# Running and verification

Use Godot 4.6.3 .NET, not the standard editor. Install SDK 10.0.403 (global.json). The portable core targets netstandard2.1/C#8 while the host and console tests target net10.0. Unity/团结 and IL2CPP have not been tested.

From repository root:

```
dotnet build HmiGame.csproj
/path/to/Godot_v4.6.3-stable_mono_linux.x86_64 --editor --path .
```

After editor import/build, run with F5 or the same executable `--path .`. On cloud use the provided isolated toolchain's env.sh and bin/godot-net; those external tools are not repository dependencies or bundled downloads.

Controls: WASD/arrows/mouse or touchscreen joystick; Space/dodge button accelerates current movement briefly (stationary activation is a no-op), no invulnerability; Esc pauses, or closes the codex without toggling underlying combat. Focus loss pauses. Close codex after focus loss leaves pause visible. Companions independently roam or station; recall redeploys once, not continuous following. Coins only come from kills and reset with a new run.

```
GODOT_NET=/path/to/mono-editor scripts/verify.sh
```

The aggregate script checks dependency boundaries, fresh core tests, compilation, import and headless managed execution. It is not a native graphical/device/Unity certificate. Native selection, movement, pause, codex, upgrades, shop and next-wave paths must be verified separately. Actual evidence lives under docs/evidence/FEAT-0001.

A diagnostic `-- --smoke` starts summon/seed1030 and exits after120 presentation frames; `-- --smoke --capture=/absolute/path.png` saves an actual viewport image. Neither grants free coins, modifies combat rules or asserts a full run passed.

Exports require matching Godot.NET templates and the included solution. Linux and Android may need additional platform SDKs. No secrets/signing material/cache/build outputs belong in Git. Current export outcomes are only those explicitly recorded in fresh evidence; historical lost APKs cannot be delivered or claimed newly tested.

## Linux debug export

Install the matching **Godot 4.6.3 .NET** export templates, then:

```sh
mkdir -p builds
dotnet build HmiGame.csproj
/path/to/mono-editor --headless --editor --path . --import
/path/to/mono-editor --headless --path . --export-debug Linux builds/HmiGame.x86_64
builds/HmiGame.x86_64 --headless -- --smoke
```

Distribute the entire export directory, including the .NET dependency folder, not just the executable and PCK. The preset explicitly includes neutral gameplay JSON and excludes prior build/export output, documentation and tool sources. Linux export success does not imply Android, macOS or Windows export certification.

Balance note: card descriptions retain the archived demonstration-distance numbers; the simulation scales those distances by0.04 to meters. These are not real-world meter labels or final tuned values. Prices, percentages and durations are not scaled. Future content/UI refinement should present consistent explicit units.

## Verification tooling and notices

The aggregate `scripts/verify.sh` is a Linux/Bash check using Python3 and GNU `timeout`; game execution itself does not need Python. On other systems, run the listed .NET/Godot commands with the platform's own timeout/process controls; those platforms have not been certified by this cloud run.

Export presets must include `content/fonts/LICENSE-Noto.txt` and `content/licenses/*`. `scripts/check_notices.py` checks hashes, completeness, filters and the absence of signing fields. Optional Android reproduction uses `docs/android-debug-export-presets.cfg` in an isolated working copy. The notice-corrected APK r2 is the current package; earlier APKs are superseded. Android package verification is not a device playtest.
