#!/usr/bin/env bash
#
# Assemble the redistributable addon under dist/addons/tileset_helper.
#
# Builds the Windows debug and release libraries, then stages everything an end
# user needs (the two .dll files, a distribution .gdextension that points at the
# addons path, the README and the LICENSE) under dist/addons/tileset_helper.
# Zip the dist/addons folder for the Godot Asset Library.
#
# Usage:
#   ./package.sh                  # build both targets and assemble
#   ./package.sh --skip-build     # assemble from existing builds in bin/
#   ./package.sh --zip            # also write dist/tileset_helper-windows.zip
#
set -euo pipefail

SKIP_BUILD=0
ZIP=0
while [[ $# -gt 0 ]]; do
	case "$1" in
		--skip-build) SKIP_BUILD=1; shift ;;
		--zip)        ZIP=1; shift ;;
		-h|--help)    sed -n '2,14p' "$0"; exit 0 ;;
		*) echo "Unknown argument: $1" >&2; exit 1 ;;
	esac
done

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
BIN="$REPO_ROOT/bin"
PKG="$REPO_ROOT/dist/addons/tileset_helper"
PKG_BIN="$PKG/bin"

DEBUG_DLL="$BIN/libtileset_helper.windows.template_debug.x86_64.dll"
RELEASE_DLL="$BIN/libtileset_helper.windows.template_release.x86_64.dll"

if [[ "$SKIP_BUILD" -eq 0 ]]; then
	"$SCRIPT_DIR/update.sh" --target all
fi

for f in "$DEBUG_DLL" "$RELEASE_DLL"; do
	[[ -f "$f" ]] || { echo "Missing $(basename "$f") - build first (omit --skip-build)." >&2; exit 1; }
done

# Clean and recreate the package tree.
rm -rf "$PKG"
mkdir -p "$PKG_BIN"

echo "==> Staging addon to $PKG"
cp -f "$DEBUG_DLL" "$RELEASE_DLL" "$PKG_BIN"/

# Distribution descriptor: addons path, Windows-only, requires Godot 4.6+. Editor-only:
# the `editor` feature tag is present only in the running editor, never in an exported
# game, so this never gets bundled into a downstream user's build.
cat > "$PKG/tileset_helper.gdextension" <<'EOF'
[configuration]

entry_symbol = "tileset_helper_library_init"
compatibility_minimum = "4.6"
reloadable = true

[libraries]

windows.editor.x86_64 = "res://addons/tileset_helper/bin/libtileset_helper.windows.template_debug.x86_64.dll"
EOF

# README and LICENSE (whatever extension the license uses).
[[ -f "$REPO_ROOT/README.md" ]] && cp -f "$REPO_ROOT/README.md" "$PKG"/
find "$REPO_ROOT" -maxdepth 1 -type f -name 'LICENSE*' -exec cp -f {} "$PKG"/ \;

if [[ "$ZIP" -eq 1 ]]; then
	ZIP_PATH="$REPO_ROOT/dist/tileset_helper-windows.zip"
	rm -f "$ZIP_PATH"
	if command -v zip >/dev/null 2>&1; then
		( cd "$REPO_ROOT/dist" && zip -rq "$ZIP_PATH" addons )
		echo "==> Wrote $ZIP_PATH"
	else
		echo "WARNING: 'zip' not found; skipped archive. Zip the dist/addons folder manually." >&2
	fi
fi

echo "Done. Package contents:"
find "$PKG" -type f | sed "s#^$REPO_ROOT/#  #"
