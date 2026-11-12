#!/usr/bin/env bash
set -euo pipefail
variant="${1:-12}"
case "$variant" in
  12) tfm=net10.0; property=12 ;;
  10.11) tfm=net9.0; property=10.11 ;;
  *) echo 'Usage: bash scripts/build.sh [12|10.11]' >&2; exit 2 ;;
esac
base="$(cd "$(dirname "$0")/.." && pwd)"
mkdir -p "$base/dist/jf$variant"
dotnet publish "$base/src/EmberEDL.csproj" -c Release -f "$tfm" -p:JellyfinVariant="$property" -o "$base/dist/jf$variant"
find "$base/dist/jf$variant" -maxdepth 1 -name '*.dll' ! -name 'Jellyfin.Plugin.EmberEDL.dll' -delete
echo "Built: $base/dist/jf$variant/Jellyfin.Plugin.EmberEDL.dll"
