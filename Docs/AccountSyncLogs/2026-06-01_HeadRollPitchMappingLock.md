# Head Roll/Pitch Mapping Lock - 2026-06-01

Context:
- User clarified the desired head control exactly:
  - `roll < 0` => look up.
  - `roll > 0` => look down.
  - `yaw > 0` => look left.
  - `yaw < 0` => look right.
- The previous attempt still used the wrong pitch source.

Implemented:
- `Assets/Scripts/Hardware/HeadTiltInputProvider.cs`
  - `virtual yaw` now stays on physical yaw with `invertYaw = true`.
  - `virtual pitch` now uses physical roll with `invertPitch = true`.
  - Unused physical axes no longer participate in stillness-lock hold/break logic.
  - Existing center-return damping remains active for small residual drift near neutral.
- `Assets/Scenes/FinalDemo.unity`
  - Head `invertPitch` serialized value set to `1` so FinalDemo matches the runtime default.

QA:
- In `PadTrackingTest`, press `Recenter Head Tilt`.
- Turn left/right:
  - physical yaw `+` should move the camera left.
  - physical yaw `-` should move the camera right.
- Tilt according to the mounted roll axis:
  - physical roll `-` should move the camera up.
  - physical roll `+` should move the camera down.
- Look diagonally and return to center. Neutral should settle more cleanly than before.

Notebook compatibility:
- Unity-side only. No Arduino sketch, port, baud, or dependency changes.
