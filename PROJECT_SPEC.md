# Bell Ringer Project Specification

## 1. Project Identity

- Working title: `Bell Ringer`
- Engine: `Unity 2022.3.9f1`
- Platform target: `Windows PC + Arduino-connected custom hardware`
- Interaction mode: `eyes-closed sensory first-person exploration`
- Core promise: the player navigates and acts in a 3D space primarily through spatial audio, vibration, and diffused eyelid-visible light rather than conventional screen-based vision.

## 2. Design Thesis

`Bell Ringer` is not a conventional VR game and should not be designed as one.
It is a sensory interaction game that borrows a head-mounted form factor but deliberately removes normal visual dependence. The player wears a light-emitting headset, closes their eyes, and uses:

- directional sound to understand where something is
- diffused light through closed eyelids to feel orientation, proximity, urgency, and success
- handheld motion to search, tune, cleanse, and track
- vibration to confirm contact, danger, and charge states

The emotional arc matters as much as the mechanics. The player should feel:

1. curiosity
2. disorientation
3. learning
4. tension
5. mastery
6. release

The game must therefore be designed around trust in sensory feedback. Every important action must have a clear, repeatable sensory signature.

## 3. High-Level Experience Goals

### 3.1 Must-Have Experience Goals

- The player can tell left/right/front urgency through sound and light without looking.
- The player can actively probe space instead of passively waiting for hints.
- The player can feel when they are correctly aligned with a target.
- The final silence feels meaningfully different from the noisy beginning.

### 3.2 Should-Have Experience Goals

- The player develops a rhythm: listen, orient, sweep, confirm, cleanse.
- Danger is legible before it becomes frustrating.
- Motion of the hand tool feels ritualistic rather than purely mechanical.

### 3.3 Avoid

- Reliance on conventional visual UI.
- Precision tasks that require millimeter accuracy from unstable hardware.
- Maze design that is difficult only because of sensor noise.
- Long periods with no feedback.

## 4. Product Scope Decision

This project should be built as a strong `MVP-first prototype`, not as a fully content-rich game.

The project succeeds if the following loop works reliably:

1. Player hears a bell or noise source.
2. Player turns or moves the hand controller to search.
3. Sound and light communicate direction and state.
4. Player finds and aligns with a target.
5. A cleansing interaction resolves the noise.
6. The world becomes calmer.

If this loop works cleanly, additional stages and boss behaviors can be added later.

## 5. Recommended MVP Slice

### 5.1 Include in MVP

- one tutorial sequence
- one exploration area
- one danger/noise type
- one cleansing mechanic
- one short climax encounter
- one calm ending state
- serial communication between Unity and Arduino
- head orientation input
- hand orientation input
- simple vibration feedback
- LED light pattern output
- spatial audio-driven guidance

### 5.2 Exclude From Initial MVP

- large maze with many branches
- enemy variety
- advanced enemy AI
- fully free 3D hand position tracking if unreliable
- multiplayer
- save/load
- polished menus and settings

## 6. Core Fantasy

The player is not fighting monsters with sight.
The player is restoring order to a space corrupted by ringing noise.
The bell is both a guide and a tool of purification.

The world should feel like an abstract acoustic ritual space, not a literal realistic building.

## 7. Player Role and Inputs

### 7.1 Player Role

The player is a `ringer` or `keeper` who can sense disturbances and cleanse them by matching, holding, and stabilizing resonance.

### 7.2 Input Channels

- head rotation: determines facing and therefore how audio/light cues are interpreted
- hand rotation: used for searching, tuning, and cleansing
- hand position: optional in full form, simplified in MVP if needed
- button input: used for scan pulse, confirm, or channel action

### 7.3 Output Channels

- binaural/spatial audio
- diffused LED light through eyelids
- vibration motor in controller
- optional monitor view for observers only

## 8. Sensory Language

The project needs a clear sensory vocabulary. Codex should preserve these mappings unless there is a strong reason to change them.

### 8.1 Audio Meaning

- clean bell: guidance, truth, target, progress
- low hum: neutral ambience, space depth
- harsh ringing: corruption, threat, active disturbance
- filtered whisper/noise burst: near interaction, unstable resonance
- silence: reward, emotional payoff

### 8.2 Light Meaning

