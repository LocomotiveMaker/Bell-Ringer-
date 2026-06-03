# FinalDemo Recovery Continuation - 2026-06-04

Context:
- Continued after token cutoff from the FinalDemo structural recovery pass.
- The previous pass had already restored rain/tinnitus/boss structure, but rain physical brightness and operator visibility still needed a safer follow-up.

Implemented:
- `Assets/Scripts/FinalDemo/FinalDemoLightRouter.cs`
  - Kept the C# logical rain frame path so rain and bell can appear together, with bell pixels overriding rain pixels.
  - Reduced rain physical defaults further for closed-eye safety:
    - `rainBrightness = 0.055`
    - `rainDensity = 0.2`
    - `rainHardwareBrightnessMultiplier = 0.25`
    - `rainHardwareColorMultiplier = 0.25`
    - `rainHardwareMaximumBrightness = 0.055`
  - Reduced rain dot count by changing the generated drop count from `2 + density * 5` to `1 + density * 5`.
  - Slowed rain seed change to reduce choppy/popping movement.

- `Assets/Scenes/FinalDemo.unity`
  - Serialized the new rain controls on `LightRouter_LED` so Unity Inspector values match runtime behavior and can be tuned directly.

- `Assets/Scripts/FinalDemo/FinalDemoOperatorControls.cs`
  - LED preview now shows `FinalDemoLightRouter.LastAction`.
  - LED preview now shows `FinalDemoLightRouter.LastRainDebug`, including rain level, density, drops, height, visibility, and seed.

Validation:
- `powershell -ExecutionPolicy Bypass -File tools\Build-DotNetProject.ps1 -ProjectPath BellRinger.Runtime.csproj`
  - Passed.
  - Existing `PadTrackingReceiver.PadTrackingPacket` serialization warnings remain; no new errors.

QA focus:
- In FinalDemo rain stage, verify the physical WS rain is lower brightness and has fewer lit dots than the previous over-bright state.
- Confirm the monitor 16x8 preview still remains readable because preview boost is separate from the physical hardware cap.
- During rain, shake/call bell and confirm green bell pixels can appear over blue rain pixels.
- If the board is still painful, lower `LightRouter_LED > Rain Hardware Maximum Brightness` first, then `Rain Density`.

Notebook compatibility:
- No COM ports, Arduino firmware, package installs, or device paths changed.
