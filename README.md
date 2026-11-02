# HMI-Game — Ready / Strafe and rejected melee-priority aim

**Historical completed experiment; not accepted for current production integration.** This branch is preserved for source traceability, not as a release. It retains its original rule-2 gameplay baseline, not the current rule-3 projectile/save-upgrade game.

See [experiment contract and honest result](docs/public/experiment-status.md), [architecture](docs/public/architecture.md), [validation](docs/public/validation-status.md) and [engineering rules](AGENTS.md).

## Build
Requires Godot4.6.3 **.NET** and the SDK pinned by `global.json` (10.0.403).

```sh
dotnet restore HmiGame.sln
dotnet build -m:1 HmiGame.csproj
export GODOT_NET=/path/to/Godot_v4.6.3-stable_mono
"$GODOT_NET" --editor --path .
```

Use fresh isolated user-data only. Do not upgrade real player saves with this experimental snapshot. Historical fixture runners may require separately supplied baseline inputs. The public copy contains source/probes and necessary runtime assets, not screenshots, recordings, caches or full recovery transcripts.

Asset provenance records actual included file hashes. Third-party notices remain in `content/licenses/` and the font license. Public availability is not an additional blanket license.