- warm white/gold: target guidance, success, safe focus
- soft blue/white: scan return, space contour, walls or echoes
- red/orange: danger, corrupted zone, wrong alignment
- pulsing brightening: getting closer to valid interaction
- smooth steady glow: locked alignment or completed channeling
- erratic flicker: unstable or conflicting signal

### 8.3 Vibration Meaning

- short pulse: input accepted
- periodic pulse: scanning active
- rough buzz: danger proximity
- smooth sustained hum: cleansing held correctly
- quick success burst: cleanse completed

## 9. Game Flow

### 9.1 Sequence A: Onboarding

Goals:

- teach that sound source direction corresponds to light direction
- teach that the hand tool can trigger feedback
- teach one simple action-response loop

Suggested flow:

1. Player begins in near-dark quiet with faint distant bell.
2. Turning toward the bell strengthens centered light and cleans the audio image.
3. Reaching tutorial anchor with hand/button triggers first confirmation vibration.
4. Player receives bell tool.
5. A guided scan pulse reveals nearby surfaces with light sweeps and soft echo tones.

Success condition:

- player intentionally turns toward target and performs one successful scan

### 9.2 Sequence B: Exploration

Goals:

- teach active sensing
- establish tension
- make space feel navigable without vision

Suggested flow:

1. Noise pockets and barriers appear.
2. Player uses scan pulses to infer nearby walls or route direction.
3. Red-biased cues indicate corrupted regions to avoid or purify.
4. A stable bell tone marks safe path or next objective.

Success condition:

- player crosses area using repeated listen-orient-scan behavior

### 9.3 Sequence C: Cleansing Encounter

Goals:

- convert abstract signal following into a precise but readable ritual action

Suggested flow:

1. Distortion source becomes active.
2. Player hears directional ringing and receives increasing red flicker.
3. When controller orientation is near target resonance, vibration becomes smoother and light stabilizes.
4. Holding alignment while channel button is pressed fills a cleanse state.
5. Successful cleanse transforms audio from harsh ringing into clear bell.

Success condition:

- player matches orientation and sustains lock long enough to complete cleansing

### 9.4 Sequence D: Climax

Goals:

- combine orientation, pursuit, sustained hold, and emotional intensity

Recommended MVP version:

- a moving resonance source that shifts direction over time
- player must reacquire it multiple times
- no strict 3D free-space chase required if hardware is unstable

Success condition:

- player maintains cumulative lock over several short windows

### 9.5 Sequence E: Release

Goals:

- strong sensory contrast from earlier sections

Suggested flow:

- noise disappears
- lights settle into warm, slow-breathing glow
- environmental sound becomes gentle and spacious
- vibration ceases entirely

Success condition:

- player remains still and experiences intentional quiet for a few seconds

## 10. Mechanics Specification

### 10.1 Orientation Guidance

Primary implementation:

- compare player head forward vector with target direction
- drive sound panning/spatialization from world position
- drive LED cue center/offset from signed horizontal and vertical angle

Interpretation rules:

- closer alignment means more centered, smoother, brighter cue
- poor alignment means weaker, more off-axis, noisier cue

### 10.2 Scan Pulse

Purpose:

- player-triggered active sensing

Implementation:

- button press or controller motion emits pulse
- nearby surfaces or nodes respond with transient light/audio return
- pulse should have cooldown to avoid spam noise

Design rule:

- scan feedback must be simple and interpretable within one second

### 10.3 Cleansing / Tuning

Primary mechanic:

- target has desired resonance parameters
- player manipulates hand orientation to enter tolerance
- while inside tolerance and holding action, cleanse meter increases
- leaving tolerance drains or pauses progress

Recommended MVP parameters:

- use one main angular match first
- optionally add a second parameter only if the first is reliable

Avoid:

- multi-axis precision puzzles too early
- overly punishing reset-on-failure rules

### 10.4 Danger Field

Purpose:

- create tension without relying on visible hazards

Implementation:

- corrupted areas emit red flicker, rough vibration, sharp noise
- intensity increases with proximity
- entering deep corruption should be discouraged, not instantly punished in MVP

### 10.5 Tracking Encounter

Full vision:

- player follows moving target with hand tool over time

Fallback vision:

- target moves mostly in directional sectors rather than requiring full XYZ tracking
- success is based on reacquiring heading repeatedly

## 11. Accessibility and Positioning

