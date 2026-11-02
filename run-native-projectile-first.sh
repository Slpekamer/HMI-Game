#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
if [[ -n "${HMI_TOOLCHAIN_ENV:-}" ]]; then
  [[ -f "$HMI_TOOLCHAIN_ENV" ]] || { echo "Missing HMI_TOOLCHAIN_ENV file" >&2; exit 2; }
  source "$HMI_TOOLCHAIN_ENV"
fi
: "${GODOT_NET:?Set GODOT_NET to the Godot .NET executable}"
: "${HMI_PROJECTILE_EVIDENCE:?Set HMI_PROJECTILE_EVIDENCE to the separately retained baseline evidence directory}"
export HMI_PROJECTILE_EVIDENCE
[[ -f "$HMI_PROJECTILE_EVIDENCE/native-manifest.json" ]] || { echo "Baseline native-manifest.json is required; evidence is not distributed in this code snapshot" >&2; exit 2; }
cd "$ROOT"
python3 - <<'PY'
import json,hashlib,pathlib,os
r=pathlib.Path('.');m=json.loads((pathlib.Path(os.environ['HMI_PROJECTILE_EVIDENCE'])/'native-manifest.json').read_text());bad=[p for p,h in m.items() if not (r/p).is_file() or hashlib.sha256((r/p).read_bytes()).hexdigest()!=h]
assert not bad,bad
print('NATIVE_MANIFEST_MATCH',len(m))
PY
for HERO in crit summon; do
 OUT="$ROOT/evidence/projectile-p2/native-$HERO"
 X="$ROOT/evidence/projectile-p2/native-fresh-xdg-$HERO"
 test ! -e "$OUT"; test ! -e "$X"
 mkdir -p "$OUT" "$X/data" "$X/config" "$X/cache"
 XDG_DATA_HOME="$X/data" XDG_CONFIG_HOME="$X/config" XDG_CACHE_HOME="$X/cache" PROJECTILE_NATIVE=1 PROJECTILE_HERO="$HERO" PROJECTILE_OUTPUT="$OUT" "$GODOT_NET" --audio-driver Dummy --path "$ROOT" scenes/projectile_flight_probe.tscn > "$OUT/runtime.log" 2>&1
 echo "PROJECTILE_NATIVE_${HERO}_EXIT=0"
 if grep -Ein 'ERROR:|FATAL|ObjectDB.*leak|resources still in use|Unhandled' "$OUT/runtime.log"; then exit 1; fi
 cmp "$OUT/authority.jsonl" "$HMI_PROJECTILE_EVIDENCE/headless-final-$HERO/authority.jsonl"
 cmp "$OUT/visual.jsonl" "$HMI_PROJECTILE_EVIDENCE/headless-final-$HERO/visual.jsonl"
done
echo PROJECTILE_FIRST_PAIR_EXIT=0
