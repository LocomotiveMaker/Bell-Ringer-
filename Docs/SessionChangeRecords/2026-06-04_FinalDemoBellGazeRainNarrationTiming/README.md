# FinalDemo Bell Gaze / Rain Narration Timing - 2026-06-04

## Changed

- Reduced `BellGaze` bell-call interval by about 30%.
- Reduced rain audio gain by about 30%.
- Changed rain-stage narration timing:
  - `NarrRainFocusBell` now queues after `NarrBellEscaped` finishes, plus 3 more seconds.
  - `NarrPadShakeAssist` is blocked while `NarrRainFocusBell` is playing and for 2 seconds after it ends.
  - `NarrPadShakeAssist` cooldown now applies when the narration actually plays, not when it is only queued.

## Unchanged

- No bell model, pad model, light-router visuals, tinnitus, boss, COM port, or tracker behavior changed in this pass.

