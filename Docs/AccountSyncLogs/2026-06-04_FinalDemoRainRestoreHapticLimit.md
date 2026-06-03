# FinalDemo Rain Restore / Haptic Limit - 2026-06-04

Superseded:
- Rain LED routing was corrected again in `2026-06-04_FinalDemoRainDirectOnlyNoAnchor.md`.
- Current rain LED no longer uses `LED frame` compositing. It uses only the legacy `LED rain` command, and the BellFollowRain continuous bell anchor is disabled so rain is not forced into the broken composite path after 2-3 seconds.

Context:
- User forked the conversation and reported that rain had returned to the old failure where it starts, then disappears after about 2-3 seconds.
- User wanted rain restored to the earlier point where it worked but was too bright, with physical rain brightness reduced by about 60%.
- User also requested all gameplay pad vibration be removed except for a short list of explicit events.

Implemented:
- `Assets/Scripts/FinalDemo/FinalDemoLightRouter.cs`
  - Restored the late rain / held-frame preservation route from the earlier working rain state.
  - Rain again stores `_rainCompositeIntensity` briefly and can be composited under an active higher-priority frame instead of being skipped.
  - Restored `SendLogicalFrameToHardware(...)` for held-frame rain preservation.
  - Set default `rainHardwareBrightnessMultiplier` to `0.4`, meaning physical rain is 60% dimmer.

- `Assets/Scenes/FinalDemo.unity`
  - Serialized `rainHardwareBrightnessMultiplier: 0.4` on `WorldLight_FinalDemo > FinalDemoLightRouter`.

- `Assets/Scripts/FinalDemo/FinalDemoHapticRouter.cs`
  - Added `StartLowestTinnitusCleanseHum(...)` for the lowest practical cleanse hum.

- `Assets/Scripts/FinalDemo/FinalDemoDirector.cs`
  - Removed automatic haptics from opening close bell, pad shake bell assist, bell gaze intermediate successes, general tinnitus approach lock, boss pattern start, boss miss, boss completion, and boss defeat.
  - Kept gameplay haptics only for:
    - reaching the first bell follow target,
    - reaching the second bell/rain follow target,
    - final bell acquisition after gaze successes,
    - entering exact general tinnitus pose tolerance,
    - lowest continuous hum while cleansing general tinnitus,
    - entering exact boss weakpoint tolerance.

Validation:
- `powershell -ExecutionPolicy Bypass -File tools\Build-DotNetProject.ps1 -ProjectPath BellRinger.Runtime.csproj`
  - Passed with existing `PadTrackingReceiver` serialization warnings and 0 errors.

QA notes:
- In `BellFollowRain`, rain should no longer disappear when narration or bell output starts around the 2-3 second mark.
- Physical rain brightness should be lower than the earlier bright state, using multiplier `0.4`.
- During gameplay, vibration should occur only at the six listed events. Manual haptic test buttons in the operator panel still call the haptic router only when explicitly pressed.

Notebook compatibility:
- No COM port, Arduino firmware, package, or device-path behavior changed.
