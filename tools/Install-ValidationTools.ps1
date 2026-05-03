param(
    [string]$DotNetChannel = "8.0",
    [string]$ArduinoCliUrl = "https://downloads.arduino.cc/arduino-cli/arduino-cli_latest_Windows_64bit.zip"
)

$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$runtimeRoot = Join-Path $projectRoot "tools\.runtime"
$downloadsRoot = Join-Path $runtimeRoot "downloads"
$dotnetRoot = Join-Path $runtimeRoot "dotnet"
$arduinoRoot = Join-Path $runtimeRoot "arduino-cli"
$arduinoDataRoot = Join-Path $runtimeRoot "arduino-data"
$arduinoUserRoot = Join-Path $runtimeRoot "arduino-user"
$arduinoConfigPath = Join-Path $runtimeRoot "arduino-cli.yaml"

New-Item -ItemType Directory -Force -Path $downloadsRoot, $dotnetRoot, $arduinoRoot, $arduinoDataRoot, $arduinoUserRoot | Out-Null

$dotnetInstallScript = Join-Path $downloadsRoot "dotnet-install.ps1"
Invoke-WebRequest -UseBasicParsing -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile $dotnetInstallScript
& $dotnetInstallScript -Channel $DotNetChannel -InstallDir $dotnetRoot -NoPath

$arduinoZipPath = Join-Path $downloadsRoot "arduino-cli.zip"
Invoke-WebRequest -UseBasicParsing -Uri $ArduinoCliUrl -OutFile $arduinoZipPath
Expand-Archive -Path $arduinoZipPath -DestinationPath $arduinoRoot -Force

$arduinoCli = Join-Path $arduinoRoot "arduino-cli.exe"
if (-not (Test-Path $arduinoCli)) {
    $arduinoCli = (Get-ChildItem -Path $arduinoRoot -Recurse -Filter "arduino-cli.exe" | Select-Object -First 1).FullName
}

& $arduinoCli config init --overwrite --dest-file $arduinoConfigPath
& $arduinoCli config set --config-file $arduinoConfigPath directories.data $arduinoDataRoot
& $arduinoCli config set --config-file $arduinoConfigPath directories.downloads (Join-Path $arduinoDataRoot "staging")
& $arduinoCli config set --config-file $arduinoConfigPath directories.user $arduinoUserRoot
& $arduinoCli core update-index --config-file $arduinoConfigPath
& $arduinoCli core install arduino:avr --config-file $arduinoConfigPath
& $arduinoCli lib update-index --config-file $arduinoConfigPath
& $arduinoCli lib install "Adafruit NeoPixel" --config-file $arduinoConfigPath

Write-Host "DOTNET_ROOT=$dotnetRoot"
& (Join-Path $dotnetRoot "dotnet.exe") --info
Write-Host "ARDUINO_CLI=$arduinoCli"
& $arduinoCli version
& $arduinoCli core list --config-file $arduinoConfigPath
& $arduinoCli lib list --config-file $arduinoConfigPath
