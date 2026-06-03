# FinalDemo Tinnitus Lock / LED / Walls - 2026-06-03

Context:
- User requested three linked fixes in `FinalDemo`.
- General tinnitus should keep the authored world positions, but when the player approaches it should snap the player into the intended observation spot and then lock movement.
- Boss tinnitus should use authored world/path positions but ignore rotation matching.
- Tinnitus and boss LED output needed to shrink in both brightness and visible footprint.
- Pad yellow light should stay available as a feature, but stop appearing during gameplay.
- Authoring wall objects should become invisible blockers instead of being destroyed or left visible.

Implemented:
- `Assets/Scripts/FinalDemo/FinalDemoPoseAuthoringMarker.cs`
  - Added accessors for stored pad target position/rotation so gameplay can use the saved authored pose instead of accidentally reading the broken transform-local values.

- `Assets/Scripts/FinalDemo/FinalDemoDirector.cs`
  - General tinnitus now resolves its pad target from stored authored pose values when markers are present.
  - General tinnitus approach lock now snaps the player rig on the XZ plane so the tinnitus lands at the intended camera-space target, then locks movement.
  - Boss patterns now set `RequireRotation = false` and use position-only pose matching.
  - Pad anchor light updates are restricted to `Preflight` only.
  - Legacy rain/sky helper meshes are hidden at runtime instead of destroyed.
  - `BackWall_White_Authoring*`, `frontWall_White_Authoring`, `LeftSoftWall_Authoring`, `RightSoftWall_Authoring`, `firMove`, `secondMov`, `firTin`, `secondTin` are configured as invisible colliders at runtime.
  - Stage-gated blockers now open/close from stage state:
    - `firMove` opens after `BellFollowOne`
    - `secondMov` opens after `BellFollowRain`
    - `firTin` opens after first general tinnitus pose lock
    - `secondTin` opens after second general tinnitus pose lock

- `Assets/Scripts/FinalDemo/FinalDemoLightRouter.cs`
  - Added separate reduction multipliers for tinnitus/boss intensity, board range, and pattern size.
  - Current default behavior halves tinnitus/boss brightness and visible footprint relative to the prior output.

Validation:
- `powershell -ExecutionPolicy Bypass -File tools\Build-DotNetProject.ps1 -ProjectPath BellRinger.Runtime.csproj`
- `powershell -ExecutionPolicy Bypass -File tools\Build-DotNetProject.ps1 -ProjectPath BellRinger.Editor.csproj`
- Build passed using the local validation runtime in `tools\.runtime`.

Notes:
- The first build attempt hit a file lock on `obj\Debug\BellRinger.Runtime.dll`; a subsequent build completed successfully.
- This change keeps notebook compatibility because it only touches FinalDemo scene/runtime logic and local C# code paths.
