param(
    [string]$Fqbn = "arduino:avr:uno",
    [string]$SketchPath = ""
)

$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
. (Join-Path $PSScriptRoot "Use-ValidationTools.ps1")

function Get-ShortPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    $fullPath = [IO.Path]::GetFullPath($Path)
    $shortPath = (& cmd.exe /d /c "for %I in (`"$fullPath`") do @echo %~sI").Trim()
    if ([string]::IsNullOrWhiteSpace($shortPath)) {
        return $fullPath
    }

    return $shortPath
}

if ([string]::IsNullOrWhiteSpace($SketchPath)) {
    $SketchPath = Join-Path $projectRoot "arduino\BellRingerSerialTemplate"
}

$buildRoot = Join-Path $projectRoot "tools\.runtime\abuild"
$buildPath = Join-Path $buildRoot ([IO.Path]::GetFileName($SketchPath))
$buildRootFull = [IO.Path]::GetFullPath($buildRoot)
$buildPathFull = [IO.Path]::GetFullPath($buildPath)
if (-not $buildPathFull.StartsWith($buildRootFull, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to clean unexpected Arduino build path: $buildPathFull"
}

if (Test-Path -LiteralPath $buildPathFull) {
    Remove-Item -LiteralPath $buildPathFull -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $buildPathFull | Out-Null
$buildPathCli = Get-ShortPath $buildPathFull
$sketchPathCli = Get-ShortPath (Resolve-Path $SketchPath).Path

arduino-cli compile `
    --config-file $env:BELL_RINGER_ARDUINO_CONFIG `
    --fqbn $Fqbn `
    --build-path $buildPathCli `
    $sketchPathCli
