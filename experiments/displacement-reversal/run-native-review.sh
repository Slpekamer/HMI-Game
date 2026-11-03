#!/usr/bin/env bash
# Launch through an existing observed native terminal. Never creates a display.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
BASE="${DISPLACEMENT_OUTPUT:?Set fresh output directory}";GODOT_BIN="${GODOT_BIN:?Set Godot Mono executable}"
FONT="${DISPLACEMENT_CJK_FONT:-/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc}"
test -f "$FONT";test ! -e "$BASE";mkdir -p "$BASE";cd "$ROOT"
dotnet build -m:1 > "$BASE/build.log" 2>&1
"$GODOT_BIN" --headless --editor --path "$ROOT" --import > "$BASE/import.log" 2>&1
unset ROUTE_HEADLESS PROJECTILE_FRAMECHECK DISPLACEMENT_TAP_PATTERN
export DISPLACEMENT_GAIT=StrafeR DISPLACEMENT_REVERSALS=1 DISPLACEMENT_LIFECYCLE_PROBE=0 DISPLACEMENT_RESTART_MATRIX=0 STOP_MODE=flight
for CASE in reversal60 repeated65;do
 if [[ "$CASE" == reversal60 ]];then
  export DISPLACEMENT_REVERSE_FRAME=60 DISPLACEMENT_DIRECTION_PATTERN='';TITLE='左右反向与收步'
 else
  export DISPLACEMENT_REVERSE_FRAME=65 DISPLACEMENT_DIRECTION_PATTERN=LLLLLLLLLLLLLLLRRRRRRRRRRRRRRRLLLLLLLLLLLLLLL;TITLE='连续左右切换与收步'
 fi
 for VARIANT in A B;do
  OUT="$BASE/native-$CASE-$VARIANT";mkdir -p "$OUT/xdg/data" "$OUT/xdg/config" "$OUT/xdg/cache"
  export XDG_DATA_HOME="$OUT/xdg/data" XDG_CONFIG_HOME="$OUT/xdg/config" XDG_CACHE_HOME="$OUT/xdg/cache" PROJECTILE_MOVIE_OUTPUT="$OUT" DISPLACEMENT_CANDIDATE=0
  [[ "$VARIANT" == B ]] && export DISPLACEMENT_CANDIDATE=1
  "$GODOT_BIN" --audio-driver Dummy --fixed-fps 60 --path "$ROOT" --write-movie "$OUT/capture.avi" scenes/controlled_gait_movie.tscn > "$OUT/runtime.log" 2>&1
  if grep -E 'ERROR:|FATAL|Unhandled|ObjectDB.*leak|resources still in use' "$OUT/runtime.log";then exit 1;fi
 done
 python3 experiments/displacement-run/sequence_movie.py "$BASE" "$CASE" "$BASE/comparison-$CASE" --frames 180 --title "$TITLE" --font "$FONT"
done
printf "file '%s'\n" "$BASE/comparison-reversal60/comparison-sequential.mp4" "$BASE/comparison-repeated65/comparison-sequential.mp4" > "$BASE/concat.txt"
ffmpeg -v error -f concat -safe 0 -i "$BASE/concat.txt" -c copy -movflags +faststart "$BASE/卷卷-左右反向与连续切换-实验待审核.mp4"
ffmpeg -v error -i "$BASE/卷卷-左右反向与连续切换-实验待审核.mp4" -f null -
echo 'LATERAL_REVERSAL_NATIVE_CAPTURE_COMPLETE: stamp checked; numeric and visual review still required'
