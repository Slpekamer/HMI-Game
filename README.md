# Review candidate: stopped Crit launch facing

This independent WIP adds a narrow visual-only correction to the integrated rule-3 source below. When Crit is stopped and a genuine projectile is launched, the visual body turns smoothly toward that recorded launch direction. It does not fix firing backward while moving, change projectile paths, replay attacks or add turn-in-place footwork. This candidate is **awaiting user review**, not merged into the main branch.

See [candidate contract and scoped evidence](docs/public/stationary-facing.md) and the [review queue](docs/public/review-queue.md). Existing shadows, initial-turn residuals and device-validation limits remain.

# 软糖突围 / HMI-Game

Work-in-progress survivor game using Godot **4.6.3 .NET**, Compatibility rendering and a portable C# ECS simulation. This is a source checkpoint, not a stable release or a tested Android package.

## Included
- Five food-themed heroes, character selection, card/shop/codex flows and wave progression.
- 60-second combat waves; one-second ground previews before enemy activation.
- Rule 3: real straight-flight projectiles, swept collision and damage on impact; ricochet continues after a real hit. Cyan and caramel bodies have distinct filled silhouettes and short tails.
- Bounded enemy/player hurt coloration; no full-screen damage flash.
- Explicit old-save upgrade or preserve-old/start-new confirmation, separate save slots and fail-closed recovery.
- Floating touch input, action-mapped gamepad UI navigation and codex focus restoration. Desktop event injection is not physical-controller certification.

Experimental surface-impact effects, forced attack-facing, attention/strafe research and rejected shadow adjustments are **not included**. Standing/moving attack direction, shadow flicker and crowd occlusion remain known limitations.

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
