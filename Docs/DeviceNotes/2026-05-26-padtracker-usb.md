# 2026-05-26 PadTracker USB Notes (Notebook)

## Summary

- Switched phone webcam bridge from Wi-Fi to USB for reliability.
- Use `.\tools\Run-PadTracker.cmd` instead of typing raw `powershell ...` commands.
- Prefer leaving `-Backend` unset (auto fallback). Use explicit `DSHOW` or `MSMF` only for troubleshooting.

## Why

- Some virtual cameras open but do not deliver frames reliably over Wi-Fi.
- PowerShell profiles and execution policy can block scripts on fresh machines.

## Commands

Scan camera indices:

```powershell
cd C:\Bell-Ringer-
.\tools\Run-PadTracker.cmd -Scan
```

Start tracker (recommended):

```powershell
cd C:\Bell-Ringer-
.\tools\Run-PadTracker.cmd -CameraIndex 1 -Width 1280 -Height 720 -Fps 30 -DetectMaxDim 720 -PreviewMaxDim 960
```

Troubleshooting (explicit backend + lower fps):

```powershell
cd C:\Bell-Ringer-
.\tools\Run-PadTracker.cmd -CameraIndex 1 -Width 1280 -Height 720 -Fps 30 -Backend DSHOW -DetectMaxDim 720 -PreviewMaxDim 960
```

```powershell
cd C:\Bell-Ringer-
.\tools\Run-PadTracker.cmd -CameraIndex 1 -Width 1280 -Height 720 -Fps 30 -Backend MSMF -DetectMaxDim 720 -PreviewMaxDim 960
```

## Repo Changes

- Added `.cmd` wrappers to avoid profile/execution-policy issues:
  - `tools\Run-PadTracker.cmd`
  - `tools\Build-PadTracker.cmd`
- Updated `Docs\PadTrackingPhoneCameraSetup.md` to recommend backend auto fallback first.
