#!/usr/bin/env bash
# Execute from an observed native terminal. Does not create a display or server.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
BASE="${TURNING_OUTPUT:?Set a fresh output directory}";GODOT_BIN="${GODOT_BIN:?Set Godot Mono executable}"
FONT="${DISPLACEMENT_CJK_FONT:-/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc}"
test -f "$FONT";test ! -e "$BASE";mkdir -p "$BASE";cd "$ROOT"
dotnet build -m:1 > "$BASE/build.log" 2>&1
"$GODOT_BIN" --headless --editor --path "$ROOT" --import > "$BASE/import.log" 2>&1
unset ROUTE_HEADLESS PROJECTILE_FRAMECHECK DISPLACEMENT_TAP_PATTERN
export DISPLACEMENT_GAIT=Run DISPLACEMENT_TURNING_RUN=1 DISPLACEMENT_TURN_FRAME=60 DISPLACEMENT_LIFECYCLE_PROBE=0 DISPLACEMENT_RESTART_MATRIX=0 STOP_MODE=flight
for ANGLE in 90 -90 180;do
 export DISPLACEMENT_TURN_DEGREES="$ANGLE"
 for VARIANT in A B;do
  OUT="$BASE/native-turn-$ANGLE-$VARIANT";mkdir -p "$OUT/xdg/data" "$OUT/xdg/config" "$OUT/xdg/cache"
  export XDG_DATA_HOME="$OUT/xdg/data" XDG_CONFIG_HOME="$OUT/xdg/config" XDG_CACHE_HOME="$OUT/xdg/cache" PROJECTILE_MOVIE_OUTPUT="$OUT" DISPLACEMENT_CANDIDATE=0
  [[ "$VARIANT" == B ]] && export DISPLACEMENT_CANDIDATE=1
  "$GODOT_BIN" --audio-driver Dummy --fixed-fps 60 --path "$ROOT" --write-movie "$OUT/capture.avi" scenes/controlled_gait_movie.tscn > "$OUT/runtime.log" 2>&1
  if grep -E 'ERROR:|FATAL|Unhandled|ObjectDB.*leak|resources still in use' "$OUT/runtime.log";then exit 1;fi
 done
done
python3 experiments/displacement-turning-run/verify.py "$BASE" > "$BASE/verification.json"
python3 experiments/displacement-turning-run/sequence_movie.py "$BASE" "$BASE/卷卷-随移动转身与前进跑步-实验待审核.mp4" --font "$FONT"
ffmpeg -v error -i "$BASE/卷卷-随移动转身与前进跑步-实验待审核.mp4" -f null -
echo 'TURNING_RUN_NATIVE_CAPTURE_COMPLETE: source stamps checked; mesh/visual review remains separate'
