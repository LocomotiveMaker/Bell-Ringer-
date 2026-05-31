# Head Pitch Stillness Restore - 2026-06-01

Context:
- After pad-light experimentation was rolled back, head up/down movement was broken even in `PadTrackingTest`.
- The shared cause was `HeadTiltInputProvider` freezing physical pitch whenever stillness hold was active.

Implemented:
- Restored a `holdPitchWhenStill` switch in `Assets/Scripts/Hardware/HeadTiltInputProvider.cs`.
- The default remains `false`.
- With the default, stillness hold may stabilize yaw/roll, but physical pitch continues to feed virtual pitch every frame.

QA:
- In `PadTrackingTest`, press `Recenter Head Tilt`, then look up/down.
- `Head physical pitch` and `Head virtual pitch` should both change.
- Left/right yaw should keep the previous behavior.

Notebook compatibility:
- No port, baud, scene, Arduino, or dependency setting changed.
