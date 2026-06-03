# FinalDemo Bell Follow Near Interval / Gate Visibility - 2026-06-03

Context:
- User requested two polish changes:
  - During the first and second bell follow stages, bell call intervals should become up to 40% faster as the player approaches the bell.
  - `firMove`, `secondMov`, `firTin`, and `secondTin` should be visible white authoring walls in the editor but transparent during play.

Implemented:
- `Assets/Scripts/FinalDemo/FinalDemoTuningProfile.cs`
  - Added `bellFollowNearIntervalReduction`, default `0.4`.
  - This means the scheduled bell interval can be reduced by up to 40% near the arrival point.

- `Assets/Scripts/FinalDemo/FinalDemoDirector.cs`
  - `ResolveNextBellFollowCallIntervalSeconds()` now applies a distance-based multiplier during `BellFollowOne` and `BellFollowRain`.
  - The proximity value is based on the distance from the stage start point to the target, with `BellArrivalRadius` treated as the closest point.
  - Added progress-gate-specific barrier handling for `firMove`, `secondMov`, `firTin`, `secondTin`.
  - During play, these gate colliders remain active when blocking, but their renderers are forcibly disabled.
  - In edit mode, `OnValidate()` enables these four renderers and applies a white material property block for authoring visibility.

- `Assets/Scripts/Debug/Editor/FinalDemoDirectorInspector.cs`
  - Exposed `bellFollowNearIntervalReduction` in FinalDemoRoot Quick Tuning under Bell Timing / Follow / Gaze.

Validation:
- `powershell -ExecutionPolicy Bypass -File tools\Build-DotNetProject.ps1 -ProjectPath BellRinger.Runtime.csproj`
  - Passed. Existing `PadTrackingReceiver` serialization warnings appeared in the parallel run.
- Parallel Editor build hit the usual temporary `obj\Debug\BellRinger.Runtime.dll` file lock.
- Re-run:
  - `powershell -ExecutionPolicy Bypass -File tools\Build-DotNetProject.ps1 -ProjectPath BellRinger.Editor.csproj`
  - Passed with 1 existing analyzer warning and 0 errors.

QA notes:
- In `FinalDemo`, during `BellFollowOne` and `BellFollowRain`, stand far from the bell and wait for calls, then approach the bell. Calls should gradually tighten, up to 40% faster near the arrival radius.
- In the editor, `firMove`, `secondMov`, `firTin`, and `secondTin` should be visible white blocks for placement.
- In Play Mode, those four walls should not be visible, but should still block movement until their stage condition opens them.

Notebook compatibility:
- No COM port, Arduino firmware, device path, or notebook-specific behavior changed.
