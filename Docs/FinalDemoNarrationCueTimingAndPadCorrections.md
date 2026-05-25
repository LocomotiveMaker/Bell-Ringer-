# FinalDemo Narration Cue Timing And Pad Corrections

Last updated: 2026-05-26

This document locks the current FinalDemo narration set, its stage timing, the
queue/no-interrupt rule, and the current pad pose correction changes requested
for the playable demo.

Use this document together with:

- `Docs/FinalDemoFlowAudioIntegration.md`
- `Docs/FinalDemoDecisionLock.md`

If this document conflicts with older narration notes, this document wins.

## Scope

The current FinalDemo no longer uses the temporary 6-line `WindowsTemp` narration
set as the active plan. The active plan is the 11 generated Korean WAV files
placed directly under:

- `Assets/Audio/Curated/Narration`

The Korean filenames are acceptable. Do not rename them unless the user asks.

## Current Narration Asset Set

These are the current source files to assign in the cue library.

| Index | File | Line |
| --- | --- | --- |
| 1 | `Take1-2_정면을 바라본 채, 잠시 기다려 주세요._2026-05-26.wav` | `정면을 바라본 채, 잠시 기다려 주세요.` |
| 2 | `Take2-3_종소리를 따라,  천천히 이동하세요._2026-05-26.wav` | `종소리를 따라, 천천히 이동하세요.` |
| 3 | `Take3-1_다른 소리 사이에서도, 종소리를 놓치지 마세요._2026-05-26.wav` | `다른 소리 사이에서도, 종소리를 놓치지 마세요.` |
| 4 | `Take4-1_패드를 흔들어, 종소리를 다시 확인하세요._2026-05-26.wav` | `패드를 흔들어, 종소리를 다시 확인하세요.` |
| 5 | `Take5-1_종소리가 들리는 곳을 바라보세요._2026-05-26.wav` | `종소리가 들리는 곳을 바라보세요.` |
| 6 | `Take6-7_이제, 종은 당신의 손에 있습니다._2026-05-26.wav` | `이제, 종은 당신의 손에 있습니다.` |
| 7 | `Take7-1_거슬리는 소리가 나는 곳으로 향해보세요._2026-05-26.wav` | `거슬리는 소리가 나는 곳으로 향해보세요.` |
| 8 | `Take8-1_그 자리에 머물러 소리를 정화하세요._2026-05-26.wav` | `그 자리에 머물러 소리를 정화하세요.` |
| 9 | `Take9-3_소리의 위치에, 패드를 가져다대세요._2026-05-26.wav` | `소리의 위치에, 패드를 가져다대세요.` |
| 10 | `Take10-1_소리가 시작되는 지점을 찾으세요._2026-05-26.wav` | `소리가 시작되는 지점을 찾으세요.` |
| 11 | `Take11-1_움직이는 소리를 놓치지 마세요._2026-05-26.wav` | `움직이는 소리를 놓치지 마세요.` |

## Cue ID Mapping

Keep the existing cue IDs where they already match the new narration meaning.
Add only the missing IDs needed to cover the 11-line set.

### Existing cue IDs to keep

- `NarrFollowBell`: file 2
- `NarrPadShakeAssist`: file 4
- `NarrLookBell`: file 5
- `NarrFindTinnitusPose`: file 9
- `NarrHoldPose`: file 8
- `NarrBossTrack`: file 11

### New cue IDs to add

- `NarrFaceForwardWait`: file 1
- `NarrRainFocusBell`: file 3
- `NarrBellInHand`: file 6
- `NarrApproachTinnitus`: file 7
- `NarrFindSoundOrigin`: file 10

## Required Code Touch Points

These are the minimum implementation files that should reflect this document.

- `Assets/Scripts/FinalDemo/FinalDemoStage.cs`
- `Assets/Scripts/FinalDemo/FinalDemoCueLibrary.cs`
- `Assets/Scripts/FinalDemo/FinalDemoDirector.cs`
- `Assets/Scripts/FinalDemo/FinalDemoAudioRouter.cs`
- `Assets/Scripts/Gameplay/PadPoseProvider.cs`

Secondary files that should use corrected pad data if needed:

- `Assets/Scripts/FinalDemo/FinalDemoAuthoringCapture.cs`
- `Assets/Scripts/Gameplay/PadPoseMatchEvaluator.cs`
- observer/debug panels that display pad position or rotation

## Stage Timing Map

This is the current desired timing for each narration cue in FinalDemo.

### 1. `NarrFaceForwardWait`

- File: `Take1-2_정면을 바라본 채, 잠시 기다려 주세요._2026-05-26.wav`
- Stage: `Preflight`
- Timing:
  play once after required readiness is valid and the operator is about to start
  the experience, before `OpeningAmbience`.
- Repeat rule:
  do not repeat during the same run. Allow replay only after full reset to
  `Preflight`.

### 2. `NarrFollowBell`