This project may be described as `sensory-first`, `low-vision inspired`, or `eyes-closed interaction research`.
It should not claim medical, therapeutic, or universal accessibility benefits unless validated through testing.

Safe framing:

- explores non-visual interaction
- investigates layered sensory feedback
- may inform accessible design ideas

Unsafe framing:

- guaranteed accessible for all blind users
- therapeutic benefit claim
- validated training benefit without evidence

## 12. Technical Architecture

### 12.1 Engine Layer

- Unity handles game state, audio world, scene logic, tuning rules, encounter sequencing, and feedback mapping.
- Arduino handles sensor acquisition, LED driving, vibration motor output, and button events.

### 12.2 Recommended Unity Architecture

Use a lightweight modular architecture:

- `GameFlowManager`: stage progression and state transitions
- `HardwareBridge`: serial communication abstraction
- `HeadInputProvider`: normalized head orientation data
- `HandInputProvider`: normalized hand orientation and optional position
- `FeedbackMapper`: converts game state into LED/vibration commands
- `AudioCueController`: manages spatial sound targets and intensity
- `EncounterController`: per-scene logic for scan, cleanse, and climax interactions

Prefer event-driven communication between systems, with only small polling sections where real-time input is needed.

### 12.3 State Machine Principle

Do not rely on `delay()`-style blocking logic anywhere.

Use state-driven progression with timers based on `Time.deltaTime` and explicit state enums such as:

- `Boot`
- `Calibration`
- `Tutorial`
- `Exploration`
- `Encounter`
- `Climax`
- `Resolution`

### 12.4 Scene Strategy

Recommended scene list:

- `Bootstrap`
- `MainMenu` or temporary direct-start scene
- `PrototypeArena`
- `FinalDemoScene`

Keep early development inside one test scene to reduce friction. Split only when interaction is stable.

## 13. Hardware Strategy

### 13.1 Headset Light Module

Required behavior:

- light must be diffused, not direct-point visible
- brightness must be software-limited
- output patterns must remain readable through closed eyelids

Design priorities:

- safety
- comfort
- repeatability
- low heat

Recommended practice:

- keep LEDs behind diffusion layer
- leave physical distance from eyes
- define conservative brightness cap
- never design gameplay around maximum brightness

### 13.2 Controller

Controller should feel like a ritual instrument, not a generic gamepad.

Minimum hardware:

- one IMU
- one button
- one vibration motor
- optional visible marker or tracked point

Desired behavior:

- easy to hold in one hand
- easy to rotate deliberately
- clear sense of forward direction

### 13.3 Position Tracking Policy

This project must not depend on perfect 6DoF hand tracking for MVP completion.

Preferred hierarchy:

1. reliable orientation-only gameplay
2. approximate 2D hand zone tracking if stable
3. optional depth-enhanced interaction if proven usable

If position tracking becomes unstable, redesign mechanic before increasing hardware complexity.

## 14. Tracking Recommendations

### 14.1 Recommended MVP Input Model

Use:

- head rotation
- hand rotation
- optional simple hand zone or near/far estimate

Do not require:

- precise absolute 3D hand coordinates

### 14.2 If Camera Tracking Is Used

Prefer robust, debuggable solutions over clever fragile ones.

Priority order:

1. marker-based pose estimation
2. coarse blob or LED tracking with thresholding
3. depth inference only if clearly stable

Use camera data for:

- screen-space left/right/up/down guidance
- zone entry
- coarse proximity bands

Avoid building critical mechanics around noisy per-frame depth estimates.

## 15. Audio Direction

Audio is the most important channel and must be treated as primary gameplay UI.

Requirements:

- all critical targets use true 3D positioned audio
- each important source must have a distinct timbre
- distance and obstruction should be readable by ear
- no unnecessary overlapping loops

Production rule:

- if light and vibration disagree with audio, audio should usually be treated as the source of truth

## 16. Visual Style for Observer Screen

Even though the player closes their eyes, the monitor view still matters for development and presentation.

Observer screen goals:

- clearly represent source positions for debugging
- show encounter states and lock progress
- provide moody atmospheric visuals for demos

Art direction:

- low-poly or abstract ritual geometry
- dark environment with sparse meaningful highlights
- avoid visual clutter not tied to interaction

The observer screen is secondary. Never sacrifice player-readable sensory feedback for monitor spectacle.

