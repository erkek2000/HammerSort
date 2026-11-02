param(
    [Parameter(Mandatory=$true)][string]$GameDir,
    [Parameter(Mandatory=$true)][string]$BepInExDir,
    [Parameter(Mandatory=$true)][string]$DllPath
)
$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $BepInExDir 'core\Mono.Cecil.dll')
$resolver = [Mono.Cecil.DefaultAssemblyResolver]::new()
$resolver.AddSearchDirectory((Join-Path $GameDir 'valheim_Data\Managed'))
$resolver.AddSearchDirectory((Join-Path $BepInExDir 'core'))
$resolver.AddSearchDirectory((Split-Path -Parent ([IO.Path]::GetFullPath($DllPath))))
$parameters = [Mono.Cecil.ReaderParameters]::new()
$parameters.AssemblyResolver = $resolver
$plugin = [Mono.Cecil.AssemblyDefinition]::ReadAssembly([IO.Path]::GetFullPath($DllPath), $parameters)
$optionalPluginGuid = 'Raelaziel.OdinArchitect'
if (@($plugin.MainModule.AssemblyReferences | Where-Object { $_.Name -in @('OdinArchitect','Jotunn') }).Count -ne 0) {
    throw 'Optional compatibility introduced a required mod assembly reference'
}
$pluginType = $plugin.MainModule.Types | Where-Object FullName -eq 'HammerSort.HammerSortPlugin'
$odinDependency = @($pluginType.CustomAttributes | Where-Object {
    $_.AttributeType.FullName -eq 'BepInEx.BepInDependency' -and $_.ConstructorArguments[0].Value -eq $optionalPluginGuid
})
if ($odinDependency.Count -ne 1 -or [int]$odinDependency[0].ConstructorArguments[1].Value -ne 2) {
    throw 'OdinArchitect must be a soft dependency'
}
Write-Output 'PASS: OdinArchitect is optional; no OdinArchitect or Jotunn assembly dependency.'
function Get-Types($types) {
    foreach ($type in $types) { $type; Get-Types $type.NestedTypes }
}
$checked = 0
$patches = 0
foreach ($reference in $plugin.MainModule.GetMemberReferences()) {
    if ($reference -is [Mono.Cecil.MethodReference] -or $reference -is [Mono.Cecil.FieldReference]) {
        if ($null -eq $reference.Resolve()) { throw "Unresolved member: $reference" }
        ++$checked
    }
}
foreach ($type in (Get-Types $plugin.MainModule.Types)) {
    $patch = $type.CustomAttributes | Where-Object { $_.AttributeType.FullName -eq 'HarmonyLib.HarmonyPatch' }
    if (!$patch) { continue }
    $targetType = $patch.ConstructorArguments[0].Value.Resolve()
    $name = $patch.ConstructorArguments[1].Value
    $targets = @($targetType.Methods | Where-Object Name -eq $name)
    if ($targets.Count -ne 1) { throw "Missing/ambiguous patch target: $($targetType.FullName).$name" }
    $target = $targets[0]
    foreach ($method in $type.Methods) {
        foreach ($parameter in $method.Parameters) {
            $parameterName = $parameter.Name
            if ($parameterName -eq '__instance') {
                if ($parameter.ParameterType.FullName -ne $targetType.FullName) { throw 'Invalid __instance type' }
            }
            elseif ($parameterName.StartsWith('___')) {
                $fieldName = $parameterName.Substring(3)
                $field = $targetType.Fields | Where-Object Name -eq $fieldName
                if (!$field -or $field.FieldType.FullName -ne $parameter.ParameterType.FullName) { throw "Invalid injected field: $fieldName" }
            }
            elseif ($parameterName -match '^__(\d+)$') {
                $index = [int]$Matches[1]
                $actualType = $parameter.ParameterType.FullName.TrimEnd('&')
                if ($index -ge $target.Parameters.Count -or $target.Parameters[$index].ParameterType.FullName -ne $actualType) { throw "Invalid indexed parameter: $parameterName" }
            }
            elseif (!$parameterName.StartsWith('__')) {
                $argument = $target.Parameters | Where-Object Name -eq $parameterName
                if (!$argument -or $argument.ParameterType.FullName -ne $parameter.ParameterType.FullName) { throw "Invalid named parameter: $parameterName" }
            }
        }
    }
    ++$patches
    Write-Output "PATCH OK: $($target.FullName)"
}
$game = $resolver.Resolve([Mono.Cecil.AssemblyNameReference]::new('assembly_valheim', [version]'0.0.0.0'))
foreach ($entry in @(
    @{Type='Humanoid'; Name='GetRightItem'; Return='ItemDrop/ItemData'},
    @{Type='BuildUi'; Name='ConfigureButtonNavigation'; Return='System.Void'}
)) {
    $type = $game.MainModule.Types | Where-Object Name -eq $entry.Type
    $methods = @($type.Methods | Where-Object Name -eq $entry.Name)
    if ($methods.Count -ne 1 -or $methods[0].IsStatic -or $methods[0].Parameters.Count -ne 0 -or $methods[0].ReturnType.FullName -ne $entry.Return) { throw "Invalid cached delegate: $($entry.Name)" }
}
$pieceTable = $game.MainModule.Types | Where-Object Name -eq 'PieceTable'
$field = $pieceTable.Fields | Where-Object Name -eq 'm_availablePiecesByCategory'
if (!$field -or $field.FieldType.FullName -ne 'System.Collections.Generic.List`1<System.Collections.Generic.List`1<Piece>>') { throw 'Unexpected PieceTable layout' }
Write-Output "PASS: $checked member references, $patches Harmony targets, injected parameters, cached delegates and PieceTable layout."
$plugin.Dispose()
$resolver.Dispose()
