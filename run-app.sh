#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

"${SCRIPT_DIR}/setup-env.sh"

export PATH="${HOME}/.dotnet:${PATH}"

echo "Building application..."
dotnet build "${SCRIPT_DIR}/ImportDialogApp.csproj" -v minimal

if [[ "${1:-}" == "--build-only" ]]; then
  echo "Build-only mode finished."
  exit 0
fi

if [[ "$(uname -s)" != "Linux" ]]; then
  echo "Starting application..."
  dotnet run --project "${SCRIPT_DIR}/ImportDialogApp.csproj" --no-build
  exit 0
fi

echo "WindowsDesktop runtime is unavailable on Linux."
echo "Build completed successfully; run this project on Windows to open the WPF window."
exit 0
