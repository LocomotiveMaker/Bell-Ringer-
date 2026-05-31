# FinalDemo Pad Forward Pull Tuning - 2026-06-01

Context:
- User wanted the visible pad in FinalDemo to be pulled further toward the camera after the previous depth-direction fix.

Implemented:
- `Assets/Scripts/FinalDemo/FinalDemoPadSceneVisual.cs`
  - `baseViewLocalPosition.z`: `0.70 -> 0.62`
  - `depthLocalZRange`: `(0.45, 0.95) -> (0.34, 0.82)`
- `Assets/Scenes/FinalDemo.unity`
  - Current scene instance `baseViewLocalPosition.z`: `0.70 -> 0.62`

Meaning:
- Lower `baseViewLocalPosition.z` = the pad sits closer to the player by default.
- Lower `depthLocalZRange` = the whole near/far motion band also stays closer to the player.

Validation:
- `BellRinger.Runtime.csproj` builds after the change.
