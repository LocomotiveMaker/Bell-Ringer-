Add-Type -AssemblyName System.Drawing

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$outRoot = Join-Path $repoRoot "Docs\IconExports_512"

$categoryDirs = @(
    "sensory",
    "flow",
    "flow_clean",
    "legend",
    "tech",
    "modules",
    "colors"
)

foreach ($dir in $categoryDirs) {
    New-Item -ItemType Directory -Force -Path (Join-Path $outRoot $dir) | Out-Null
}

function Get-ForegroundBounds {
    param(
        [System.Drawing.Bitmap]$Bitmap,
        [System.Drawing.Rectangle]$Region,
        [int]$Threshold = 36
    )

    $samplePoints = @(
        [System.Drawing.Point]::new($Region.Left + 2, $Region.Top + 2),
        [System.Drawing.Point]::new($Region.Right - 3, $Region.Top + 2),
        [System.Drawing.Point]::new($Region.Left + 2, $Region.Bottom - 3),
        [System.Drawing.Point]::new($Region.Right - 3, $Region.Bottom - 3)
    )

    $samples = @()
    foreach ($point in $samplePoints) {
        if ($point.X -ge 0 -and $point.X -lt $Bitmap.Width -and $point.Y -ge 0 -and $point.Y -lt $Bitmap.Height) {
            $samples += $Bitmap.GetPixel($point.X, $point.Y)
        }
    }

    if (-not $samples.Count) {
        return $Region
    }

    $avgA = [int](($samples | Measure-Object -Property A -Average).Average)
    $avgR = [int](($samples | Measure-Object -Property R -Average).Average)
    $avgG = [int](($samples | Measure-Object -Property G -Average).Average)
    $avgB = [int](($samples | Measure-Object -Property B -Average).Average)

    $useAlphaOnly = $avgA -lt 8

    $width = $Region.Width
    $height = $Region.Height
    $foreground = New-Object 'bool[,]' $width, $height

    for ($localY = 0; $localY -lt $height; $localY++) {
        for ($localX = 0; $localX -lt $width; $localX++) {
            $x = $Region.Left + $localX
            $y = $Region.Top + $localY
            $pixel = $Bitmap.GetPixel($x, $y)

            if ($pixel.A -le 12) {
                continue
            }

            $isForeground = $false
            if ($useAlphaOnly) {
                $isForeground = $true
            }
            else {
                $distance = [Math]::Abs($pixel.R - $avgR) + [Math]::Abs($pixel.G - $avgG) + [Math]::Abs($pixel.B - $avgB)
                $isForeground = $distance -ge $Threshold
            }

            if ($isForeground) {
                $foreground[$localX, $localY] = $true
            }
        }
    }

    $visited = New-Object 'bool[,]' $width, $height
    $bestCount = 0
    $bestLocalBounds = $null
    $directions = @(
        @(1, 0),
        @(-1, 0),
        @(0, 1),
        @(0, -1)
    )

    for ($startY = 0; $startY -lt $height; $startY++) {
        for ($startX = 0; $startX -lt $width; $startX++) {
            if (-not $foreground[$startX, $startY] -or $visited[$startX, $startY]) {
                continue
            }

            $queue = [System.Collections.Generic.Queue[System.Drawing.Point]]::new()
            $queue.Enqueue([System.Drawing.Point]::new($startX, $startY))
            $visited[$startX, $startY] = $true

            $count = 0
            $minLocalX = $startX
            $maxLocalX = $startX
            $minLocalY = $startY
            $maxLocalY = $startY

            while ($queue.Count -gt 0) {
                $point = $queue.Dequeue()
                $count++

                if ($point.X -lt $minLocalX) { $minLocalX = $point.X }
                if ($point.X -gt $maxLocalX) { $maxLocalX = $point.X }
                if ($point.Y -lt $minLocalY) { $minLocalY = $point.Y }
                if ($point.Y -gt $maxLocalY) { $maxLocalY = $point.Y }

                foreach ($dir in $directions) {
                    $nextX = $point.X + $dir[0]
                    $nextY = $point.Y + $dir[1]

                    if ($nextX -lt 0 -or $nextX -ge $width -or $nextY -lt 0 -or $nextY -ge $height) {
                        continue
                    }

                    if ($visited[$nextX, $nextY] -or -not $foreground[$nextX, $nextY]) {
                        continue
                    }

                    $visited[$nextX, $nextY] = $true
                    $queue.Enqueue([System.Drawing.Point]::new($nextX, $nextY))
                }
            }

            if ($count -gt $bestCount) {
                $bestCount = $count
                $bestLocalBounds = [System.Drawing.Rectangle]::new(
                    $minLocalX,
                    $minLocalY,
                    ($maxLocalX - $minLocalX + 1),
                    ($maxLocalY - $minLocalY + 1)
                )
            }
        }
    }

    if ($bestCount -le 0 -or -not $bestLocalBounds) {
        return $Region
    }

    return [System.Drawing.Rectangle]::new(
        $Region.Left + $bestLocalBounds.Left,
        $Region.Top + $bestLocalBounds.Top,
        $bestLocalBounds.Width,
        $bestLocalBounds.Height
    )
}

