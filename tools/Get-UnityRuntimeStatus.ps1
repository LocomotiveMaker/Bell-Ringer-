param(
    [string]$StatusPath = "",
    [switch]$Watch
)

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

if ([string]::IsNullOrWhiteSpace($StatusPath)) {
    $StatusPath = Join-Path $projectRoot "Logs\runtime-status.json"
}

function Show-Status {
    if (-not (Test-Path $StatusPath)) {
        Write-Host "Status file not found: $StatusPath"
        return
    }

    Get-Content $StatusPath | Write-Host
}

if ($Watch) {
    while ($true) {
        Clear-Host
        Show-Status
        Start-Sleep -Milliseconds 500
    }
}

Show-Status
