"""Build a real rule2 reader from immutable Git blobs and test synthetic upgrade states.

Never pass user saves. No historical production source or serialized format is rewritten.
The baseline repository may have unrelated worktree edits: only the explicit commit is read.
"""
from pathlib import Path
import argparse
import hashlib
import json
import os
import re
import subprocess
import tempfile


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--baseline", type=Path, required=True)
parser.add_argument("--expected-sha", required=True)
args = parser.parse_args()
if not re.fullmatch("[0-9a-f]{40}", args.expected_sha):
    parser.error("A full lowercase Git commit SHA is required")
baseline = args.baseline.resolve()


def git(repository, *arguments):
    return subprocess.check_output(["git", "-C", str(repository), *arguments])


if git(baseline, "rev-parse", args.expected_sha + "^{commit}").decode().strip() != args.expected_sha:
    parser.error("Commit mismatch")
root = Path(__file__).resolve().parents[1]
work = Path(tempfile.mkdtemp(prefix="hmi-exact-prior-upgrade-"))
old = work / "old"
old.mkdir()
print("Exact prior reader evidence:", work, flush=True)
manifest = {}
entries = git(baseline, "ls-tree", "-rz", args.expected_sha, "src/Core",
              "src/Adapters/Persistence", "content/gameplay.json", "global.json")
for entry in entries.split(b"\0"):
    if not entry:
        continue
    metadata, raw_name = entry.split(b"\t", 1)
    mode, kind, blob = metadata.decode().split()
    name = raw_name.decode()
    if mode not in ("100644", "100755") or kind != "blob":
        raise RuntimeError("Historical source must be regular files: " + name)
    if (Path(name).is_absolute() or ".." in Path(name).parts
            or not (name.startswith(("src/Core/", "src/Adapters/Persistence/"))
                    or name in ("content/gameplay.json", "global.json"))):
        raise RuntimeError("Unexpected source path: " + name)
    data = git(baseline, "cat-file", "blob", blob)
    target = old / name
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(data)
    manifest[name] = digest(target)
prior = (old / "src/Core/WaveRules.cs").read_text()
if not re.search(r'public const string Version\s*=\s*"hmi-core-wave60-preview1-2"', prior):
    raise RuntimeError("Not the historical rule2 source")
if (old / "content/gameplay.json").read_bytes() != (root / "content/gameplay.json").read_bytes():
    raise RuntimeError("Catalog differs; this fixture contract is not matched to the old reader")
if not (old / "global.json").is_file():
    raise RuntimeError("Historical SDK pin is missing")

reader = old / "tests/ExactPriorReader"
reader.mkdir(parents=True)
(reader / "Program.cs").write_bytes((root / "tests/RuleUpgradeTests/ExactPriorReader.cs.template").read_bytes())
(reader / "ExactPriorReader.csproj").write_text('''<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><Nullable>enable</Nullable><ImplicitUsings>enable</ImplicitUsings></PropertyGroup>
  <ItemGroup><ProjectReference Include="../../src/Adapters/Persistence/Hmi.Persistence.csproj"/><None Include="../../content/gameplay.json" Link="gameplay.json" CopyToOutputDirectory="PreserveNewest"/></ItemGroup>
</Project>
''')
current_paths = git(root, "ls-files", "src/Core", "src/Adapters/Persistence",
                    "tests/RuleUpgradeTests", "content/gameplay.json", "global.json").decode().splitlines()
current_paths += ["scripts/test_exact_prior_upgrade_reader.py", "tests/RuleUpgradeTests/ExactPriorReader.cs.template"]
evidence = {
    "source_commit": args.expected_sha,
    "source_sha256": manifest,
    "reader_template_sha256": digest(reader / "Program.cs"),
    "current_head": git(root, "rev-parse", "HEAD").decode().strip(),
    "current_worktree_status": git(root, "status", "--porcelain").decode(),
    "current_source_sha256": {name: digest(root / name) for name in sorted(set(current_paths))},
    "commands": [],
}
env = os.environ.copy()
env.update(DOTNET_PROCESSOR_COUNT="1", DOTNET_CLI_USE_MSBUILD_SERVER="0", MSBUILDDISABLENODEREUSE="1")


def save_evidence():
    (work / "provenance.json").write_text(json.dumps(evidence, indent=2))


def run(label, command, cwd):
    entry = {"label": label, "args": command, "cwd": str(cwd)}
    evidence["commands"].append(entry)
    save_evidence()
    try:
        result = subprocess.run(command, cwd=cwd, env=env, stdout=subprocess.PIPE,
                                stderr=subprocess.STDOUT, text=True, timeout=240)
    except subprocess.TimeoutExpired as error:
        output = error.stdout or b""
        (work / (label + ".log")).write_text(output.decode(errors="replace") if isinstance(output, bytes) else output)
        entry["result"] = "timeout"
        save_evidence()
        raise SystemExit("FAIL " + label + " timeout; evidence " + str(work))
    (work / (label + ".log")).write_text(result.stdout)
    entry["exit_code"] = result.returncode
    save_evidence()
    print(label, "exit", result.returncode, flush=True)
    if result.returncode:
        print(result.stdout)
        raise SystemExit(result.returncode)
    return result.stdout


evidence["prior_sdk"] = run("prior-sdk", ["dotnet", "--version"], old).strip()
evidence["current_sdk"] = run("current-sdk", ["dotnet", "--version"], root).strip()
run("build-reader", ["dotnet", "build", str(reader / "ExactPriorReader.csproj"), "-m:1", "-p:UseSharedCompilation=false"], old)
run("build-current", ["dotnet", "build", str(root / "tests/RuleUpgradeTests/RuleUpgradeTests.csproj"), "-m:1", "-p:UseSharedCompilation=false"], root)
dll = reader / "bin/Debug/net10.0/ExactPriorReader.dll"
current = root / "tests/RuleUpgradeTests/bin/Debug/net10.0/RuleUpgradeTests.dll"
evidence["reader_runtime_sha256"] = {p.name: digest(p) for p in sorted(dll.parent.iterdir()) if p.is_file()}
evidence["current_runtime_sha256"] = {p.name: digest(p) for p in sorted(current.parent.iterdir()) if p.is_file()}
output = run("upgrade-with-reader", ["dotnet", str(current), "--old-reader", str(dll)], root)
for stage in range(7):
    expected = "Selected" if stage == 0 else "Uncertain"
    marker = f"OLD_READER_STAGE {stage} EXACT_PRIOR_READER PASS kind={expected} rules=hmi-core-wave60-preview1-2"
    if output.splitlines().count(marker) != 1:
        raise RuntimeError("Missing or duplicated positive control/stage " + str(stage))
if "OLD_READER NOT_RUN" in output or not re.search(r"^PASS RuleUpgrade \d+$", output, re.MULTILINE):
    raise RuntimeError("Incomplete reader run")
evidence["result"] = "passed"
save_evidence()
print(output)
print("PASS exact prior source reader; synthetic after-write upgrade states only; evidence", work)
