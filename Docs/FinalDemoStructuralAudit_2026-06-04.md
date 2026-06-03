# FinalDemo Structural Audit - 2026-06-04

Purpose: document why FinalDemo rain LED, tinnitus audio, and tinnitus pose authoring diverged from the working test scenes before making another implementation pass.

Scope: read-only investigation. No gameplay, scene, audio, LED, Arduino, or tuning code was changed while producing this document.

## Sources Checked

- `AGENT.md`
- `Docs/AccountSyncLogs/2026-06-04_FinalDemoRainDimPadShakeBell.md`
- `Docs/AccountSyncLogs/2026-06-04_FinalDemoTinnitusAudioPoseBossTwoPoint.md`
- `Docs/AccountSyncLogs/2026-06-04_FinalDemoRainDirectOnlyNoAnchor.md`
- `Docs/AccountSyncLogs/2026-06-04_FinalDemoRainRestoreHapticLimit.md`
- `Docs/AccountSyncLogs/2026-06-04_FinalDemoRainLegacyNoComposite.md`
- `Docs/FinalDemoFlowAudioIntegration.md`
- `Docs/AudioCueUsage.md`
- `Assets/Scripts/Debug/BellRingerSpatialLightTextureSampleController.cs`
- `Assets/Scripts/Debug/TinnitusTestController.cs`
- `Assets/Scripts/Audio/TinnitusAudioController.cs`
- `Assets/Scripts/FinalDemo/FinalDemoDirector.cs`
- `Assets/Scripts/FinalDemo/FinalDemoLightRouter.cs`
- `Assets/Scripts/FinalDemo/FinalDemoAudioRouter.cs`
- `Assets/Scripts/FinalDemo/FinalDemoCueLibrary.cs`
- `Assets/Scripts/FinalDemo/FinalDemoAuthoringCapture.cs`
- `Assets/Scripts/FinalDemo/FinalDemoPoseAuthoringMarker.cs`
- `Assets/Scripts/Gameplay/PadPoseProvider.cs`
- `Assets/Scripts/Gameplay/PadPoseMatchEvaluator.cs`
- `Assets/ScriptableObjects/FinalDemo/FinalDemoCueLibrary.asset`
- `Assets/ScriptableObjects/FinalDemo/FinalDemoTuningProfile.asset`
- `Assets/Scenes/FinalDemo.unity`
- `arduino/HeadMpu9250LedBridge/HeadMpu9250LedBridge.ino`

## High-Level Finding

The current failures are not one brightness value or one missing clip. The FinalDemo scene has forked away from the test scenes in three places:

- Rain LED uses a FinalDemo-specific direct `LED rain` route plus a separate dense monitor preview, not the smoother sample scene rain grammar.
- Tinnitus audio uses a partly overridden procedural controller and a CueLibrary that does not fully match `FinalDemoFlowAudioIntegration.md`.
- Tinnitus/boss pose authoring mixes camera-space captured values, scene/world objects, transform-local marker values, and separate sound/LED ranges, so what is visible in the scene is not always the value used for matching.

The correct next implementation should restore a single tested grammar for each subsystem instead of adding more one-off multipliers.

## Rain LED Structure

Working sample scene behavior:

- `BellRingerSpatialLightTextureSampleController.SendRainFloor()` computes rain from head/camera look direction.
- It uses `rainBrightness = 0.07`, `rainNeutralRows = 2`, `rainLookDownRows = 5`, `litPixelScale`, `density`, `minimalHeight`, `phase`, and `visibility`.
- Looking up can return false and clear/hide rain. Looking down increases visible rows.
- The sample sends `HardwareBridge.SendLedRain(rainColor, level, seed, centerX, centerY, width, minimalHeight, phase, density, peakContrast)`.
- The sample preview and hardware both use the same drop-count/ring style logic.

Current FinalDemo behavior:

- `FinalDemoDirector.TickAmbientRainLightLayer()` calls `FinalDemoLightRouter.ShowRainFloorBand(_currentRainIntensity)` every `0.12s`.
- `FinalDemoLightRouter.ShowRainFloorBand()` always sends `SendLedRain(... cx=7.5 cy=0.65 w=16 h=2.4 density=0.9 contrast=1.75 ...)`.
- It currently multiplies hardware level by `rainHardwareBrightnessMultiplier = 0.1` and RGB by `rainHardwareColorMultiplier = 0.3`.
- The monitor preview does not use the same sparse drop grammar. `RenderRainLogicalFrame()` fills every x in bottom rows 0, 1, 2, and 3.
- `ResolveRainPixelBrightness()` keeps every rain pixel active with `0.7 + abs(sin(...)) * 0.3`, so density never really drops.
- Firmware `handleLedRainCommand()` computes `dropCount = 2 + density * 5`; FinalDemo density `0.9` becomes about 6 drops per command, with a newly incremented seed every call.

Why brightness-only fixes failed:

- The physical pattern is still dense because density stays high.
- The preview remains dense because it ignores density and fills the full bottom band.
- The perceived closed-eye brightness is dominated by lit pixel count, contrast, and update discontinuity, not only RGB or level.
- Updating every `0.12s` with a new seed makes the pattern jump, so it reads as choppy/dotted rather than smooth rain.

Recommended rain correction:

- Stop treating FinalDemo rain as a unique band renderer.
- Port the sample scene rain grammar into a reusable helper, then let FinalDemo call that helper.
- Expose at least these FinalDemo controls: physical brightness, preview brightness, density/lit-pixel scale, neutral rows, look-down rows, update interval, seed speed, phase speed, peak contrast.
- For safety, reduce density first, then brightness. A likely first target is density around `0.25-0.45`, contrast around `1.0-1.25`, hardware level/color around the current reduced range, and rows from the sample logic.
- Keep monitor preview and hardware using the same logical rain source. If preview must be boosted, apply preview-only multiplier after the logical frame is built.
- Do not reintroduce rain/bell frame compositing until the direct rain grammar is stable. Previous handoff logs show composite attempts caused full-bright bottom rows and stray red/green pixels.

## Rain Audio And Stage Timing

Current FinalDemo rain stage:

- `BeginRainLayer()` starts `RainLightBed` and `RainStrongBed` loops at volume scale 0.
- `TickRainLayer()` ramps `_currentRainIntensity` over `RainIntensityRampSeconds`.
- `Assets/ScriptableObjects/FinalDemo/FinalDemoTuningProfile.asset` currently stores `rainIntensityRampSeconds: 8`.
- `RainLightBed` is `Assets/Audio/Curated/Rain/Bed/04_mixkit_light_rain_loop_long.wav`.
- `RainStrongBed` is `Assets/Audio/Curated/Rain/Bed/07_gimi_long_rain_bed_alt.wav`.
- `RainCloseDrops` is `Assets/Audio/Curated/Rain/CloseDrops/02_mixkit_heavy_rain_drops.wav`.

Important separation:

- Rain sound can be adjusted with `rainAudioGainMultiplier` and cue volumes.
- Rain LED cannot be safely adjusted only through `_currentRainIntensity` because FinalDemo has fixed density and a dense preview renderer.

## Tinnitus Audio Structure

Working test scene behavior:

- `TinnitusTestController` creates a `TinnitusAudioController`.
- It passes both `continuousGlitchClip` and `shortGlitchClip`.
- The default controller values are close to the screenshot/test profile: volume around `0.052`, base frequency around `6627`, beat offset around `10`, `glitchDensity` around `0.455`, `roughness` around `0.528`, `burstIntensity` around `0.353`.
- The test UI can adjust these values live.

Current FinalDemo procedural behavior:

- `StartProceduralTinnitus()` calls `controller.SetGlitchClips(TinnitusLongGlitch, null)`.
- It sets `GlitchDensity = 0`, `Roughness = 0.18`, `BurstIntensity = 0`.
- It sets volume to `0.052 * GeneralTinnitusToneVolume * range01`.
- `FinalDemoTuningProfile.asset` stores `generalTinnitusToneVolume: 0.16`, so the base procedural tone can become very quiet.
- Match tone volume is also very low: `tinnitusMatchToneVolume: 0.0027`.

Interpretation:

- The short repeating tick was probably over-suppressed by disabling the short glitch and synthetic density, but the replacement went too far and removed much of the tinnitus identity.
- FinalDemo should not invent a separate silent/near-silent tinnitus preset. It should import the tested tinnitus preset and then apply a final demo volume multiplier.

Recommended tinnitus audio correction:

