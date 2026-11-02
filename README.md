# HammerSort

By **ERKEK2000**.

Automatically puts hammer pieces you have enough materials for first. Pieces you cannot afford follow immediately below, in the same original order.

For example, if the game's order is **A, B, C, D**, and you can afford B and D, the result is **B, D, A, C**. Once you can afford A as well, it becomes **A, B, D, C**.

## Features

- Supports Valheim 1.0's build browser and compact hammer grid.
- Keeps the existing order inside both groups; no alphabetical sorting.
- Updates automatically when your inventory changes and when opening or rebuilding the menu.
- Retains the selected piece, search visibility, tabs, favorites and controller navigation.
- Keeps repair/remove actions available.
- Client-side: install on the player's computer. No server installation required.
- Requires only BepInEx. MyLittleUI and Jotunn are not dependencies.

## Install

Install [HammerSort on Thunderstore](https://thunderstore.io/c/valheim/p/ERKEK2000/HammerSort/) with r2modman or Thunderstore Mod Manager. BepInExPack Valheim is the only dependency. Start the game using **Start modded**.

If your manager does not offer local mod import, open its profile folder and copy `plugins/HammerSort/HammerSort.dll` from the ZIP into `BepInEx/plugins/HammerSort/HammerSort.dll`.

For a local build, use **Settings > Profile > Import local mod** in r2modman and select the release ZIP from `dist/`.

If you used the private test build, remove that local copy before installing this release so that only one HammerSort DLL is installed.

## Configuration

After the first modded launch, settings are saved to `BepInEx/config/ERKEK2000.valheim.hammersort.cfg`:

| Setting | Default | Behavior |
| --- | --- | --- |
| `General.Enabled` | `true` | Enable automatic sorting. Disabling restores the captured game order. |
| `General.OtherBuildTools` | `false` | Also sort the hoe, cultivator and other tools with build menus. |

Changes made through BepInEx ConfigurationManager apply immediately. When editing the file manually, close the game first.

## What counts as affordable?

The mod checks the quantity of every required material in your inventory. Stack quantities are combined and the game's world-level item restrictions are respected. Zero-cost pieces and no-cost building modes are supported.

Station range, ground suitability, support and placement permission do not affect the order. A piece can be near the top because you have its materials and still need a workbench nearby before placing it. Nearby chest contents are not counted; mods that change resource sourcing need separate integration.

## Performance

Sorting is a stable linear partition, rather than a comparison sort. Inventory quantities are counted once per change and reused across both menus. Buffers are retained, and the browser reuses existing buttons. The unchanged-frame path performs no material scan or sort. Sorting does not instantiate UI objects or run network requests.

## Compatibility and validation

Built against Valheim **1.0.21**, Steam build **26249040**, with BepInExPack Valheim **5.4.2360**. The current 1.0 `PieceTable` layout is used. Earlier game versions are not supported by this build.

The build and automated sorting tests passed. Basic in-game functionality was confirmed by the author on Valheim 1.0.21. MyLittleUI's installed assembly was inspected as a reference for its craftable-first behavior; HammerSort uses its own implementation and separate build-menu hooks. Runtime compatibility with other menu replacements is unverified.

## Quick in-game check

1. Equip a hammer and open the build menu. In any tab, confirm affordable pieces appear first.
2. Pick up, drop or spend a material while the menu is open. Confirm the groups update and retain their original internal order.
3. Keep one piece selected while its affordability changes. Confirm the same piece remains selected.
4. Try search, favorites, the compact grid and controller navigation if used.
5. Disable `Enabled` through ConfigurationManager and confirm the original order returns.

## Development references

- [MyLittleUI source](https://github.com/shudnal/MyLittleUI)
- [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)
- [Thunderstore packaging rules](https://wiki.thunderstore.io/mods/packaging-your-mods)

Licensed under MIT. No game assemblies, BepInEx binaries or third-party mod code are included.

## Source and development

See [BUILDING.md](BUILDING.md) for build instructions and [PUBLISHING.md](PUBLISHING.md) for release steps. Requires a local Valheim installation for game assembly references.
