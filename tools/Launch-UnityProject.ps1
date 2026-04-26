param(
    [string]$UnityVersion = "2022.3.9f1",
    [string]$SerialPort = "COM9",
    [int]$BaudRate = 115200,
    [switch]$SimulateHardware,
    [string]$StatusPath = ""
)

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$resolveScript = Join-Path $PSScriptRoot "Resolve-UnityEditor.ps1"
$unityEditorPath = & $resolveScript -UnityVersion $UnityVersion

if ([string]::IsNullOrWhiteSpace($StatusPath)) {
    $StatusPath = Join-Path $projectRoot "Logs\runtime-status.json"
}

if (-not [string]::IsNullOrWhiteSpace($SerialPort)) {
    $env:BELL_RINGER_SERIAL_PORT = $SerialPort
}

$env:BELL_RINGER_SERIAL_BAUD = $BaudRate.ToString()
$env:BELL_RINGER_STATUS_PATH = $StatusPath

if ($SimulateHardware) {
    $env:BELL_RINGER_SIMULATE_HARDWARE = "1"
}

Start-Process -FilePath $unityEditorPath -ArgumentList @("-projectPath", $projectRoot) -WorkingDirectory $projectRoot
