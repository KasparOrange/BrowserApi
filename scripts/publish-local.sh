#!/usr/bin/env bash
set -euo pipefail

# ─── Config ──────────────────────────────────────────────────────────
SERVER="mitware@146.0.32.204"
REMOTE_NUGET_DIR="/home/mitware/projects/MitWare/tools/nupkg"
REMOTE_NUGET_DIR_DEV="/home/mitware/projects/MitWare-dev/tools/nupkg"
# ─────────────────────────────────────────────────────────────────────

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
NUPKG_DIR="$REPO_ROOT/nupkgs"
PROJECT="$REPO_ROOT/src/BrowserApi.SourceGen/BrowserApi.SourceGen.csproj"

# Version: read from arg or default to timestamp-based prerelease
VERSION="${1:-0.1.0-local.$(date +%Y%m%d%H%M%S)}"

echo "==> Packing BrowserApi.SourceGen v$VERSION"
rm -f "$NUPKG_DIR"/*.nupkg
dotnet pack "$PROJECT" -c Release -o "$NUPKG_DIR" -p:Version="$VERSION" --nologo -v quiet

NUPKG="$NUPKG_DIR/BrowserApi.SourceGen.$VERSION.nupkg"
if [ ! -f "$NUPKG" ]; then
    echo "ERROR: Package not found at $NUPKG"
    exit 1
fi

echo "==> Copying to $SERVER (main + dev)"
scp "$NUPKG" "$SERVER:$REMOTE_NUGET_DIR/"
scp "$NUPKG" "$SERVER:$REMOTE_NUGET_DIR_DEV/"

echo ""
echo "==> Done. Version: $VERSION"
echo "    MitWare.Blazor.csproj should reference:"
echo ""
echo "    <PackageReference Include=\"BrowserApi.SourceGen\" Version=\"$VERSION\">"
