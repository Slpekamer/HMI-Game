# M1 recovered core — fresh evidence 2026-10-31

This is newly reconstructed source, not the lost original M1 byte snapshot. No Godot host/UI/art runtime files are included in this milestone. Baseline scene remains empty. No main merge authorized.

## Executed against this immutable source snapshot

Environment: Linux x86_64 cloud CPU; SDK10.0.403; core netstandard2.1/C#8; console harness net10.0. Commands from snapshot root:

```
dotnet build src/Core/Hmi.Core.csproj -c Release -v minimal
dotnet run --project tests/CoreTests/Hmi.CoreTests.csproj -c Release
```

Build passed zero warnings/errors. Fresh suite passed538761 assertions. The separate reviewer also ran reconstructed sources successfully before this snapshot was copied. These are automated assertions, not player samples or exhaustive proof.

Representative tests: schema and dependency validation; no empty slot pools; effective stat-domain bounds; generation reuse; scoped/stale/duplicate commands; pause/reset; wallet conservation and stock/ownership caps; consumable duration1/10/15 seconds; all five mechanisms and mixed traits; limited ricochet, stationary independent companions, bounded impact; replay; six-wave progression using controlled fixtures that remove enemies. Real playthrough and balance are not proven by these fixtures.

Warm300tick controlled CPU measurements in this snapshot run:

| Setup | Time | Managed allocation | Cosmetic drops |
| --- | ---: | ---: | ---: |
| Honey32 enemies |3.88ms|0 bytes|0|
| Honey128 |12.85ms|0 bytes|0|
| Honey512 |56.86ms|0 bytes|0|
| Mixed traits128 |21.64ms|0 bytes|2895|

The bounded2048-event buffer keeps first events and drops newest cosmetic events. This stress loss is disclosed, not a green claim for every visual event. Authoritative damage/economy do not depend on the cosmetic stream. Impact pair work remains quadratic. These microbenchmarks exclude rendering and mobile devices.

Unrun for this milestone: native Godot UI, final exported gameplay package, Android device, hardware GPU, Unity/团结, IL2CPP/AOT, cross-platform bitwise replay. Historical pre-reset tests and exports do not certify this reconstruction.

See adjacent actual output and SHA256 source manifest. Publication is separately verified by the publisher; no remote commit SHA is fabricated here.
