param(
    [string]$PortName = "COM9",
    [int]$BaudRate = 115200,
    [int]$Brightness = 80,
    [double]$DurationSeconds = 1.0
)

$ErrorActionPreference = "Stop"

$serialPort = New-Object System.IO.Ports.SerialPort($PortName, $BaudRate)
$serialPort.NewLine = "`n"
$serialPort.DtrEnable = $true
$serialPort.ReadTimeout = 200
$serialPort.WriteTimeout = 500

try {
    $serialPort.Open()
    Start-Sleep -Milliseconds 2200

    $clampedBrightness = [Math]::Max(0, [Math]::Min(255, $Brightness))
    $serialPort.WriteLine("LED fill b=$clampedBrightness")
    Write-Host "Sent: LED fill b=$clampedBrightness"
    Start-Sleep -Milliseconds ([Math]::Max(50, [int]($DurationSeconds * 1000)))

    $serialPort.WriteLine("LED clear")
    Write-Host "Sent: LED clear"
}
finally {
    if ($serialPort.IsOpen) {
        $serialPort.Close()
    }

    $serialPort.Dispose()
}
