# Final candidate r2 Linux export verification — 2026-10-31

Fresh current source snapshot includes preview yaw152 and corrected documentation image names/.gdignore. Original live root was not edited. Build, import, Linux debug export, actual standalone smoke, then repeat-import/export/smoke all exited0. Build0 warnings/errors and no engine ERROR:/WARNING: lines across logs.

Standalone repeat result: `HMI_SMOKE phase=Combat wave=1 entities=16 tick=57`.

## Repeat-export exclusion regression
Preset now uses `builds/HmiGame.x86_64`; exclusions: docs/*,tests/*,tools/*,scripts/*,builds/*,exports/*,android/*,global.json,source-snapshot-hashes.json. Required include_filter remains content/*.json.
Deliberate JSON and imported .tscn sentinel files were created only in sandbox builds/. Packed-resource view checked by actual Godot --main-pack: both sentinels, global.json and source manifest absent; required content/gameplay.json present. This checks the resource pack, not merely export logs. Repeat export and standalone execution passed.

## Traceability
- Source snapshot manifest SHA256: `32350751210027f0e9ca6af01f864fd5c77de3b6d3f6a8e453b69d522d631cd3` (captured before isolated preset override/sentinels).
- Runtime-only source manifest SHA256: `9a2ff1638536da08aa04432b005f4f9d4ac49614d722e9308d79baceb9b74f87`.
- Revised export preset SHA256: `83b3ac2c0f87fee5a7b13f667569ca423da568ed1ffabf757d344fecc48c4794`.
- Final binary SHA256: `799724fa12794d5ef47b1a82ff1a29765bdcb6e12d462cda27ed892358c8542b`.
- Final PCK SHA256: `3cf081d4d29e8139a3a1646dd917ce2e5ef77065be7abe8ab2d6bcd7e3bf123c`.
- Full output manifest output-hashes.json: 197 files / 162440716 bytes. Keep entire out/ including .NET data files, not just executable/PCK.

## Commands
After sourcing toolchain env.sh, cwd isolated project/:
```sh
dotnet build HmiGame.csproj -v minimal
"$GODOT_NET" --headless --editor --path . --import
"$GODOT_NET" --headless --path . --export-debug Linux "$TOOLCHAIN_ROOT/game-export-final-r2/out/HmiGame.x86_64"
timeout 90 ../out/HmiGame.x86_64 --headless -- --smoke
cd ../out
"$GODOT_NET" --headless --main-pack HmiGame.pck --script "$TOOLCHAIN_ROOT/game-export-final-r2/pack-probe.gd"
```
No Android, native GUI review, device test, Unity compatibility or release approval is implied. Subsequent runtime-source changes require fresh verification.
