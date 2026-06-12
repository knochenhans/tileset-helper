#!/usr/bin/env bash
#
# Build the tileset-helper editor extension and optionally deploy it into a project.
#
# After you pull a new version of the source, run this to rebuild the native library.
# SCons writes the build output into the repo's bin folder. Pass a destination to also
# copy the runtime files (the .dll/.so and the .gdextension) into your project's bin.
#
# This is an editor-only plugin, so the default target is template_debug - the Godot
# editor loads that build. When deploying, the script first clears any "~" reload shadow
# in the destination so the editor picks up the new build on the next project reload.
#
# Usage:
#   ./update.sh                                  # build template_debug (no deploy)
#   ./update.sh --target template_release
#   ./update.sh --target all                     # build debug and release
#   ./update.sh --target all --dest /path/to/MyGame/bin
#
set -euo pipefail

TARGET="template_debug"
DEST=""

while [[ $# -gt 0 ]]; do
	case "$1" in
		--target) TARGET="$2"; shift 2 ;;
		--dest)   DEST="$2";   shift 2 ;;
		-h|--help) sed -n '2,18p' "$0"; exit 0 ;;
		*) echo "Unknown argument: $1" >&2; exit 1 ;;
	esac
done

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
SOURCE_BIN="$REPO_ROOT/bin"

# Pick the platform SCons expects from the host OS. Git Bash / MSYS / Cygwin on
# Windows report MINGW*/MSYS*/CYGWIN* from uname and build the windows target.
case "$(uname -s)" in
	Linux*)                   PLATFORM="linux" ;;
	Darwin*)                  PLATFORM="macos" ;;
	MINGW*|MSYS*|CYGWIN*)     PLATFORM="windows" ;;
	*)                        PLATFORM="linux" ;;
esac

if [[ "$TARGET" == "all" ]]; then
	TARGETS=("template_debug" "template_release")
else
	TARGETS=("$TARGET")
fi

for t in "${TARGETS[@]}"; do
	echo "==> Building platform=$PLATFORM target=$t"
	( cd "$REPO_ROOT" && scons "platform=$PLATFORM" "target=$t" )
done

if [[ -n "$DEST" ]]; then
	mkdir -p "$DEST"
	echo "==> Deploying runtime files to $DEST"

	# Clear the editor's reload shadow copies so the new build is picked up.
	find "$DEST" -maxdepth 1 -type f -name '~libtileset_helper.*' -delete 2>/dev/null || true

	# Native libraries (build output).
	find "$SOURCE_BIN" -maxdepth 1 -type f \
		\( -name 'libtileset_helper.*.so' -o -name 'libtileset_helper.*.dll' \
		   -o -name 'libtileset_helper.*.dylib' \) \
		-exec cp -f {} "$DEST"/ \;

	# The extension descriptor (source of truth lives in the repo's bin/).
	find "$REPO_ROOT/bin" -maxdepth 1 -type f -name 'tileset_helper.gdextension*' \
		-exec cp -f {} "$DEST"/ \; 2>/dev/null || true

	echo "Done. Reload the project in the Godot editor to pick up the new build."
fi
