param(
    [string]$UnityVersion = "2022.3.9f1",
    [switch]$UseProjectCopy = $true
)

$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$logsDirectory = Join-Path $projectRoot "Logs"
$tempDirectory = Join-Path $projectRoot "Temp"
$captureDirectory = Join-Path $tempDirectory "ObserverVisualCaptures"
$captureProjectRoot = Join-Path $tempDirectory "UnityObserverCaptureProject"
$logPath = Join-Path $logsDirectory "observer-display-capture.log"
$resolveScript = Join-Path $PSScriptRoot "Resolve-UnityEditor.ps1"
$unityEditorPath = powershell -ExecutionPolicy Bypass -File $resolveScript -UnityVersion $UnityVersion
$executionProjectRoot = $projectRoot

New-Item -ItemType Directory -Force -Path $logsDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $tempDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $captureDirectory | Out-Null
Remove-Item -LiteralPath $logPath -Force -ErrorAction SilentlyContinue

if ($UseProjectCopy) {
    New-Item -ItemType Directory -Force -Path $captureProjectRoot | Out-Null
    New-Item -ItemType Directory -Force -Path (Join-Path $captureProjectRoot "Assets") | Out-Null

    foreach ($folderName in @("Packages", "ProjectSettings")) {
        Copy-Item `
            -LiteralPath (Join-Path $projectRoot $folderName) `
            -Destination (Join-Path $captureProjectRoot $folderName) `
            -Recurse `
            -Force
    }

    $captureScriptsPath = Join-Path $captureProjectRoot "Assets\Scripts"
    if (Test-Path $captureScriptsPath) {
        Remove-Item -LiteralPath $captureScriptsPath -Recurse -Force
    }

    Copy-Item `
        -LiteralPath (Join-Path $projectRoot "Assets\Scripts") `
        -Destination (Join-Path $captureProjectRoot "Assets") `
        -Recurse `
        -Force

    New-Item -ItemType Directory -Force -Path (Join-Path $captureProjectRoot "Assets\Resources") | Out-Null
    $captureObserverAssetsPath = Join-Path $captureProjectRoot "Assets\Resources\ObserverAssets"
    if (Test-Path $captureObserverAssetsPath) {
        Remove-Item -LiteralPath $captureObserverAssetsPath -Recurse -Force
    }

    Copy-Item `
        -LiteralPath (Join-Path $projectRoot "Assets\Resources\ObserverAssets") `
        -Destination (Join-Path $captureProjectRoot "Assets\Resources") `
        -Recurse `
        -Force

    $executionProjectRoot = $captureProjectRoot
}

$env:BELL_RINGER_OBSERVER_CAPTURE_DIR = $captureDirectory
$arguments = "-batchmode -quit -projectPath `"$executionProjectRoot`" -logFile `"$logPath`" -executeMethod BellRinger.Debug.Editor.ObserverDisplayCaptureTool.CaptureAll"

$process = Start-Process -FilePath $unityEditorPath -ArgumentList $arguments -WorkingDirectory $executionProjectRoot -WindowStyle Hidden -PassThru -Wait
Remove-Item Env:\BELL_RINGER_OBSERVER_CAPTURE_DIR -ErrorAction SilentlyContinue

if ($process.ExitCode -ne 0) {
    if (Test-Path $logPath) {
        Get-Content $logPath | Select-Object -Last 120
    }

    throw "Observer display capture failed with exit code $($process.ExitCode)."
}

Get-ChildItem -LiteralPath $captureDirectory -Filter "*.png" | Select-Object FullName,Length,LastWriteTime
if (Test-Path $logPath) {
    Get-Content $logPath | Select-Object -Last 60
}
