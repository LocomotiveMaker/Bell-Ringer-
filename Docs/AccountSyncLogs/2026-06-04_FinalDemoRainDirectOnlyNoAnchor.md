# FinalDemo Rain Direct Only / No Rain Anchor Suppression - 2026-06-04

Context:
- User reported rain looked correct for the first 2-3 seconds, then bottom rows became full-bright and upper red/green pixels appeared.
- Diagnosis: the initial period used the direct firmware `LED rain` command. After the bell follow rain stage settled, continuous bell anchor output caused rain to go through the `LED frame` composite path, which produced the bad physical board pattern.

Implemented:
- `Assets/Scripts/FinalDemo/FinalDemoLightRouter.cs`
  - Removed rain `LED frame` compositing from the active path.
  - Removed `_rainCompositeIntensity`, `_rainCompositeUntilRealtime`, `_hardwareFrame`, `IsRainCompositeActive()`, `SendLogicalFrameToHardware(...)`, and `CompositeRainUnderExistingLogicalFrame(...)`.
  - Rain hardware output now uses only `HardwareBridge.SendLedRain(...)`.
  - Set `rainHardwareBrightnessMultiplier` to `0.25` for a stronger visible safety reduction on the physical WS board.

- `Assets/Scenes/FinalDemo.unity`
  - Serialized `rainHardwareBrightnessMultiplier: 0.25`.

- `Assets/Scripts/FinalDemo/FinalDemoDirector.cs`
  - Disabled continuous bell anchor output during `BellFollowRain`.
  - Bell follow rain still plays actual bell calls, but the constant anchor no longer keeps priority above rain and no longer forces rain into a composite frame path.

Validation:
- `powershell -ExecutionPolicy Bypass -File tools\Build-DotNetProject.ps1 -ProjectPath BellRinger.Runtime.csproj`
  - Passed with existing `PadTrackingReceiver` serialization warnings and 0 errors.

QA notes:
- In BellFollowRain, rain should stay in the same legacy pattern from the first seconds onward.
- If a bell call occurs, rain may be briefly interrupted for the bell call priority, but it should return to the legacy `LED rain` pattern instead of showing full-bright rows or red/green top noise.
- If rain is now too dim, raise `WorldLight_FinalDemo > FinalDemoLightRouter > Rain Hardware Brightness Multiplier`; do not re-enable rain frame compositing.

Notebook compatibility:
- No COM port, Arduino firmware, package, or device-path behavior changed.
