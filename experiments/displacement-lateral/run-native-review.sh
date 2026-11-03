#!/usr/bin/env bash
# Run through an existing observed native terminal; creates no display/server.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
BASE="${DISPLACEMENT_OUTPUT:?Set a fresh output directory}";GODOT_BIN="${GODOT_BIN:?Set Godot Mono executable}"
FONT="${DISPLACEMENT_CJK_FONT:-/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc}"
test -f "$FONT" || { echo 'Set DISPLACEMENT_CJK_FONT to an installed Chinese font';exit 2; }
test ! -e "$BASE" || { echo 'Output directory must be fresh';exit 2; };mkdir -p "$BASE";cd "$ROOT"
dotnet build -m:1 > "$BASE/build.log" 2>&1
"$GODOT_BIN" --headless --editor --path "$ROOT" --import > "$BASE/import.log" 2>&1
unset ROUTE_HEADLESS PROJECTILE_FRAMECHECK
export DISPLACEMENT_GAIT=StrafeL DISPLACEMENT_LIFECYCLE_PROBE=0 DISPLACEMENT_RESTART_MATRIX=0 STOP_MODE=flight DISPLACEMENT_TAP_END_MOVING=0
for CASE in sidehop-taps1 sidehop-shorttap2;do
 if [[ "$CASE" == sidehop-taps1 ]];then
  export DISPLACEMENT_TAP_START=150 DISPLACEMENT_TAP_PATTERN=0101010101010101;TITLE='左侧同步小跳与连续点按收步'
 else
  export DISPLACEMENT_TAP_START=30 DISPLACEMENT_TAP_PATTERN=0110;TITLE='左侧同步小跳短按后停止'
 fi
 for VARIANT in A B;do
  OUT="$BASE/native-$CASE-$VARIANT";mkdir -p "$OUT/xdg/data" "$OUT/xdg/config" "$OUT/xdg/cache"
  export XDG_DATA_HOME="$OUT/xdg/data" XDG_CONFIG_HOME="$OUT/xdg/config" XDG_CACHE_HOME="$OUT/xdg/cache" PROJECTILE_MOVIE_OUTPUT="$OUT" DISPLACEMENT_CANDIDATE=0
  [[ "$VARIANT" == B ]] && export DISPLACEMENT_CANDIDATE=1
  "$GODOT_BIN" --audio-driver Dummy --fixed-fps 60 --path "$ROOT" --write-movie "$OUT/capture.avi" scenes/controlled_gait_movie.tscn > "$OUT/runtime.log" 2>&1
  if grep -E 'ERROR:|FATAL|Unhandled|ObjectDB.*leak|resources still in use' "$OUT/runtime.log";then exit 1;fi
 done
 python3 experiments/displacement-run/sequence_movie.py "$BASE" "$CASE" "$BASE/comparison-$CASE" --frames 195 --title "$TITLE" --font "$FONT"
done
printf "file '%s'\n" "$BASE/comparison-sidehop-taps1/comparison-sequential.mp4" "$BASE/comparison-sidehop-shorttap2/comparison-sequential.mp4" > "$BASE/concat.txt"
ffmpeg -v error -f concat -safe 0 -i "$BASE/concat.txt" -c copy -movflags +faststart "$BASE/卷卷-左侧同步小跳与收步-实验待审核.mp4"
ffmpeg -v error -i "$BASE/卷卷-左侧同步小跳与收步-实验待审核.mp4" -f null -
echo 'LEFT_SIDEHOP_NATIVE_CAPTURE_COMPLETE: source stamps checked; numeric/visual review still required'
