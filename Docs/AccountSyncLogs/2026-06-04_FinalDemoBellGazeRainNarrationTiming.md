# FinalDemo Bell Gaze / Rain Narration Timing - 2026-06-04

Context:
- User requested only three changes:
  - Bell gaze bell calls about 30% faster.
  - Rain sound about 30% quieter.
  - In rain stage, `NarrRainFocusBell` should play 3 seconds after `NarrBellEscaped`, and `NarrPadShakeAssist` should be invalid during the 2 seconds after `NarrRainFocusBell`.

Implemented:
- `Assets/Scripts/FinalDemo/FinalDemoTuningProfile.cs`
  - Reduced `bellGazeCallIntervalSeconds` default from `1.5` to `1.05`.
  - Reduced `rainAudioGainMultiplier` default from `0.975` to `0.68`.

- `Assets/Scripts/FinalDemo/FinalDemoDirector.cs`
  - Added `HandleNarrationStarted(...)` to chain `NarrRainFocusBell` from the actual start/length of `NarrBellEscaped`.
  - Removed the old rain-loop-time-based `NarrRainFocusBell` trigger.
  - Added a temporary block window so `NarrPadShakeAssist` cannot queue or remain valid while `NarrRainFocusBell` is active and for 2 seconds after.
  - Moved pad-shake narration cooldown stamping to real playback time instead of queue time.

Validation:
- Needs runtime QA in `FinalDemo` for subjective timing and narration overlap.
- Intended behavior is now:
  - Enter rain stage.
  - `NarrBellEscaped` still starts on its existing delay.
  - After that narration finishes, wait 3 seconds.
  - Then play `NarrRainFocusBell`.
  - Any `NarrPadShakeAssist` attempt during that narration or within 2 seconds after is dropped.

