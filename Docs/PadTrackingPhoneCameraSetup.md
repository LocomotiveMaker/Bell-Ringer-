# Pad Tracking Phone Camera Setup

This is the first software step for pad position tracking.

The current prototype assumes:

- the phone camera becomes a normal Windows webcam device
- the external tracker reads that webcam
- the tracker sends UDP packets to Unity
- Unity shows a live debug ghost for the pad

## What The User Must Prepare

The user still has a few setup steps that the assistant cannot perform physically:

- the finished V-board
- the phone
- a USB cable
- one phone-to-PC webcam bridge app

For the bridge app, USB mode is preferred over Wi-Fi for this project.

The tested software path for this prototype is:

- `DroidCam` on Android and Windows, because the official site says it supports both `USB` and `Windows` webcam output:
  [DroidCam official site](https://www.droidcam.app/)

Other valid options if the user prefers them:

- `Iriun Webcam`, which the Play Store listing and the official site say supports `USB` and `Wi-Fi`:
  [Iriun Play Store](https://play.google.com/store/apps/details?id=com.jacksoftw.webcam)
  [Iriun official site](https://iriun.net/)
- `Camo`, which the official site says supports Android and Windows:
  [Camo Studio](https://camo.com/studio)

## Recommended First Test

Use this order exactly:

1. Install a phone webcam bridge app.
2. Connect the phone to the PC over USB.
3. Make sure Windows now sees the phone feed as a webcam device.
4. Scan the available camera indices.
5. Run the tracker on the correct camera index.
6. Open the Unity pad-tracking test scene and press Play.
7. Hold the V-board by hand in front of the phone camera.

## Commands

### 1. Scan camera indices

First run once:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Build-PadTracker.ps1 -Restore
```

Then run:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Run-PadTracker.ps1 -Scan
```

The tracker will try camera indices `0` through `7` and print which ones open successfully.

The user should write down the webcam index that corresponds to the phone feed.

### 2. Start the live tracker

Example:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Run-PadTracker.ps1 -CameraIndex 0 -Width 1280 -Height 720 -Fps 60
```

Replace `0` with the camera index found in the scan step.

### 3. Create the Unity scene

If the scene does not already exist, run:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Create-PadTrackingTestScene.ps1
```

Then open:

- [PadTrackingTest.unity](</C:/Bell Ringer/Assets/Scenes/PadTrackingTest.unity>)

### 4. Run the Unity scene

Press Play in Unity.

The on-screen panel will show:

- whether UDP packets are arriving
- whether marker detection is fresh
- which marker IDs are visible
- approximate `X / Y / Z` camera-space position

## Important User-Tuned Values

These are the values the user may need to adjust manually.

### Camera index

This must match the webcam device created by the phone bridge app.

If the wrong camera index is used, the tracker may open:

- the laptop webcam
- a virtual camera
- nothing at all

### Marker size

The generated V-board uses a marker image size of `50 mm`.

That means the tracker defaults to:

- `-MarkerSizeMm 50`

Do not change this unless the printed black marker square itself is not `50 mm`.

### Horizontal field of view

The first prototype estimates depth without full camera calibration.

Because of that, `Z` is only approximate for now.

The default value is:

- `-HorizontalFovDegrees 68`

If the depth feels wrong:

- increase the value if the pad appears farther than it should
- decrease the value if the pad appears closer than it should

This is a temporary prototype shortcut.

Later, full calibration can replace this guessed FOV.

## Marker Rules

The tracker expects:

- dictionary: `DICT_ARUCO_ORIGINAL`
- left marker ID: `23`
- right marker ID: `47`

The user should not change these in the print step or the software step unless both sides are changed together.

## What The User Should Expect In The First Test

This first tracker is intentionally practical, not fully final.

It should already give:

- good `X / Y` motion
- usable approximate `Z`
- last-position hold if markers disappear briefly

It does not yet give:

- final real-world centimeter accuracy
- final rotation from the camera
- final calibration quality

That is acceptable for the hand-held V-board test.

## Stop The Tracker

When the preview window is open:

- press `Q`
- or press `Esc`

If the preview window is disabled, stop it with:

- `Ctrl + C`
