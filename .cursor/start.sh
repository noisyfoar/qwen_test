#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=/dev/null
source "$ROOT_DIR/.cursor/dotnet-env.sh"

echo "[start] Environment is ready."
dotnet --info >/dev/null 2>&1 && echo "[start] dotnet is available."
