# 2026-05-26 Notebook Runtime COM40/COM30 Fix

## Summary

- Notebook target wiring is `HardwareBridge`/head telemetry on `COM40` at `115200` and dedicated pad IMU on `COM30` at `230400`.
- `COM30` was verified to emit pad IMU telemetry at `230400`.
- `COM40` appeared in `System.IO.Ports.SerialPort.GetPortNames()` and WMI, but failed to open through `System.IO.Ports.SerialPort` with `'COM40' port does not exist`.
- `cmd /c mode` listed `COM30` only, which means `COM40` is currently not an openable runtime serial port even if upload tooling can see it.

## Runtime Code Changes

- `FinalDemoDirector` now applies `FinalDemoTuningProfile` serial routing before serial receivers connect.
- `HardwareBridge` now receives the profile hardware port (`COM40`) and baud (`115200`) unless `BELL_RINGER_SERIAL_PORT` or `BELL_RINGER_SERIAL_BAUD` is set.
- `HeadImuReceiver` is kept on shared `HardwareBridge` telemetry when `BELL_RINGER_HEAD_IMU_PORT` is not set. This avoids opening the same head/LED board twice.
- `PadImuReceiver` now receives the profile pad IMU port (`COM30`) and baud (`230400`) unless `BELL_RINGER_PAD_IMU_PORT` or `BELL_RINGER_PAD_IMU_BAUD` is set.
- `HardwareBridge` no longer falls back to an arbitrary first COM port when a preferred port is configured but unavailable. This prevents it from stealing `COM30` from the pad IMU.
- If an environment variable overrides a different profile port, `FinalDemoDirector` logs a Unity warning.
- `FinalDemoDirector` warning calls use `UnityEngine.Debug.LogWarning` explicitly because the project also has a `BellRinger.Debug` namespace.

## Current Diagnosis

- Pad rotation should be able to reach Unity through `COM30`; the port and telemetry format are valid.
- Head rotation cannot reach Unity through `COM40` until `COM40` is an openable runtime serial port, not just an upload/ghost COM entry.
- If Unity still shows no pad rotation, check whether another process has `COM30` open. The previous bug could also make `HardwareBridge` steal `COM30`; the code change above prevents that.

## Verification Commands

From `C:\Bell-Ringer-`:

```powershell
cmd /c mode
```

Expected minimum result:

- `COM30` must be listed for pad IMU.
- `COM40` must also be listed before the head/shared bridge can work in Unity.

Short pad serial read:

```powershell
$sp = [System.IO.Ports.SerialPort]::new("COM30", 230400)
$sp.ReadTimeout = 500
$sp.Open()
1..5 | ForEach-Object { try { $sp.ReadLine() } catch {} }
$sp.Close()
```

Expected pad lines include fields such as `wy=`, `wp=`, `wr=`, `wqw=`, `gx=`, `gy=`, `gz=`.

## Camera Note

The Iriun black-screen issue is separate from Arduino/Unity serial rotation. If `Run-PadTracker.cmd` opens but shows a black image, OpenCV is receiving a camera stream that contains black frames. Test Iriun first in the Windows Camera app, close other camera users, then retry a lower-resolution tracker command:

```powershell
.\tools\Run-PadTracker.cmd -CameraIndex 1 -Width 640 -Height 480 -Fps 30 -Backend DSHOW -DetectMaxDim 720 -PreviewMaxDim 960
```
