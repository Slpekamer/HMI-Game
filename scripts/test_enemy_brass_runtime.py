"""Strict isolated import and imported-enemy material/overlay lifetime checks."""
from pathlib import Path
import os
import shutil
import subprocess
import tempfile

root = Path(__file__).resolve().parents[1]
work = Path(tempfile.mkdtemp(prefix="hmi-enemy-brass-"))
project = work / "project"
shutil.copytree(root, project, ignore=shutil.ignore_patterns(
    ".godot", ".git", "bin", "obj", "builds", "exports", "android",
    "__pycache__", "*.import", "*.uid"))
(project / "src/Adapters/Godot/EnemyBrassProbe.cs").write_text(
    (root / "tests/Presentation/EnemyBrassProbe.cs.template").read_text())
(project / "scenes/main.tscn").write_text(
    '[gd_scene load_steps=2 format=3]\n'
    '[ext_resource type="Script" path="res://src/Adapters/Godot/EnemyBrassProbe.cs" id="1"]\n'
    '[node name="EnemyBrassProbe" type="Node3D"]\nscript = ExtResource("1")\n')
env = os.environ.copy()
for key, name in (("XDG_CONFIG_HOME", "config"), ("XDG_DATA_HOME", "data"), ("XDG_CACHE_HOME", "cache")):
    env[key] = str(work / name)
    Path(env[key]).mkdir()
engine = env["GODOT_NET"]
for name, command in (
    ("build", ["dotnet", "build", "HmiGame.csproj", "-m:1", "-v", "minimal", "-p:UseSharedCompilation=false"]),
    ("import", [engine, "--headless", "--editor", "--path", str(project), "--import"]),
    ("run", [engine, "--headless", "--path", str(project)]),
):
    result = subprocess.run(command, cwd=project, env=env, stdout=subprocess.PIPE,
                            stderr=subprocess.STDOUT, text=True, timeout=240)
    (work / f"{name}.log").write_text(result.stdout)
    print(f"Enemy brass {name}: exit {result.returncode}; evidence {work}", flush=True)
    if result.returncode or any(token in result.stdout for token in (
            "ERROR:", "Exception", "FATAL", "ObjectDB instances leaked", "resources still in use")):
        print(result.stdout)
        raise SystemExit(1)
    if name == "run":
        print(result.stdout)
        if "ENEMY_BRASS_PROBE PASS" not in result.stdout:
            raise SystemExit("Missing explicit pass marker")
