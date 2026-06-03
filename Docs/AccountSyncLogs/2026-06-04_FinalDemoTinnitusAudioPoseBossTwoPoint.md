# FinalDemo Tinnitus Audio / Pose Match / Boss Two-Point Cleanup - 2026-06-04

Context:
- Rain pattern was already restored to the stable direct `LED rain` path. User asked to keep that structure and only lower physical board brightness.
- General/boss tinnitus had become silent after previous tick-removal work.
- General tinnitus rotation match updated, but position match stayed effectively unusable in FinalDemo.
- Boss tinnitus should use three cleanse patterns, each with only two target positions: first hold point, then second reach/hold point.

Implemented:
- `Assets/Scripts/FinalDemo/FinalDemoLightRouter.cs`
  - Kept the direct legacy rain command path.
  - Reduced `rainHardwareBrightnessMultiplier` from `0.25` to `0.1`, a 60% reduction from the current stable value.

- `Assets/Scenes/FinalDemo.unity`
  - Serialized `rainHardwareBrightnessMultiplier: 0.1`.

- `Assets/Scripts/FinalDemo/FinalDemoTuningProfile.cs`
  - Re-enabled `finalDemoProceduralTinnitusEnabled` by default.
  - Reduced general tinnitus procedural tone from `0.8` to `0.16`.
  - Reduced match feedback tone from `0.0135` to `0.0027`.
  - Reduced boss base volume from `0.42` to `0.084`.
  - Disabled `tinnitusPadBellFeedbackEnabled` by default.

- `Assets/Scripts/FinalDemo/FinalDemoDirector.cs`
  - Removed the pad-position bell feedback call during tinnitus cleansing.
  - Restored procedural tinnitus, but disabled short glitch clip and synthetic glitch density to avoid the repeated tick/click.
  - Uses continuous long glitch + quiet procedural high tone at the tinnitus world position.
  - Fixed general tinnitus pose target selection: if an authoring marker has an impossible transform-local camera-space pose, FinalDemo now uses the stored captured pose values instead. This specifically avoids `TinnitusA_HealPose_Authoring` local Z around `65m` being treated as a pad camera-space target.
  - Changed boss weakpoint authoring path lookup to use only waypoint 1 and waypoint 2. Later waypoints are ignored for active boss target matching.
  - Changed boss pattern reset logic so the first target can transition to the second target without immediately failing. The player gets the remaining pattern time to reach the second target; failing to reach it, or leaving it after entering, resets the pattern to the first target.

Validation:
- `powershell -ExecutionPolicy Bypass -File tools\Build-DotNetProject.ps1 -ProjectPath BellRinger.Runtime.csproj`
  - Passed.
  - Existing `PadTrackingReceiver.PadTrackingPacket` serialization warnings remain; no new errors.

QA notes:
- Rain should look identical to the currently stable pattern, only dimmer on the physical WS board. The monitor/preview brightness is not intentionally changed.
- In GeneralTinnitus stages, position match should now move when the captured pad position is approached. Rotation match behavior is unchanged.
- Tinnitus match low/high tones are restored but much quieter.
- The old pad-centered bell feedback during tinnitus cleansing is intentionally removed.
- Boss patterns now use only two active positions per pattern: first waypoint, then second waypoint.

Notebook compatibility:
- No COM port, Arduino firmware, package, or device-path behavior changed.
