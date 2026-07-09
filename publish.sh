#!/usr/bin/env bash
# Builds the shippable single-file SymbolCommander.exe (run on Linux or Windows).
set -euo pipefail
cd "$(dirname "$0")"
export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$DOTNET_ROOT:$PATH"

# Target the test project explicitly: a bare `dotnet test` on the solution discovers
# nothing here (the net8.0-windows WPF project derails solution-level test discovery).
dotnet test tests/SymbolCommander.Core.Tests
dotnet publish src/SymbolCommander.App -c Release -r win-x64 --self-contained \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

OUT="src/SymbolCommander.App/bin/Release/net8.0-windows/win-x64/publish/SymbolCommander.exe"
echo
echo "Built: $OUT ($(du -h "$OUT" | cut -f1))"
