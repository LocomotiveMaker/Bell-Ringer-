# Head Yaw/Pitch Axis Swap - 2026-06-01

Context:
- User confirmed that head up/down was receiving the physical yaw axis rather than the real VR/head pitch axis.
- The intended fix is to swap the current vertical input source with the remaining head-turning source in Unity, without changing Arduino firmware.

Implemented:
- `Assets/Scripts/Hardware/HeadTiltInputProvider.cs`
  - Added explicit `yawAxisSource` and `pitchAxisSource` fields.
  - Default mapping is now:
    - virtual yaw `<-` physical pitch telemetry.
    - virtual pitch `<-` physical yaw telemetry.
  - Existing `invertYaw`, `invertPitch`, deadzone, sensitivity, clamp, and smoothing still apply after axis selection.
- Added near-center return damping:
  - `snapToNeutralVirtualDegrees` default increased to `0.3`.
  - New `centerReturnSoftZoneDegrees` and `centerReturnDamping` reduce small residual drift after diagonal gaze returns to center.
- Stillness hold now avoids locking whichever physical axis currently drives virtual pitch, so up/down remains responsive.

QA:
- In `PadTrackingTest`, press `Recenter Head Tilt`.
- Turn head left/right: virtual yaw should move as before.
- Move head up/down: virtual pitch should now respond to the real VR/head up/down motion rather than the old yaw source.
- Look diagonally, then return to center. Residual virtual yaw/pitch should damp toward zero more aggressively than before.

Notebook compatibility:
- No serial ports, Arduino sketches, baud rates, or dependencies changed.
- This is a Unity-side axis mapping change shared by PadTrackingTest and FinalDemo.
