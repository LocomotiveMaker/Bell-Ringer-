param(
    [int]$CameraIndex = 0,
    [int]$Width = 1280,
    [int]$Height = 720,
    [int]$Fps = 60,
    [string]$UdpHost = "127.0.0.1",
    [int]$UdpPort = 39051,
    [double]$HorizontalFovDegrees = 68.0,
    [double]$MarkerSizeMm = 50.0,
    [int]$DetectMaxDim = 720,
    [int]$PreviewMaxDim = 1280,
    [ValidateSet('Auto','DSHOW','MSMF','ANY')]
    [string]$Backend = 'Auto',
    [switch]$NoPreview,
    [switch]$Scan,
    [switch]$Restore,
    [int]$ScanCount = 8
)

$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
. (Join-Path $PSScriptRoot "Use-ValidationTools.ps1")

$projectPath = Resolve-Path (Join-Path $projectRoot "tools\PadTracker\BellRinger.PadTracker.csproj")
$nugetConfigPath = Resolve-Path (Join-Path $projectRoot "NuGet.config")
$builtAppPath = Join-Path $projectRoot "tools\PadTracker\bin\Debug\net8.0\BellRinger.PadTracker.dll"
$hasBuiltApp = Test-Path $builtAppPath

if ($Restore -or -not $hasBuiltApp) {
    $buildArguments = @("build", $projectPath.Path, "--configfile", $nugetConfigPath.Path)
    if (-not $Restore) {
        $buildArguments += "--no-restore"
    }

    dotnet @buildArguments
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

$arguments = @($builtAppPath)

if ($Scan) {
    $arguments += "--scan-cameras"
    $arguments += "--scan-count"
    $arguments += $ScanCount
} else {
    $arguments += "--camera-index"
    $arguments += $CameraIndex
    $arguments += "--width"
    $arguments += $Width
    $arguments += "--height"
    $arguments += $Height
    $arguments += "--fps"
    $arguments += $Fps
    $arguments += "--udp-host"
    $arguments += $UdpHost
    $arguments += "--udp-port"
    $arguments += $UdpPort
    $arguments += "--horizontal-fov-deg"
    $arguments += $HorizontalFovDegrees
    $arguments += "--marker-size-mm"
    $arguments += $MarkerSizeMm
    $arguments += "--detect-max-dim"
    $arguments += $DetectMaxDim
    $arguments += "--preview-max-dim"
    $arguments += $PreviewMaxDim
    if ($Backend -ne 'Auto') {
        $arguments += "--backend"
        $arguments += $Backend.ToLowerInvariant()
    }

    if ($NoPreview) {
        $arguments += "--no-preview"
    }
}

dotnet @arguments
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
