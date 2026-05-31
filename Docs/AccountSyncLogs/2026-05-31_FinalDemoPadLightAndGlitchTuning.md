# FinalDemo Pad Light + Glitch Tuning - 2026-05-31

Context:
- User reported four issues after the previous glitch pass:
  - the screen-space glitch overlay was visible from the start of the demo
  - regular tinnitus lock still did not feel fully fixed
  - raised pad yellow light was not triggering correctly
  - pad depth on the observer monitor felt nearly static
  - bell/pad soft outer glow was missing after imported models replaced placeholder meshes

Implemented:
- `FinalDemoGlobalGlitchOverlay` now renders only when there is active stage boost.
  - No more always-on purple screen overlay during opening/preflight.
  - The overlay is now effectively “object glitch always available, camera glitch only near tinnitus/boss.”
- `FinalDemoDirector` global glitch stage boost logic changed:
  - regular tinnitus overlay strength is driven by proximity and cleanse progress
  - boss overlay strength is driven by boss approach/pattern/defeat stages
  - rain stage no longer injects global glitch
- Regular tinnitus position lock was reinforced:
  - `EnforceLockedPlayerPosition()` now runs in both `Update()` and `LateUpdate()`
  - `LockGeneralTinnitusPoseCheck()` now immediately re-applies the locked player position after locking
  - this addresses script execution order cases where movement was re-applied later in the same frame
- Pad LED mapping was rebuilt around actual pad view-local pose:
  - `FinalDemoPadSceneVisual` now computes pad view-local Y/Z using a forward reference distance and stronger lift/depth multipliers
  - pad size on the observer screen also changes with depth for clearer near/far feedback
  - `FinalDemoLightRouter` gained `ShowPadAnchorLocal()` and `ClearPadAnchor()`
  - `FinalDemoDirector.TickPadAnchorLight()` now:
    - reads `PadPoseProvider.CameraSpacePosition`
    - treats `y = 0.08m` as the effective raised maximum reference
    - hides the pad light below the configured lift threshold
    - maps the LED using the same resolved pad view-local position used by the scene visual
- Added subtle always-on local aura shells:
  - `Assets/Scripts/FinalDemo/FinalDemoVisualHalo.cs`
  - attached automatically to bell visual with green halo
  - attached automatically to pad visual with amber/yellow halo
  - implemented as additive runtime halo meshes so imported-model mode still shows the outer light

Validation:
- `BellRinger.Runtime.csproj` builds.
- `BellRinger.Editor.csproj` builds.
- Remaining warnings are unchanged:
  - `USG0001`
  - `PadTrackingReceiver.PadTrackingPacket` DTO field warnings

QA focus:
- In `FinalDemo`, opening/preflight should no longer show the purple full-screen glitch sheet.
- In `GeneralTinnitusOne`, approach until lock:
  - player X/Z should really stop
  - tinnitus light/audio center should stay stable while locked
- Raise the pad upward:
  - yellow pad LED should appear only after the lift threshold
  - LED vertical movement should feel restored
- Move the pad closer/farther:
  - the observer monitor pad should now show much larger front/back change
- Bell and pad should both show a faint colored outer aura even with imported models active.

Notebook compatibility:
- No serial port, UDP port, renderer asset, or hardware sketch defaults changed.
- The new halo is a runtime mesh/material helper and does not alter URP renderer assets.
