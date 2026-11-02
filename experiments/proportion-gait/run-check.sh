#!/usr/bin/env bash
# Explicit opt-in controlled asset drill, never normal game startup.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
MODE="${1:-headless}"; [[ "$MODE" == headless || "$MODE" == native ]] || { echo 'Use headless or native';exit 2; }
GODOT_BIN="${GODOT_BIN:-godot}"; BASE="${PROPORTION_OUTPUT:-$ROOT/artifacts/proportion-gait-$MODE}"
test ! -e "$BASE" || { echo 'Output directory must be fresh';exit 2; }
mkdir -p "$BASE";cd "$ROOT"
dotnet build -m:1
"$GODOT_BIN" --headless --editor --path "$ROOT" --import > "$BASE/import.log" 2>&1
for CLIP in Run StrafeR Backpedal;do
 for VARIANT in A B;do
  OUT="$BASE/$MODE-$CLIP-$VARIANT";mkdir -p "$OUT/xdg/data" "$OUT/xdg/config" "$OUT/xdg/cache"
  export XDG_DATA_HOME="$OUT/xdg/data" XDG_CONFIG_HOME="$OUT/xdg/config" XDG_CACHE_HOME="$OUT/xdg/cache"
  export PROJECTILE_MOVIE_OUTPUT="$OUT" GAIT_CLIP="$CLIP" PROPORTION_CANDIDATE=0
  [[ "$VARIANT" == B ]] && export PROPORTION_CANDIDATE=1
  unset ROUTE_HEADLESS PROJECTILE_FRAMECHECK WEIGHT_TRANSFER
  ARGS=(--audio-driver Dummy --fixed-fps 60 --path "$ROOT")
  if [[ "$MODE" == headless ]];then export ROUTE_HEADLESS=1;ARGS+=(--headless);else ARGS+=(--write-movie "$OUT/capture.avi");fi
  "$GODOT_BIN" "${ARGS[@]}" scenes/controlled_gait_movie.tscn > "$OUT/runtime.log" 2>&1
  if grep -E 'ERROR:|FATAL|Unhandled|ObjectDB.*leak|resources still in use' "$OUT/runtime.log";then exit 1;fi
 done
 python3 "$ROOT/experiments/proportion-gait/verify.py" "$BASE" "$MODE" "$CLIP" > "$BASE/$CLIP-verification.json"
done
echo "PROPORTION_${MODE}_CHECK_COMPLETE"
