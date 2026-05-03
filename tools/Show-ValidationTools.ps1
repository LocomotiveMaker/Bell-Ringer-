$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "Use-ValidationTools.ps1")

dotnet --info
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

arduino-cli --config-file $env:BELL_RINGER_ARDUINO_CONFIG version
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

arduino-cli --config-file $env:BELL_RINGER_ARDUINO_CONFIG core list
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

arduino-cli --config-file $env:BELL_RINGER_ARDUINO_CONFIG lib list
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
