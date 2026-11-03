#!/usr/bin/env bash
set -euo pipefail
GODOT_BIN="${GODOT_BIN:?Set official Godot Mono executable}"
name=$1;enabled=$2;rate=${3:-60};hero=${4:-1};haste=${5:-0};long=${6:-0};maxspeed=${7:-0};route=${8:-standard}
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
out="${NORMAL_PROBE_BASE:?Set fresh output parent}/$name"
test ! -e "$out";mkdir -p "$out/xdg/data" "$out/xdg/config" "$out/xdg/cache"
env XDG_DATA_HOME="$out/xdg/data" XDG_CONFIG_HOME="$out/xdg/config" XDG_CACHE_HOME="$out/xdg/cache" NORMAL_PROBE_OUTPUT="$out" NORMAL_PROBE_RATE="$rate" NORMAL_PROBE_HERO="$hero" NORMAL_PROBE_HASTE="$haste" NORMAL_PROBE_LONG_FRAME="$long" NORMAL_PROBE_MAX_SPEED="$maxspeed" NORMAL_PROBE_ROUTE="$route" NORMAL_PRESENTATION_CANDIDATE="$enabled" LAUNCH_POSE_FORCE=invalid MOVING_LAUNCH_AIM=1 "$GODOT_BIN" --headless --audio-driver Dummy --path "$root" scenes/normal_game_probe.tscn > "$out/runtime.log" 2>&1
