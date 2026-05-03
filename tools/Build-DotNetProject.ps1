param(
    [string]$ProjectPath = "BellRinger.Runtime.csproj",
    [switch]$Restore
)

$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
. (Join-Path $PSScriptRoot "Use-ValidationTools.ps1")

$resolvedProjectPath = Resolve-Path (Join-Path $projectRoot $ProjectPath)
$arguments = @("build", $resolvedProjectPath.Path)
if (-not $Restore) {
    $arguments += "--no-restore"
}

dotnet @arguments
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
