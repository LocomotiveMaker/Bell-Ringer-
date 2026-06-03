# FinalDemo Barrier Collision / Rain Layering - 2026-06-03

Context:
- User reported that authoring barriers (`frontWall_White_Authoring`, `LeftSoftWall_Authoring`, `RightSoftWall_Authoring`, `BackWall_White_Authoring`, `firMove`, `secondMov`, `firTin`, `secondTin`) visibly toggled but the player still walked through them.
- Rain LED appeared briefly after the first bell follow transition, then seemed to disappear once later bell output/narration began.
- User also wanted the runtime 16x8 preview to read much more clearly and asked for a check that multiple light sources can coexist with a fixed priority order.

Implemented:
- `Assets/Scripts/Gameplay/BellRingerSimpleMoveLookController.cs`
  - Added legacy-safe collision default restoration in `Awake`, `Start`, and `OnValidate`.
  - Existing scenes that were missing the newly added serialized collision fields now recover sane defaults automatically:
    - `usePhysicsCollision = true`
    - `collisionRadius = 0.24`
    - `collisionHeight = 1.55`
    - `collisionSkinWidth = 0.03`
    - `collisionMask = ~0`

- `Assets/Scripts/FinalDemo/FinalDemoDirector.cs`
  - Barrier colliders are now fit to renderer bounds instead of relying on a default `BoxCollider` shape.
  - Added extra rain-stage preload coverage for:
    - `RainCloseDrops`
    - `BellStrongAssist`
    - `NarrRainFocusBell`

- `Assets/Scripts/FinalDemo/FinalDemoLightRouter.cs`
  - Kept layered compositing with priority `pad > bell > tinnitus > wall > rain`.
  - Added overwrite thresholds so extremely dim bell spill does not erase the rain floor layer across the whole board.
  - Rain intensity boost is now applied consistently to the composited frame used by both hardware and the preview.

- `Assets/Scenes/FinalDemo.unity`
  - Serialized the new movement collision fields into `PlayerRig` so `FinalDemo` does not depend only on script fallback.
  - Increased LED preview runtime boost from `1.8` to `3.0`.
  - Reduced `bellOrbitIdleAnchorScale` from `0.4` to `0.24` so silent opening bell light is dimmer.

- `arduino/HeadMpu9250LedBridge/HeadMpu9250LedBridge.ino`
- `arduino/BellRingerSerialTemplate/BellRingerSerialTemplate.ino`
  - Added missing logical display size constants required by the new `LED frame` command path.

Validation:
- `powershell -ExecutionPolicy Bypass -File tools\Build-DotNetProject.ps1 -ProjectPath BellRinger.Runtime.csproj`
- `powershell -ExecutionPolicy Bypass -File tools\Build-DotNetProject.ps1 -ProjectPath BellRinger.Editor.csproj`
- `powershell -ExecutionPolicy Bypass -File tools\Build-ArduinoSketch.ps1 -SketchPath tools\.runtime\arduino-build-inputs\HeadMpu9250LedBridge_BuildCheck -Fqbn esp32:esp32:esp32s3`
- `powershell -ExecutionPolicy Bypass -File tools\Build-ArduinoSketch.ps1 -SketchPath tools\.runtime\arduino-build-inputs\BellRingerSerialTemplate_BuildCheck -Fqbn esp32:esp32:esp32s3`

Notes:
- The standard Arduino build script still collides with old fixed build folders under `tools\.runtime\abuild`; using uniquely named temporary sketch folders avoided that cleanup issue.
- Notebook compatibility unchanged. Work is limited to local Unity runtime/scene code plus Arduino sketch compile fixes.
