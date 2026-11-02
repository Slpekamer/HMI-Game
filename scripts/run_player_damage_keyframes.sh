#!/usr/bin/env bash
set -euo pipefail
: "${GODOT_NET:?source toolchain env first}"
work=${1:?Pass exact prebuilt test work root}; root="$work/project"; out="$work/evidence/native-keyframes";mkdir -p "$out"
unset ENEMY_DAMAGE_C READY_STRAFE_PILOT READY_MELEE_PRIORITY PLAYER_FIRST_HEADLESS RICOCHET_BEAM RICOCHET_RIBBON RICOCHET_FACET_SHADING
for mode in A B; do
 iso=$(mktemp -d); mkdir -p "$out/$mode"; off=0; if [[ "$mode" == A ]]; then off=1; fi
 XDG_CONFIG_HOME="$iso/config" XDG_DATA_HOME="$iso/data" PLAYER_DAMAGE_BASELINE="$off" READY_OUTPUT="$out/$mode" "$GODOT_NET" --audio-driver Dummy --path "$root" res://scenes/PlayerDamageFirstDrawProbe.tscn > "$out/$mode/runtime.log" 2>&1
 if grep -E 'ERROR:|FATAL|ObjectDB instances leaked|resources still in use' "$out/$mode/runtime.log"; then exit 1; fi
 grep 'NATURAL_PLAYER_' "$out/$mode/runtime.log"
done
cmp "$out/A/authority.tsv" "$out/B/authority.tsv"
echo PLAYER_DAMAGE_KEYFRAME_AB_EXIT=0
