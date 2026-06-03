# FinalDemo Rain / Tinnitus Lock / Orbit Schedule - 2026-06-03

Context:
- User reported that general tinnitus still was not snapping to a clear fixed point.
- Rain LED still was not visible in `FinalDemo`, including the 16x8 preview.
- Progress walls (`firMove`, `secondMov`, `firTin`, `secondTin`) were impossible to verify because they were invisible.
- User wanted the opening bell orbit sound timing to be manually authorable from the root inspector for tomorrow's exhibit.

Implemented:
- `Assets/Scripts/FinalDemo/FinalDemoDirector.cs`
  - Added root-inspector `openingBellCueSchedule` and `bellOrbitIdleAnchorScale`.
  - `BellOrbit` now plays bell cues from the inspector schedule when entries exist instead of using the old fixed interval.
  - General tinnitus lock now prefers an explicit authored lock point transform instead of reversing the pad pose.
    - Supported names: `TinnitusA_LockPoint_Authoring`, `TinnitusB_LockPoint_Authoring`
    - Fallback: existing `TinnitusA_HealPose_Authoring` / `TinnitusB_HealPose_Authoring` transform position if close enough to the tinnitus object.
    - Final fallback: stable 0.9 m stand-off snap from the tinnitus toward the approaching player.
  - Rain LED is now emitted from a late ambient pass during `BellFollowRain`, so bell movement/anchor updates no longer hide it every frame.
  - `BellFollowRain` no longer uses the continuous bell anchor while the rain layer is active.
  - Bell movement texture keeps its audio during `BellFollowRain`, but its LED anchor no longer suppresses rain visibility.
  - Authoring barriers are now visible at runtime for verification when they are blocking.

- `Assets/Scripts/FinalDemo/FinalDemoLightRouter.cs`
  - Bell wave brightness cap increased only slightly while wave radius/width increased.
  - Rain preview/hardware intensity received extra boost so the 16x8 monitor preview reads more clearly.
  - Rain logical frame now fills more lower rows to make the floor band readable.

- `Assets/Scripts/FinalDemo/FinalDemoOperatorControls.cs`
  - Added extra preview-only brightness multiplication so the runtime 16x8 preview is easier to read without changing all gameplay light values.

- `Assets/Scripts/FinalDemo/FinalDemoTuningProfile.cs`
  - Default rain ramp increased from 3 seconds to 5 seconds.

- `Assets/Scripts/Debug/Editor/FinalDemoPolishTool.cs`
  - Updated the polish tool default rain ramp to 5 seconds for consistency.

Validation:
- `powershell -ExecutionPolicy Bypass -File tools\Build-DotNetProject.ps1 -ProjectPath BellRinger.Runtime.csproj`
- `powershell -ExecutionPolicy Bypass -File tools\Build-DotNetProject.ps1 -ProjectPath BellRinger.Editor.csproj`
- Build passed. The first runtime build attempt hit the usual `obj\Debug\BellRinger.Runtime.dll` file lock, then the rerun succeeded.

Notes:
- To author a precise general tinnitus snap point in-scene, place or move:
  - `TinnitusA_LockPoint_Authoring`
  - `TinnitusB_LockPoint_Authoring`
  If those do not exist, moving `TinnitusA_HealPose_Authoring` / `TinnitusB_HealPose_Authoring` near the tinnitus also works.
- Notebook compatibility remains unchanged. This work only touches local Unity C# runtime/editor code paths.
