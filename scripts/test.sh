#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=/dev/null
source "$ROOT_DIR/.cursor/dotnet-env.sh"

echo "Running .NET diagnostics..."
dotnet --info >/dev/null

echo "Restoring project..."
dotnet restore "ImportDialogApp/ImportDialogApp.csproj" -p:EnableWindowsTargeting=true

echo "Building project..."
dotnet build "ImportDialogApp/ImportDialogApp.csproj" -c Release --no-restore -p:EnableWindowsTargeting=true

echo "Done."
