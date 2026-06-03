# FinalDemo Range / Rain / Tinnitus Feedback - 2026-06-03

Context:
- User confirmed the final direction for the latest FinalDemo polish pass.
- Goals were to keep bell sound/light range aligned, keep rain light underneath bell light, separate tinnitus lock radius from the larger sound/LED range, reduce hardware rain brightness without changing the monitor preview, and make tinnitus pose progress more visible.

Implemented:
- `Assets/Scripts/FinalDemo/FinalDemoTuningProfile.cs`
  - `GeneralTinnitusApproachRadius` no longer multiplies by `GeneralTinnitusRadiusScale`.
  - This separates the close-range player snap/lock trigger from the larger visual/sound range authoring scale.

- `Assets/Scripts/FinalDemo/FinalDemoDirector.cs`
  - Bell follow anchor light now remains available in the follow stages even when no bell clip is actively playing, so the bell location can stay faintly readable.
  - General tinnitus match tone now plays from the actual current pad target pose world position, not from the tinnitus body.
  - Existing FinalDemo procedural tinnitus generation remains disabled by default through `FinalDemoProceduralTinnitusEnabled`.

- `Assets/Scripts/FinalDemo/FinalDemoLightRouter.cs`
  - Rain LED can now composite under the current higher-priority logical frame instead of disappearing while bell/tinnitus output is held.
  - Rain keeps lower priority; bell/tinnitus pixels are not overwritten by rain.
  - Hardware-only rain brightness multiplier remains `0.3`, preserving the brighter monitor preview.

- `Assets/Scripts/FinalDemo/FinalDemoOperatorControls.cs`
  - Runtime HUD now shows explicit progress bars for general tinnitus pose matching and boss pattern matching.

- `Assets/Scenes/FinalDemo.unity`
  - Serialized `rainHardwareBrightnessMultiplier: 0.3` on the scene light router so the physical WS board keeps the reduced rain brightness.

Validation:
- `powershell -ExecutionPolicy Bypass -File tools\Build-DotNetProject.ps1 -ProjectPath BellRinger.Editor.csproj`
  - Passed. It also built the runtime dependency.
- First parallel Runtime build hit the usual temporary `obj\Debug\BellRinger.Runtime.dll` file lock.
- Re-run:
  - `powershell -ExecutionPolicy Bypass -File tools\Build-DotNetProject.ps1 -ProjectPath BellRinger.Runtime.csproj`
  - Passed with 0 warnings and 0 errors.

QA notes:
- In `FinalDemo`, after entering rain follow, check that rain stays visible under bell light instead of switching off while bell sound/LED occurs.
- At the physical board, rain should be much dimmer than the preview; the preview should remain bright enough to read.
- At general tinnitus, approaching the object should only snap/lock at the smaller approach radius, while sound/LED range still follows the separate scene range circle.
- During general tinnitus cleansing, the low match tone should come from the pad answer pose and rise in frequency as the position match improves.
- Runtime HUD should show position match, rotation match, and cleanse progress for general tinnitus, and boss match/progress for boss stages.

Notebook compatibility:
- No COM port, Arduino firmware, package, or device-path behavior changed.
