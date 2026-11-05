"""Geometry, native instance bounds, and teardown/rebuild checks in an isolated copy.

Import diagnostics remain a separate failing gate even if the runtime probe passes.
Native software-rendered success does not certify hardware performance or aesthetics.
"""
from pathlib import Path
import os
import shutil
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[1]
svg = ET.parse(root / "content/environment/arena_icing.svg").getroot()
tags = [node.tag.rsplit("}", 1)[-1] for node in svg.iter()]
assert tags.count("rect") == 1 and tags.count("path") >= 4 and "pattern" not in tags, "Preserve the grid-free explicit-shape floor asset"
work = Path(tempfile.mkdtemp(prefix="hmi-candy-arena-"))
project = work / "project"
shutil.copytree(root, project, ignore=shutil.ignore_patterns(
    ".godot", ".git", "bin", "obj", "builds", "exports", "android",
    "__pycache__", "*.import", "*.uid"))
(project / "src/Adapters/Godot/CandyArenaProbe.cs").write_text(
    (root / "tests/Presentation/CandyArenaProbe.cs.template").read_text())
(project / "scenes/main.tscn").write_text(
    '[gd_scene load_steps=2 format=3]\n'
    '[ext_resource type="Script" path="res://src/Adapters/Godot/CandyArenaProbe.cs" id="1"]\n'
    '[node name="CandyArenaProbe" type="Node"]\nscript = ExtResource("1")\n')
native = "--native" in sys.argv[1:]
engine = os.environ["GODOT_NET"]
env = os.environ.copy()
for key, folder in (("XDG_DATA_HOME", "data"), ("XDG_CONFIG_HOME", "config"), ("XDG_CACHE_HOME", "cache")):
    env[key] = str(work / folder)
    Path(env[key]).mkdir()
steps = [
    ("build", ["dotnet", "build", "HmiGame.csproj", "-m:1", "-v", "minimal", "-p:UseSharedCompilation=false"]),
    ("import", [engine, "--headless", "--editor", "--path", str(project), "--import"]),
    ("run", [engine] + ([] if native else ["--headless"]) + ["--audio-driver", "Dummy", "--path", str(project)] + (["--", "--native"] if native else [])),
]
failed = []
for name, command in steps:
    try:
        result = subprocess.run(command, cwd=project, env=env, stdout=subprocess.PIPE,
                                stderr=subprocess.STDOUT, text=True, timeout=240)
    except subprocess.TimeoutExpired as error:
        output = error.stdout or ""
        if isinstance(output, bytes):
            output = output.decode(errors="replace")
        (work / (name + ".log")).write_text(output)
        raise SystemExit(f"BLOCKED candy arena {name}: timeout; evidence {work}")
    (work / (name + ".log")).write_text(result.stdout)
    bad = result.returncode or any(token in result.stdout for token in (
        "ERROR:", "Exception", "FATAL", "ObjectDB instances leaked", "resources still in use"))
    if name == "run" and "CANDY_ARENA_PROBE PASS" not in result.stdout:
        bad = True
    print(f"Candy arena {name}: {'FAIL' if bad else 'PASS'}; exit {result.returncode}; {work / (name + '.log')}")
    if bad or name == "run":
        print(result.stdout)
    if bad:
        failed.append(name)
        if name == "build" or result.returncode:
            break
print(f"Evidence: {work}; native={native}; failing gates={failed}")
raise SystemExit(1 if failed else 0)
