#!/usr/bin/env bash
# Run only from an observed native desktop terminal.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
BASE="${NORMAL_NATIVE_OUTPUT:?Set fresh output directory}"
GODOT_BIN="${GODOT_BIN:?Set official Godot Mono binary}"
FONT="${DISPLACEMENT_CJK_FONT:-/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc}"
test ! -e "$BASE";test -f "$FONT";mkdir -p "$BASE";cd "$ROOT"
dotnet build -m:1 > "$BASE/build.log" 2>&1
"$GODOT_BIN" --headless --editor --path "$ROOT" --import > "$BASE/import.log" 2>&1
for variant in A B;do
 out="$BASE/$variant";mkdir -p "$out/xdg/data" "$out/xdg/config" "$out/xdg/cache";enable=0;[[ "$variant" == B ]]&&enable=1
 export XDG_DATA_HOME="$out/xdg/data" XDG_CONFIG_HOME="$out/xdg/config" XDG_CACHE_HOME="$out/xdg/cache" NORMAL_PROBE_OUTPUT="$out" NORMAL_PROBE_NATIVE=1 NORMAL_PROBE_RATE=60 NORMAL_PROBE_HERO=1 NORMAL_PROBE_HASTE=0 NORMAL_PROBE_LONG_FRAME=0 NORMAL_PRESENTATION_CANDIDATE="$enable" LAUNCH_POSE_FORCE=invalid MOVING_LAUNCH_AIM=1
 "$GODOT_BIN" --audio-driver Dummy --fixed-fps 60 --path "$ROOT" --write-movie "$out/capture.avi" scenes/normal_game_probe.tscn > "$out/runtime.log" 2>&1
 if grep -E 'ERROR:|FATAL|Unhandled|ObjectDB.*leak|resources still in use' "$out/runtime.log";then exit 1;fi
done
cmp "$BASE/A/authority.jsonl" "$BASE/B/authority.jsonl"
python3 experiments/combined-normal-game/sequence_movie.py "$BASE" "$BASE/卷卷-正常游戏路径组合候选-待审核.mp4" --font "$FONT"
ffmpeg -v error -i "$BASE/卷卷-正常游戏路径组合候选-待审核.mp4" -f null -
echo NORMAL_COMBINED_NATIVE_CAPTURE_COMPLETE