- File: `Take2-3_종소리를 따라,  천천히 이동하세요._2026-05-26.wav`
- Stage: `BellFollowOne`
- Timing:
  play once after the first follow target is committed and the bell has started
  calling from that position.
- Repeat rule:
  one time per run.

### 3. `NarrRainFocusBell`

- File: `Take3-1_다른 소리 사이에서도, 종소리를 놓치지 마세요._2026-05-26.wav`
- Stage: `BellFollowRain`
- Timing:
  play once on first entry into the rain/wind masking section, after the rain bed
  has audibly started.
- Repeat rule:
  one time per run.

### 4. `NarrPadShakeAssist`

- File: `Take4-1_패드를 흔들어, 종소리를 다시 확인하세요._2026-05-26.wav`
- Stage: `BellFollowOne`, `BellFollowRain`
- Timing:
  use as an assist cue when the player has been lost long enough for bell assist,
  or when the design explicitly wants to teach the pad shake interaction.
- Repeat rule:
  allow repeated use, but not more than once per assist cooldown window.
- Extra rule:
  do not spam this cue every time the player shakes the pad. It is a narrated
  hint, not the shake response sound itself.

### 5. `NarrLookBell`

- File: `Take5-1_종소리가 들리는 곳을 바라보세요._2026-05-26.wav`
- Stage: `BellGaze`
- Timing:
  play once after rain/wind has stopped or dropped sharply and the first stable
  gaze bell target is active.
- Repeat rule:
  one time per run.

### 6. `NarrBellInHand`

- File: `Take6-7_이제, 종은 당신의 손에 있습니다._2026-05-26.wav`
- Stage: `BellAcquisition`
- Timing:
  play once after the acquisition sound/haptic has begun and before the tinnitus
  section starts.
- Repeat rule:
  one time per run.

### 7. `NarrApproachTinnitus`

- File: `Take7-1_거슬리는 소리가 나는 곳으로 향해보세요._2026-05-26.wav`
- Stage: `GeneralTinnitusOne`
- Timing:
  play once on the first general tinnitus stage after the acquisition transition
  silence.
- Repeat rule:
  one time per run by default.
- Note:
  do not play this again automatically on `GeneralTinnitusTwo` unless testing
  proves the second encounter still needs spoken guidance.

### 8. `NarrHoldPose`

- File: `Take8-1_그 자리에 머물러 소리를 정화하세요._2026-05-26.wav`
- Stage: `GeneralTinnitusOne`, optional for `GeneralTinnitusTwo`
- Timing:
  play when the pad first enters a valid cleanse hold state and actual progress
  has started.
- Repeat rule:
  default is once on the first successful hold of the first tinnitus encounter.
- Note:
  this line is the replacement for the older shorter line `그 자리에 머물러 주세요.`

### 9. `NarrFindTinnitusPose`

- File: `Take9-3_소리의 위치에, 패드를 가져다대세요._2026-05-26.wav`
- Stage: `GeneralTinnitusOne`, optional for `GeneralTinnitusTwo`
- Timing:
  use after the player has already reached the tinnitus encounter but has not made
  pose-match progress for a short time.
- Suggested trigger:
  player is in the tinnitus stage, has faced/revealed the tinnitus, but progress
  is still `0` after an assist delay.
- Repeat rule:
  do not queue this immediately after `NarrApproachTinnitus`. Leave a meaningful
  gap and only use it if the player still needs pose guidance.

### 10. `NarrFindSoundOrigin`

- File: `Take10-1_소리가 시작되는 지점을 찾으세요._2026-05-26.wav`
- Stage: `BossPatternOne`
- Timing:
  play once at the beginning of the first boss pattern, before the moving phase
  starts.
- Repeat rule:
  one time per run.

### 11. `NarrBossTrack`

- File: `Take11-1_움직이는 소리를 놓치지 마세요._2026-05-26.wav`
- Stage: `BossPatternOne`
- Timing:
  play once when the first boss pattern exits the fixed opening hold and enters
  the moving chase portion.
- Repeat rule:
  one time per run.

## Recommended Stage Order Summary

This is the concise route-level order of narration use.

1. `Preflight`: 1
2. `BellFollowOne`: 2
3. `BellFollowRain`: 3, then 4 as needed
4. `BellGaze`: 5
5. `BellAcquisition`: 6
6. `GeneralTinnitusOne`: 7, then 9 if no progress, then 8 on first valid hold
7. `GeneralTinnitusTwo`: normally no narration
8. `BossPatternOne`: 10, then 11
9. `ForestEnding`: no narration

## Narration Queue Rules

The user explicitly wants narration to never interrupt another narration line.

### Required behavior

- Use one narration playback lane only.
- If a narration trigger occurs while another narration clip is playing, queue the
  new narration instead of interrupting.
- The next narration starts only after the previous narration clip has fully
  finished.

### Queue policy

- Queue order must be FIFO.
- The same narration cue ID should not exist more than once across
  `currently playing + queued`.
- Before a queued narration starts, validate that its stage/condition is still
  relevant. If stale, drop it.
