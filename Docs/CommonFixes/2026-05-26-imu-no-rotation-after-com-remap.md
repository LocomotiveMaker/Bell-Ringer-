# 2026-05-26 IMU Rotation Not Updating Even After COM Remap

## Short Answer

Yes, the user **can** remap the notebook COM numbers to match the desktop layout such as:

- head + LED bridge -> `COM40`
- pad IMU -> `COM30`

But that alone is **not sufficient**.

If Unity still does not receive rotation values after the remap, the cause is usually one of these:

1. the scene is still using a different serial route than expected
2. environment variables are overriding the Inspector values
3. the wrong sketch is on the device
4. another program is holding the COM port
5. the head path is `shared bridge telemetry`, not a separate head IMU port

## Why Matching The COM Numbers Is Not Enough

In the current project, the serial routing is split across multiple components.

### FinalDemo scene behavior

`Assets/Scenes/FinalDemo.unity` currently contains:

- `HardwareBridge`
  - `preferredPortName: COM40`
  - `baudRate: 115200`
- `HeadImuReceiver`
  - `useSharedHardwareBridgeTelemetry: 1`
  - `preferredPortName: ""`
  - `baudRate: 230400`
- `PadImuReceiver`
  - `useSharedHardwareBridgeTelemetry: 0`
  - `preferredPortName: COM30`
  - `baudRate: 230400`

This means:

- head rotation in `FinalDemo` is expected to come from the **same device as the LED bridge**
- pad rotation is expected to come from a **separate serial device**

So if the head ESP32 is not successfully opened by `HardwareBridge`, the head rotation will also fail even if the COM number looks correct.

## Important Current Limitation

`FinalDemoTuningProfile` has fields like:

- `hardwareSerialPort`
- `headImuSerialPort`
- `padImuSerialPort`

However, in the current runtime code those values are **not actually consumed** by the demo bootstrap.

So changing only the values inside:

- `Assets/ScriptableObjects/FinalDemo/FinalDemoTuningProfile.asset`

does **not** guarantee that the serial ports used at runtime will change.

The real active sources are:

- environment variables
- serialized Inspector values on scene components

## Recommended Fix Strategy

Do **not** rely on Windows COM renumbering as the main solution.

Use one of these two paths.

### Path A: keep notebook-native COM numbers

Example:

- head + LED bridge = `COM7`
- pad IMU = `COM8`

Then explicitly route Unity to those ports:

- `BELL_RINGER_SERIAL_PORT=COM7`
- `BELL_RINGER_SERIAL_BAUD=115200`
- `BELL_RINGER_PAD_IMU_PORT=COM8`
- `BELL_RINGER_PAD_IMU_BAUD=230400`
- leave `BELL_RINGER_HEAD_IMU_PORT` unset when head telemetry should come from the same head+LED bridge

This is the safest option for the current `FinalDemo` wiring.

### Path B: force notebook ports to mimic desktop

Example:

- head + LED bridge -> `COM40`
- pad IMU -> `COM30`

If this path is used, also verify all of the following:

- `HardwareBridge` really opens `COM40` at `115200`
- `HeadImuReceiver` remains on shared bridge mode
- `PadImuReceiver` really opens `COM30` at `230400`
- no stale environment variable overrides remain
- no serial monitor is already using those ports

## Required Sketches

The head device must use:

- `arduino/HeadMpu9250LedBridge/HeadMpu9250LedBridge.ino`

Do **not** use:

- `arduino/HeadMpu9250Tilt/HeadMpu9250Tilt.ino`

for the final head device, because that sketch is IMU-only and does not match the combined LED/head bridge expectation.

The pad device must use:

- `arduino/PadMpu9250Orientation/PadMpu9250Orientation.ino`

## Frequent Failure Cases

### 1. Arduino upload succeeds, but Unity still gets no rotation

This is possible because upload success only proves:

- the bootloader port was reachable

It does **not** prove:

- Unity is opening the same port afterward
- Unity is using the correct baud
- the sketch is sending the expected telemetry format

### 2. Head path is accidentally treated as dedicated serial

In `FinalDemo`, the head path is designed to come through `HardwareBridge`.

If `BELL_RINGER_HEAD_IMU_PORT` is set to a wrong value, or if `HeadImuReceiver.preferredPortName` is forced to a wrong COM, the head can stop using the shared route and fail silently.

### 3. Pad IMU stays stale

`PadImuReceiver` does not auto-scan if `preferredPortName` is populated with a bad port.

So if it still says `COM30` but the notebook pad is actually on `COM8`, it can remain disconnected until the preferred port is corrected or cleared.

### 4. COM port already in use

If any of these are open, Unity may fail to connect:

- Arduino IDE Serial Monitor
- Arduino IDE Serial Plotter
- another Unity scene already holding the port
- external serial terminal tools

## Minimum Checklist When Rotation Does Not Move

1. Confirm the head board is flashed with `HeadMpu9250LedBridge`.
2. Confirm the pad board is flashed with `PadMpu9250Orientation`.
3. Close every serial monitor window.
4. Check the real COM ports in Device Manager.
5. Check whether environment variables are overriding the scene.
6. In `FinalDemo`, verify:
   - `HardwareBridge.preferredPortName`
   - `HeadImuReceiver.useSharedHardwareBridgeTelemetry`
   - `HeadImuReceiver.preferredPortName`
   - `PadImuReceiver.preferredPortName`
7. Run Play Mode and check the debug overlay:
   - is `HardwareBridge` connected?
   - is head sample `fresh`?
   - is pad sample `fresh`?
   - is the last raw IMU line changing?

## Practical Recommendation For The Notebook

For now, the most reliable setup is:

- do not chase `COM30/COM40` unless there is a strong reason
- keep the notebook actual ports such as `COM7` and `COM8`
- route Unity through environment variables or the live scene Inspectors

That avoids an extra layer of Windows COM remap ambiguity.

## If The User Wants A Future-Proof Fix

The structural fix would be:

1. make `FinalDemoTuningProfile` actually apply serial settings at runtime
2. centralize serial routing so only one place owns the ports
3. show the active port source clearly in the debug UI:
   - scene default
   - environment override
   - auto-detected port

That is the correct long-term direction, but it is separate from the immediate notebook bring-up.
