# TileSet Helper

A free editor-only Godot 4 GDExtension that adds copy/paste of tile collision and navigation shapes to the built-in TileSet editor. Authoring shapes tile-by-tile is slow; this makes it fast, especially across recoloured copies of the same tile sheet.

It adds nothing at runtime and ships nothing into your game - it only changes the editor.

[Get it pre-compiled on the Godot Asset Store for free](https://store.godotengine.org/asset/fogspire-games/tileset-helper/)

## What it adds

**Copy / Paste a shape.** In a tile's collision or navigation shape editor, the advanced (`...`) menu gains **Copy Shape(s)** and **Paste Shape(s)**. Copy grabs the shapes you see; paste drops them onto another tile's shape editor. The clipboard is just points, so you can copy a physics shape and paste it as navigation, or the reverse.

**Hotkeys.** Hover any shape editor and press **Ctrl+C** / **Ctrl+V**. Same as the menu items, without opening the menu.

**Copy across sources.** The Tile Sources advanced (`...`) menu gains **Copy Physics/Navigation to Other Sources**. Pick a template source and one or more targets, toggle Physics / Navigation, and it copies every tile's shapes from the template onto the targets, matched by atlas coordinates. One undo step. Built for propagating one finished atlas onto its recoloured twins.

**Copy between layers.** The same menu gains **Copy Between Physics/Navigation**. Pick a from-layer and a to-layer (any physics or navigation layer) and it copies shapes from one to the other on every tile of every source. Physics-to-navigation bakes a navigation polygon from the collision shapes; navigation-to-physics does the reverse. One undo step.

<img width="522" height="492" alt="image" src="https://github.com/user-attachments/assets/f67d7d99-a14c-4d08-b02b-80e483bb1ece" />

<img width="493" height="294" alt="image" src="https://github.com/user-attachments/assets/99633d5a-88a1-470c-a734-4f1cfa51a4a3" />

<img width="465" height="291" alt="image" src="https://github.com/user-attachments/assets/5c51a824-13de-409b-9feb-2f0e29c1a1ec" />

## Build

Requires `godot-cpp` checked out as a sibling folder (`../godot-cpp`) and SCons.

```
scons platform=windows target=template_debug
```

The editor loads the `template_debug` build, so that is all you need.

## Install / update

Copy `bin/libtileset_helper.*.dll` and `bin/tileset_helper.gdextension` into your project's `bin/` folder, then **Project > Reload Current Project**.

Or use the helper script, which builds and deploys in one step (and clears the editor's reload shadow so the new build is picked up):

```
scripts/update.ps1 -Dest "C:\MyGame\bin"      # Windows
scripts/update.sh  --dest /path/to/MyGame/bin # Linux / macOS
```

## Caveat

It works by walking the editor's node tree to find the TileSet editor's menus, so it is tied to the editor layout of the Godot version it was built against (4.6). Lookups are defensive and idempotent: if a future Godot moves things, the menu items simply do not appear - nothing breaks.

## Changelog

### 1.0.0
* Initial Release
