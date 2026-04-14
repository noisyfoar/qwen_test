#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DOTNET_DIR="${HOME}/.dotnet"
INSTALL_SCRIPT="/tmp/dotnet-install.sh"

if [[ ! -x "${DOTNET_DIR}/dotnet" ]]; then
  echo "Installing .NET SDK 8 into ${DOTNET_DIR}..."
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o "${INSTALL_SCRIPT}"
  bash "${INSTALL_SCRIPT}" --channel 8.0 --install-dir "${DOTNET_DIR}"
fi

export PATH="${DOTNET_DIR}:${PATH}"

echo "Restoring project..."
dotnet restore "${SCRIPT_DIR}/ImportDialogApp.csproj"

echo "Environment is ready."
