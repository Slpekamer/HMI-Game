# Portable simulation boundary

The Core is the engine-neutral fixed-tick gameplay authority. Data-only state and generation-safe entity identities are advanced by explicitly ordered systems; Godot types and storage I/O stay in adapters.

Current rule and architecture contracts are summarized in [architecture](../../docs/public/architecture.md) and [projectile rule 3](../../docs/public/projectile-rule3.md). See [validation status](../../docs/public/validation-status.md) for the scope of current regression results and unresolved limitations.

Build with `dotnet build src/Core/Hmi.Core.csproj`. Run the independent regression project with `dotnet run --project tests/CoreTests/Hmi.CoreTests.csproj -c Release`. Historical test totals do not substitute for running the current source.
