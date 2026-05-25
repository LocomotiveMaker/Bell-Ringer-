param(
    [switch]$Restore
)

$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
. (Join-Path $PSScriptRoot "Use-ValidationTools.ps1")

$projectFilePath = Join-Path $projectRoot "tools\PadTracker\BellRinger.PadTracker.csproj"
$nugetConfigFilePath = Join-Path $projectRoot "NuGet.config"

if (-not (Test-Path $projectFilePath)) {
    throw "Pad tracker project file not found at '$projectFilePath'."
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet was not found. Install .NET 8 SDK before rebuilding the pad tracker."
}

$projectPath = Resolve-Path $projectFilePath
$nugetConfigPath = Resolve-Path $nugetConfigFilePath
$arguments = @("build", $projectPath.Path)
if (-not $Restore) {
    $arguments += "--no-restore"
}
$arguments += "--configfile"
$arguments += $nugetConfigPath.Path

dotnet @arguments
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
