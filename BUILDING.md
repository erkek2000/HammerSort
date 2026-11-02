# HammerSort source

Author: **ERKEK2000**.

See [the mod README](package/README.md) for behavior, installation, configuration and the in-game check.

## Build on Windows

Uses the installed .NET Framework C# compiler, without downloading an SDK or NuGet packages. Game/BepInEx assemblies are referenced locally and are not distributed.

```powershell
.\build.ps1 -GameDir 'C:\Program Files (x86)\Steam\steamapps\common\Valheim' -BepInExDir 'C:\path\to\r2modman\profile\BepInEx' -Test
.\package.ps1
```

The BepInEx path is auto-detected from r2modman profiles if omitted. Pass `-BuildDir` to choose the compiler output directory. The default is `work/build` relative to this source folder. The build script never modifies the game or mod-manager profiles.

Alternatively, with a .NET SDK:

```powershell
dotnet build HammerSort.csproj -c Release -p:GameDir='C:\path\to\Valheim' -p:BepInExDir='C:\path\to\profile\BepInEx'
.\package.ps1 -DllPath '.\bin\Release\netstandard2.0\HammerSort.dll'
```

`StableOrder.cs` is independent of Unity and has randomized transition, restoration, rebuild and no-change tests. `ToolSelection.cs` is also independent of Unity and is tested with OdinArchitect present, absent and disabled, including live state changes and the vanilla fast path. Game hooks, referenced APIs and the absence of hard OdinArchitect/Jotunn dependencies can be checked using `tests/VerifyApi.ps1`, which reads the installed Mono.Cecil library. This is metadata verification, not an in-game test.

```powershell
.\tests\VerifyApi.ps1 -GameDir 'C:\path\to\Valheim' -BepInExDir 'C:\path\to\profile\BepInEx' -DllPath '.\work\build\HammerSort.dll'
```

For a Thunderstore release, select the publishing team **ERKEK2000**. Thunderstore derives the author/namespace from the selected team; there is no author field in `manifest.json`. Optionally fill in `package/manifest.json`'s `website_url` with your source repository before packaging. The ZIP is structured for local import and upload; it is not published automatically. See [publishing instructions](PUBLISHING.md).

Release ZIPs are written to `dist/`. Local build output stays in `work/`; both directories are excluded from Git and source archives.

`SelectionRebuildTests.cs` models the verified game call order: `Player.SetSelectedPiece(Piece)` finds an index, calls `SetBuildCategory` (which rebuilds the lists), then uses the saved index. It reproduces the Stone Portal/Stakewall mismatch when the first lookup uses a sorted table. Restoring the captured baseline before that lookup keeps the index valid through the rebuild. The tests cover inventory changes, grid row boundaries and copying a placed piece. `VerifyApi.ps1` also verifies that the new prefix targets the Piece overload, leaving compact-grid coordinate selection unchanged.
