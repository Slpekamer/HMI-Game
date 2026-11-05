"""Material-only enemy study. Preserve the original geometry/animation binary chunk.

Input is pinned to the approved candidate's segmented enemy, not a regenerated
Blender export. This does not modify other assets that use the brass material name.
"""
import argparse
import copy
import hashlib
import json
from pathlib import Path
import struct

SOURCE_SHA256 = "15369930f954a47c8472652ff7b052f4096951d2cd902cc14edc7a2af8003244"
BRASS_SRGB = "E7BF72"


def chunks(data):
    magic, version, length = struct.unpack_from("<III", data)
    if magic != 0x46546C67 or version != 2 or length != len(data):
        raise ValueError("Expected an intact glTF 2 binary")
    result, position = [], 12
    while position < len(data):
        size, kind = struct.unpack_from("<II", data, position)
        position += 8
        if size % 4 or position + size > len(data):
            raise ValueError("Invalid GLB chunk length")
        result.append((kind, data[position:position + size]))
        position += size
    if not result or result[0][0] != 0x4E4F534A:
        raise ValueError("Missing first JSON chunk")
    return result


def linear(srgb):
    value = srgb / 255
    return value / 12.92 if value <= .04045 else ((value + .055) / 1.055) ** 2.4


def retune(data):
    if hashlib.sha256(data).hexdigest() != SOURCE_SHA256:
        raise ValueError("Source differs from the reviewed segmented enemy baseline")
    parts = chunks(data)
    document = json.loads(parts[0][1])
    modified = copy.deepcopy(document)
    brass = [material for material in modified["materials"] if material["name"] == "brass"]
    if len(brass) != 1:
        raise ValueError("Expected exactly one enemy brass material")
    pbr = brass[0]["pbrMetallicRoughness"]
    pbr["baseColorFactor"] = [linear(int(BRASS_SRGB[i:i + 2], 16)) for i in (0, 2, 4)] + [1]
    pbr["metallicFactor"] = .2
    pbr["roughnessFactor"] = .55
    payload = json.dumps(modified, separators=(",", ":"), ensure_ascii=False).encode()
    payload += b" " * (-len(payload) % 4)
    parts[0] = (parts[0][0], payload)
    body = b"".join(struct.pack("<II", len(chunk), kind) + chunk for kind, chunk in parts)
    return struct.pack("<III", 0x46546C67, 2, len(body) + 12) + body


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("baseline", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    if args.baseline.resolve() == args.output.resolve():
        parser.error("Keep the baseline separate from the output")
    result = retune(args.baseline.read_bytes())
    args.output.write_bytes(result)
    print("ENEMY_BRASS_STUDY", hashlib.sha256(result).hexdigest())
