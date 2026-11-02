param(
    [string]$DllPath = (Join-Path $PSScriptRoot 'work\build\HammerSort.dll'),
    [string]$OutputDir = (Join-Path $PSScriptRoot 'dist')
)
$ErrorActionPreference = 'Stop'
$manifest = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'package\manifest.json') | ConvertFrom-Json
if ($manifest.name -notmatch '^[A-Za-z0-9_]+$' -or $manifest.version_number -notmatch '^\d+\.\d+\.\d+$' -or $manifest.description.Length -gt 250) { throw 'Invalid Thunderstore manifest.' }
if (!(Test-Path -LiteralPath $DllPath)) { throw "Build first: $DllPath" }
$pluginVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo([IO.Path]::GetFullPath($DllPath)).FileVersion
if ($pluginVersion -ne "$($manifest.version_number).0") { throw 'Manifest and DLL versions differ.' }
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
Add-Type -AssemblyName System.Drawing
$iconPath = Join-Path $PSScriptRoot 'package\icon.png'
$bitmap = [Drawing.Bitmap]::new(256,256)
$graphics = [Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
$background = [Drawing.Drawing2D.LinearGradientBrush]::new([Drawing.Rectangle]::new(0,0,256,256),[Drawing.Color]::FromArgb(20,42,35),[Drawing.Color]::FromArgb(11,23,24),[single]90)
$gold = [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(237,194,100))
$green = [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(119,205,143))
$muted = [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(85,103,100))
$handle = [Drawing.Pen]::new([Drawing.Color]::FromArgb(167,116,63),16)
$edge = [Drawing.Pen]::new([Drawing.Color]::FromArgb(61,90,76),2)
$font = [Drawing.Font]::new('Segoe UI',12,[Drawing.FontStyle]::Bold)
try {
    $graphics.FillRectangle($background,0,0,256,256)
    $graphics.DrawRectangle($edge,10,10,235,235)
    $graphics.DrawLine($handle,105,64,72,125)
    $graphics.TranslateTransform(113,57)
    $graphics.RotateTransform(28)
    $graphics.FillRectangle($gold,-39,-18,78,36)
    $graphics.ResetTransform()
    $graphics.FillRectangle($green,142,43,62,12)
    $graphics.FillRectangle($green,142,65,62,12)
    $graphics.FillRectangle($muted,142,87,62,12)
    $graphics.FillRectangle($muted,142,109,62,12)
    $graphics.FillRectangle($green,42,155,172,9)
    $graphics.FillRectangle($green,42,173,172,9)
    $graphics.FillRectangle($muted,42,191,172,9)
    $graphics.DrawString('HAMMER SORT',$font,$gold,[single]64,[single]215)
    $bitmap.Save($iconPath,[Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $graphics.Dispose(); $bitmap.Dispose(); $background.Dispose(); $gold.Dispose(); $green.Dispose(); $muted.Dispose(); $handle.Dispose(); $edge.Dispose(); $font.Dispose()
}
Add-Type -AssemblyName System.IO.Compression.FileSystem
function Write-Zip($Path, $Files) {
    # Replaces only the explicitly named output ZIP, never source directories.
    $stream = [IO.File]::Open($Path,[IO.FileMode]::Create)
    $zip = [IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($file in $Files) {
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,$file.Source,$file.Entry,[IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    }
    finally { $zip.Dispose(); $stream.Dispose() }
}
$files = @(
    @{Source=[IO.Path]::GetFullPath($DllPath); Entry='plugins/HammerSort/HammerSort.dll'},
    @{Source=(Join-Path $PSScriptRoot 'LICENSE'); Entry='LICENSE.txt'}
)
foreach ($name in @('manifest.json','README.md','CHANGELOG.md','icon.png')) {
    $files += @{Source=(Join-Path $PSScriptRoot "package\$name"); Entry=$name}
}
$release = Join-Path $OutputDir "$($manifest.name)-$($manifest.version_number).zip"
Write-Zip $release $files
$sourceFiles = Get-ChildItem -LiteralPath $PSScriptRoot -File -Recurse | ForEach-Object {
    $relative = $_.FullName.Substring($PSScriptRoot.Length + 1).Replace('\','/')
    if ($relative -notmatch '^(bin|obj|work|dist|\.git|\.vs|\.idea)/') { @{Source=$_.FullName; Entry="HammerSort/$relative"} }
}
$sourceZip = Join-Path $OutputDir "$($manifest.name)-$($manifest.version_number)-source.zip"
Write-Zip $sourceZip $sourceFiles
Write-Output "Packaged $release"
Write-Output "Source $sourceZip"
