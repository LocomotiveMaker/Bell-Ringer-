param(
    [string]$SourcePpt = "",
    [string]$OutputPpt = "C:\Bell Ringer\Docs\BellRingerExpoSlide3Refined.pptx",
    [string]$WorkDir = "C:\Bell Ringer\tools\tmp\expo_slide3_work",
    [string]$AssetDir = "C:\Bell Ringer\Docs\ExpoSlide3Assets"
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.IO.Compression.FileSystem

if ([string]::IsNullOrWhiteSpace($SourcePpt)) {
    $candidate = Get-ChildItem -LiteralPath "C:\Bell Ringer\Docs" -File |
        Where-Object {
            $_.Extension -eq ".pptx" -and
            $_.Name -notlike '~$*' -and
            $_.Name -notlike '*Draft*' -and
            $_.Name -notlike '*Refined*' -and
            $_.Name -like '*MD_EXPO_B1.pptx'
        } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($null -eq $candidate) {
        throw "Could not locate the source expo PPTX in C:\Bell Ringer\Docs"
    }

    $SourcePpt = $candidate.FullName
}

$zipPath = Join-Path $WorkDir "source.zip"
$extractDir = Join-Path $WorkDir "ppt"
$scriptPath = "C:\Bell Ringer\tools\ppt-draft\updateBellRingerExpoSlide3.mjs"
$tempZip = Join-Path $WorkDir "rebuilt.zip"

if (Test-Path -LiteralPath $WorkDir) {
    Remove-Item -LiteralPath $WorkDir -Recurse -Force
}

New-Item -ItemType Directory -Path $WorkDir | Out-Null
Copy-Item -LiteralPath $SourcePpt -Destination $zipPath
[System.IO.Compression.ZipFile]::ExtractToDirectory($zipPath, $extractDir)

Push-Location "C:\Bell Ringer\tools\ppt-draft"
try {
    node $scriptPath --pptDir $extractDir --assetDir $AssetDir
}
finally {
    Pop-Location
}

if (Test-Path -LiteralPath $tempZip) {
    Remove-Item -LiteralPath $tempZip -Force
}

[System.IO.Compression.ZipFile]::CreateFromDirectory($extractDir, $tempZip)
Copy-Item -LiteralPath $tempZip -Destination $OutputPpt -Force

Write-Output "Updated PPT written to: $OutputPpt"
Write-Output "Generated assets written to: $AssetDir"
