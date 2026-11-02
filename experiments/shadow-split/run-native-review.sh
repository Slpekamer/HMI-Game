#!/usr/bin/env bash
# Launch only through an observed existing native desktop; no virtual display.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
BASE="${SHADOW_OUTPUT:?Set fresh output directory}";GODOT_BIN="${GODOT_BIN:?Set Godot Mono executable}"
FONT="${DISPLACEMENT_CJK_FONT:-/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc}"
test -f "$FONT";test ! -e "$BASE";mkdir -p "$BASE";cd "$ROOT"
dotnet build -m:1 > "$BASE/build.log" 2>&1
"$GODOT_BIN" --headless --editor --path "$ROOT" --import > "$BASE/import.log" 2>&1
unset ROUTE_HEADLESS PROJECTILE_FRAMECHECK
export DISPLACEMENT_CANDIDATE=1 DISPLACEMENT_LIFECYCLE_PROBE=0 DISPLACEMENT_RESTART_MATRIX=0 STOP_MODE=flight DISPLACEMENT_TAP_END_MOVING=0
export DISPLACEMENT_TAP_START=150 DISPLACEMENT_TAP_PATTERN=0101010101010101
for VARIANT in A B;do
 OUT="$BASE/native-shadow-$VARIANT";mkdir -p "$OUT/xdg/data" "$OUT/xdg/config" "$OUT/xdg/cache"
 export XDG_DATA_HOME="$OUT/xdg/data" XDG_CONFIG_HOME="$OUT/xdg/config" XDG_CACHE_HOME="$OUT/xdg/cache" PROJECTILE_MOVIE_OUTPUT="$OUT" SHADOW_SPLIT_CANDIDATE=0
 [[ "$VARIANT" == B ]] && export SHADOW_SPLIT_CANDIDATE=1
 "$GODOT_BIN" --audio-driver Dummy --fixed-fps 60 --path "$ROOT" --write-movie "$OUT/capture.avi" scenes/controlled_gait_movie.tscn > "$OUT/runtime.log" 2>&1
 if grep -E 'ERROR:|FATAL|Unhandled|ObjectDB.*leak|resources still in use' "$OUT/runtime.log";then exit 1;fi
done
for FILE in authority.jsonl camera.tsv pose.tsv motion.jsonl sole.jsonl locomotion.jsonl;do
 cmp "$BASE/native-shadow-A/$FILE" "$BASE/native-shadow-B/$FILE"
done
python3 experiments/shadow-split/sequence_movie.py "$BASE" shadow "$BASE/comparison-shadow" --frames 195 --title '阴影稳定性（同一跑步动作）' --font "$FONT"
ffmpeg -v error -i "$BASE/comparison-shadow/comparison-sequential.mp4" -f null -
echo 'SHADOW_NATIVE_CAPTURE_COMPLETE: trace parity and stamps passed; visual review still required'
