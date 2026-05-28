# FinalDemo Model Orientation Handoff - 2026-05-28

Context:
- Read `AGENT.md`, `Docs/FinalDemoAudioReview_2026-05-28.md`, and the latest `Docs/AccountSyncLogs` handoff notes before changing the FinalDemo model path.
- Change record for this conversation is in `Docs/SessionChangeRecords/2026-05-28_FinalDemoModelOrientation/README.md`.

Completed:
- Updated `FinalDemoModelPresenter` so bell X -90 orientation correction is treated as the default presentation state and the rendered bell is recentered on the authored model position after fitting.
- Updated the pad presentation to use Y 90 degrees.
- Added pad texture material application using `Assets/Art/Models/Pad/gamepads/textures/5.png`, with `Resources/FinalDemoModels/Pad/5` as fallback.
- Added guarded duplicate pad hiding for the two-gamepad FBX case.
- Synced `FinalDemo.unity` and `FinalDemoPolishTool` with the new pad texture/orientation settings.

Follow-up completed in the same conversation:
- Bell model default euler is now `(-180, 0, 0)` after the user confirmed it still needed another -90 degrees on X.
- Pad model default euler is now `(0, 270, 0)` after the user confirmed it needed another 180 degrees on Y.
- `PadPoseProvider` now applies a default camera-space position offset of `(0, 0.1, 0)` after the existing sign and axis-scale corrections, so `Pad pos` Y and every consumer of `CameraSpacePosition` share the same raised baseline.
- Bell orbit calls `TickBellContinuousAnchor` without requiring an active bell cue, so the bell position dot can stay visible during silent parts of the initial orbit.

Second follow-up completed in the same conversation:
- Initial bell orbit no longer attaches sound-reactive bell wave lights to the recurring bell call, so the continuous bell anchor point is not overwritten by waveform light output.
- General tinnitus now behaves like the boss entry: the player can approach, then movement locks once inside the tinnitus approach radius and pose matching starts from that fixed player position.
- General tinnitus radius scale is now 1.3x for reveal/light/audio reach.
- Tinnitus pose matching now emits guidance haptics when the pad is partially matched but not yet inside tolerance.
- Boss patterns now require rotation matching again, and the authoring capture panel exposes F7/F8/F9 plus buttons for BossPose 1/2/3 capture.
- FinalDemo tinnitus and boss LED output is scaled down to 30% and uses a smooth repeating pattern modeled on `TinnitusLightPatternController`, not audio envelope waveform flashes.

Notebook compatibility:
- No notebook runtime path, hardware serial port, or device default changes.
