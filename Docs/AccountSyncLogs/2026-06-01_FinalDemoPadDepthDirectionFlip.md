# FinalDemo Pad Depth Direction Flip - 2026-06-01

Context:
- User reported that pad z-axis movement was reflected in the game world in the opposite direction.
- The issue was limited to the FinalDemo pad scene visual depth presentation.

Implemented:
- In `Assets/Scripts/FinalDemo/FinalDemoPadSceneVisual.cs`, the depth interpolation input is now inverted before it drives local z and scale.
- This flips the perceived near/far motion without changing the underlying pad camera-space pose data.

Validation:
- `BellRinger.Runtime.csproj` builds successfully after the change.

Scope:
- Visual-only change for the FinalDemo pad representation.
- No change to pad pose matching, camera tracking packet parsing, or hardware serial behavior.
