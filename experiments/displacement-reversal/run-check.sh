#!/usr/bin/env bash
# Existing headless renderer; no display/server creation.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
BASE="${DISPLACEMENT_OUTPUT:?Set a fresh output directory}";GODOT_BIN="${GODOT_BIN:?Set Godot Mono executable}"
test ! -e "$BASE" || { echo 'Output directory must be fresh';exit 2; };mkdir -p "$BASE";cd "$ROOT"
dotnet build -m:1 > "$BASE/build.log" 2>&1
"$GODOT_BIN" --headless --editor --path "$ROOT" --import > "$BASE/import.log" 2>&1
export ROUTE_HEADLESS=1 DISPLACEMENT_GAIT=StrafeR DISPLACEMENT_REVERSALS=1 DISPLACEMENT_LIFECYCLE_PROBE=0 DISPLACEMENT_RESTART_MATRIX=0 STOP_MODE=flight
unset PROJECTILE_FRAMECHECK DISPLACEMENT_TAP_PATTERN
failed=0
for FRAME in {60..74};do for VARIANT in A B;do
 OUT="$BASE/reverse-$FRAME-$VARIANT";mkdir -p "$OUT/xdg/data" "$OUT/xdg/config" "$OUT/xdg/cache"
 export XDG_DATA_HOME="$OUT/xdg/data" XDG_CONFIG_HOME="$OUT/xdg/config" XDG_CACHE_HOME="$OUT/xdg/cache" PROJECTILE_MOVIE_OUTPUT="$OUT" DISPLACEMENT_CANDIDATE=0 DISPLACEMENT_REVERSE_FRAME="$FRAME"
 [[ "$VARIANT" == B ]] && export DISPLACEMENT_CANDIDATE=1
 if ! "$GODOT_BIN" --headless --audio-driver Dummy --fixed-fps 60 --path "$ROOT" scenes/controlled_gait_movie.tscn > "$OUT/runtime.log" 2>&1;then failed=1;fi
 if grep -E 'ERROR:|FATAL|Unhandled|ObjectDB.*leak|resources still in use' "$OUT/runtime.log";then failed=1;fi
 echo "RECORDED reversal-frame=$FRAME variant=$VARIANT"
done;done
[[ "$failed" == 0 ]] && python3 experiments/displacement-reversal/verify.py "$BASE" > "$BASE/verification.json"
exit "$failed"
