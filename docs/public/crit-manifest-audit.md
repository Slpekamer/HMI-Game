# Crit animation manifest correction

## Requirement and scope

Keep the two release inventories consistent with the committed GLB without replacing any reviewed model or weakening validation. This correction changes only the `hero_crit.sha256` value in `content/animated-manifest.json`. Runtime selection, gameplay, animation data, other inventory fields and validators are unchanged.

## Cause and evidence

Commit `beb3ad68408fc42b1e85c469c00f88968070ac96` replaced the content Crit asset with `ready-attack-r2` and updated `content/models/manifest.json` and `food-heroes-provenance.json`, but left the animated inventory at the previous `r9` hash. The inconsistency already exists in baseline `4030b405`; it was not introduced by the input, APK or art candidate changes.

- Historical `a77c55d:content/models/hero_crit.glb`: 3,734,124 bytes, SHA-256 `38d76d5fe62ecd39c0b4506672c31038f62a797dc493dcbc72ede3f609d916bb`.
- Current `content/models/hero_crit.glb`: 3,747,244 bytes, SHA-256 `49fad35726ee57dc69f2c00f0e06f20039aa6b059ca8c75f1ac301d554c4559d`. This independently matches the combined inventory and current provenance. The previous hash remains correctly preserved in `previous_runtime_provenance`.
- The old 3,690,232-byte GLB BIN chunk is an exact prefix of the new 3,699,864-byte BIN chunk. JSON differences comprise appended accessors (183 to 209), appended buffer views (186 to 212), buffer byte length, and input/output accessor references for six arm rotation samplers per clip. Existing geometry, texture bytes, materials, nodes, skins, morphs and animation channel targets are unchanged.
- Both content versions have 29,924 triangles, two mesh instances/surfaces, one material, 17 bones and Blink/Smile/Surprise morphs. Actual accessor times remain Attack 0.4333333373 s, Idle 1.6333333254 s and Run 0.8333333135 s, matching both inventories. Provenance's shorter nominal clip times are not the exact exported accessor durations.

Changing the combined inventory back would be false for the committed bytes. Reverting the GLB or substituting the accepted normal rig would alter behavior, so neither is part of this fix.

## Normal and legacy references

`AssetVisuals.Create` selects `experiments/displacement-run/runtime/accepted-static-rebuilt.glb` for normal Crit through `NormalPresentationCandidate.UsesAcceptedModel`, and for the explicit controlled-gait fixture. Disabling the normal candidate selects the content GLB unless the controlled fixture is enabled. The manifest validator explicitly checks `content/models/<id>.glb`, not this normal-rig override.

The accepted normal rig is a distinct unchanged asset: SHA-256 `081a1e5472f1ebebd5e8ecd633acc158c5b84ae50f4d4dd1f5c4ce12082e5041`, 29,924 triangles, one mesh instance/surface, one material and 17 bones. It also contains Backpedal and StrafeR clips. Its independent static food-hero contract passes; that result neither re-approves its motion nor makes it interchangeable with the two-surface legacy content model.

## Reproduction and results

Run from the repository root with Python 3 and Pillow. Outputs below are temporary reports, not release mutations:

```sh
git show beb3ad6 -- content/models/manifest.json content/animated-manifest.json
git show a77c55d:content/models/hero_crit.glb > /tmp/hero_crit-r9.glb
sha256sum /tmp/hero_crit-r9.glb content/models/hero_crit.glb \
  experiments/displacement-run/runtime/accepted-static-rebuilt.glb
python3 tools/art/validate_food_heroes.py content/models --output /tmp/food-contract.json
python3 tools/art/validate_animated_assets.py content /tmp/animated-contract.json
python3 - <<'PY'
import hashlib, json, struct, sys
from pathlib import Path
sys.path.insert(0, 'tools/art')
from validate_food_heroes import check
for p in [Path('/tmp/hero_crit-r9.glb'), Path('content/models/hero_crit.glb'),
          Path('experiments/displacement-run/runtime/accepted-static-rebuilt.glb')]:
    print(json.dumps(check(p)))
def glb(path):
    data = Path(path).read_bytes()
    n = struct.unpack_from('<I', data, 12)[0]
    return json.loads(data[20:20+n]), data[28+n:]
old, old_bin = glb('/tmp/hero_crit-r9.glb')
new, new_bin = glb('content/models/hero_crit.glb')
assert new_bin[:len(old_bin)] == old_bin
for key in ('meshes', 'nodes', 'skins', 'materials', 'images', 'textures', 'samplers'):
    assert old.get(key) == new.get(key), key
combined = {a['id']: a for a in json.loads(Path('content/models/manifest.json').read_text())['assets']}
animated = json.loads(Path('content/animated-manifest.json').read_text())['assets']
for asset in animated:
    assert asset == combined[asset['id']]
    path = Path('content/models') / (asset['id'] + '.glb')
    assert hashlib.sha256(path.read_bytes()).hexdigest() == asset['sha256']
    assert len(glb(path)[0]['meshes']) == asset['meshes']
print('PASS historical binary prefix, static asset structure, eight full inventory entries and actual hashes/mesh counts')
PY
```

Before correction, the unchanged animated validator fails at `declared == actual`, with “Animated manifest must exactly cover combined release animated entries.” After correction it passes all eight assets. The separate five-hero food contract passes; the old Crit and accepted normal rig each independently pass that static contract. All eight full animated inventory records now equal their combined-inventory records, with actual GLB hashes and mesh counts independently checked.

These are static asset and inventory checks, not new Godot import, motion/pose acceptance, native UI, device, export or performance certification. Existing editor-shutdown failures and other integration limitations remain unaffected.
