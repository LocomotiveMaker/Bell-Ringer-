# FinalDemo Audio Follow-up - 2026-05-28

This log records the handoff work requested after `Docs/FinalDemoAudioReview_2026-05-28.md`.

Goals:
- keep a dedicated cross-account change log folder
- update `AGENT.md` so future work reads this folder first
- improve FinalDemo audio wiring based on the review document

Planned implementation in this session:
1. Remove `ForestBed` usage from opening/preflight flow.
2. Route `BellMovementTexture` as a real moving sound source during bell motion.
3. Start/stop `TinnitusHealingLoop` during valid cleanse hold.
4. Trigger `BossWeakpointMove` during boss weakpoint movement.
5. Update `FinalDemoCueLibrary.asset` defaults and alternates to match the selected sound set more closely.

Verification target:
- `BellRinger.Runtime.csproj` build passes after the changes.

Notebook compatibility:
- No notebook-specific device path changes are planned in this session.
- Changes stay inside FinalDemo runtime/audio wiring and documentation.

Completed:
- Created `Docs/AccountSyncLogs` and added this handoff log plus a folder `README`.
- Added an `AGENT.md` note telling future work to read the latest handoff log first.
- Removed `ForestBed` from `Preflight` and `OpeningAmbience`.
- Wired `BellMovementTexture` as a moving loop during:
  - `BellOrbit`
  - bell relocation inside `BellGaze`
- Wired `TinnitusHealingLoop` to start while the pad pose is inside tolerance and stop on loss/resolve.
- Wired `BossWeakpointMove` as a moving loop during the boss weakpoint motion phase.
- Updated cue asset defaults/alternates for:
  - bell movement
  - tinnitus long glitch
  - tinnitus burst family
  - tinnitus pose lock / pose lost
  - tinnitus resolve alternate
  - boss base / glitch / hit / defeat rise
  - forest bed alternate

Current remaining follow-up:
- Wall-contact audio is still not added to FinalDemo stage logic in this pass.

Verification result:
- `BellRinger.Runtime.csproj` build passed on 2026-05-28.
- Remaining warnings were pre-existing `PadTrackingReceiver.PadTrackingPacket` field warnings and one `USG0001` analyzer warning.

Additional pass for FinalDemo LED and bell orbit:
- `BellRingerAudioLedMapper` now returns no LED frame when a source is outside the configured horizontal/vertical view limits instead of clamping it to the board edge.
- `FinalDemoLightRouter` no longer falls back to the center pixel when bell/tinnitus world positions are outside the board. It clears the LED frame instead.
- FinalDemo light output has a runtime brightness boost for hardware and the 16x8 preview frame.
- Bell light anchors are now gated by active bell cue playback, so the green point is not kept alive when no bell sound is active.
- Bell orbit no longer plays `BellMovementTexture`.
- Bell orbit uses the profile path by default, with a left/front/right/left/center/up/center route.
- After orbit completes, the bell moves from the current center point to the first follow target while `BellMovementTexture` fades in/out over the move.
- Default final hardware serial port in `FinalDemoTuningProfile` is now `COM40` for the current VR S3 setup.

Verification result:
- `BellRinger.Runtime.csproj` build passed again after this pass.
- Remaining warnings are still the existing `PadTrackingReceiver.PadTrackingPacket` field warnings and one `USG0001` analyzer warning.

Additional pass for bell follow movement and calmer bell light:
- Added a real relocation state when transitioning from `BellFollowOne` to `BellFollowRain`, so the bell moves from target 1 to target 2 instead of teleporting.
- Movement texture now also emits a low-intensity bell anchor from the same moving world position, keeping sound and LED direction aligned.
- Bell follow call interval now starts at 11 seconds and decreases by 0.5 seconds after each missed call down to 8 seconds.
- Bell orbit defaults are slower and more circular: `bellOrbitSeconds` is 40 seconds and `bellOrbitCallIntervalSeconds` is 0.72 seconds.
- Bell orbit profile points now follow a front-facing circular route around the player rather than cutting through arbitrary points.
- Bell wave output is dimmer and smoother. Reactive bell light now favors onset events instead of continuous rapid flicker.
- FinalDemo LED mapping distance was expanded during later tuning so distant bell sound and distant bell light use comparable reach.
- Bell sources get a light distance reverb when far from the listener.

Verification result:
- `BellRinger.Runtime.csproj` build passed after this pass.
- Remaining warnings are still the existing `PadTrackingReceiver.PadTrackingPacket` field warnings and one `USG0001` analyzer warning.

Additional VR/headphone tuning pass:
- Bell wave LED output is capped at 35% normalized brightness, with a slower envelope attack/release and less onset spike.
- Bell movement anchor light is dimmer to reduce eye strain while the bell flies between authored points.
- Binaural side exaggeration is stronger, so slightly left/right sources are perceived more laterally.
- FinalDemo now starts with HRTF/software binaural preview enabled by default for headphone play; the operator button can still turn it off.
- Opening ambience after the face-forward narration is now a silent/static wait instead of starting the bell orbit loop.
- Bell orbit defaults are faster than the prior slow pass: 28.6 seconds total and 0.58 second call interval.
- Bell orbit profile route now follows: left, center, right, behind, left, center, up, behind, below, center.
- Bell orbit profile interpolation is now an open spline, so the final center point goes directly into the first follow-target move instead of wrapping back to the first orbit point.
- Bell follow/pad-shake assist repeats less often, and bell cue/light max distance is expanded by about 30% for the follow stages.

Verification result:
- `BellRinger.Runtime.csproj` build passed after the VR/headphone tuning pass.
- Latest incremental build after the cue-distance correction reported 0 warnings and 0 errors.

Additional combined tuning/model pass:
- Bell LED output was reduced again: bell point brightness boost is 0.87, wave boost is 0.43, and bell wave maximum brightness is 0.21.
- Bell opening orbit is faster again: 19.1 seconds total and 0.39 second call interval.
- Bell follow stages now apply a route-plane blocker so the player cannot move far beyond the active bell target and permanently lose the bell.
- Opening ambience has its own `OpeningAmbienceBed` cue using `05_mixkit_open_ground_texture_b_very_low.wav`.
- Rain ramp time is now 3 seconds from rain-zone entry.
- Tinnitus child glitch sources are pinned to the same local origin as the procedural tone so software binaural direction uses one world position.
- Bell/pad model assets were copied into `Assets/Resources/FinalDemoModels` for FinalDemo-only runtime fallback, and `FinalDemoModelPresenter` applies them to `bellVisual`/`padVisual`.
- Added `FinalDemoKoreanGuide` and `FinalDemoModelPresenter` directly to `FinalDemoRoot` so the FinalDemo root exposes Korean tuning notes and model settings in the Inspector.
- Added `Bell Ringer/Final Demo/Apply Polish` as an editor menu pass for persistent scene/model setup when Unity is not locked by an active session.

Verification result:
- `BellRinger.Runtime.csproj` and `BellRinger.Editor.csproj` builds passed after the final scene/meta pass.
- Remaining warnings are the pre-existing `PadTrackingReceiver.PadTrackingPacket` field warnings and `USG0001`; no new FinalDemo warnings remain.
