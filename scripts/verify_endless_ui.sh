#!/usr/bin/env bash
set -euo pipefail
bash scripts/verify_codex.sh
python3 scripts/test_endless_presentation.py
