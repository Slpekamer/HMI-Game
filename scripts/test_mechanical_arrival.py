"""Actual-engine spawn preview regression; test code exists only in an isolated copy.

Headless results establish behavior/lifecycle, never visual or device performance.
"""
from pathlib import Path
import os
import sys
import shutil
import subprocess
import tempfile

root = Path(__file__).resolve().parents[1]
work = Path(tempfile.mkdtemp(prefix="hmi-mechanical-arrival-"))
project = work / "project"
shutil.copytree(root, project, ignore=shutil.ignore_patterns(
    ".godot", ".git", "bin", "obj", "builds", "exports", "android",
    "__pycache__", "*.import", "*.uid"))
(project / "src/Adapters/Godot/MechanicalArrivalProbe.cs").write_text(
    (root / "tests/Presentation/MechanicalArrivalProbe.cs.template").read_text())
(project / "scenes/main.tscn").write_text(
    '[gd_scene load_steps=2 format=3]\n'
    '[ext_resource type="Script" path="res://src/Adapters/Godot/MechanicalArrivalProbe.cs" id="1"]\n'
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
    ("run", [engine] + ([] if native else ["--headless"]) + ["--path", str(project)]),
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
        raise SystemExit(f"BLOCKED spawn preview {name}: timeout; evidence {work}")
    log.write_text(result.stdout)
    print(f"Spawn preview {name}: exit {result.returncode}; log {log}")
    if name == "run" or result.returncode:
        print(result.stdout)
    if result.returncode or "ERROR:" in result.stdout or "Exception" in result.stdout or "FATAL" in result.stdout:
        raise SystemExit(f"FAIL spawn preview {name}; evidence {work}")
    if name == "run" and "MECHANICAL_ARRIVAL_PROBE PASS" not in result.stdout:
        raise SystemExit(f"FAIL missing spawn preview pass marker; evidence {work}")
print("PASS spawn marker behavior; actual pixels and enemy entrance NOT VERIFIED", "; evidence", work)
