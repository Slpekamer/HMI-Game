#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
python3 tools/ui/validate_runtime_glyphs.py
dotnet run --project tests/CodexTests/Hmi.CodexTests.csproj -c Release
bash scripts/verify.sh
python3 scripts/test_codex_presentation.py
