"""Actual-engine floor contact regression; test code exists only in an isolated copy.

Headless results establish behavior/lifecycle, never visual or device performance.
"""
from pathlib import Path
import os
import sys
import shutil
import subprocess
import tempfile

root = Path(__file__).resolve().parents[1]
work = Path(tempfile.mkdtemp(prefix="hmi-floor-contact-"))
project = work / "project"
shutil.copytree(root, project, ignore=shutil.ignore_patterns(
    ".godot", ".git", "bin", "obj", "builds", "exports", "android",
    "__pycache__", "*.import", "*.uid"))
(project / "src/Adapters/Godot/FloorContactProbe.cs").write_text(
    (root / "tests/Presentation/FloorContactProbe.cs.template").read_text())
(project / "scenes/main.tscn").write_text(
    '[gd_scene load_steps=2 format=3]\n'
    '[ext_resource type="Script" path="res://src/Adapters/Godot/FloorContactProbe.cs" id="1"]\n'
    '[node name="PresentationProbe" type="Node"]\nscript = ExtResource("1")\n')
native = "--native" in sys.argv[1:]
engine = os.environ["GODOT_NET"]
env = os.environ.copy()
env["XDG_CONFIG_HOME"] = str(work / "editor-config")
Path(env["XDG_CONFIG_HOME"]).mkdir()
env["XDG_DATA_HOME"] = str(work / "user-data")
Path(env["XDG_DATA_HOME"]).mkdir()
steps = [
    ("build", ["dotnet", "build", "HmiGame.csproj", "-m:1", "-v", "minimal"]),
    ("import", [engine, "--headless", "--editor", "--path", str(project), "--import"]),
    ("run", [engine] + ([] if native else ["--headless"]) + ["--path", str(project)] + (["--", "--floor-native"] if native else [])),
]
for name, args in steps:
    log = work / (name + ".log")
    try:
        result = subprocess.run(args, cwd=project, env=env, stdout=subprocess.PIPE,
                                stderr=subprocess.STDOUT, text=True, timeout=240)
    except subprocess.TimeoutExpired as error:
        output = error.stdout or ""
        if isinstance(output, bytes):
            output = output.decode(errors="replace")
        log.write_text(output)
        raise SystemExit(f"BLOCKED floor contact {name}: timeout; evidence {work}")
    log.write_text(result.stdout)
    print(f"Floor contact {name}: exit {result.returncode}; log {log}")
    if name == "run" or result.returncode:
        print(result.stdout)
    if result.returncode or "ERROR:" in result.stdout or "Exception" in result.stdout or "FATAL" in result.stdout:
        raise SystemExit(f"FAIL floor contact {name}; evidence {work}")
    if name == "run" and "FLOOR_CONTACT_PROBE PASS" not in result.stdout:
        raise SystemExit(f"FAIL missing floor contact pass marker; evidence {work}")
print("PASS native instance geometry" if native else "PASS headless shapes only; native 220-instance storage gate NOT RUN", "; evidence", work)
