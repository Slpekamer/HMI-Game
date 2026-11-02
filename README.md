# HMI-Game — rejected impact-surface experiment

**Experimental archive, not the integrated game branch and not a recommended implementation.** This completed experiment was rejected because the target-surface micro-burst remained barely visible at normal game scale while adding disproportionate geometry work. Do not merge it as an accepted visual improvement.

This standalone source snapshot retains its earlier rule-3 projectile adapter baseline and the actual experimental code. It is not the newer integrated save-upgrade/focus/body checkpoint. Use fresh isolated test data only; do not use it to upgrade a real player save.

## What is here
- Real ECS straight-flight projectiles and damage on impact.
- Filled cyan/caramel projectile bodies and existing enemy hurt coloration.
- Experimental target-own-mesh surface placement for a small, normally depth-tested impact symbol.
- Source probes for the surface math, bounded host integration and initial visual checks.

The stable development branch keeps only the improved bodies and existing hurt coloration; it does not include this surface resolver.

## Build
Requires Godot 4.6.3 **.NET** and SDK10.0.403 (see `global.json`).

```sh
dotnet restore HmiGame.sln
dotnet build -m:1 HmiGame.csproj
export GODOT_NET=/path/to/Godot_v4.6.3-stable_mono
"$GODOT_NET" --editor --path .
```

Do not interpret successful compilation or source-probe counts as visual acceptance. Historical compatibility tests may require an explicitly supplied legacy checkout. Original screenshots, recordings and operational history are not part of this public copy.

See [experiment contract and result](docs/public/impact-surface-rejected.md), [validation status](docs/public/validation-status.md) and [engineering rules](AGENTS.md). Asset provenance and third-party notices remain beside the assets and in `content/licenses/`. Public availability does not add a blanket license.
