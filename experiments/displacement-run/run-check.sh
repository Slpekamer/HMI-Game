#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
MODE="${1:-headless}";case "$MODE" in headless|native|lifecycle|restart);;*) echo 'Use headless, native, lifecycle or restart';exit 2;;esac
GODOT_BIN="${GODOT_BIN:-godot}";BASE="${DISPLACEMENT_OUTPUT:-$ROOT/artifacts/displacement-run-$MODE}"
test ! -e "$BASE" || { echo 'Output directory must be fresh';exit 2; };mkdir -p "$BASE";cd "$ROOT"
dotnet build -m:1 > "$BASE/build.log" 2>&1
"$GODOT_BIN" --headless --editor --path "$ROOT" --import > "$BASE/import.log" 2>&1
STOPS=(flight stance);VARIANTS=(A B)
if [[ "$MODE" == lifecycle || "$MODE" == restart ]];then STOPS=("$MODE");VARIANTS=(B);fi
for STOP in "${STOPS[@]}";do
 for VARIANT in "${VARIANTS[@]}";do
  OUT="$BASE/$MODE-$STOP-$VARIANT";mkdir -p "$OUT/xdg/data" "$OUT/xdg/config" "$OUT/xdg/cache"
  export XDG_DATA_HOME="$OUT/xdg/data" XDG_CONFIG_HOME="$OUT/xdg/config" XDG_CACHE_HOME="$OUT/xdg/cache"
  export PROJECTILE_MOVIE_OUTPUT="$OUT" STOP_MODE="$STOP" DISPLACEMENT_CANDIDATE=0 DISPLACEMENT_LIFECYCLE_PROBE=0
  [[ "$VARIANT" == B ]] && export DISPLACEMENT_CANDIDATE=1
  [[ "$MODE" == lifecycle ]] && export DISPLACEMENT_LIFECYCLE_PROBE=1
  unset ROUTE_HEADLESS PROJECTILE_FRAMECHECK
  ARGS=(--audio-driver Dummy --fixed-fps 60 --path "$ROOT")
  if [[ "$MODE" == native ]];then ARGS+=(--write-movie "$OUT/capture.avi");else export ROUTE_HEADLESS=1;ARGS+=(--headless);fi
  "$GODOT_BIN" "${ARGS[@]}" scenes/controlled_gait_movie.tscn > "$OUT/runtime.log" 2>&1
  if grep -E 'ERROR:|FATAL|Unhandled|ObjectDB.*leak|resources still in use' "$OUT/runtime.log";then exit 1;fi
 done
 if [[ "$MODE" == headless || "$MODE" == native ]];then python3 "$ROOT/experiments/displacement-run/verify.py" "$BASE" "$MODE" "$STOP" > "$BASE/$STOP-verification.json";fi
done
echo "DISPLACEMENT_${MODE}_CHECK_COMPLETE"
