# FinalDemo LED Route Restore - 2026-06-03

Context:
- User reported that after the barrier/rain layering work, existing bell LED output became broken/stuttered and rain became a very bright solid blue lower band instead of the earlier sample-scene-like texture.
- The requested rollback scope was LED output behavior only. General tinnitus position lock, barrier/collision fixes, pad-light gameplay suppression, and other non-LED changes should remain.

Implemented:
- `Assets/Scripts/FinalDemo/FinalDemoLightRouter.cs`
  - Reverted FinalDemo LED routing from composited `LED frame` output back to the earlier direct command path:
    - bell uses `SendLedRipple` / `SendLedPulseCore`
    - rain uses `SendLedRain`
    - tinnitus uses `SendLedTinnitus`
    - wall noise uses `SendLedWallNoise`
  - Removed runtime layer compositing, layer expiry, and `SendLedFrame` flushing from FinalDemo light output.
  - Restored the earlier rain logical preview pattern with lower-row streak weighting instead of the boosted solid lower band.
  - Kept the tinnitus/boss tinnitus reduction controls from the tinnitus lock work:
    - intensity multiplier
    - board range scale
    - pattern size scale

- `Assets/Scripts/FinalDemo/FinalDemoOperatorControls.cs`
  - Removed the extra runtime LED preview boost so the 16x8 preview again reflects only the router logical frame multiplied by the normal preview brightness.

- `Assets/Scenes/FinalDemo.unity`
  - Restored `bellOrbitIdleAnchorScale` to `0.4`.
  - Removed obsolete serialized LED layering/boost fields from the FinalDemo scene.

Intentionally kept:
- General tinnitus snap/lock behavior.
- Boss tinnitus position-only matching.
- Barrier collision/default movement fixes.
- Stage-gated authoring wall behavior.
- Pad LED suppression during gameplay.
- Existing Arduino `LED frame` support, because it is harmless when unused and avoids firmware churn before the exhibit.

Validation:
- `powershell -ExecutionPolicy Bypass -File tools\Build-DotNetProject.ps1 -ProjectPath BellRinger.Editor.csproj`
  - Passed. It also built `BellRinger.Runtime` through the Editor project dependency.
- `powershell -ExecutionPolicy Bypass -File tools\Build-DotNetProject.ps1 -ProjectPath BellRinger.Runtime.csproj`
  - First parallel attempt hit the usual temporary `obj\Debug\BellRinger.Runtime.dll` file lock.
  - Rerun passed with 0 warnings and 0 errors.

Notebook compatibility:
- No notebook-specific runtime path, device port, or platform behavior was changed.