## 17. Feedback Mapping Rules

### 17.1 Target Direction to Light

Map target direction relative to head orientation into a compact, legible LED representation.

Examples:

- centered target -> centered stable bright cluster
- target left -> left-shifted pattern
- target above -> upper pattern emphasis
- target near and aligned -> larger or smoother pulse

### 17.2 Danger to Light

Use distinct rhythm, not only color difference.

Examples:

- safe guidance -> smooth pulse
- danger -> jagged intermittent flashes

### 17.3 Cleanse Lock to Vibration

- out of tolerance -> little or rough feedback
- near tolerance -> soft periodic confirmation
- locked -> smooth continuous hum
- completed -> short celebratory burst

## 18. Calibration Requirements

Calibration should be simple enough to run before every playtest.

Minimum calibration:

- head neutral forward
- hand neutral forward
- brightness test
- vibration test
- serial connection test

Optional calibration:

- camera alignment
- controller tracked marker visibility

If calibration takes more than one minute, the setup is too heavy for repeated testing.

## 19. Engineering Priorities

Priority order for implementation:

1. stable serial connection
2. reliable sensor reading
3. stable feedback loop
4. readable audio direction
5. one complete interaction loop
6. content expansion
7. polish

This order should not be inverted.

## 20. Folder and Code Organization

Recommended Unity project structure:

```text
Assets/
  Audio/
  Materials/
  Prefabs/
  Scenes/
  Scripts/
    Core/
    Hardware/
    Gameplay/
    Audio/
    Debug/
  Settings/
  UI/
arduino/
docs/
```

Recommended script responsibility split:

- `Core`: state machines, scene boot, shared services
- `Hardware`: serial parsing, device packets, calibration data
- `Gameplay`: encounters, scan logic, tuning logic, progression
- `Audio`: source control, cue mixing, debug listeners
- `Debug`: inspector tools, simulated hardware input, overlays

## 21. Development Roadmap

### Phase 1: Foundation

- create Unity project structure
- establish Git workflow
- define hardware packet format
- receive live IMU/button data in Unity
- send test LED/vibration commands back to Arduino

Exit condition:

- round-trip communication is stable for repeated reconnects

### Phase 2: Sensory Prototype

- implement directional audio target
- implement basic light mapping from target angle
- implement vibration confirmation states
- create calibration scene

Exit condition:

- one tester can consistently face and identify target direction

### Phase 3: Core Loop

- add scan pulse
- add simple exploration space
- add one corruption target
- implement cleanse hold mechanic

Exit condition:

- full loop from search to cleanse works in one scene

### Phase 4: Demo Structure

- add intro
- add tension buildup
- add short climax variant
- add ending transformation
- improve observer presentation

Exit condition:

- end-to-end demo can be shown without manual intervention

### Phase 5: Polish

- reduce sensor drift impact
- tune audio/light curves
- simplify calibration
- improve comfort and reliability

Exit condition:

- repeated demo sessions produce consistent results

## 22. Risk Register

### High Risk

- unstable hand position tracking
- serial timing issues under frequent LED updates
- sensor drift and noisy calibration
- sensory overload causing confusion instead of clarity

### Medium Risk

- scene scope expanding too early
- too many simultaneous audio layers
- weak distinction between safe and dangerous feedback

### Low Risk

- monitor-side visual polish
- additional lore or narrative dressing

## 23. Risk Responses

- If position tracking is unstable, remove precision dependence and reframe mechanic around orientation.
- If LED readability is poor, simplify patterns before adding more colors or motion.
- If audio clutter rises, reduce source count rather than increasing volume.
- If calibration is annoying, automate defaults and shorten the playtest boot flow.

## 24. Testing Strategy

### 24.1 Internal Testing Questions

- Can the player tell where to turn within 3 seconds?
- Can the player tell danger from guidance without explanation?
- Can the player identify success and failure states by feel?
- Can the player complete one cleanse without visual monitor help?

### 24.2 Observation Metrics

- time to orient toward bell
- number of failed cleanses
- number of unnecessary scans
- moments where player freezes in confusion
- headset discomfort reports

### 24.3 Debug Tools

Build internal tools early:

- simulated serial input
- on-screen angle and lock indicators
- LED pattern preview in inspector or debug window
- reconnect button for hardware bridge

## 25. Non-Goals

