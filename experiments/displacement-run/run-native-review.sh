#!/usr/bin/env bash
# Parent launches through an existing observed native terminal. Never creates a display.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
BASE="${DISPLACEMENT_OUTPUT:?Set a fresh output directory}";GODOT_BIN="${GODOT_BIN:?Set Godot Mono executable}"
test ! -e "$BASE" || { echo 'Output directory must be fresh';exit 2; };mkdir -p "$BASE";cd "$ROOT"
dotnet build -m:1 > "$BASE/build.log" 2>&1
"$GODOT_BIN" --headless --editor --path "$ROOT" --import > "$BASE/import.log" 2>&1
unset ROUTE_HEADLESS PROJECTILE_FRAMECHECK
printf 'chapters=flight-stop,restart-delay-4,restart-delay-1-worst-boundary\n' > "$BASE/capture-case.txt"
export DISPLACEMENT_LIFECYCLE_PROBE=0 DISPLACEMENT_RESTART_STANCE=0 DISPLACEMENT_RESTART_DELAY=4
for STOP in flight restart4 restart1;do
 export STOP_MODE=flight DISPLACEMENT_RESTART_MATRIX=0
 if [[ "$STOP" != flight ]];then export STOP_MODE=restart DISPLACEMENT_RESTART_MATRIX=1 DISPLACEMENT_RESTART_DELAY="${STOP#restart}";fi
 for VARIANT in A B;do
  OUT="$BASE/native-$STOP-$VARIANT";mkdir -p "$OUT/xdg/data" "$OUT/xdg/config" "$OUT/xdg/cache"
  export XDG_DATA_HOME="$OUT/xdg/data" XDG_CONFIG_HOME="$OUT/xdg/config" XDG_CACHE_HOME="$OUT/xdg/cache" PROJECTILE_MOVIE_OUTPUT="$OUT" DISPLACEMENT_CANDIDATE=0
  [[ "$VARIANT" == B ]] && export DISPLACEMENT_CANDIDATE=1
  "$GODOT_BIN" --audio-driver Dummy --fixed-fps 60 --path "$ROOT" --write-movie "$OUT/capture.avi" scenes/controlled_gait_movie.tscn > "$OUT/runtime.log" 2>&1
  if grep -E 'ERROR:|FATAL|Unhandled|ObjectDB.*leak|resources still in use' "$OUT/runtime.log";then exit 1;fi
 done
 if [[ "$STOP" == flight ]];then python3 experiments/displacement-run/verify.py "$BASE" native flight > "$BASE/flight-verification.json";fi
 FRAMES=180;[[ "$STOP" != flight ]] && FRAMES=195
 python3 experiments/displacement-run/align_movie.py "$BASE" "$STOP" "$BASE/comparison-$STOP" --frames "$FRAMES"
done
echo 'DISPLACEMENT_NATIVE_REVIEW_CAPTURE_COMPLETE: appearance and restart continuity still require review'
