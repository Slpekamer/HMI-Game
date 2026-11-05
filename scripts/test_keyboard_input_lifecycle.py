"""Actual-engine keyboard input lifecycle regression; test code exists only in an isolated copy.

Headless results establish behavior/lifecycle, never visual or device performance.
"""
from pathlib import Path
import os
import shutil
import subprocess
import tempfile

root = Path(__file__).resolve().parents[1]
work = Path(tempfile.mkdtemp(prefix="hmi-keyboard-input-lifecycle-"))
project = work / "project"
shutil.copytree(root, project, ignore=shutil.ignore_patterns(
    ".godot", ".git", "bin", "obj", "builds", "exports", "android",
    "__pycache__", "*.import", "*.uid"))
(project / "src/Adapters/Godot/KeyboardInputLifecycleProbe.cs").write_text(
    (root / "tests/Presentation/KeyboardInputLifecycleProbe.cs.template").read_text())
(project / "scenes/main.tscn").write_text(
    '[gd_scene load_steps=2 format=3]\n'
    '[ext_resource type="Script" path="res://src/Adapters/Godot/KeyboardInputLifecycleProbe.cs" id="1"]\n'
    '[node name="Keyboard input lifecycle probe" type="Node"]\nscript = ExtResource("1")\n')
engine = os.environ["GODOT_NET"]
env = os.environ.copy()
env["XDG_CONFIG_HOME"] = str(work / "editor-config")
Path(env["XDG_CONFIG_HOME"]).mkdir()
env["XDG_DATA_HOME"] = str(work / "user-data")
Path(env["XDG_DATA_HOME"]).mkdir()
steps = [
    ("build", ["dotnet", "build", "HmiGame.csproj", "-m:1", "-v", "minimal"]),
    ("import", [engine, "--headless", "--editor", "--path", str(project), "--import"]),
    ("run", [engine, "--headless", "--path", str(project)]),
]
diagnostic_failures = []
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
        raise SystemExit(f"BLOCKED keyboard input lifecycle {name}: timeout; evidence {work}")
    log.write_text(result.stdout)
    print(f"Keyboard input lifecycle {name}: exit {result.returncode}; log {log}")
    if name == "run" or result.returncode:
        print(result.stdout)
    if result.returncode:
        raise SystemExit(f"FAIL keyboard input lifecycle {name}; evidence {work}")
    if "ERROR:" in result.stdout or "Exception" in result.stdout:
        diagnostic_failures.append(name)
        # A zero-exit import can still contain strict diagnostics. Preserve that
        # failure, but collect the independent runtime result before reporting it.
    if name == "run" and "KEYBOARD_INPUT_LIFECYCLE_PROBE PASS" not in result.stdout:
        raise SystemExit(f"FAIL missing keyboard input lifecycle pass marker; evidence {work}")
if diagnostic_failures:
    raise SystemExit(f"FAIL strict diagnostics in {', '.join(diagnostic_failures)}; evidence {work}")
print("PASS isolated actual-engine keyboard input lifecycle behavior; evidence", work)
