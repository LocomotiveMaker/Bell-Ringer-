param(
    [string]$UnityVersion = "2022.3.9f1",
    [switch]$SkipRuntimeTests,
    [bool]$UseProjectCopy = $true
)

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$logsDirectory = Join-Path $projectRoot "Logs"
$tempDirectory = Join-Path $projectRoot "Temp"
$validationProjectRoot = Join-Path $tempDirectory "UnityCliValidationProject"
$validationLogPath = Join-Path $logsDirectory "unity-cli-validation.log"
$editModeLogPath = Join-Path $logsDirectory "unity-editmode-tests.log"
$editModeResultsPath = Join-Path $logsDirectory "unity-editmode-tests.xml"
$editModeStatusPath = Join-Path $tempDirectory "bellringer-editmode-status.json"
$executionProjectRoot = $projectRoot

New-Item -ItemType Directory -Force -Path $logsDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $tempDirectory | Out-Null

$resolveScript = Join-Path $PSScriptRoot "Resolve-UnityEditor.ps1"
$unityEditorPath = & $resolveScript -UnityVersion $UnityVersion

function Write-Section {
    param([string]$Title)
    Write-Host ""
    Write-Host "[$Title]"
}

function Show-UnityLogSummary {
    param([string]$LogPath)

    if (-not (Test-Path $LogPath)) {
        Write-Host "Log not found: $LogPath"
        return
    }

    $logLines = Get-Content $LogPath
    $errorLines = $logLines | Where-Object { $_ -match "(?i)\berror\b" }
    $warningLines = $logLines | Where-Object { $_ -match "(?i)\bwarning\b" }

    Write-Host "Log: $LogPath"
    Write-Host "Errors: $($errorLines.Count)"
    Write-Host "Warnings: $($warningLines.Count)"

    $tailLines = $logLines | Select-Object -Last 20
    if ($tailLines.Count -gt 0) {
        Write-Host "Tail:"
        $tailLines | ForEach-Object { Write-Host $_ }
    }
}

function Invoke-UnityBatchCommand {
    param(
        [string]$Arguments
    )

    $process = Start-Process `
        -FilePath $unityEditorPath `
        -ArgumentList $Arguments `
        -WorkingDirectory $executionProjectRoot `
        -WindowStyle Hidden `
        -PassThru `
        -Wait

    return $process.ExitCode
}

function Sync-ValidationProjectCopy {
    if (Test-Path $validationProjectRoot) {
        Remove-Item -LiteralPath $validationProjectRoot -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path $validationProjectRoot | Out-Null

    foreach ($folderName in @("Assets", "Packages", "ProjectSettings")) {
        Copy-Item `
            -LiteralPath (Join-Path $projectRoot $folderName) `
            -Destination (Join-Path $validationProjectRoot $folderName) `
            -Recurse `
            -Force
    }

    $script:executionProjectRoot = $validationProjectRoot
}

if ($UseProjectCopy) {
    Write-Section "Validation Copy"
    Sync-ValidationProjectCopy
    Write-Host "Project copy: $executionProjectRoot"
}

Write-Section "Validation"
$validationArguments = "-batchmode -nographics -quit -projectPath `"$executionProjectRoot`" -logFile `"$validationLogPath`" -executeMethod BellRinger.Debug.Editor.BellRingerCli.RunProjectValidation"
$validationExitCode = Invoke-UnityBatchCommand -Arguments $validationArguments
Show-UnityLogSummary -LogPath $validationLogPath

$editModeExitCode = 0

if (-not $SkipRuntimeTests) {
    Write-Section "EditMode Tests"
    $env:BELL_RINGER_SIMULATE_HARDWARE = "1"
    $env:BELL_RINGER_DISABLE_OVERLAY = "1"
    $env:BELL_RINGER_STATUS_PATH = $editModeStatusPath

    $editModeArguments = "-batchmode -nographics -quit -projectPath `"$executionProjectRoot`" -logFile `"$editModeLogPath`" -runTests -testPlatform EditMode -testResults `"$editModeResultsPath`""
    $editModeExitCode = Invoke-UnityBatchCommand -Arguments $editModeArguments

    Remove-Item Env:\BELL_RINGER_SIMULATE_HARDWARE -ErrorAction SilentlyContinue
    Remove-Item Env:\BELL_RINGER_DISABLE_OVERLAY -ErrorAction SilentlyContinue
    Remove-Item Env:\BELL_RINGER_STATUS_PATH -ErrorAction SilentlyContinue

    Show-UnityLogSummary -LogPath $editModeLogPath

    if (Test-Path $editModeStatusPath) {
        Write-Host "Status snapshot: $editModeStatusPath"
        Get-Content $editModeStatusPath | Write-Host
    }

    if (Test-Path $editModeResultsPath) {
        Write-Host "Test results: $editModeResultsPath"
    }
}

if ($validationExitCode -ne 0 -or $editModeExitCode -ne 0) {
    exit 1
}

Write-Host ""
Write-Host "Unity CLI validation completed successfully."
