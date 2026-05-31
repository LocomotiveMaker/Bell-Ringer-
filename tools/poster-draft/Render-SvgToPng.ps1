param(
    [Parameter(Mandatory = $true)]
    [string]$SvgPath,

    [Parameter(Mandatory = $true)]
    [string]$PngPath,

    [int]$Width = 1600,
    [int]$Height = 2250
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

if (Test-Path -LiteralPath $PngPath) {
    Remove-Item -LiteralPath $PngPath -Force
}

$form = New-Object System.Windows.Forms.Form
$form.Width = $Width
$form.Height = $Height
$form.ShowInTaskbar = $false
$form.StartPosition = "Manual"
$form.Location = New-Object System.Drawing.Point(-32000, -32000)
$form.FormBorderStyle = "None"

$browser = New-Object System.Windows.Forms.WebBrowser
$browser.ScrollBarsEnabled = $false
$browser.ScriptErrorsSuppressed = $true
$browser.Width = $Width
$browser.Height = $Height
$browser.Dock = "Fill"
$form.Controls.Add($browser)

$script:done = $false
$browser.Add_DocumentCompleted({
    $script:done = $true
})

$uri = "file:///" + ($SvgPath -replace "\\", "/")
$browser.Navigate($uri)
$form.Show()

$timeout = [DateTime]::UtcNow.AddSeconds(20)
while (-not $script:done -and [DateTime]::UtcNow -lt $timeout) {
    [System.Windows.Forms.Application]::DoEvents()
    Start-Sleep -Milliseconds 100
}

Start-Sleep -Milliseconds 500
[System.Windows.Forms.Application]::DoEvents()

$bmp = New-Object System.Drawing.Bitmap $Width, $Height
$rect = New-Object System.Drawing.Rectangle 0, 0, $Width, $Height
$browser.DrawToBitmap($bmp, $rect)
$bmp.Save($PngPath, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

$form.Close()
$form.Dispose()

if (-not (Test-Path -LiteralPath $PngPath)) {
    throw "PNG was not created: $PngPath"
}

Get-Item -LiteralPath $PngPath | Select-Object FullName, Length, LastWriteTime
