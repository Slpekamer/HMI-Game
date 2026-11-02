#!/usr/bin/env bash
set -euo pipefail
: "${GODOT_NET:?source toolchain env before launch}"
root=$(cd "$(dirname "$0")/.." && pwd)
out=${1:?Pass an output directory outside the source tree};mkdir -p "$out"
iso=$(mktemp -d)
unset ENEMY_DAMAGE_C READY_STRAFE_PILOT READY_MELEE_PRIORITY DAMAGE_KEYFRAMES DAMAGE_SHORT360 DAMAGE_FIRST_HEADLESS READY_OUTPUT
sha256sum "$root/.godot/mono/temp/bin/Debug/HmiGame.dll"
XDG_DATA_HOME="$iso/data" XDG_CONFIG_HOME="$iso/config" "$GODOT_NET" --audio-driver Dummy --path "$root" >"$out/runtime.log" 2>&1
printf 'INTEGRATED_DAMAGE_MAIN_EXIT=0\n'
if grep -E 'ERROR:|FATAL|ObjectDB instances leaked|resources still in use' "$out/runtime.log"; then exit 1; fi
