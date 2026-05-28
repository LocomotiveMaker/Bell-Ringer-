# FinalDemo Model Orientation Change Record

Date: 2026-05-28

Scope:
- FinalDemo bell and pad runtime model presentation only.

Recorded changes:
- Bell model: kept the -90 degree X axis correction as the default model presentation state and recentered rendered bounds after the correction so the visible bell stays on the intended authored position.
- Pad model: changed the FinalDemo pad model yaw from 180 degrees to 90 degrees.
- Pad material: replaced the white override path with a texture material using `Assets/Art/Models/Pad/gamepads/textures/5.png`, with the existing Resources copy as runtime fallback.
- Pad duplicate handling: when the imported pad FBX exposes two similar top-level rendered model groups, the presenter keeps only one visible group.
- Scene/editor sync: updated `FinalDemo.unity` and the `Bell Ringer/Final Demo/Apply Polish` editor pass to use the same model settings.

Follow-up changes:
- Bell model: applied another -90 degrees on X, so the FinalDemo default model euler is now `(-180, 0, 0)`.
- Pad model: applied another 180 degrees on Y, so the FinalDemo default model euler is now `(0, 270, 0)`.
- Pad ArUco pose: added `cameraSpacePositionOffset` in `PadPoseProvider` and set the FinalDemo default to `(0, 0.1, 0)`, so `Pad pos` Y is raised by 0.1 after sign/scale correction.
- Bell orbit LED: initial bell orbit now emits the continuous bell anchor point even when no bell cue is currently sounding.

Second follow-up changes:
- Bell orbit LED: initial orbit bell calls now play without attaching sound-reactive bell waves, so the orbit can keep a single continuous bell anchor point at the bell position.
- General tinnitus: the stage now waits until the player is within the tinnitus approach radius, then locks player movement and starts checking the captured pad position + rotation.
- General tinnitus radius: added a 1.3x radius scale for the general tinnitus reveal/light/audio range.
- General tinnitus and boss: added match-guidance haptics while the pad is close to the target but not fully inside tolerance.
- Boss tinnitus: boss patterns now require rotation again instead of position-only matching.
- Authoring capture: added F7/F8/F9 and UI buttons for Boss 1/2/3 pose capture.
- Tinnitus/boss light: reduced tinnitus-family LED output to 30% of prior level and switched FinalDemo tinnitus/boss lighting away from audio waveform-reactive flashes to a smoother repeating tinnitus pattern based on the test scene style.

Notebook compatibility:
- No notebook runtime paths, hardware COM ports, or device defaults were changed.
