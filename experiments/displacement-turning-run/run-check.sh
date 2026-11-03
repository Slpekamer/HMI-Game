#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
MODE="${1:-smoke}";case "$MODE" in smoke|phases|taps|lifecycle);;*) echo 'Use smoke, phases, taps or lifecycle';exit 2;;esac
GODOT_BIN="${GODOT_BIN:-godot}";OUT="${TURNING_OUTPUT:-$ROOT/artifacts/turning-run-$MODE}"
test ! -e "$OUT" || { echo 'Output must be fresh';exit 2; };mkdir -p "$OUT";cd "$ROOT"
dotnet build -m:1 > "$OUT/build.log" 2>&1
"$GODOT_BIN" --headless --editor --path "$ROOT" --import > "$OUT/import.log" 2>&1
run() {
 local name="$1" angle="$2" turn="$3" pattern="$4" tapstart="$5" lifecycle="$6"
 for variant in A B;do
  [[ $lifecycle != 0 && $variant == A ]] && continue
  local folder="$OUT/$name-$variant";mkdir -p "$folder/xdg/data" "$folder/xdg/config" "$folder/xdg/cache"
  export XDG_DATA_HOME="$folder/xdg/data" XDG_CONFIG_HOME="$folder/xdg/config" XDG_CACHE_HOME="$folder/xdg/cache"
  export PROJECTILE_MOVIE_OUTPUT="$folder" ROUTE_HEADLESS=1 DISPLACEMENT_GAIT=Run DISPLACEMENT_TURNING_RUN=1
  export DISPLACEMENT_TURN_DEGREES="$angle" DISPLACEMENT_TURN_FRAME="$turn" DISPLACEMENT_CANDIDATE=0
  export DISPLACEMENT_TAP_PATTERN="$pattern" DISPLACEMENT_TAP_START="$tapstart" DISPLACEMENT_TAP_END_MOVING=0
  export DISPLACEMENT_LIFECYCLE_PROBE=0 DISPLACEMENT_LIFECYCLE_FRAME="$lifecycle"
  unset STOP_MODE DISPLACEMENT_RESTART_MATRIX DISPLACEMENT_RESTART_DELAY DISPLACEMENT_RESTART_STANCE PROJECTILE_FRAMECHECK
  [[ $variant == B ]] && export DISPLACEMENT_CANDIDATE=1
  [[ $lifecycle != 0 ]] && export DISPLACEMENT_LIFECYCLE_PROBE=1
  "$GODOT_BIN" --headless --audio-driver Dummy --fixed-fps 60 --path "$ROOT" scenes/controlled_gait_movie.tscn > "$folder/runtime.log" 2>&1
  if grep -E 'ERROR:|FATAL|Unhandled|ObjectDB.*leak|resources still in use' "$folder/runtime.log";then exit 1;fi
  test -f "$folder/summary.json"
 done
}
case "$MODE" in
 smoke) for angle in 0 90 -90 180 -180;do run "turn-$angle" "$angle" 60 '' 150 0;done;;
 phases) for angle in 90 -90 180;do for turn in $(seq 60 74);do run "turn-$angle-at-$turn" "$angle" "$turn" '' 150 0;done;done;;
 taps) for angle in 90 -90 180;do for pattern in 0 01 001 0001 10101010 11001100;do run "turn-$angle-tap-$pattern" "$angle" 60 "$pattern" 63 0;done;done;;
 lifecycle) for angle in 90 -90 180;do for frame in 61 64 70 153;do run "turn-$angle-pause-$frame" "$angle" 60 '' 150 "$frame";done;done;;
esac
if [[ "$MODE" != lifecycle ]];then python3 "$ROOT/experiments/displacement-turning-run/verify.py" "$OUT" > "$OUT/verification.json";fi
echo "TURNING_RUN_${MODE}_CHECK_COMPLETE"
