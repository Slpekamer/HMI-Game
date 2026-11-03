#!/usr/bin/env bash
# Start only from an observed native desktop terminal. Internal review of known velocity-continuity risk.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
BASE="${NORMAL_SPEED_NATIVE_OUTPUT:?Set fresh output directory}"
REF="${NORMAL_SPEED_REFERENCE:?Set matching headless reference parent}"
GODOT_BIN="${GODOT_BIN:?Set official Godot Mono binary}"
FONT="${DISPLACEMENT_CJK_FONT:-/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc}"
test ! -e "$BASE";test -f "$FONT";mkdir -p "$BASE";cd "$ROOT"
dotnet build -m:1 -nr:false -p:UseSharedCompilation=false > "$BASE/build.log" 2>&1
"$GODOT_BIN" --headless --editor --path "$ROOT" --import > "$BASE/import.log" 2>&1
for name in interior30 maximum60 longframe60;do
 rate=30;long=0;route=interior-stops;ref=heading-air-interior-30-B;label='最高移速 · 内场反向 | 预设装备＋加速 · 原尺度 · 30帧原速'
 if [[ "$name" == maximum60 ]];then rate=60;route=standard;ref=native-speed-reference-60-B;label='最高移速 · 墙边转向 | 预设装备＋加速 · 原尺度 · 60帧原速';fi
 if [[ "$name" == longframe60 ]];then rate=60;long=1;route=standard;ref=heading-air-max-60-B;fi
 out="$BASE/$name/B";mkdir -p "$out/xdg/data" "$out/xdg/config" "$out/xdg/cache"
 env XDG_DATA_HOME="$out/xdg/data" XDG_CONFIG_HOME="$out/xdg/config" XDG_CACHE_HOME="$out/xdg/cache" NORMAL_PROBE_OUTPUT="$out" NORMAL_PROBE_NATIVE=1 NORMAL_PROBE_RATE="$rate" NORMAL_PROBE_HERO=1 NORMAL_PROBE_HASTE=1 NORMAL_PROBE_LONG_FRAME="$long" NORMAL_PROBE_MAX_SPEED=1 NORMAL_PROBE_ROUTE="$route" NORMAL_PRESENTATION_CANDIDATE=1 LAUNCH_POSE_FORCE=invalid MOVING_LAUNCH_AIM=1 "$GODOT_BIN" --audio-driver Dummy --fixed-fps "$rate" --path "$ROOT" --write-movie "$out/capture.avi" scenes/normal_game_probe.tscn > "$out/runtime.log" 2>&1
 if grep -E 'ERROR:|FATAL|Unhandled|ObjectDB.*leak|resources still in use' "$out/runtime.log";then exit 1;fi
 for trace in authority.jsonl presentation.jsonl substeps.jsonl camera.tsv pose.tsv sole.jsonl boot-geometry.jsonl;do cmp "$out/$trace" "$REF/$ref/$trace";done
 if [[ "$name" == longframe60 ]];then continue;fi # Internal raw stress frames only: injected elapsed time is not a normal-speed movie.
 python3 experiments/combined-normal-game/sequence_movie.py "$BASE/$name" "$BASE/$name-review.mp4" --font "$FONT" --label "$label"
 ffmpeg -v error -i "$BASE/$name-review.mp4" -f null -
done
echo NORMAL_SPEED_NATIVE_CAPTURE_COMPLETE
