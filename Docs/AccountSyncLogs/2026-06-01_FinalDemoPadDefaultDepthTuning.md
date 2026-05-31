# FinalDemo Pad Default Depth Tuning - 2026-06-01

Context:
- User reported that the pad depth direction was fixed, but the default in-world pad position still felt too far away.
- Requested target feel: roughly `z = 0.7` at the default/resting pose.

Implemented:
- `Assets/Scripts/FinalDemo/FinalDemoPadSceneVisual.cs`
  - `baseViewLocalPosition.z` changed from `0.92` to `0.7`
  - `depthLocalZRange` changed from `0.48..1.95` to `0.45..0.95`
- `Assets/Scenes/FinalDemo.unity`
  - serialized `baseViewLocalPosition.z` changed from `0.92` to `0.7`

Validation:
- `BellRinger.Runtime.csproj` builds after the change.

Scope:
- FinalDemo pad visual only.
- No change to tracking packets, pose matching logic, or hardware behavior.
