# Final Demo Rain, Footsteps, Boss Feedback Changes

Date: 2026-06-04

## Modified Areas

- Restored the rain arrival outro so the transition into `BellGaze` preserves the rain audio loops and lets rain audio fade out over the existing 3 second setting while rain light fades out over the existing 1.5 second setting.
- Added final-demo footstep playback using `Assets/Audio/RawCandidates/Footstep/footsteps_1.wav` and `footsteps_2.wav`.
- Footsteps now play from the player's lower foot position at a small volume and alternate `FootstepOne` -> `FootstepTwo` while actual planar movement continues.
- Changed the tinnitus match sine tone range from `40-120Hz` to `40-100Hz`.
- Kept the match tone source at the answer position for both general tinnitus and boss tinnitus through the existing match feedback path.
- Changed boss approach start detection to use the `BossTinnitus` lock center with an x/z +-5m box instead of the previous radius check against the boss visual position.
- Changed boss pattern haptics so entering the exact tolerance triggers one strong pulse, waits 1 second with no cleanse hum, then emits only the weakest cleanse hum while the pad remains inside tolerance.
- Adjusted the rain outro so its 3 second audio fade and 1.5 second light fade start immediately on `BellGaze` entry, not after a later bell-gaze success.
- Raised the answer-position sine tone volume to `0.01053` in both code defaults and `FinalDemoTuningProfile.asset`, and made the `FinalDemoDirector` inspector label explicit as `Answer sine tone volume`.
- Added boss cleanse narration after boss pattern 1 and 2 using `Take15-1_두번째 정화를 시작하세요._2026-06-04.wav` and `Take16-1_마지막 정화를 시작하세요_2026-06-04.wav`.
- Disabled the runtime `FinalDemo_VisualHalo` attachment path by removing the `EnsureVisualHalos()` startup call.
- Preserved active rain outro loops across stage changes until the 3 second audio fade completes, so a fast bell-gaze completion cannot cut the rain fade early.
- Kept `BossBasePulse` as the boss approach loop and stopped it explicitly on boss pattern entry; boss pattern no longer restarts the base pulse.
- Made boss tinnitus light use the boss sound/light range with 1.2x intensity and 1.5x board range compared with the normal boss pattern call.
- Made boss capture buttons apply immediately to the active boss pattern, matching the general tinnitus capture behavior.
- Changed active boss weakpoint targeting to read `BossWeakpoint_Current` scene position first, so moving that object in the scene controls the in-game weakpoint location.
- Added the boss-approach entry narration by queuing `NarrBossTrack` in `BeginBossApproach()` and mapping it to `Assets/Audio/Curated/Narration/Take10-1_진동이 느껴지는 지점을 찾으세요._2026-05-26.wav`.

## Files Changed

- `Assets/Scripts/FinalDemo/FinalDemoDirector.cs`
- `Assets/Scripts/FinalDemo/FinalDemoFootstepAudio.cs`
- `Assets/Scripts/FinalDemo/FinalDemoFootstepAudio.cs.meta`
- `Assets/Scripts/FinalDemo/FinalDemoAuthoringCapture.cs`
- `Assets/Scripts/FinalDemo/FinalDemoStage.cs`
- `Assets/Scripts/FinalDemo/FinalDemoCueLibrary.cs`
- `Assets/Scripts/FinalDemo/FinalDemoTuningProfile.cs`
- `Assets/Scripts/FinalDemo/FinalDemoMatchToneFeedback.cs`
- `Assets/Scripts/Debug/Editor/FinalDemoDirectorInspector.cs`
- `Assets/ScriptableObjects/FinalDemo/FinalDemoTuningProfile.asset`
- `Assets/ScriptableObjects/FinalDemo/FinalDemoCueLibrary.asset`
- `BellRinger.Runtime.csproj`
