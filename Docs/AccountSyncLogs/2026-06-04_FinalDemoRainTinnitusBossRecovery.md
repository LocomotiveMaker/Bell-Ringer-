# FinalDemo Rain / Tinnitus / Boss Recovery - 2026-06-04

Context:
- Continued after token cutoff from the structural audit in `Docs/FinalDemoStructuralAudit_2026-06-04.md`.
- User confirmed simultaneous rain + bell LED should be allowed, with bell covering rain where they overlap.

Implemented:
- `Assets/Scripts/FinalDemo/FinalDemoLightRouter.cs`
  - Replaced FinalDemo's dense direct rain band with a C# logical rain frame using the smoother sample-scene rain grammar.
  - Added sparse rain controls: brightness, density, neutral rows, look-down rows, preview boost, seed rate, phase speed, peak contrast.
  - Hardware rain pixels now get an additional physical brightness/color scale and a max-brightness cap.
  - Bell LED output composites over the stored rain frame so rain can remain visible under bell, while bell pixels take priority.

- `Assets/ScriptableObjects/FinalDemo/FinalDemoCueLibrary.asset`
  - Fixed `TinnitusBurst` default clip from wind to the documented short electric glitch.
  - Fixed `TinnitusPoseLock` default clip from movement/body step to the documented soft UI risk cue.

- `Assets/Scripts/FinalDemo/FinalDemoDirector.cs`
  - Restored FinalDemo procedural tinnitus closer to the tested `TinnitusAudioController` profile, with quiet final volume scaling instead of disabling instability.
  - General tinnitus matching now uses stored captured camera-space pose as the single source of truth.
  - Split general tinnitus outer sound/LED range from approach lock range:
    - If `Range_TinnitusLock_01` / `Range_TinnitusLock_02` exists, it is used for lock trigger distance/radius.
    - If missing, code falls back to the center of `Range_Tinnitus_01` / `Range_Tinnitus_02` with `GeneralTinnitusApproachRadius`.
  - Runtime summary now shows `lockDist` instead of the misleading sound-range distance.

- `Assets/Scripts/FinalDemo/FinalDemoAuthoringCapture.cs`
  - Replaced boss pose capture buttons with six direct boss path buttons:
    - Boss 1 Start / End
    - Boss 2 Start / End
    - Boss 3 Start / End
  - Hotkeys are now F7/F8, F9/F10, F11/F12 for those six points.
  - UI clearly states boss uses position only and ignores rotation.
  - Tinnitus marker summaries now show stored camera-space values, matching runtime behavior.

Validation:
- `powershell -ExecutionPolicy Bypass -File tools\Build-DotNetProject.ps1 -ProjectPath BellRinger.Runtime.csproj`
  - Passed.
  - Existing `PadTrackingReceiver.PadTrackingPacket` serialization warnings remain; no new compile errors.

QA focus:
- In FinalDemo rain, physical WS brightness should be lower and dot count should be lower/choppiness reduced.
- During rain, pad-shake or bell cue LED should appear over rain instead of being fully hidden by rain.
- General tinnitus 1 and 2 should lock when the player reaches the lock center/radius. If finer control is needed, add scene objects named `Range_TinnitusLock_01` and `Range_TinnitusLock_02` with `FinalDemoRangeAuthoring`.
- Capture boss targets from the Authoring Capture panel using the six Start/End buttons, not the old boss pose markers.

Notebook compatibility:
- No COM ports, Arduino firmware, packages, or device-path assumptions changed.
