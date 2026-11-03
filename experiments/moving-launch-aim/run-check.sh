#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
MODE="${1:-smoke}"; [[ "$MODE" == smoke || "$MODE" == taps ]] || exit 2
BASE="${MOVING_AIM_OUTPUT:?Set a fresh output directory}"; GODOT_BIN="${GODOT_BIN:?Set Godot Mono executable}"
test ! -e "$BASE";mkdir -p "$BASE";cd "$ROOT"
dotnet build -m:1 > "$BASE/build.log" 2>&1
"$GODOT_BIN" --headless --editor --path "$ROOT" --import > "$BASE/import.log" 2>&1
export ROUTE_HEADLESS=1 DISPLACEMENT_CANDIDATE=1 DISPLACEMENT_GAIT=Run DISPLACEMENT_TURNING_RUN=1 DISPLACEMENT_TURN_FRAME=60 DISPLACEMENT_LIFECYCLE_PROBE=0 DISPLACEMENT_RESTART_MATRIX=0 DISPLACEMENT_TAP_START=63 DISPLACEMENT_TAP_END_MOVING=0 STOP_MODE=flight
unset PROJECTILE_FRAMECHECK
patterns=(''); [[ "$MODE" == taps ]] && patterns=(0 10101010)
for ANGLE in 90 -90 180;do
 export DISPLACEMENT_TURN_DEGREES="$ANGLE"
 for PATTERN in "${patterns[@]}";do
  export DISPLACEMENT_TAP_PATTERN="$PATTERN"
  for VARIANT in A B;do
   OUT="$BASE/turn-$ANGLE-pattern-${PATTERN:-steady}-$VARIANT";mkdir -p "$OUT/xdg/data" "$OUT/xdg/config" "$OUT/xdg/cache"
   export XDG_DATA_HOME="$OUT/xdg/data" XDG_CONFIG_HOME="$OUT/xdg/config" XDG_CACHE_HOME="$OUT/xdg/cache" PROJECTILE_MOVIE_OUTPUT="$OUT" MOVING_LAUNCH_AIM=0
   [[ "$VARIANT" == B ]] && export MOVING_LAUNCH_AIM=1
   "$GODOT_BIN" --headless --audio-driver Dummy --fixed-fps 60 --path "$ROOT" scenes/controlled_gait_movie.tscn > "$OUT/runtime.log" 2>&1
   if grep -E 'ERROR:|FATAL|Unhandled|ObjectDB.*leak|resources still in use' "$OUT/runtime.log";then exit 1;fi
  done
 done
done
python3 experiments/moving-launch-aim/verify-native.py "$BASE" > "$BASE/verification.json"
echo "MOVING_AIM_${MODE}_CHECK_COMPLETE"