- Restore a named `TinnitusAudioSettings` preset shared by `TinnitusTestController` and `FinalDemoDirector`.
- In FinalDemo, remove only the specific unwanted periodic click source, not all glitch/instability.
- Use `TinnitusLongGlitch` and `TinnitusBurst` cues from the CueLibrary after fixing their clip mappings.
- Keep procedural base tone audible but quiet. A safe next test is to restore the test instability parameters and apply only a final bus/volume multiplier.
- Add operator UI text showing whether procedural tone, continuous glitch, short burst, healing loop, and match tone are active.

## Tinnitus CueLibrary Mismatches

`Docs/FinalDemoFlowAudioIntegration.md` says:

- Tinnitus long default: `Assets/Audio/Curated/Tinnitus/LongGlitch/04_mixkit_horror_radio_signal_long.wav`
- Tinnitus burst default: `Assets/Audio/Curated/Tinnitus/BurstGlitch/02_mixkit_small_electric_glitch.wav`
- Tinnitus pose lock: `Assets/Audio/Curated/Narration/Cue/11_mixkit_narration_soft_ui_risk.wav`
- Tinnitus pose lost: `Assets/Audio/Curated/Narration/Cue/05_mixkit_narration_tone_d.wav`
- Tinnitus healing loop: `Assets/Audio/Curated/Tinnitus/HealingMotion/01_bigsoundbank_radio_frequency_sweep_healing.wav`
- Tinnitus resolve default: `Assets/Audio/Curated/Tinnitus/HealingMotion/05_mixkit_fast_sci_fi_sweep.wav`

Current CueLibrary mappings:

| Cue | Current clip | Status |
| --- | --- | --- |
| TinnitusLongGlitch | `Assets/Audio/Curated/Tinnitus/LongGlitch/04_mixkit_horror_radio_signal_long.wav` | Matches document |
| TinnitusBurst | `Assets/Audio/Curated/Wind/Light/02_mixkit_wind_blowing_open_air_long.wav` | Wrong default |
| TinnitusBurst alternates | `05_mixkit_digital_signal_interference`, `04_mixkit_glitch_static`, `03_mixkit_electric_buzz_glitch` | Plausible alternates |
| TinnitusPoseLock | `Assets/Audio/Curated/Movement/Body/11_mixkit_open_ground_step_a_reality_risk.wav` | Wrong; explains footstep-like cue |
| TinnitusPoseLock alternate | `Assets/Audio/Curated/Tinnitus/Lock/02_mixkit_gear_fast_lock_tap.wav` | Plausible but not document default |
| TinnitusPoseLost | `Assets/Audio/Curated/Narration/Cue/05_mixkit_narration_tone_d.wav` | Matches document |
| TinnitusHealingLoop | `Assets/Audio/Curated/Tinnitus/HealingMotion/01_bigsoundbank_radio_frequency_sweep_healing.wav` | Matches document |
| TinnitusResolve | `Assets/Audio/Curated/Tinnitus/HealingMotion/05_mixkit_fast_sci_fi_sweep.wav` | Matches document |
| BossBasePulse | `Assets/Audio/Curated/Tinnitus/Boss/02_mixkit_heartbeat_boss_heavy_a.wav` | Matches document |
| BossGlitchBurst | `Assets/Audio/Curated/Tinnitus/Boss/12_mixkit_boss_electric_burst_b.wav` | Matches document |
| BossWeakpointMove | `Assets/Audio/Curated/Interaction/WallScan/09_mixkit_low_wall_scan_hit_a.wav` | Matches document |
| BossHit | `Assets/Audio/Curated/Interaction/WallScan/13_mixkit_hard_surface_return.wav` | Matches document |

Recommended cue correction:

- Change `TinnitusBurst` default to `Assets/Audio/Curated/Tinnitus/BurstGlitch/02_mixkit_small_electric_glitch.wav`.
- Change `TinnitusPoseLock` default to `Assets/Audio/Curated/Narration/Cue/11_mixkit_narration_soft_ui_risk.wav` or explicitly decide to use a `Tinnitus/Lock` candidate instead.
- Keep forest/birds only in `ForestBed` and forest ending stages. Do not assign forest or movement-body clips to tinnitus cues.

## General Tinnitus Pose Authoring

Runtime matching:

- `PadPoseProvider.CameraSpacePosition` applies correction: invert X, scale `(1.5, 1.25, 1.5)`, offset `(0, 0.1, 0)`.
- `PadPoseMatchEvaluator` compares `Vector3.Distance(CurrentCameraSpacePosition, targetCameraSpacePosition)`.
- General tinnitus requires position plus yaw/pitch/roll.

Authoring marker behavior:

- `FinalDemoPoseAuthoringMarker` has `useTransformLocalPose`.
- If `useTransformLocalPose` is true, `TargetCameraSpacePosition` is `transform.localPosition`.
- It also stores `targetCameraSpacePosition` captured from the pad.
- `FinalDemoDirector.ShouldUseStoredGeneralTinnitusPose()` overrides to stored values only when transform local pose looks impossible: magnitude over 3m or z over 3m.

Current scene data:

- `TinnitusA_HealPose_Authoring` has local position `(-1.3200003, -0.03, 65.420006)` and stored target `(-0.16, -0.03, 0.68)`.
- `TinnitusB_HealPose_Authoring` has local position `(15.79, 3.1, 74.34)` and stored target `(0.18, 0.02, 0.74)`.
- Both markers have `useTransformLocalPose: 1`.
- The code currently falls back to stored pose because the transform-local pose is impossible.

Why the marked Y can feel wrong:

- The visible scene marker position is not necessarily the target used by `PadPoseMatchEvaluator`.
- The match target may be the stored camera-space value, while the marker object remains far away in world/local scene coordinates.
- `PadPoseProvider` also adds `+0.1` Y offset and scales Y by `1.25`, so camera-space Y is not raw ArUco Y.
- If the operator moves a scene object visually but does not recapture/store the camera-space target, the visible marker and actual target diverge.

Recommended general tinnitus pose correction:

- Make captured camera-space pose the single source of truth for matching.
- Set `useTransformLocalPose = false` for general tinnitus capture markers, or create a true camera-space authoring root where local transform is guaranteed to equal camera-space.
- On every capture, update both stored values and a clear visible marker that is transformed through `PlayerCamera.TransformPoint(cameraSpacePosition)`.
- Add UI readout: target camera-space position, current camera-space position, per-axis delta X/Y/Z, position match, yaw/pitch/roll deltas, and whether stage lock has occurred.
- Add separate scene markers for approach/lock world position and pad answer pose. Do not reuse one object to mean both.

## General Tinnitus Stage Trigger

Current behavior:

- Stage order includes `GeneralTinnitusOne` then `GeneralTinnitusTwo`.
- `BeginGeneralTinnitusStage()` starts procedural tinnitus and sets the target pose.
- Before lock, `TickGeneralTinnitus()` only locks when planar distance to the tinnitus world position is within `GeneralTinnitusApproachRadius`.
- `FinalDemoTuningProfile.asset` stores `generalTinnitusApproachRadius: 1.25`.
- Sound/LED range is separate. `Range_Tinnitus_01` radius is `26`, `Range_Tinnitus_02` radius is `17.3`.

Why second tinnitus can look inactive:

- The player may be inside the sound/LED range but outside the smaller 1.25m approach-lock radius.
- The scene object's visible range circle is not the same as the lock trigger.
- `FinalDemo_TinnitusB` and `Range_Tinnitus_02` exist, but their world/local z positions are large scene-authored values. The lock trigger depends on the actual resolved world position and player planar distance.
- There is no strong stage-specific feedback when the player is in the large range but not close enough to lock.

Recommended second tinnitus correction:

- Add a separate `FinalDemoRangeAuthoring` or marker for `TinnitusLockRange_01` and `TinnitusLockRange_02`, visible in editor.
- Use that lock marker/radius for `_generalTinnitusPoseLocked`, not a hidden global `GeneralTinnitusApproachRadius`.
- Keep sound/LED range as a separate outer range.
- Add on-screen debug: current stage, tinnitus index, distance to outer range, distance to lock range, lock status.

## Boss Tinnitus Authoring

Current implementation:

- Boss has three `FinalDemoPoseAuthoringMarker` references: `BossPose_01`, `BossPose_02`, `BossPose_03`.
- Boss also has three `FinalDemoAuthoringPath` references: `BossWeakpointPath_01`, `_02`, `_03`.
- `FinalDemoAuthoringCapture` has buttons/hotkeys for only three boss pose captures: F7/F8/F9.
- `UpdateBossPatternTarget()` sets `RequireRotation = false`.
- It uses `TryResolveBossAuthoringPathTarget(patternIndex, moving ? move01 : 0f)` and that function selects waypoint 0 or 1 only.
- `BossWeakpointPath_03_Authoring` still has three waypoints in the scene, although the code ignores waypoint 3 after the recent two-point change.