These are explicitly not required for success:

- realistic photoreal graphics
- broad content quantity
- production-grade accessibility certification
- perfect hand tracking
- advanced story writing

## 26. Deliverable Definition

The project is considered successful when all of the following are true:

- the game runs end-to-end in Unity without broken state flow
- Arduino hardware exchanges live input/output data with Unity
- the player can complete a guided sensory interaction loop with eyes closed
- the experience communicates a clear emotional transition from noise to calm

## 27. Working Rules for Codex

When Codex edits this project, default decisions should follow these principles:

- preserve MVP-first scope
- reduce complexity before adding new systems
- prefer reliable feedback over technically impressive but fragile features
- preserve sensory language consistency across sound, light, and vibration
- keep gameplay understandable without visual dependence
- treat debugging and calibration tools as first-class features

## 28. First Build Checklist

- create scenes and folders
- create serial bridge script skeleton
- create game state enum and bootstrap flow
- create hardware simulation mode for in-editor testing
- create one tutorial bell target
- create one light mapper prototype
- create one cleanse target prototype
- verify full input-output loop

## 29. Final Note

The most important thing about `Bell Ringer` is not how many systems it contains.
It is whether the player feels that an invisible world became understandable through sound, light, and touch.

When in doubt, cut complexity and strengthen the clarity of that one feeling.

## 30. Current Implementation Snapshot

Last updated: `2026-05-05`

This section records the actual project structure implemented so far. It is descriptive, not aspirational. Earlier sections define the product direction; this section is the current engineering map.

### 30.1 Top-Level Runtime Structure

```text
Assets/
  Audios/
    freesound_community-bicycle-bell-66855.mp3
    rain/boons_freak-rain-sound-188158.mp3
  Scenes/
    SampleScene.unity
    LightTextureTest.unity
    AudioLabTest.unity
  Scripts/
    Audio/
    Core/
    Debug/
    Gameplay/
    Hardware/
  Settings/
    Audio/
    LightTextures/
arduino/
  BellRingerSerialTemplate/BellRingerSerialTemplate.ino
  README.md
tools/
  Build-ArduinoSketch.ps1
  Build-DotNetProject.ps1
  Create-LightTextureTestScene.ps1
  Get-UnityRuntimeStatus.ps1
  Install-ValidationTools.ps1
  Invoke-UnityValidation.ps1
  Launch-UnityProject.ps1
  Resolve-UnityEditor.ps1
  Show-ValidationTools.ps1
  Test-ArduinoLedOutput.ps1
  Use-ValidationTools.ps1
```

Generated folders such as `tools/.runtime/`, `Library/`, `Temp/`, `obj/`, and `Logs/` are not part of the authored structure.

### 30.2 Runtime Bootstrap

`Assets/Scripts/Core/BellRingerRuntimeBootstrap.cs` creates a persistent `BellRingerRuntime` GameObject before scene load.

It attaches:

- `HardwareBridge`: serial connection, simulation fallback, LED command output, telemetry parsing.
- `BellRingerRuntimeStatusWriter`: writes runtime state to `Logs/runtime-status.json`.
- `BellRingerDebugOverlay`: on-screen connection/status overlay, toggled by `F1`.
- `BellRingerAudioDemoBootstrapper`: legacy sample audio bootstrap, skipped when the newer spatial light texture sample controller is present.

Design consequence:

- scenes do not need to manually contain a `HardwareBridge`
- duplicate bridge components should not own or destroy scene roots
- serial connection state is globally available through `HardwareBridge.Instance`

### 30.3 Hardware Bridge

Primary Unity class:

- `Assets/Scripts/Hardware/HardwareBridge.cs`

Current defaults:

- serial port: `COM9`
- baud rate: `115200`
- environment override: `BELL_RINGER_SERIAL_PORT`
- baud override: `BELL_RINGER_SERIAL_BAUD`
- forced simulation override: `BELL_RINGER_SIMULATE_HARDWARE`

Supported outbound commands:

- `PING`
- `LED clear`
- `LED fill b=<0-255>`
- `LED x=<0-15> y=<0-7> b=<0-255>`
- `LED ripple cx=<x> cy=<y> radius=<r> width=<w> red=<r> green=<g> blue=<b> level=<0-1>`
- `LED wall cx=<x> cy=<y> w=<w> h=<h> red=<r> green=<g> blue=<b> level=<0-1> seed=<n>`
- `LED rain cx=<x> cy=<y> w=<w> h=<h> red=<r> green=<g> blue=<b> level=<0-1> seed=<n> phase=<seconds>`
- `OUT vib=<0-1> lr=<r> lg=<g> lb=<b> pulse=<0-1>`

Current status APIs:

- `GetStatusSnapshot()` exposes connection mode, active port, available ports, last command, last error, telemetry, and last update time.
- `BuildEnvironmentSummary()` is used for quick local diagnostics.

### 30.4 Arduino Firmware

Primary sketch:

- `arduino/BellRingerSerialTemplate/BellRingerSerialTemplate.ino`

Current board assumptions:

- Arduino Uno-compatible target
- baud `115200`
- left WS2812B 8x8 matrix on `D6`
- right WS2812B 8x8 matrix on `D7`
- button on `D2`
- Adafruit NeoPixel library

Current LED coordinate model:

- Unity sends one logical 16x8 display.
- `x=0..7` maps to the left matrix.
- `x=8..15` maps to the right matrix.
- left matrix index: `(localX * 8) + localY`
- right matrix index: `(7 - localX) * 8 + (7 - localY)`

Current sketch behavior:

- command parsing is line-based over serial
- telemetry is currently disabled with `kEnableTelemetry = false`
- `LED ripple` draws a ring around a logical coordinate
- `LED wall` draws a rectangular gray glitch texture
- `LED rain` draws a full-width lower screen rain band with animated blue droplets
- every sketch change requires re-uploading `BellRingerSerialTemplate.ino`

### 30.5 Light Texture System

Primary classes:

- `Assets/Scripts/Audio/BellRingerLightTexturePreset.cs`
- `Assets/Scripts/Audio/BellRingerLightTexturePlayer.cs`
- `Assets/Scripts/Debug/BellRingerLightTextureTestDriver.cs`
- `Assets/Scripts/Debug/BellRingerSpatialLightTextureSampleController.cs`
- `Assets/Scripts/Debug/Editor/BellRingerLightTextureSceneBuilder.cs`

Current preset assets:

- `Assets/Settings/LightTextures/GreenBellRipple.asset`
- `Assets/Settings/LightTextures/SoftWideRipple.asset`
- `Assets/Settings/LightTextures/ThinFastRipple.asset`

Implemented light texture modes:

- `Bell Point`: a moving point sound source that emits green ripple light from the source direction.
- `Wall Noise`: a gray rectangular glitch area projected from a wall object into the 16x8 board.
- `Rain Floor`: a full-width lower screen blue rain band; looking upward suppresses it, looking downward increases its height up to about five rows.

Current tuning:

- bell, wall, and rain brightness have been reduced from earlier test values to keep headset output conservative
- rain color is tuned toward deeper blue
- the Unity board preview applies a visual boost so low hardware brightness remains visible on monitor

Important design rule:

- the Unity preview is not a separate effect; it should represent the intended 16x8 board output as closely as possible while using a display-only brightness multiplier.

### 30.6 Audio System

Primary classes:

- `Assets/Scripts/Audio/BellRingerAudioBus.cs`
- `Assets/Scripts/Audio/BellRingerAudioSourcePreset.cs`
- `Assets/Scripts/Audio/BellRingerAudioPresetApplier.cs`
- `Assets/Scripts/Audio/BellRingerProceduralToneSource.cs`
- `Assets/Scripts/Audio/BellRingerSpatialAudioLedController.cs`
- `Assets/Scripts/Audio/BellRingerSpatialSoundTarget.cs`
- `Assets/Scripts/Debug/BellRingerAudioLabController.cs`
- `Assets/Scripts/Debug/Editor/BellRingerAudioLabSceneBuilder.cs`

Current audio preset assets:

- `Assets/Settings/Audio/Bell3D.asset`
- `Assets/Settings/Audio/MuffledFarBell.asset`
- `Assets/Settings/Audio/GeneratedTone3D.asset`
- `Assets/Settings/Audio/Flat2DReference.asset`

Current audio clips in active use:

- bell test: `Assets/Audios/freesound_community-bicycle-bell-66855.mp3`
- rain test: `Assets/Audios/rain/boons_freak-rain-sound-188158.mp3`

Implemented audio lab features:

- compare 3D bell, muffled far bell, and flat 2D reference
- adjust master, bell bus, and generated tone gain
- compare low-pass/distant sound texture
- generate procedural tones at selected frequencies
- switch procedural tone waveform between sine, triangle, square, and saw
- display available Unity spatializer plugins

Current HRTF status:

- no required spatializer plugin is assumed to be installed
- spatializer toggles are test-facing only until a real plugin is configured

### 30.7 Scenes

`Assets/Scenes/SampleScene.unity`

- current integrated spatial light texture sample scene
- contains `BellRingerSpatialLightTextureDemo`
- runtime creates sample bell, wall, and rain floor objects
- supports WASD movement and right mouse look through `BellRingerSimpleMoveLookController`
- includes on-screen `Spatial Light Texture` controls
- includes `LED Board Preview` to show expected 16x8 board output

`Assets/Scenes/LightTextureTest.unity`

- focused light texture testing scene
- now includes wall/rain/bell mode switching through the spatial light texture sample controller
- older isolated ripple-only UI is disabled to reduce UI conflict
- intended for quick light texture tuning without full gameplay

`Assets/Scenes/AudioLabTest.unity`

- focused audio and procedural frequency testing scene
- independent from hardware LED testing
- used to compare spatial audio settings, filters, generated tones, and possible spatializer behavior

### 30.8 Debug and Validation Tools

Runtime debug tools:

- `BellRingerDebugOverlay`: connection state, port, last command, telemetry, last error
- `BellRingerRuntimeStatusWriter`: status JSON for external inspection
- `BellRingerHardwareConnectionProbe`: hardware connection probing behavior from earlier tests
- `LED Board Preview`: current intended board output in Unity GUI

Editor/build tooling:

- `tools/Build-DotNetProject.ps1`: builds Unity-generated C# projects with local .NET runtime
- `tools/Build-ArduinoSketch.ps1`: compiles `BellRingerSerialTemplate.ino`
- `tools/Create-LightTextureTestScene.ps1`: recreates the light texture test scene through Unity editor automation
- `tools/Get-UnityRuntimeStatus.ps1`: reads runtime status JSON
- `tools/Install-ValidationTools.ps1`: installs local validation dependencies
- `tools/Show-ValidationTools.ps1`: prints configured validation runtime paths
- `tools/Test-ArduinoLedOutput.ps1`: sends direct LED test commands to the Arduino

Current local validation dependencies:

- .NET SDK installed under `tools/.runtime/dotnet`
- `arduino-cli` installed under `tools/.runtime/arduino-cli`
- Arduino AVR core installed locally
- Adafruit NeoPixel library installed locally

### 30.9 Tests

Current edit-mode test file:

- `Assets/Tests/EditMode/HardwareBridgeCommandTests.cs`

Covered command formatting:

- `LED fill`
- `LED ripple`
- `LED wall`
- `LED rain`

These tests validate command construction, not physical LED output.

### 30.10 Current Development State

Working now:

- Unity can connect to Arduino over `COM9` at `115200`.
- Unity can send LED fill, dot, ripple, wall, and rain commands.
- Arduino can render a logical 16x8 display across two 8x8 WS2812B matrices.
- Bell, wall, and rain light textures can be selected in test scenes.
- Board preview exists to reduce mismatch between intended output and perceived hardware output.
- Audio lab exists for spatial audio and procedural tone experiments.
- Local .NET and Arduino validation scripts are available.

Still provisional:

- MPU9250 head/controller sensor integration is not implemented in the current Unity-Arduino loop.
- vibration output protocol exists at command level but final motor driving behavior is not yet proven.
- HRTF spatialization is not active until a Unity spatializer plugin is installed/configured.
- brightness/color tuning must continue on physical hardware because LED power and diffusion can change perceived color.
- rain and wall light textures are still experimental and should be treated as tuning targets, not final art.

### 30.11 Recommended Next Implementation Order

Immediate next steps:

1. stabilize physical LED perception with the board preview open
2. confirm rain/wall/bell patterns on headset hardware after every sketch upload
3. integrate MPU9250 telemetry into Arduino output
4. map head orientation to Unity listener orientation
5. map controller orientation to scan/cleanse input
6. implement one complete search-to-cleanse interaction loop

Do not expand content until steps 1-6 are stable.
