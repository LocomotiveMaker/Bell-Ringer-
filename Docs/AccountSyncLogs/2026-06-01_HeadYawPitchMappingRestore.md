# Head Yaw/Pitch Mapping Restore - 2026-06-01

Context:
- The previous Unity-side head mapping set `virtual yaw <- physical pitch` and `virtual pitch <- physical yaw`.
- User clarified the correct behavior:
  - real head yaw should drive in-game left/right.
  - real head pitch should drive in-game up/down.

Implemented:
- Restored the default head axis sources in `Assets/Scripts/Hardware/HeadTiltInputProvider.cs`:
  - `yawAxisSource = PhysicalYaw`
  - `pitchAxisSource = PhysicalPitch`
- Kept the recent center-return damping and stillness-lock improvements.

QA:
- In `PadTrackingTest`, press `Recenter Head Tilt`.
- Turn head left/right: `Head virtual yaw` should change.
- Move head up/down: `Head virtual pitch` should change.
- If direction is reversed, only `invertYaw` or `invertPitch` should need adjustment.

Notebook compatibility:
- No Arduino, serial, scene object, or dependency changes were made.
