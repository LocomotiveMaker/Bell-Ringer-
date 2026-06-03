# FinalDemo Bell Shake / Audio Inspector - 2026-06-03

Context:
- User asked to implement the previously planned items 4 and 5:
  - Bell follow audio and pad-shake bell response cleanup.
  - Make the FinalDemoRoot inspector expose the audio cue structure and periodic tuning values.

Implemented:
- `Assets/Scripts/FinalDemo/FinalDemoDirector.cs`
  - Pad shake bell assist is no longer only called from `TickBellFollow`.
  - Added a stage-aware pad shake resolver:
    - `BellFollowOne` plays the pad-shake bell response from the first follow bell target.
    - `BellFollowRain` plays it from the second follow bell target.
    - `BellAcquisition` and later implemented stages play it from the in-hand bell position at the user center.
    - Bell relocation/orbit movement still does not respond to pad shake.
  - Kept `BellDistantCall` follow scheduling in one shared resolver so follow stage 1 and follow stage 2 use the same interval rules:
    - initial interval
    - minimum interval
    - miss interval reduction
    - post-clip gap through `ScheduleNextCueAfterPlayback`

- `Assets/Scripts/Debug/Editor/FinalDemoDirectorInspector.cs`
  - Added a custom inspector for `FinalDemoDirector`.
  - Selecting `FinalDemoRoot` now shows `FinalDemoRoot Quick Tuning` under the normal inspector.
  - The new section exposes editable `FinalDemoCueLibrary` cue rows:
    - cue id / label
    - main clip
    - bus
    - default volume
    - loop
    - spatialized
    - min/max distance
  - The same root inspector also exposes grouped `FinalDemoTuningProfile` values:
    - bell timing / follow / gaze
    - pad shake
    - LED / sound reactive
    - walls / progress conditions
    - tinnitus / boss tinnitus
  - `bellFollowCallIntervalSeconds` is shown as a legacy/compatibility value because current follow calls use the initial/minimum/reduction schedule.

- `BellRinger.Editor.csproj`
  - Added the new custom inspector file to the local editor build project so command-line validation compiles it.

Validation:
- `powershell -ExecutionPolicy Bypass -File tools\Build-DotNetProject.ps1 -ProjectPath BellRinger.Editor.csproj`
  - Passed. It also compiled the runtime dependency.
  - Re-run after the legacy interval label change passed with only the pre-existing `USG0001` analyzer warning.
- `powershell -ExecutionPolicy Bypass -File tools\Build-DotNetProject.ps1 -ProjectPath BellRinger.Runtime.csproj`
  - First parallel attempt hit the usual temporary `obj\Debug\BellRinger.Runtime.dll` file lock.
  - Rerun passed with 0 warnings and 0 errors.

QA notes:
- In `FinalDemo`, enter `BellFollowOne` and shake the pad. The bell response should come from target 1.
- Enter `BellFollowRain` after the second relocation completes and shake the pad. The bell response should come from target 2.
- After `BellAcquisition`, shake the pad during tinnitus/boss/forest stages. The bell response should be centered at the in-hand bell offset.
- Select `FinalDemoRoot` in the scene hierarchy and check the `FinalDemoRoot Quick Tuning` foldouts in the Inspector.

Notebook compatibility:
- No runtime device path, COM port, Arduino firmware, or notebook-specific behavior changed.
- The inspector addition is Editor-only.
