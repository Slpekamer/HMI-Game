"""Exact material-only contract and deterministic generation; no visual claims."""
import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[1]
BASE = "bbece68bfac2ed7e977ba5ab8f7a4c01e827a98a"
ASSET = "content/models/enemy_chaser.glb"
spec = importlib.util.spec_from_file_location("retune", ROOT / "tools/art/retune_enemy_brass.py")
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
before = subprocess.check_output(["git", "show", f"{BASE}:{ASSET}"], cwd=ROOT)
after = (ROOT / ASSET).read_bytes()
assert module.retune(before) == after, "Candidate must be exactly reproducible"
old_chunks, new_chunks = module.chunks(before), module.chunks(after)
assert old_chunks[1:] == new_chunks[1:], "Geometry, animation and all non-JSON bytes must remain identical"
old, new = json.loads(old_chunks[0][1]), json.loads(new_chunks[0][1])
restored = copy.deepcopy(new)
changed = []
for index, (a, b) in enumerate(zip(old["materials"], new["materials"], strict=True)):
    if a == b:
        continue
    changed.append(index)
    assert a["name"] == b["name"] == "brass"
    pbr = b["pbrMetallicRoughness"]
    assert pbr["baseColorFactor"][-1] == 1
    assert pbr["metallicFactor"] == .2 and pbr["roughnessFactor"] == .55
    assert pbr["baseColorFactor"][:3] == [module.linear(int("E7BF72"[i:i + 2], 16)) for i in (0, 2, 4)]
    allowed = ("baseColorFactor", "metallicFactor", "roughnessFactor")
    for key in allowed:
        restored["materials"][index]["pbrMetallicRoughness"][key] = a["pbrMetallicRoughness"][key]
assert len(changed) == 1, "Only enemy brass changes"
assert restored == old, "Nodes, surfaces, animations, alpha, extensions and every other field must remain unchanged"
sha = hashlib.sha256(after).hexdigest()
for name in ("content/models/manifest.json", "content/animated-manifest.json"):
    manifest = json.loads((ROOT / name).read_text())
    records = [entry for entry in manifest["assets"] if entry["id"] == "enemy_chaser"]
    assert len(records) == 1 and records[0]["sha256"] == sha, name
for invalid in (b"invalid", after, before[:-4]):
    try:
        module.retune(invalid)
    except ValueError:
        pass
    else:
        raise AssertionError("Unknown, truncated or already modified input was accepted")
print(json.dumps({"passed": True, "candidate_sha256": sha,
                  "opaque_chunks_sha256": [hashlib.sha256(chunk).hexdigest() for _, chunk in new_chunks[1:]],
                  "scope": "One material, three PBR properties; native appearance remains unreviewed"}, indent=2))
