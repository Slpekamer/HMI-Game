# Restore graph silent exit1 diagnosis

Read-only investigation of live source; snapshot isolated here under project/.

- Default dotnet restore HmiGame.csproj -v diag: exit1, no normal error diagnostics; child restore graph stops before Persistence.
- Identical source dotnet restore HmiGame.csproj -m:1 -v minimal: exit0, all3 projects restored.
- MSBuild out-of-process worker failure file preserved under worker-failures/: SocketException(13) Permission denied constructing NamedPipeServerStream in OutOfProcNode.
- This is a restricted execution IPC failure, not evidence of incompatible Godot.NET project references/frameworks.

Minimal safe invocation: dotnet build HmiGame.csproj -m:1. Avoids parallel worker IPC; does not weaken sandbox permissions or disable security. No global setting, SDK change, source modification, or network workaround performed.
