# Publish HammerSort as ERKEK2000

1. Sign in to [Thunderstore's upload page](https://thunderstore.io/package/create/).
2. Use **Create team** if necessary and create a team named **ERKEK2000**, subject to availability. Select **ERKEK2000** in the upload form's **Team** field; this determines the author/namespace shown on Thunderstore and in mod managers.
3. Upload **HammerSort-1.1.0.zip** from `dist/`. The source ZIP is for development and is not the mod upload package.
4. Select **Valheim** under **Communities**.
5. Choose the relevant available categories, such as **Client-side**, **User Interface** and **Quality of Life**, if offered. Select **No** for NSFW.
6. Review the package details and click **Submit**.

The ZIP already includes the required manifest, 256x256 PNG icon, README, changelog, license and plugin DLL. The package name is **HammerSort** and this release is **1.1.0**, adding optional OdinArchitect support. The website URL points to https://github.com/erkek2000/HammerSort. Upload it under the existing **ERKEK2000** team to update the existing listing. Thunderstore will not overwrite already published versions.

The README, DLL metadata, project author, license and BepInEx plugin ID use **ERKEK2000**. A package-level `author` field is not needed in `manifest.json`.

When installing the public release locally, remove the private test copy first so only one HammerSort DLL loads. The release's config is `BepInEx/config/ERKEK2000.valheim.hammersort.cfg`.

For future public updates, keep the **ERKEK2000** team and **HammerSort** package name, increase the version in the plugin source, AssemblyInfo, project file and manifest, rebuild, then upload again. Published versions cannot be overwritten.

References: [upload form](https://thunderstore.io/package/create/), [package specification](https://wiki.thunderstore.io/mods/creating-a-package), [updating packages](https://wiki.thunderstore.io/mods/updating-a-package).
