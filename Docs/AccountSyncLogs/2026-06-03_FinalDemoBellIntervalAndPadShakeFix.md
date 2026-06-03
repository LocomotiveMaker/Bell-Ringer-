# FinalDemo Bell Interval / Pad Shake Fix - 2026-06-03

Context:
- User reported that bell calls in the follow section were still too frequent.
- User also reported that shaking the pad did not produce a bell response.
- User suspected multiple bell sounds were running on different schedules.

Findings:
- Follow `BellDistantCall` already used the slower shared schedule.
- However, `BellStrongAssist` was also triggered independently after `bellAssistTimeoutSeconds`, repeating on `bellAssistRepeatSeconds`.
- This allowed two different bell one-shots to overlap or alternate on different intervals.
- Pad shake was gated only by `PadImuReceiver.MotionIntensity01`, with a relatively high default threshold and no visible reason when blocked.

Implemented:
- `Assets/Scripts/FinalDemo/FinalDemoTuningProfile.cs`
  - Added `bellFollowAutomaticStrongAssistSound`, default false.
  - Lowered default `padShakeAssistMotionThreshold` from `0.58` to `0.25`.
  - Lowered default `padShakeAssistCooldownSeconds` from `4.5` to `2`.

- `Assets/Scripts/FinalDemo/FinalDemoDirector.cs`
  - Automatic follow assist no longer plays `BellStrongAssist` unless `bellFollowAutomaticStrongAssistSound` is explicitly enabled.
  - Assisted scheduled `BellDistantCall` still gets the configured assist gain.
  - Pad shake bell response now records a clear status string:
    - stage disabled
    - bell relocating
    - Pad IMU missing
    - Pad IMU stale
    - motion below threshold
    - cooldown
    - played
  - When `BellPadShakeResponse` plays during follow stages, the next scheduled `BellDistantCall` is pushed back so the two bell cues do not immediately overlap.

- `Assets/Scripts/FinalDemo/FinalDemoInputStatus.cs`
  - Runtime status text now shows:
    - `Pad IMU motion`
    - `Pad shake bell` last decision/status

- `Assets/Scripts/Debug/Editor/FinalDemoDirectorInspector.cs`
  - Root quick tuning now exposes `BellStrongAssist 자동 반복음`.

Bell cue behavior after this change:
- Follow scheduled bell:
  - `BellDistantCall`
  - Uses the slow initial/minimum/reduction interval schedule.
- Follow automatic assist:
  - Does not play separate `BellStrongAssist` by default.
  - Can be re-enabled from FinalDemoRoot quick tuning.
- Pad shake:
  - `BellPadShakeResponse`
  - Uses pad shake threshold/cooldown.
  - Pushes the next scheduled follow bell back to avoid immediate double bell.
- Bell relocation:
  - `BellMovementTexture`
  - Still only a short movement loop while the bell moves.

Validation:
- `powershell -ExecutionPolicy Bypass -File tools\Build-DotNetProject.ps1 -ProjectPath BellRinger.Editor.csproj`
  - Passed. It also compiled the runtime dependency.
- `powershell -ExecutionPolicy Bypass -File tools\Build-DotNetProject.ps1 -ProjectPath BellRinger.Runtime.csproj`
  - First parallel attempt hit the usual temporary `obj\Debug\BellRinger.Runtime.dll` file lock.
  - Rerun passed with 0 warnings and 0 errors.

QA notes:
- In `FinalDemo`, watch the runtime status panel:
  - `Pad IMU motion` should rise when the pad is shaken/rotated.
  - `Pad shake bell` should say why it did or did not play.
- If `Pad IMU motion` never rises above `0.25`, lower `패드 흔들기 / 흔들림 감지 기준` from FinalDemoRoot quick tuning.
- Keep `BellStrongAssist 자동 반복음` off unless a deliberately more intrusive assist bell is needed.

Notebook compatibility:
- No device path, COM port, Arduino firmware, or notebook-specific runtime behavior changed.
