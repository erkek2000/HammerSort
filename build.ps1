param(
    [string]$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\Valheim',
    [string]$BepInExDir = '',
    [string]$BuildDir = (Join-Path $PSScriptRoot 'work\build'),
    [switch]$Test
)
$ErrorActionPreference = 'Stop'
if (!$BepInExDir) {
    $profiles = Join-Path $env:APPDATA 'r2modmanPlus-local\Valheim\profiles'
    if (Test-Path -LiteralPath $profiles) {
        $BepInExDir = Get-ChildItem -LiteralPath $profiles -Directory |
            ForEach-Object { Join-Path $_.FullName 'BepInEx' } |
            Where-Object { Test-Path -LiteralPath (Join-Path $_ 'core\BepInEx.dll') } |
            Select-Object -First 1
    }
    if (!$BepInExDir) { $BepInExDir = Join-Path $GameDir 'BepInEx' }
}
$managed = Join-Path $GameDir 'valheim_Data\Managed'
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $csc)) { throw 'Install .NET Framework 4.x, or build HammerSort.csproj with the .NET SDK.' }
$references = @(
    (Join-Path $managed 'mscorlib.dll'),
    (Join-Path $managed 'System.dll'),
    (Join-Path $managed 'System.Core.dll'),
    (Join-Path $managed 'netstandard.dll'),
    (Join-Path $managed 'UnityEngine.CoreModule.dll'),
    (Join-Path $managed 'UnityEngine.dll'),
    (Join-Path $managed 'UnityEngine.UI.dll'),
    (Join-Path $managed 'assembly_valheim.dll'),
    (Join-Path $BepInExDir 'core\BepInEx.dll'),
    (Join-Path $BepInExDir 'core\0Harmony.dll')
)
foreach ($reference in $references) {
    if (!(Test-Path -LiteralPath $reference)) { throw "Missing build reference: $reference" }
}
New-Item -ItemType Directory -Force -Path $BuildDir | Out-Null
$compilerArgs = @('/nologo', '/noconfig', '/target:library', '/optimize+', '/debug-', '/nostdlib+', '/warnaserror+',
    "/out:$(Join-Path $BuildDir 'HammerSort.dll')")
$compilerArgs += $references | ForEach-Object { "/reference:$_" }
$compilerArgs += Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' | ForEach-Object FullName
& $csc @compilerArgs
if ($LASTEXITCODE -ne 0) { throw "Compilation failed ($LASTEXITCODE)." }
Write-Output "Built $(Join-Path $BuildDir 'HammerSort.dll')"
if ($Test) {
    $testExe = Join-Path $BuildDir 'HammerSort.Tests.exe'
    & $csc /nologo /target:exe /optimize+ /warnaserror+ "/out:$testExe" `
        (Join-Path $PSScriptRoot 'src\StableOrder.cs') (Join-Path $PSScriptRoot 'tests\StableOrderTests.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
    & $testExe
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
}
