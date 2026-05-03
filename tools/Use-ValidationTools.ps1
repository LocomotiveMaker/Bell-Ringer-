$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$runtimeRoot = Join-Path $projectRoot "tools\.runtime"
$dotnetRoot = Join-Path $runtimeRoot "dotnet"
$arduinoRoot = Join-Path $runtimeRoot "arduino-cli"
$dotnetHome = Join-Path $runtimeRoot "dotnet-home"
$nugetPackages = Join-Path $runtimeRoot "nuget-packages"

New-Item -ItemType Directory -Force -Path $dotnetHome, $nugetPackages | Out-Null

$env:DOTNET_ROOT = $dotnetRoot
$env:DOTNET_CLI_HOME = $dotnetHome
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:DOTNET_NOLOGO = "1"
$env:NUGET_PACKAGES = $nugetPackages
$env:PATH = "$dotnetRoot;$arduinoRoot;$env:PATH"
$env:BELL_RINGER_ARDUINO_CONFIG = Join-Path $runtimeRoot "arduino-cli.yaml"

Write-Host "DOTNET_ROOT=$env:DOTNET_ROOT"
Write-Host "DOTNET_CLI_HOME=$env:DOTNET_CLI_HOME"
Write-Host "ARDUINO_CONFIG=$env:BELL_RINGER_ARDUINO_CONFIG"