Mismatch with current design request:

- User now wants three cleanse patterns, each with two explicitly capturable target positions: total 6 buttons.
- Current UI is still three pose buttons plus path waypoint capture.
- Current gameplay uses path waypoint positions, not six named answer-pose markers.

Recommended boss correction:

- Replace boss authoring UI with six explicit buttons:
  `Boss 1 Start`, `Boss 1 End`, `Boss 2 Start`, `Boss 2 End`, `Boss 3 Start`, `Boss 3 End`.
- Store each as camera-space pad target position. Boss should continue to ignore rotation unless user changes that rule.
- Keep visible world weakpoint markers by converting the captured camera-space values through the player camera.
- Remove or disable waypoint 3 from active authoring to avoid operator confusion.
- Add UI that clearly says boss matching uses position only.

## Light Priority And Simultaneous Output

Current `FinalDemoLightRouter` has priority order:

- `Pad = 5`
- `Rain = 10`
- `WallNoise = 20`
- `Tinnitus = 30`
- `Bell = 40`
- `CriticalPad = 50`

Current rain strategy:

- Direct rain is emitted only if `TryEmit(Rain)` succeeds.
- Higher-priority bell/tinnitus can temporarily suppress rain.
- Previous composite attempts tried to keep rain under bell, but handoff logs record that this caused full-bright rows and stray colors.

Recommended approach:

- First restore rain as a safe sparse direct pattern.
- Only after that, if simultaneous rain and bell is still required, create a single C# logical-frame compositor with explicit per-pixel masks and hardware safety caps.
- Do not mix firmware `LED rain` and C# `LED frame` routes in the same stage until the hardware preview and physical board are validated.

## Implementation Order

1. Freeze ad hoc multipliers and do not change gameplay until the following diagnostic changes are in place.
2. Add a rain diagnostic readout showing actual `level`, `RGB`, `density`, `height`, `contrast`, `seed step`, update interval, and physical/preview multipliers.
3. Extract sample-scene rain generation into a shared rain pattern helper and make FinalDemo use it.
4. Fix CueLibrary mappings for tinnitus cues against `FinalDemoFlowAudioIntegration.md`.
5. Restore FinalDemo tinnitus audio from the tested tinnitus preset, then apply one volume multiplier instead of overriding all instability values.
6. Split general tinnitus outer sound/LED range from inner lock range.
7. Make camera-space captured pose the single source of truth for general tinnitus matching, and show per-axis target/current/error UI.
8. Replace boss authoring with six explicit two-point capture targets.
9. Re-run the three test scenes and FinalDemo with the same pattern/audio/pose helper paths.
10. Only then tune brightness, density, range, and haptics for closed-eye comfort.

## QA Checklist After The Next Implementation Pass

- In `LightTextureTest` or the spatial sample, rain and FinalDemo rain use the same density/row/phase grammar.
- In FinalDemo, rain has fewer lit pixels and lower physical brightness, not just lower color values.
- Looking up reduces rain LED; looking down increases rows but does not fill the whole board at high brightness.
- Rain preview and hardware differ only by preview boost, not by pattern structure.
- `TinnitusBurst` does not play wind.
- `TinnitusPoseLock` does not play movement/footstep assets.
- General tinnitus 1 and 2 both show outer range, inner lock range, and lock state in UI.
- Capturing a tinnitus answer shows current/target/delta for X/Y/Z, and Y delta matches the physical movement expectation.
- Boss authoring exposes six capture buttons and the runtime uses exactly those six captured points.
- Forest birds and footstep/body sounds never play during general tinnitus unless explicitly assigned by a cue decision.

## Open Clarification

The latest user message says rain light must support an additional feature while rain is active, but the sentence ends after "동시에". Before implementing a new compositor or priority rule, clarify whether the required simultaneous behavior is:

- rain continues under bell light,
- bell light temporarily overrides rain,
- rain temporarily ducks while bell/tinnitus emits,
- or another specific behavior.
