# FinalDemo Rain Dim / Pad Shake Bell Fix - 2026-06-04

Context:
- User reported `rainHardwareBrightnessMultiplier` changes did not visibly reduce rain brightness.
- User requested 70% rain light reduction, 25% rain audio reduction, pad-shake bell light during rain, and pad-shake bell sound during Bell Gaze only before bell acquisition.

Implemented:
- `Assets/Scripts/FinalDemo/FinalDemoLightRouter.cs`
  - Added `rainHardwareColorMultiplier = 0.3`.
  - Rain hardware output now scales RGB color as well as `level`, so physical WS brightness is reduced even if the uploaded firmware or command path does not visibly respond to the previous `level` multiplier alone.
  - Kept the stable direct `LED rain` path. No frame-composite rain path was restored.

- `Assets/Scenes/FinalDemo.unity`
  - Serialized `rainHardwareColorMultiplier: 0.3`.
  - Kept `rainHardwareBrightnessMultiplier: 0.1`.

- `Assets/Scripts/FinalDemo/FinalDemoTuningProfile.cs`
  - Reduced `rainAudioGainMultiplier` from `1.3` to `0.975` for a 25% rain audio reduction.

- `Assets/Scripts/FinalDemo/FinalDemoDirector.cs`
  - Pad-shake bell assist now guarantees a minimum sound/light range using `RainAssistVolumeFloor`, so the bell response does not collapse to zero when the player is far from the bell.
  - Pad-shake bell assist is now allowed in `BellGaze`, using the current bell placeholder position.
  - Pad-shake bell assist remains disabled after BellGaze/BellAcquisition and later stages.

Validation:
- `powershell -ExecutionPolicy Bypass -File tools\Build-DotNetProject.ps1 -ProjectPath BellRinger.Runtime.csproj`
  - Passed.
  - Existing `PadTrackingReceiver.PadTrackingPacket` serialization warnings remain; no new errors.

QA notes:
- During rain, shaking the pad should briefly let bell LED output override rain because Bell priority is higher than Rain.
- If the rain is still too bright on the physical WS board, lower `WorldLight_FinalDemo > FinalDemoLightRouter > Rain Hardware Color Multiplier`; this is now the most robust physical brightness control.

Notebook compatibility:
- No COM port, Arduino firmware, package, or device-path behavior changed.
