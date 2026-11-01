#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
: "${GODOT_NET:?Set GODOT_NET to the Godot 4.6.3 .NET executable}"
python3 scripts/check_architecture.py
python3 tests/Presentation/test_food_contract.py
python3 scripts/check_notices.py
python3 tools/ui/validate_runtime_glyphs.py
dotnet build -m:1 src/Core/Hmi.Core.csproj -c Release -v minimal
dotnet build -m:1 tests/CoreTests/Hmi.CoreTests.csproj -c Release -v minimal
dotnet run --no-build --project tests/CoreTests/Hmi.CoreTests.csproj -c Release
dotnet build -m:1 tests/CodexTests/Hmi.CodexTests.csproj -v minimal
dotnet run --no-build --project tests/CodexTests/Hmi.CodexTests.csproj
dotnet build -m:1 HmiGame.csproj -v minimal
logs=$(mktemp -d /tmp/hmi-verify.XXXXXX)
# Isolate editor configuration from unrelated Android export/editor sessions.
export XDG_CONFIG_HOME="$logs/editor-config"
export XDG_DATA_HOME="$logs/user-data"
mkdir -p "$XDG_DATA_HOME"
mkdir -p "$XDG_CONFIG_HOME"
python3 tools/art/validate_assets.py content "$logs/assets.json"
python3 tools/art/validate_animated_assets.py content "$logs/animated-assets.json"
if [ -f content/food-heroes-manifest.json ]; then
  python3 tools/art/validate_food_heroes.py content/models --output "$logs/food-assets.json"
fi
python3 tools/ui/validate_revision_ui.py
python3 tools/ui/validate_portraits.py "$logs/portraits.json"
timeout 120 "$GODOT_NET" --headless --editor --path . --import >"$logs/import.log" 2>&1
timeout 60 "$GODOT_NET" --headless --path . --script tests/Presentation/animation_probe.gd >"$logs/animation.log" 2>&1
# Every staged food asset gets the actual imported-instance/root/morph gate,
# even before the optional all-five completion manifest exists.
food_ids=$(python3 -c 'import json; print(" ".join(a["id"].removeprefix("hero_") for a in json.load(open("content/animated-manifest.json"))["assets"] if a.get("runtime_contract")=="food-hero-v1"))')
for hero in $food_ids; do
  timeout 60 "$GODOT_NET" --headless --path . --script tests/Presentation/food_hero_probe.gd -- "--hero=$hero" >"$logs/food-$hero.log" 2>&1
done
if [ -f content/food-heroes-manifest.json ]; then
  timeout 60 "$GODOT_NET" --headless --path . --script tests/Presentation/food_hero_probe.gd >"$logs/food-import.log" 2>&1
fi
timeout 60 "$GODOT_NET" --headless --path . --script tests/Presentation/camera_mapping_probe.gd >"$logs/camera.log" 2>&1
timeout 60 "$GODOT_NET" --headless --path . --script tools/ui/check_catalog_text.gd >"$logs/text-layout.log" 2>&1
timeout 60 "$GODOT_NET" --headless --path . --script tools/ui/check_layout.gd >"$logs/selection-layout.log" 2>&1
timeout 60 "$GODOT_NET" --headless --path . --script tools/ui/check_layout.gd -- --smoke >"$logs/combat-layout.log" 2>&1
timeout 45 "$GODOT_NET" --headless --path . -- --smoke >"$logs/smoke.log" 2>&1
cat "$logs/smoke.log"
grep -q HMI_SMOKE "$logs/smoke.log"
if grep -E 'ERROR:|Exception|SCRIPT ERROR' "$logs"/*.log; then echo 'Engine diagnostics require review'; exit 1; fi
python3 scripts/test_host_lifecycle.py
python3 scripts/test_character_presentation.py
python3 scripts/test_attack_cadence.py
python3 scripts/test_asset_cache_lifecycle.py
python3 scripts/test_selection_portraits.py
python3 scripts/test_codex_presentation.py
python3 scripts/test_endless_presentation.py
python3 scripts/test_persistence_ui.py
python3 scripts/test_checkpoint_host.py
dotnet build -m:1 tests/CheckpointStore/CheckpointStore.csproj -v minimal
dotnet run --no-build --project tests/CheckpointStore/CheckpointStore.csproj
dotnet build -m:1 tests/CheckpointSessionTests/CheckpointSessionTests.csproj -v minimal
dotnet run --no-build --project tests/CheckpointSessionTests/CheckpointSessionTests.csproj
python3 scripts/test_opaque_pages.py
python3 scripts/test_save_notice.py
echo "PASS build / core / import / smoke / host lifecycle; logs $logs"
echo 'Native visual/input, exports, real devices and Unity are separate gates.'
