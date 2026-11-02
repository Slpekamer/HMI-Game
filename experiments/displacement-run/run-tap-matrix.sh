#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
BASE="${DISPLACEMENT_OUTPUT:?Set a fresh output directory}";GODOT_BIN="${GODOT_BIN:?Set Godot Mono executable}"
test ! -e "$BASE" || { echo 'Output directory must be fresh';exit 2; };mkdir -p "$BASE";cd "$ROOT"
dotnet build -m:1 > "$BASE/build.log" 2>&1
"$GODOT_BIN" --headless --editor --path "$ROOT" --import > "$BASE/import.log" 2>&1
export ROUTE_HEADLESS=1 STOP_MODE=flight DISPLACEMENT_RESTART_MATRIX=0 DISPLACEMENT_LIFECYCLE_PROBE=0
unset PROJECTILE_FRAMECHECK
failed=0
for START in 30 150;do for PATTERN in 010 0110 0101010101010101 0011001100110011;do for END in 0 1;do
 export DISPLACEMENT_TAP_START="$START" DISPLACEMENT_TAP_PATTERN="$PATTERN" DISPLACEMENT_TAP_END_MOVING="$END"
 for VARIANT in A B;do
  OUT="$BASE/start-$START-pattern-$PATTERN-end-$END-$VARIANT";mkdir -p "$OUT/xdg/data" "$OUT/xdg/config" "$OUT/xdg/cache"
  export XDG_DATA_HOME="$OUT/xdg/data" XDG_CONFIG_HOME="$OUT/xdg/config" XDG_CACHE_HOME="$OUT/xdg/cache" PROJECTILE_MOVIE_OUTPUT="$OUT" DISPLACEMENT_CANDIDATE=0
  [[ "$VARIANT" == B ]] && export DISPLACEMENT_CANDIDATE=1
  if ! "$GODOT_BIN" --headless --audio-driver Dummy --fixed-fps 60 --path "$ROOT" scenes/controlled_gait_movie.tscn > "$OUT/runtime.log" 2>&1;then failed=1;fi
  if grep -E 'ERROR:|FATAL|Unhandled|ObjectDB.*leak|resources still in use' "$OUT/runtime.log";then failed=1;fi
 done
 echo "RECORDED start=$START pattern=$PATTERN end=$END"
done;done;done
exit "$failed"
