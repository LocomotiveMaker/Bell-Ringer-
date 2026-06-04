# FinalDemo Pad Shader / Bell Glow - 2026-06-04

## Changed

- Removed the yellow/amber pad helper visuals from the active `FinalDemo` scene:
  - `Pad_AmberGlow`
  - `Pad_ArucoPlate_Yellow`
  - `Button_Y_Yellow`
- Added a runtime guard in `FinalDemoModelPresenter` so those named pad accent renderers stay hidden when model visuals are applied.
- Improved bell green LED output in `FinalDemoLightRouter`:
  - Bell point/anchor preview now renders as a soft round bloom instead of cross-shaped pixels.
  - Bell hardware output now uses softer `LED pulse` parameters instead of the sharper ripple-style point.
  - Bell wave keeps its existing route but has a stronger center core and softer ring.
- Updated `LightRouter_LED` bell color in `FinalDemo.unity` from hard green to a slightly fuller green.

## Unchanged

- No rain, tinnitus, boss, audio, pose matching, COM port, or tracker behavior was changed.
- Pad model texture path and pose/tracking logic were not changed.

