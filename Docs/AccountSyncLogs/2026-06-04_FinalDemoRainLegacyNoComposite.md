# FinalDemo Rain Legacy Pattern / No Composite - 2026-06-04

Superseded:
- This approach was replaced by `2026-06-04_FinalDemoRainRestoreHapticLimit.md`.
- Current FinalDemo rain LED uses the late rain / held-frame preservation route again, with hardware brightness multiplier `0.4`.

Context:
- User reported the rain LED was still painful and asked to abandon combining bell and rain light.
- Requested direction: use the previous rain pattern and only reduce physical rain brightness by 70%.

Implemented:
- `Assets/Scripts/FinalDemo/FinalDemoLightRouter.cs`
  - Removed the bell/rain composite path from the active FinalDemo light route.
  - Rain no longer renders under bell/tinnitus/high-priority light. If a higher-priority light is held, rain is skipped for that tick.
  - Rain hardware output uses the legacy `HardwareBridge.SendLedRain(...)` command again.
  - Physical rain brightness is controlled by `rainHardwareBrightnessMultiplier`.
  - `Assets/Scenes/FinalDemo.unity` currently has `rainHardwareBrightnessMultiplier: 0.3`, so physical rain output is 70% dimmer.
  - Monitor preview still uses the logical rain frame for visibility, but it is not composited into hardware output.

Validation:
- `powershell -ExecutionPolicy Bypass -File tools\Build-DotNetProject.ps1 -ProjectPath BellRinger.Runtime.csproj`
  - Passed with existing `PadTrackingReceiver` serialization warnings and 0 errors.

QA notes:
- In FinalDemo rain stage, rain should use the old firmware-style rain pattern again.
- When bell light fires during rain, rain may briefly disappear instead of being merged underneath. This is intentional for safety/stability.
- If the physical rain is still too bright, lower `WorldLight_FinalDemo > FinalDemoLightRouter > Rain Hardware Brightness Multiplier`.

Notebook compatibility:
- No COM port, Arduino firmware, package, or device-path behavior changed.
