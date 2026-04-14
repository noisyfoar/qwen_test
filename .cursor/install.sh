#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=/dev/null
source "$ROOT_DIR/.cursor/dotnet-env.sh"

if ! command -v dotnet >/dev/null 2>&1; then
  INSTALL_SCRIPT="$(mktemp)"
  curl -fsSL "https://dot.net/v1/dotnet-install.sh" -o "$INSTALL_SCRIPT"
  bash "$INSTALL_SCRIPT" --channel "8.0" --install-dir "$HOME/.dotnet"
  rm -f "$INSTALL_SCRIPT"

  # shellcheck source=/dev/null
  source "$ROOT_DIR/.cursor/dotnet-env.sh"
fi

dotnet --info >/dev/null
