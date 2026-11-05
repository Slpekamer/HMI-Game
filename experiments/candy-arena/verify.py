"""Byte-exact gameplay/character/camera evidence for matched 30 Hz art captures."""
from pathlib import Path
import hashlib
import json
import sys

baseline, candidate = map(Path, sys.argv[1:3])
files = ("authority.jsonl", "pose.tsv", "camera.tsv", "sole.jsonl",
         "boot-geometry.jsonl", "presentation.jsonl", "substeps.jsonl", "attachments.jsonl")
report = {}
for name in files:
    before, after = (baseline / name).read_bytes(), (candidate / name).read_bytes()
    assert before and before == after, f"Changed or missing trace: {name}"
    report[name] = {"bytes": len(after), "sha256": hashlib.sha256(after).hexdigest()}
for folder in (baseline, candidate):
    summary = json.loads((folder / "summary.json").read_text())
    assert summary["frame"] == 300 and summary["rate"] == 30 and summary["hero"] == 1
    assert summary["candidate"] and summary["route"] == "standard"
    log = (folder / "runtime.log").read_text()
    assert "NORMAL_GAME_PROBE_PASS" in log
    assert not any(error in log for error in ("ERROR:", "FATAL", "Unhandled", "ObjectDB instances leaked", "resources still in use"))
print(json.dumps({"passed": True, "frames": 300, "files": report,
                  "limits": "Same-build scripted 10-second Crit route; not all heroes, later waves, hardware performance or owner art approval."}, indent=2))
