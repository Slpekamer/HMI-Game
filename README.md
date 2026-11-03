# Review candidate: approved combined presentation

The normal-game combination at `77e398f` and its high-speed safety revision at runtime `e999cb8` have passed user video review: Crit follows actual movement with turning Run, expresses genuine projectile launches through the reviewed energy gesture, and uses the reviewed `.015` shadow split. This remains an independent candidate, not merged into main/integration. Integration-readiness checks are in progress; visual approval is not export, hardware or release certification.

See [combination evidence and limitations](experiments/combined-normal-game/README.md) and [pre-integration status](docs/public/combined-preintegration.md). The older stopped-facing experiment is preserved as history, not the current Crit root-heading owner.

# 软糖突围 / HMI-Game

Work-in-progress survivor game using Godot **4.6.3 .NET**, Compatibility rendering and a portable C# ECS simulation. This is a source checkpoint, not a stable release or a tested Android package.

## Included
- Five food-themed heroes, character selection, card/shop/codex flows and wave progression.
- 60-second combat waves; one-second ground previews before enemy activation.
- Rule 3: real straight-flight projectiles, swept collision and damage on impact; ricochet continues after a real hit. Cyan and caramel bodies have distinct filled silhouettes and short tails.
- Bounded enemy/player hurt coloration; no full-screen damage flash.
- Explicit old-save upgrade or preserve-old/start-new confirmation, separate save slots and fail-closed recovery.
- Floating touch input, action-mapped gamepad UI navigation and codex focus restoration. Desktop event injection is not physical-controller certification.

Rejected experimental effects and directional gait/aiming studies remain separately documented and are not selected by the normal candidate path. The accepted movement/launch layers apply only to the Crit player; other heroes retain their existing paths. Original contact-neighborhood intersections, harder shadow edges and crowd occlusion remain known limitations.

## Build and run
Use the .NET edition of Godot (the standard edition cannot compile C#) and the SDK pinned by `global.json` (10.0.403). Restore requires access to the configured .NET package sources.

```sh
dotnet restore HmiGame.sln
dotnet build -m:1 HmiGame.csproj
export GODOT_NET=/path/to/Godot_v4.6.3-stable_mono
"$GODOT_NET" --editor --path .
```

Run the main scene from the editor. Use isolated test user-data when investigating save upgrades; never experiment on a player's only save copy.

```sh
python3 scripts/check_architecture.py
python3 scripts/check_notices.py
dotnet run --project tests/CoreTests/Hmi.CoreTests.csproj -c Release
dotnet run --project tests/RuleUpgradeTests/RuleUpgradeTests.csproj
```

`scripts/verify.sh` provides a broader historical verification workflow and deliberately fails on strict engine diagnostics. It is not asserted fully green for this checkpoint. Some diagnostic runners require separately retained fixture/baseline inputs; `run-native-projectile-first.sh` fails explicitly if its required evidence input is absent.

## Engineering and status
- [Engineering rules](AGENTS.md)
- [Architecture](docs/public/architecture.md)
- [Projectile rule 3](docs/public/projectile-rule3.md)
- [Save upgrade contract](docs/public/save-upgrade.md)
- [Validation and known failures](docs/public/validation-status.md)

Runtime asset provenance is retained beside the assets. Third-party notices are in `content/licenses/` and `content/fonts/LICENSE-Noto.txt`. Public availability does not create an additional blanket license; preserve each applicable notice and obtain any further rights required for redistribution.
