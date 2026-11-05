#!/usr/bin/env bash
# Capture an already built/imported project on a native rendering display.
# Does not substitute for build/import or certify physical-device performance.
set -euo pipefail
ROOT="${CANDY_REVIEW_ROOT:-$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)}"
OUT="${CANDY_REVIEW_OUTPUT:?Set a fresh output directory}"
GODOT_BIN="${GODOT_BIN:?Set official Godot 4.6.3 Mono binary}"
test ! -e "$OUT"
test -f "$ROOT/.godot/mono/temp/bin/Debug/HmiGame.dll"
mkdir -p "$OUT/xdg/data" "$OUT/xdg/config" "$OUT/xdg/cache"
export XDG_DATA_HOME="$OUT/xdg/data" XDG_CONFIG_HOME="$OUT/xdg/config" XDG_CACHE_HOME="$OUT/xdg/cache"
export NORMAL_PROBE_OUTPUT="$OUT" NORMAL_PROBE_NATIVE=1 NORMAL_PROBE_RATE=30 NORMAL_PROBE_HERO=1
export NORMAL_PROBE_HASTE=0 NORMAL_PROBE_LONG_FRAME=0 NORMAL_PROBE_MAX_SPEED=0 NORMAL_PROBE_ROUTE=standard NORMAL_PRESENTATION_CANDIDATE=1
git -C "$ROOT" rev-parse HEAD > "$OUT/commit.txt"
git -C "$ROOT" diff -- src/Adapters/Godot content/environment > "$OUT/candidate.diff"
"$GODOT_BIN" --audio-driver Dummy --fixed-fps 30 --path "$ROOT" --write-movie "$OUT/capture.avi" scenes/normal_game_probe.tscn > "$OUT/runtime.log" 2>&1
grep -q 'NORMAL_GAME_PROBE_PASS frames=300' "$OUT/runtime.log"
if grep -E 'ERROR:|FATAL|Unhandled|ObjectDB.*leak|resources still in use' "$OUT/runtime.log"; then exit 1; fi
for second in 0 2 5 8; do
  ffmpeg -v error -y -ss "$second" -i "$OUT/capture.avi" -frames:v 1 "$OUT/frame-$second.png"
done
echo "CANDY_ARENA_CAPTURE_PASS $OUT"
