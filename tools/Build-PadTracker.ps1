param(
    [switch]$Restore
)

$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
. (Join-Path $PSScriptRoot "Use-ValidationTools.ps1")

$projectPath = Resolve-Path (Join-Path $projectRoot "tools\PadTracker\BellRinger.PadTracker.csproj")
$nugetConfigPath = Resolve-Path (Join-Path $projectRoot "NuGet.config")
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
