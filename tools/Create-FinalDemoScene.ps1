param(
    [string]$UnityVersion = "2022.3.9f1"
)

$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$resolveScript = Join-Path $PSScriptRoot "Resolve-UnityEditor.ps1"
$unityEditorPath = powershell -ExecutionPolicy Bypass -File $resolveScript -UnityVersion $UnityVersion
$logPath = Join-Path $projectRoot "Logs\final-demo-scene-build.log"
$lockPath = Join-Path $projectRoot "Temp\UnityLockfile"

if (Test-Path -LiteralPath $lockPath) {
    throw "Unity project lock file exists at $lockPath. Close this project in Unity before running batch scene generation. If Unity is already closed, remove the stale lock file and retry."
}

New-Item -ItemType Directory -Force -Path (Join-Path $projectRoot "Logs") | Out-Null
Remove-Item -LiteralPath $logPath -Force -ErrorAction SilentlyContinue

$arguments = @(
    "-batchmode",
    "-nographics",
    "-quit",
    "-projectPath",
    $projectRoot,
    "-logFile",
    $logPath,
    "-executeMethod",
    "BellRinger.Debug.Editor.BellRingerFinalDemoSceneBuilder.CreateFinalDemoScene"
)

$process = Start-Process -FilePath $unityEditorPath -ArgumentList $arguments -WorkingDirectory $projectRoot -WindowStyle Hidden -PassThru -Wait
if ($process.ExitCode -ne 0) {
    if (Test-Path $logPath) {
        Get-Content $logPath | Select-Object -Last 80
    }

    throw "Unity scene generation failed with exit code $($process.ExitCode)."
}

Get-Content $logPath | Select-Object -Last 40
