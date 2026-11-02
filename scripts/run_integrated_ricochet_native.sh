#!/usr/bin/env bash
set -euo pipefail
: "${GODOT_NET:?source toolchain env before launch}"
work=${1:?Pass the isolated test work root prepared by test_integrated_ricochet.py}
project="$work/project"; out="$work/evidence/native-firstdraw"; mkdir -p "$out"
iso=$(mktemp -d)
unset RICOCHET_BEAM RICOCHET_RIBBON RICOCHET_FACET_SHADING RICOCHET_CRYSTALS RICOCHET_NO_BACKPLATE RICOCHET_FIRST_HEADLESS ENEMY_DAMAGE_C READY_STRAFE_PILOT READY_MELEE_PRIORITY DAMAGE_KEYFRAMES DAMAGE_SHORT360 DAMAGE_FIRST_HEADLESS
sha256sum "$project/.godot/mono/temp/bin/Debug/HmiGame.dll"
XDG_DATA_HOME="$iso/data" XDG_CONFIG_HOME="$iso/config" READY_OUTPUT="$out" "$GODOT_NET" --audio-driver Dummy --path "$project" res://scenes/IntegratedRicochetFirstDrawProbe.tscn >"$out/runtime.log" 2>&1
printf 'INTEGRATED_RICOCHET_NATIVE_EXIT=0\n'
if grep -E 'ERROR:|FATAL|ObjectDB instances leaked|resources still in use' "$out/runtime.log"; then exit 1; fi
grep 'NATURAL_RICOCHET_' "$out/runtime.log"
