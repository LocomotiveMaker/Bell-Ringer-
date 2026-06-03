# FinalDemo Rain LED Safety Composite - 2026-06-04

Superseded:
- This approach was replaced by `2026-06-04_FinalDemoRainLegacyNoComposite.md`.
- Current FinalDemo rain LED intentionally does not composite under bell/tinnitus light and uses the legacy `LED rain` command with reduced hardware brightness.

Context:
- User reported that rain light in FinalDemo filled the board bottom at painful brightness and sometimes produced stray red/blue light near the top.
- The issue followed recent attempts to keep rain visible under bell light.

Implemented:
- `Assets/Scripts/FinalDemo/FinalDemoLightRouter.cs`
  - FinalDemo rain LED no longer sends the firmware-side `LED rain` pattern command.
  - Rain now renders through the same C# 16x8 logical frame path used by the monitor preview, then sends `LED frame` to hardware.
  - Added an explicit rain pixel mask so only actual rain pixels are treated as rain.
  - Bell/tinnitus/wall pixels are no longer inferred by color, so violet tinnitus or cyan wall pixels cannot be mistaken for rain.
  - Hardware dimming now applies only to rain-masked pixels.
  - Added `rainHardwareMaximumBrightness` default `0.12` as a physical WS safety cap. Monitor preview remains unchanged.

Validation:
- `powershell -ExecutionPolicy Bypass -File tools\Build-DotNetProject.ps1 -ProjectPath BellRinger.Runtime.csproj`
  - Passed with existing `PadTrackingReceiver` serialization warnings and 0 errors.

QA notes:
- In `FinalDemo`, enter the rain follow stage and verify physical WS rain stays in the lower rows and is much dimmer than the monitor preview.
- Shake the pad during rain so bell light appears. Bell pixels should remain green on top; rain should stay blue underneath and should not overwrite or briefly shut off the bell.
- If the physical rain still feels too bright, reduce `rainHardwareMaximumBrightness` on `WorldLight_FinalDemo > FinalDemoLightRouter`. This changes hardware only, not the 16x8 monitor preview.

Notebook compatibility:
- No COM port, Arduino firmware, package, or device-path behavior changed.
