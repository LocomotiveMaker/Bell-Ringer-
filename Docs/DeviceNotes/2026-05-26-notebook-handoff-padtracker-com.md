# 2026-05-26 Notebook Handoff: PadTracker + COM Port Notes

## Summary

- Notebook switched to `Iriun Webcam v2.9.5` over `USB`.
- Current notebook serial ports are:
  - head + LED bridge: `COM7`
  - pad IMU: `COM8`
- Desktop-oriented defaults like `COM40` / `COM30` / `COM9` may still appear in Unity assets or Inspector fields and must not be trusted on the notebook.
- The PowerShell error below was caused by command syntax, not by PadTracker itself:

```text
Set-Location : 매개 변수 이름 'CameraIndex'과(와) 일치하는 매개 변수를 찾을 수 없습니다.
```

## Root Cause Of The PowerShell Error

This command was typed without a separator between `cd` and the tracker command:

```powershell
cd C:\Bell-Ringer-.\tools\Run-PadTracker.cmd -CameraIndex 1 ...
```

PowerShell interpreted `-CameraIndex` as an option for `cd` / `Set-Location`.

## Correct Commands

Run the commands on separate lines:

```powershell
cd C:\Bell-Ringer-
.\tools\Run-PadTracker.cmd -Scan
```

```powershell
cd C:\Bell-Ringer-
.\tools\Run-PadTracker.cmd -CameraIndex 1 -Width 1280 -Height 720 -Fps 30 -DetectMaxDim 720 -PreviewMaxDim 960
```

Or use a one-liner with a separator:

```powershell
cd C:\Bell-Ringer-; .\tools\Run-PadTracker.cmd -CameraIndex 1 -Width 1280 -Height 720 -Fps 30 -DetectMaxDim 720 -PreviewMaxDim 960
```

If the repo path contains spaces instead of hyphens, use quotes:

```powershell
cd "C:\Bell Ringer"
.\tools\Run-PadTracker.cmd -Scan
```

## Tracker Startup Order

1. Confirm the phone appears as a Windows webcam through Iriun USB mode.
2. Run camera scan first:

```powershell
cd C:\Bell-Ringer-
.\tools\Run-PadTracker.cmd -Scan
```

3. Note which camera index actually returns frames.
4. Start PadTracker with backend left unset first:

```powershell
cd C:\Bell-Ringer-
.\tools\Run-PadTracker.cmd -CameraIndex 1 -Width 1280 -Height 720 -Fps 30 -DetectMaxDim 720 -PreviewMaxDim 960
```

5. Only if needed, compare explicit backends:

```powershell
cd C:\Bell-Ringer-
.\tools\Run-PadTracker.cmd -CameraIndex 1 -Width 1280 -Height 720 -Fps 30 -Backend DSHOW -DetectMaxDim 720 -PreviewMaxDim 960
```

```powershell
cd C:\Bell-Ringer-
.\tools\Run-PadTracker.cmd -CameraIndex 1 -Width 1280 -Height 720 -Fps 30 -Backend MSMF -DetectMaxDim 720 -PreviewMaxDim 960
```

## COM Port Handling On The Notebook

### Current notebook mapping

- `COM7`: head ESP32-S3 with `HeadMpu9250LedBridge`
- `COM8`: pad ESP32-S3 with `PadMpu9250Orientation`

### Important behavior in the current code

- `HardwareBridge`
  - if the preferred port exists, it uses it
  - otherwise it falls back to the first available COM port
- `HeadImuReceiver`
  - if `preferredPortName` is empty and shared bridge telemetry is enabled, it waits for `HardwareBridge` and uses the head data from the same device
  - if `preferredPortName` is set to a wrong value, it does **not** auto-scan
- `PadImuReceiver`
  - if `preferredPortName` is set to a wrong value, it does **not** auto-scan
  - auto-scan only happens when `preferredPortName` is empty

### Recommended notebook configuration

Use environment-variable overrides when possible.

- Set `BELL_RINGER_SERIAL_PORT=COM7`
- Set `BELL_RINGER_PAD_IMU_PORT=COM8`
- Leave `BELL_RINGER_HEAD_IMU_PORT` empty / unset if the head IMU is coming from the same `COM7` bridge device

This is the safest setup for the current combined head+LED ESP32-S3 wiring.

### Alternative manual path inside Unity

If environment variables are not being used, check:

- `FinalDemoTuningProfile.asset`
  - desktop defaults may still show `COM9`, `COM40`, `COM30`
- component Inspectors:
  - `HardwareBridge.preferredPortName`
  - `HeadImuReceiver.preferredPortName`
  - `PadImuReceiver.preferredPortName`

For auto behavior:

- `HeadImuReceiver.preferredPortName` should be empty when using shared head telemetry from the LED/head bridge.
- `PadImuReceiver.preferredPortName` can be set to `COM8`, or cleared if auto scan is preferred.

## What The Notebook AI Should Check

- verify the actual repo root path first: `C:\Bell-Ringer-` vs `C:\Bell Ringer`
- verify `tools\Run-PadTracker.cmd` exists in that repo root
- verify `Iriun Webcam` is installed and the phone feed appears as a real Windows webcam
- run `.\tools\Run-PadTracker.cmd -Scan`
- identify the correct camera index that returns frames, not just opens
- confirm the active serial mapping really is `COM7` head and `COM8` pad
- in Unity Play mode, confirm:
  - hardware LED output works
  - head IMU updates arrive
  - pad IMU updates arrive
  - PadTracker UDP updates arrive
  - no stale `COM40` / `COM30` assumptions remain

## Current Cross-Reference Notes

- `Docs\DeviceNotes\2026-05-26-padtracker-usb.md`
- `Docs\DeviceNotes\2026-05-26-git-pull-untracked-meta.md`
