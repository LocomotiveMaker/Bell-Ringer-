# FinalDemo Pad Shader / Bell Glow - 2026-06-04

Context:
- User asked only for pad shader removal and prettier green bell light.
- `FinalDemoEmergencyReference_2026-06-04.md` says emergency edits should be narrow and avoid unrelated script structure changes.
- Worktree was clean before this change.

Implemented:
- `Assets/Scenes/FinalDemo.unity`
  - Disabled MeshRenderers for `Pad_AmberGlow`, `Pad_ArucoPlate_Yellow`, and `Button_Y_Yellow`.
  - Changed `LightRouter_LED` `bellColor` to `{r: 0.12, g: 1, b: 0.32, a: 1}`.

- `Assets/Scripts/FinalDemo/FinalDemoModelPresenter.cs`
  - Added a small runtime guard that hides the same pad yellow/amber accent renderers when FinalDemo model visuals are applied.
  - Left pad texture, pose, ArUco, and model placement behavior untouched.

- `Assets/Scripts/FinalDemo/FinalDemoLightRouter.cs`
  - Changed bell logical preview from direct cross pixels to a soft Gaussian-style bloom.
  - Changed bell point/anchor hardware fallback to softer `SendLedPulseCore` settings.
  - Adjusted bell wave core/ring balance so the center reads rounder and less spiky.

Validation notes:
- Needs Unity play/LED visual QA for final subjective brightness and shape.
- No rain, tinnitus, boss, audio, COM port, tracker, or pose matching code was changed.

