#!/usr/bin/env bash
set -euo pipefail
source /workspace/scratch/80f5bc988ef7/godot-toolchain-verification/env.sh
p=/workspace/scratch/80f5bc988ef7/godot-toolchain-verification/tools/tmp/hmi-combat-skill-bodies-i9i631pq/project
out=/workspace/shared/hmi-vfx-context-review/native-storage
mkdir -p "$out"
run=$(mktemp -d /tmp/hmi-body-native.XXXXXX)
export XDG_CONFIG_HOME="$run/config" XDG_DATA_HOME="$run/data" XDG_CACHE_HOME="$run/cache"
mkdir -p "$XDG_CONFIG_HOME" "$XDG_DATA_HOME" "$XDG_CACHE_HOME"
set +e
"$GODOT_NET" --audio-driver Dummy --resolution 1280x720 --path "$p" -- --native-body-storage --body-native-capture="$out" > "$out/native.log" 2>&1
s=$?
set -e
printf 'NATIVE_PROCESS_EXIT=%s\n' "$s" >> "$out/native.log"
test "$s" -eq 0
grep -q 'native_storage=True' "$out/native.log"
! grep -Eq 'ERROR:|FATAL:|leaked' "$out/native.log"
