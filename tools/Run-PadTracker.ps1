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

$projectFilePath = Join-Path $projectRoot "tools\PadTracker\BellRinger.PadTracker.csproj"
$nugetConfigFilePath = Join-Path $projectRoot "NuGet.config"
$builtExePath = Join-Path $projectRoot "tools\PadTracker\bin\Debug\net8.0\BellRinger.PadTracker.exe"
$builtDllPath = Join-Path $projectRoot "tools\PadTracker\bin\Debug\net8.0\BellRinger.PadTracker.dll"
$hasBuiltApp = (Test-Path $builtExePath) -or (Test-Path $builtDllPath)

if ($Restore -or -not $hasBuiltApp) {
    if (-not (Test-Path $projectFilePath)) {
        throw "Pad tracker project file not found at '$projectFilePath'."
    }

    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw "dotnet was not found. Install .NET 8 SDK or keep the prebuilt tracker output in tools\PadTracker\bin\Debug\net8.0."
    }

    $projectPath = Resolve-Path $projectFilePath
    $nugetConfigPath = Resolve-Path $nugetConfigFilePath
    $buildArguments = @("build", $projectPath.Path, "--configfile", $nugetConfigPath.Path)
    if (-not $Restore) {
        $buildArguments += "--no-restore"
    }

    dotnet @buildArguments
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

$arguments = @()

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

if (Test-Path $builtExePath) {
    & $builtExePath @arguments
} elseif (Test-Path $builtDllPath) {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw "dotnet was not found. Install .NET 8 runtime or rebuild the tracker on a machine with the SDK."
    }

    dotnet $builtDllPath @arguments
} else {
    throw "Pad tracker build output was not found. Run tools\Build-PadTracker.ps1 -Restore once after restoring the project."
}

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