- Clear the queued narration list when:
  - the run is reset to `Preflight`
  - the current stage is force-switched
  - the current stage is reset
  - all outputs are force-stopped

### Why stale validation is required

Without stale validation, an old help line can play too late and become nonsense.
Example:

- a pose-help line gets queued
- the player already clears the tinnitus
- the queued line finally plays after the stage has ended

That must not happen.

## Narration Audio Rules

### Assignment rules

- Every narration line must have its own distinct `FinalDemoCueId`.
- Every narration line must have its own distinct `FinalDemoCueEntry`.
- Do not share one cue entry across multiple narration files.
- Keep narration cues on `FinalDemoAudioBus.Narration`.

### Playback rules

- `loop = false`
- `spatialized = false` by default for the current demo
- play from front-center intent, but do not rely on hard 3D panning
- keep the voice mostly dry and readable

### Inspector volume rules

Per-cue narration volume must be adjustable in the inspector through:

- `FinalDemoCueEntry.defaultVolume`

Global master volume must exist and remain adjustable in the inspector through:

- `FinalDemoAudioRouter.masterVolume`

Global narration bus gain must remain adjustable in the inspector through:

- `FinalDemoAudioRouter.narrationBusGain`

Narration ducking must remain adjustable in the inspector through:

- `FinalDemoAudioRouter.narrationDuckMultiplier`
- `FinalDemoAudioRouter.defaultNarrationDuckSeconds`

Practical meaning:

- if one narration line is too loud or too quiet, change that cue's
  `defaultVolume`
- if all narration is too loud or too quiet, change `narrationBusGain`
- if the whole game is too loud or too quiet, change `masterVolume`

## Current FinalDemo Implementation Gap

The current codebase already has some narration support, but not the full 11-line
set.

### Already present in code

Existing narration cue IDs:

- `NarrFollowBell`
- `NarrLookBell`
- `NarrPadShakeAssist`
- `NarrFindTinnitusPose`
- `NarrHoldPose`
- `NarrBossTrack`

These are already declared in:

- `Assets/Scripts/FinalDemo/FinalDemoStage.cs`
- `Assets/Scripts/FinalDemo/FinalDemoCueLibrary.cs`

### Still missing for the new 11-line set

Add these cue IDs and cue entries:

- `NarrFaceForwardWait`
- `NarrRainFocusBell`
- `NarrBellInHand`
- `NarrApproachTinnitus`
- `NarrFindSoundOrigin`

### Current director wiring that should be expanded

The current `FinalDemoDirector` already plays some narration cues, but it does
not yet cover the full 11-line timing map above. The new stage timings should be
implemented there or in a dedicated narration scheduler owned by the director.

## Pad Pose Correction Changes

These changes are for the currently implemented FinalDemo runtime.

### Requested fixes

1. Pad pitch is currently applied in the wrong direction.
2. Pad forward/back movement is currently reversed.
3. Pad left/right and forward/back position application is reversed.
4. All pad movement axes should be amplified more strongly.
5. The movement amplification must be adjustable in the inspector.

### Recommended implementation location

Apply these corrections in:

- `Assets/Scripts/Gameplay/PadPoseProvider.cs`

Do not scatter these sign flips across the director, evaluator, and debug views.
The provider should be the single place where corrected pad pose values are
 produced.

### Recommended new inspector fields in `PadPoseProvider`

Add a dedicated correction block such as:

- `invertPitch`
- `invertCameraSpacePositionX`
- `invertCameraSpacePositionZ`
- `cameraSpacePositionAxisScale`

Recommended type for movement scaling:

- `Vector3 cameraSpacePositionAxisScale`

Reason:

- the user wants all axes to be amplified
- a `Vector3` keeps one inspector location while still allowing per-axis tuning
- it is safer than hardcoding one global multiplier that later fails on one axis

### Required correction behavior

- Correct pitch before resolving `ResolvedPitchDegrees`.
- Correct camera-space position before exposing `CameraSpacePosition`.
- Invert X for left/right position correction.
- Invert Z for front/back position correction.
- Apply axis scaling after inversion.

Suggested order:

1. read raw camera-space position
2. invert required axes
3. scale corrected X/Y/Z
4. expose corrected position to the rest of gameplay

### Rotation rule

Pitch should be negated before it becomes the resolved pad pitch used by:

- pose matching
- authoring capture
- debug summaries
- observer views

### Position rule

The corrected camera-space position should become the only position that other
 systems consume. This means:

- `PadPoseMatchEvaluator` should compare against corrected position
- `FinalDemoAuthoringCapture` should display and capture corrected position
- debug overlays should show corrected position, not raw tracking receiver values

## Implementation Safety Notes

- Do not update only the gameplay match logic while leaving debug/authoring views
  on raw values. That creates false tuning and makes the user's inspector edits
  misleading.
- Do not let narration cues pile up endlessly. The FIFO queue must be bounded by
  stale validation and duplicate suppression.
- Do not let ending narration return. The current FinalDemo ending remains
  narration-free.
