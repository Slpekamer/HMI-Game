#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
BASE="${SHADOW_OUTPUT:?Set fresh output directory}";GODOT_BIN="${GODOT_BIN:?Set Godot Mono executable}"
test ! -e "$BASE";mkdir -p "$BASE";cd "$ROOT"
dotnet build -m:1 > "$BASE/build.log" 2>&1
"$GODOT_BIN" --headless --editor --path "$ROOT" --import > "$BASE/import.log" 2>&1
for ASPECT in landscape portrait;do
 for VARIANT in A B;do
  OUT="$BASE/$ASPECT-$VARIANT";mkdir -p "$OUT/xdg/data" "$OUT/xdg/config" "$OUT/xdg/cache"
  export XDG_DATA_HOME="$OUT/xdg/data" XDG_CONFIG_HOME="$OUT/xdg/config" XDG_CACHE_HOME="$OUT/xdg/cache" SHADOW_COVERAGE_OUTPUT="$OUT" SHADOW_COVERAGE_ASPECT="$ASPECT" SHADOW_SPLIT_CANDIDATE=0
  [[ "$VARIANT" == B ]] && export SHADOW_SPLIT_CANDIDATE=1
  "$GODOT_BIN" --audio-driver Dummy --fixed-fps 60 --path "$ROOT" scenes/shadow_coverage_movie.tscn > "$OUT/runtime.log" 2>&1
  if grep -E 'ERROR:|FATAL|Unhandled|ObjectDB.*leak|resources still in use' "$OUT/runtime.log";then exit 1;fi
 done
done
python3 experiments/shadow-split/verify-coverage.py "$BASE"
echo 'SHADOW_COVERAGE_CAPTURE_COMPLETE: paused still diagnostics; visual inspection still required'
