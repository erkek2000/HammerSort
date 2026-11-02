# 1.1.2

- Fixed clicking a sorted build-browser icon selecting a different piece. Restore the backing table before Valheim resolves a prefab to an index, so the subsequent table rebuild keeps that index valid.
- Preserve the existing compact-grid coordinate mapping and placed-piece copying behavior.
- Added regression checks for selection across inventory changes, table rebuilds and grid row boundaries.

# 1.1.1

- Changed the default for General.OtherBuildTools to true, enabling sorting for all build tools on new configs.
- Existing saved config values remain unchanged.

# 1.1.0

- Automatically supports OdinArchitect's Odin's Hammer and Stone Chisel when the optional plugin is loaded and enabled.
- Keeps vanilla hammer sorting available when OdinArchitect is missing or disabled; no new required dependencies.
- Reuses the existing stable sorting, inventory cache and selection handling for both custom menus.
- Restores browser order if optional tool support is disabled while the menu is open.

# 1.0.0

- Initial release for Valheim 1.0.21.
- Stable material-based sorting for the hammer browser and compact grid.
- Inventory-driven updates, retained selection and reusable sorting buffers.
