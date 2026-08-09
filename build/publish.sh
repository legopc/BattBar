#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."

dotnet publish src/BattBar.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:InvariantGlobalization=true \
  -p:EnableCompressionInSingleFile=true \
  -p:CompressionLevel=optimal \
  -p:DebugType=none \
  -p:SatelliteResourceLanguages=en \
  -o dist/

echo "Built: dist/BattBar.exe"
ls -lh dist/BattBar.exe