function Export-Icon {
    param(
        [string]$SourcePath,
        [string]$DestinationPath,
        [System.Drawing.Rectangle]$SearchRegion,
        [int]$Threshold = 36,
        [int]$Padding = 12,
        [int]$CanvasSize = 512
    )

    $bitmap = [System.Drawing.Bitmap]::new($SourcePath)
    try {
        if (-not $SearchRegion) {
            $SearchRegion = [System.Drawing.Rectangle]::new(0, 0, $bitmap.Width, $bitmap.Height)
        }

        $regionBounds = Get-ForegroundBounds -Bitmap $bitmap -Region $SearchRegion -Threshold $Threshold

        $left = [Math]::Max(0, $regionBounds.Left - $Padding)
        $top = [Math]::Max(0, $regionBounds.Top - $Padding)
        $right = [Math]::Min($bitmap.Width, $regionBounds.Right + $Padding)
        $bottom = [Math]::Min($bitmap.Height, $regionBounds.Bottom + $Padding)

        $cropRect = [System.Drawing.Rectangle]::new($left, $top, ($right - $left), ($bottom - $top))
        $crop = $bitmap.Clone($cropRect, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try {
            $canvas = New-Object System.Drawing.Bitmap $CanvasSize, $CanvasSize, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
            try {
                $graphics = [System.Drawing.Graphics]::FromImage($canvas)
                try {
                    $graphics.Clear([System.Drawing.Color]::Transparent)
                    $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
                    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality

                    $margin = 28
                    $target = $CanvasSize - ($margin * 2)
                    $scale = [Math]::Min($target / $crop.Width, $target / $crop.Height)
                    $destWidth = [int][Math]::Round($crop.Width * $scale)
                    $destHeight = [int][Math]::Round($crop.Height * $scale)
                    $destX = [int][Math]::Round(($CanvasSize - $destWidth) / 2)
                    $destY = [int][Math]::Round(($CanvasSize - $destHeight) / 2)
                    $destRect = [System.Drawing.Rectangle]::new($destX, $destY, $destWidth, $destHeight)

                    $graphics.DrawImage($crop, $destRect)
                    $canvas.Save($DestinationPath, [System.Drawing.Imaging.ImageFormat]::Png)
                }
                finally {
                    $graphics.Dispose()
                }
            }
            finally {
                $canvas.Dispose()
            }
        }
        finally {
            $crop.Dispose()
        }
    }
    finally {
        $bitmap.Dispose()
    }
}

$image6 = Join-Path $repoRoot "Docs\_tmp_expo_ppt\ppt\media\image6.png"
$image9 = Join-Path $repoRoot "Docs\_tmp_expo_ppt\ppt\media\image9.png"
$s2Track = Join-Path $repoRoot "Docs\ExpoAssets\2026-05-31\RasterV2\png\S2_tracking_pipeline_v2.png"
$s2Bg = Join-Path $repoRoot "Docs\ExpoAssets\2026-05-31\RasterV2\png\S2_background_purpose_panel_v2.png"
$flowCleanRoot = Join-Path $repoRoot "Docs\ExpoAssets\2026-05-31\RasterV2\png"

$exports = @(
    @{ Source = $image6; Category = "sensory"; Name = "sensory_spatial_audio.png"; Region = @(8, 18, 76, 56); Threshold = 28; Padding = 8 },
    @{ Source = $image6; Category = "sensory"; Name = "sensory_led_hint.png"; Region = @(8, 81, 76, 56); Threshold = 28; Padding = 8 },
    @{ Source = $image6; Category = "sensory"; Name = "sensory_vibration_feedback.png"; Region = @(8, 145, 76, 56); Threshold = 28; Padding = 8 },
    @{ Source = $image6; Category = "sensory"; Name = "sensory_head_rotation.png"; Region = @(8, 209, 76, 56); Threshold = 28; Padding = 8 },
    @{ Source = $image6; Category = "sensory"; Name = "sensory_controller_motion.png"; Region = @(8, 272, 76, 56); Threshold = 28; Padding = 8 },

    @{ Source = $image9; Category = "flow"; Name = "flow_01_prepare.png"; Region = @(26, 84, 46, 38); Threshold = 28; Padding = 16 },
    @{ Source = $image9; Category = "flow"; Name = "flow_02_bell_trace.png"; Region = @(124, 80, 64, 56); Threshold = 28; Padding = 16 },
    @{ Source = $image9; Category = "flow"; Name = "flow_03_rainstorm.png"; Region = @(228, 80, 72, 56); Threshold = 28; Padding = 16 },
    @{ Source = $image9; Category = "flow"; Name = "flow_04_bell_gaze.png"; Region = @(352, 86, 56, 72); Threshold = 28; Padding = 16 },
    @{ Source = $image9; Category = "flow"; Name = "flow_05_tinnitus_purify.png"; Region = @(450, 86, 54, 48); Threshold = 28; Padding = 16 },
    @{ Source = $image9; Category = "flow"; Name = "flow_06_boss_tinnitus.png"; Region = @(549, 82, 54, 60); Threshold = 28; Padding = 16 },
    @{ Source = $image9; Category = "flow"; Name = "flow_07_forest_ending.png"; Region = @(646, 82, 50, 60); Threshold = 28; Padding = 16 },

    @{ Source = (Join-Path $flowCleanRoot "S3_icon_prepare_closed_eye_v2.png"); Category = "flow_clean"; Name = "flow_01_prepare_clean.png"; Region = @(0, 0, 1024, 1024); Threshold = 24; Padding = 0 },
    @{ Source = (Join-Path $flowCleanRoot "S3_icon_bell_trace_v2.png"); Category = "flow_clean"; Name = "flow_02_bell_trace_clean.png"; Region = @(0, 0, 1024, 1024); Threshold = 24; Padding = 0 },
    @{ Source = (Join-Path $flowCleanRoot "S3_icon_rainstorm_v2.png"); Category = "flow_clean"; Name = "flow_03_rainstorm_clean.png"; Region = @(0, 0, 1024, 1024); Threshold = 24; Padding = 0 },
    @{ Source = (Join-Path $flowCleanRoot "S3_icon_bell_gaze_v2.png"); Category = "flow_clean"; Name = "flow_04_bell_gaze_clean.png"; Region = @(0, 0, 1024, 1024); Threshold = 24; Padding = 0 },
    @{ Source = (Join-Path $flowCleanRoot "S3_icon_tinnitus_purify_v2.png"); Category = "flow_clean"; Name = "flow_05_tinnitus_purify_clean.png"; Region = @(0, 0, 1024, 1024); Threshold = 24; Padding = 0 },
    @{ Source = (Join-Path $flowCleanRoot "S3_icon_boss_tinnitus_v2.png"); Category = "flow_clean"; Name = "flow_06_boss_tinnitus_clean.png"; Region = @(0, 0, 1024, 1024); Threshold = 24; Padding = 0 },
    @{ Source = (Join-Path $flowCleanRoot "S3_icon_forest_ending_v2.png"); Category = "flow_clean"; Name = "flow_07_forest_ending_clean.png"; Region = @(0, 0, 1024, 1024); Threshold = 24; Padding = 0 },

    @{ Source = $image9; Category = "legend"; Name = "legend_bell.png"; Region = @(18, 212, 38, 34); Threshold = 28; Padding = 12 },
    @{ Source = $image9; Category = "legend"; Name = "legend_rain.png"; Region = @(168, 212, 40, 34); Threshold = 28; Padding = 12 },
    @{ Source = $image9; Category = "legend"; Name = "legend_tinnitus.png"; Region = @(333, 211, 58, 34); Threshold = 28; Padding = 12 },
    @{ Source = $image9; Category = "legend"; Name = "legend_wall.png"; Region = @(490, 211, 54, 34); Threshold = 28; Padding = 12 },
    @{ Source = $image9; Category = "legend"; Name = "legend_pad.png"; Region = @(647, 210, 42, 36); Threshold = 28; Padding = 12 },

    @{ Source = $image9; Category = "tech"; Name = "tech_audio_input.png"; Region = @(8, 274, 38, 28); Threshold = 28; Padding = 12 },
    @{ Source = $image9; Category = "tech"; Name = "tech_head_rotation_input.png"; Region = @(120, 270, 34, 34); Threshold = 28; Padding = 12 },
    @{ Source = $image9; Category = "tech"; Name = "tech_controller_pose.png"; Region = @(268, 268, 44, 36); Threshold = 28; Padding = 12 },
    @{ Source = $image9; Category = "tech"; Name = "tech_runtime_logic.png"; Region = @(384, 268, 34, 34); Threshold = 28; Padding = 12 },
    @{ Source = $image9; Category = "tech"; Name = "tech_feedback_output.png"; Region = @(542, 267, 40, 34); Threshold = 28; Padding = 12 },
    @{ Source = $image9; Category = "tech"; Name = "tech_immersive_experience.png"; Region = @(677, 266, 26, 34); Threshold = 28; Padding = 12 },

    @{ Source = $s2Track; Category = "modules"; Name = "module_head.png"; Region = @(170, 145, 300, 190); Threshold = 48; Padding = 24 },
    @{ Source = $s2Track; Category = "modules"; Name = "module_pad.png"; Region = @(705, 120, 320, 190); Threshold = 48; Padding = 24 },
    @{ Source = $s2Track; Category = "modules"; Name = "module_aruco_camera.png"; Region = @(1545, 145, 180, 230); Threshold = 42; Padding = 24 },
    @{ Source = $s2Track; Category = "modules"; Name = "module_pose_fusion.png"; Region = @(2220, 180, 180, 150); Threshold = 42; Padding = 24 },
    @{ Source = $s2Track; Category = "modules"; Name = "module_feedback_loop.png"; Region = @(2880, 180, 200, 170); Threshold = 42; Padding = 24 },

    @{ Source = $s2Bg; Category = "colors"; Name = "color_bell_green.png"; Region = @(1778, 1142, 82, 82); Threshold = 40; Padding = 10 },
    @{ Source = $s2Bg; Category = "colors"; Name = "color_wall_cyan.png"; Region = @(2038, 1142, 82, 82); Threshold = 40; Padding = 10 },
    @{ Source = $s2Bg; Category = "colors"; Name = "color_rain_deep_blue.png"; Region = @(2298, 1142, 82, 82); Threshold = 40; Padding = 10 },
    @{ Source = $s2Bg; Category = "colors"; Name = "color_tinnitus_violet.png"; Region = @(2558, 1142, 82, 82); Threshold = 40; Padding = 10 },
    @{ Source = $s2Bg; Category = "colors"; Name = "color_pad_orange.png"; Region = @(1910, 1298, 82, 82); Threshold = 40; Padding = 10 }
)

$manifest = New-Object System.Collections.Generic.List[string]

foreach ($entry in $exports) {
    $dest = Join-Path (Join-Path $outRoot $entry.Category) $entry.Name
    $rect = [System.Drawing.Rectangle]::new($entry.Region[0], $entry.Region[1], $entry.Region[2], $entry.Region[3])
    Export-Icon -SourcePath $entry.Source -DestinationPath $dest -SearchRegion $rect -Threshold $entry.Threshold -Padding $entry.Padding
    $manifest.Add(("{0}`t{1}`t{2}" -f $entry.Category, $entry.Name, (Split-Path $entry.Source -Leaf)))
}

$manifestPath = Join-Path $outRoot "_manifest.tsv"
$manifest | Set-Content -Path $manifestPath -Encoding UTF8

Write-Output ("Exported {0} icons to {1}" -f $exports.Count, $outRoot)
