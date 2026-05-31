# FinalDemo Glitch + Tinnitus Lock - 2026-05-31

Context:
- User requested implementation of the previously discussed glitch pass:
  - object-only glitch for regular tinnitus and boss tinnitus
  - optional low-default global glitch layer
- User also reported that regular tinnitus position lock did not feel fixed.

Implemented:
- Added runtime object glitch shader:
  - `Assets/Shaders/FinalDemo/FinalDemoObjectGlitch.shader`
  - `Assets/Scripts/FinalDemo/FinalDemoGlitchVisual.cs`
- Reworked `FinalDemoGlitchVisual`:
  - no longer jitters the root transform
  - now swaps renderer materials at runtime to the object glitch shader
  - keeps boss/regular presets through `Configure(bool boss)`
- Added low-default global glitch overlay:
  - `Assets/Shaders/FinalDemo/FinalDemoGlobalGlitchOverlay.shader`
  - `Assets/Scripts/FinalDemo/FinalDemoGlobalGlitchOverlay.cs`
  - created automatically by `FinalDemoDirector`
  - runs as a camera-facing overlay, not a renderer-asset-level URP post feature
  - this was chosen to avoid destabilizing the three current URP quality renderer assets
- Added operator toggle:
  - `FinalDemoOperatorControls` now exposes `Global Glitch: On/Off`

Regular tinnitus lock fix:
- The lock logic already existed before this pass:
  - `_generalTinnitusPoseLocked`
  - `LockGeneralTinnitusPoseCheck()`
  - `SetPlayerMovementLocked(true)`
- The unstable behavior came from two structural issues:
  1. Tinnitus world position was resolved from the visual transform every frame.
  2. The old glitch component jittered that same transform.
- Fix applied:
  - `FinalDemoDirector` now captures a stage-fixed general tinnitus world position once in `BeginGeneralTinnitusStage()`
  - `TickGeneralTinnitus()` uses that captured value instead of re-resolving every frame
  - player movement lock now also stores the locked world position and enforces X/Z freeze every frame and in `LateUpdate()`

Validation:
- `BellRinger.Runtime.csproj` builds.
- `BellRinger.Editor.csproj` builds.
- Only pre-existing warnings remain:
  - `USG0001`
  - `PadTrackingReceiver.PadTrackingPacket` DTO field warnings

QA focus:
- In `FinalDemo`, enter `GeneralTinnitusOne`.
- Approach the tinnitus until lock triggers.
- Confirm:
  - player translation really stops
  - tinnitus audio/light center no longer drifts while locked
  - object glitch is visible on tinnitus/boss objects
  - `Global Glitch` button toggles the low screen-space overlay

Notebook compatibility:
- No serial port, UDP port, or hardware sketch defaults changed.
- Global glitch is implemented as a runtime camera overlay to avoid renderer-asset or device-specific pipeline regressions.
