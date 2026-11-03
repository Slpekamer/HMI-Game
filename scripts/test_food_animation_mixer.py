"""Actual-engine presentation regression; test code exists only in an isolated copy.

Legacy five-asset graph/presenter contract only, not the default normal candidate.
Normal gameplay and real dispose/recreate transitions are covered by separate probes.
Headless results establish behavior/lifecycle, never visual or device performance.
"""
from pathlib import Path
import os
import shutil
import subprocess
import tempfile

root = Path(__file__).resolve().parents[1]
work = Path(tempfile.mkdtemp(prefix="hmi-food-animation-mixer-"))
project = work / "project"
shutil.copytree(root, project, ignore=shutil.ignore_patterns(
    ".godot", ".git", "bin", "obj", "builds", "exports", "android",
    "__pycache__", "*.import", "*.uid"))
(project / "src/Adapters/Godot/FoodAnimationMixerProbe.cs").write_text(
    (root / "tests/Presentation/FoodAnimationMixerProbe.cs.template").read_text())
(project / "scenes/main.tscn").write_text(
    '[gd_scene load_steps=2 format=3]\n'
    '[ext_resource type="Script" path="res://src/Adapters/Godot/FoodAnimationMixerProbe.cs" id="1"]\n'
    '[node name="PresentationProbe" type="Node3D"]\nscript = ExtResource("1")\n')
engine = os.environ["GODOT_NET"]
env = os.environ.copy()
# Keep this legacy graph/reset contract explicit, independent of the caller environment.
env["NORMAL_PRESENTATION_CANDIDATE"] = "0"
env["XDG_CONFIG_HOME"] = str(work / "editor-config")
Path(env["XDG_CONFIG_HOME"]).mkdir()
env["XDG_DATA_HOME"] = str(work / "user-data")
Path(env["XDG_DATA_HOME"]).mkdir()
steps = [
    ("build", ["dotnet", "build", "HmiGame.csproj", "-m:1", "-v", "minimal"]),
    ("import", [engine, "--headless", "--editor", "--path", str(project), "--import"]),
    ("run", [engine, "--headless", "--path", str(project)]),
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
        raise SystemExit(f"BLOCKED presentation {name}: timeout; evidence {work}")
    log.write_text(result.stdout)
    print(f"Presentation {name}: exit {result.returncode}; log {log}")
    if name == "run" or result.returncode:
        print(result.stdout)
    if result.returncode or "ERROR:" in result.stdout or "Exception" in result.stdout:
        raise SystemExit(f"FAIL presentation {name}; evidence {work}")
    if name == "run" and "FOOD_ANIMATION_MIXER_PROBE PASS" not in result.stdout:
        raise SystemExit(f"FAIL missing presentation pass marker; evidence {work}")
print("PASS isolated actual-engine presentation behavior; evidence", work)
