# HammerSort

By **ERKEK2000**.

Less searching, more building. HammerSort automatically puts pieces you can afford first in vanilla and modded build menus. Lightweight, client-side, and ready to build.

For example, if the game's order is **A, B, C, D**, and you can afford B and D, the result is **B, D, A, C**. Once you can afford A as well, it becomes **A, B, D, C**.

## Features

- Supports Valheim 1.0's build browser and compact hammer grid.
- Automatically includes OdinArchitect's hammer and chisel when the optional plugin is active.
- Keeps the existing order inside both groups; no alphabetical sorting.
- Updates automatically when your inventory changes and when opening or rebuilding the menu.
- Retains the selected piece, search visibility, tabs, favorites and controller navigation.
- Keeps repair/remove actions available.
- Client-side: install on the player's computer. No server installation required.
- Requires only BepInEx. MyLittleUI and Jotunn are not dependencies.

## Optional OdinArchitect support

OdinArchitect is detected through BepInEx when the game starts. If it is loaded and enabled, its **Odin's Hammer** and **Stone Chisel** build menus use the same material checks, stable order, automatic updates and selection handling as the vanilla hammer. This works with `OtherBuildTools = false`.

If OdinArchitect is absent or disabled in your mod manager, HammerSort still loads and sorts the vanilla hammer. OdinArchitect is a soft dependency only: installing HammerSort does not install or require it or Jotunn. The optional plugin's active state is checked only for its two tools, without repeated plugin searches or new sorting buffers.

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
| `General.OtherBuildTools` | `true` | Sort all build tools, including the hoe and cultivator. Set false to limit sorting to the vanilla hammer and active OdinArchitect tools. |

Changes made through BepInEx ConfigurationManager apply immediately. When editing the file manually, close the game first. The default applies to newly created settings; existing saved values are preserved. To enable sorting for every build tool in an existing config, set `OtherBuildTools = true`.

## What counts as affordable?

The mod checks the quantity of every required material in your inventory. Stack quantities are combined and the game's world-level item restrictions are respected. Zero-cost pieces and no-cost building modes are supported.

Station range, ground suitability, support and placement permission do not affect the order. A piece can be near the top because you have its materials and still need a workbench nearby before placing it. Nearby chest contents are not counted; mods that change resource sourcing need separate integration.

## Performance

Sorting is a stable linear partition, rather than a comparison sort. Inventory quantities are counted once per change and reused across both menus. Buffers are retained, and the browser reuses existing buttons. The unchanged-frame path performs no material scan or sort. Sorting does not instantiate UI objects or run network requests.

## Compatibility and validation

Built against Valheim **1.0.21**, Steam build **26249040**, with BepInExPack Valheim **5.4.2360**. The current 1.0 `PieceTable` layout is used. Earlier game versions are not supported by this build.

The build, automated sorting tests, optional-tool checks and selection-rebuild regression checks passed. The selection regression reproduces Stone Portal incorrectly becoming Stakewall and checks that the clicked prefab survives Valheim's list rebuild after the fix. The current build still needs an in-game smoke test. The optional integration was checked against the installed OdinArchitect **1.9.4** plugin. MyLittleUI's installed assembly was inspected as a reference for its craftable-first behavior; HammerSort uses its own implementation and separate build-menu hooks. Runtime compatibility with other menu replacements is unverified.

## Quick in-game check

1. Equip a hammer and open the build menu. In any tab, confirm affordable pieces appear first.
2. Pick up, drop or spend a material while the menu is open. Confirm the groups update and retain their original internal order.
3. Keep one piece selected while its affordability changes. Confirm the same piece remains selected.
4. Try search, favorites, the compact grid and controller navigation if used.
5. Disable `Enabled` through ConfigurationManager and confirm the original order returns.
6. With OdinArchitect enabled and `OtherBuildTools = false`, repeat the material-change check using Odin's Hammer and Stone Chisel. Then disable OdinArchitect in the manager, restart, and confirm the vanilla hammer still sorts.

## Development references

- [MyLittleUI source](https://github.com/shudnal/MyLittleUI)
- [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)
- [Thunderstore packaging rules](https://wiki.thunderstore.io/mods/packaging-your-mods)

Licensed under MIT. No game assemblies, BepInEx binaries or third-party mod code are included.

## Source and development

See [BUILDING.md](BUILDING.md) for build instructions and [PUBLISHING.md](PUBLISHING.md) for release steps. Requires a local Valheim installation for game assembly references.
