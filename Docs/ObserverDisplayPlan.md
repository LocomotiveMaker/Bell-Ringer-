# Bell Ringer Observer Display Plan

Last updated: 2026-05-25

This document defines the monitor-side presentation for `FinalDemo`. It is
separate from the closed-eye player experience and from hardware/input work.
The observer display should read state from the final demo systems, but it must
not own gameplay, audio, haptic, LED, or tracking logic.

Related documents:

- `Docs/FinalDemoImplementationPlan.md`
- `Docs/FinalDemoDecisionLock.md`
- `Docs/FinalDemoFlowAudioIntegration.md`
- `Docs/DISPLAY_EX.png`

## Goal

The monitor should make the demo understandable and visually compelling for
spectators while preserving the actual player experience as sound/light/haptic
first.

Minimum required visible elements:

- pad
- bell
- bell shake animation synced to pad shake
- rain
- tinnitus
- boss tinnitus
- forest ending
- 16x8 LED board preview showing what the player receives on the near-eye board

Nice-to-have visible elements:

- wind pushing rain directionally
- simple wall/noise planes around the tinnitus section
- low-poly dead-bell landscape inspired by the concept art
- stage/progress panel for spectators and operator
- sound-source compass or sonograph

## Art Direction

The concept is a dead sound world with one living green bell.

Visual reference:

- black/gray etched bell-space
- huge dead bells or bell shells in the distance
- small human/player scale against oversized sound objects
- scratchy monochrome texture
- the playable bell is the only clean green object
- tinnitus objects look like stored/rotting sound, not normal enemies

Practical Unity direction:

- low-poly or simple mesh silhouettes
- dark grayscale materials
- emissive green for the living bell
- violet/purple emissive for tinnitus
- deep blue rain floor particles
- cyan/white low-intensity wall/noise planes
- strong but controlled URP post-processing

Do not spend time on realistic animation. The monitor can be abstract and
stylized. The player's sensory feedback remains the primary product.

## Architecture Boundary

Create a separate observer root under `FinalDemo.unity`:

- `ObserverDisplayRoot`

Suggested scripts under:

- `Assets/Scripts/ObserverDisplay/`

Suggested components:

- `ObserverDisplayController`
- `ObserverDisplayLayout`
- `ObserverStagePanel`
- `ObserverPadView`
- `ObserverBellView`
- `ObserverRainView`
- `ObserverTinnitusView`
- `ObserverBossTinnitusView`
- `ObserverForestView`
- `ObserverLedMatrixPreview`
- `ObserverPostProcessController`
- `ObserverSoundMapPanel`

Read-only data sources:

- `FinalDemoDirector`: current stage and objective progress
- `FinalDemoInputStatus`: head/pad/ArUco state
- `FinalDemoLightRouter`: 16x8 logical LED buffer
- `FinalDemoAudioRouter`: active cue IDs and source positions, if exposed
- `FinalDemoTuningProfile`: layout/timing values only when needed

Observer display must not:

- move the player
- trigger stage progression
- play required gameplay audio
- drive haptics
- drive hardware LEDs
- overwrite pad/head tracking state

It may:

- show world positions
- mirror stage progress
- show pad pose
- show active cue names
- expose operator-only buttons through existing `FinalDemoOperatorControls`

## Layout

Use `Docs/DISPLAY_EX.png` as the first layout constraint.

The bottom center must stay visually clear because the phone camera/pad tracking
setup sits in front of the laptop. Avoid bright motion or dense UI in this area.

Recommended screen layout:

- Main world viewport: top large area, full width.
- Bottom left panel: spectator state / sound map panel.
- Bottom center: camera-safe empty area.
- Bottom right panel: 16x8 LED board preview.

### Main World Viewport

Shows the stylized game world:

- player marker or silhouette
- pad model near the player
- living green bell
- rain particles/floor splashes
- tinnitus objects
- boss object
- forest ending environment

This area should be visually interesting but not cluttered. It is acceptable for
the world to be a dark stage with symbolic objects instead of a literal level.

### Bottom Left Panel Recommendation

Do not use this area only for raw debug numbers during exhibition. Use it as a
mixed spectator/operator panel.

Recommended contents:

- current stage name
- current objective phrase
- objective progress bar
- active sound focus: bell / rain / tinnitus / boss / forest
- small top-down sound map showing player, bell, tinnitus, boss, and walls
- tracking health icons: head, pad, ArUco, LED, vibration
- operator-only small debug values collapsible or shown in a smaller row

This is better than pure debug because spectators can understand what the player
is trying to do. Raw values can still be available for the operator.

### Bottom Center

Keep this area mostly empty or black.

Allowed:

- very subtle border
- small `TRACKING CLEAR` / `ARUCO LOST` indicator at the top edge only
- no moving particles
- no bright pulses

### Bottom Right Panel

Show the near-eye 16x8 LED matrix preview.

Rules:

- Mirror the logical LED output from `FinalDemoLightRouter`.
- Use a display-only brightness multiplier so dark hardware patterns are visible.
- Preserve 16x8 layout.
- If the physical hardware is two 8x8 panels, optionally draw a thin divider.
- Do not invent separate monitor-only LED effects in this preview.

This panel lets spectators understand the player's closed-eye light channel.

## Required Visual Systems

### Pad View

Purpose: spectators understand that the player is moving a tracked object.

Representation:

- simple low-poly rectangular pad or controller plate
- yellow/gold accent
- ArUco-marker-like square on top for readability
- follows tracked pad position/rotation

Shake response:

- when pad shake is detected, add a short rotational jitter to the monitor pad
- emit a faint yellow/green link pulse from pad to bell
- do not trigger separate gameplay state from this visual effect

### Bell View

Purpose: the living object and main identity of the piece.

Representation:

- small handbell or abstract bell mesh
- green emissive core
- dark metal silhouette
- optional green spirit-like thread inside, matching the "spirit trapped in the
  bell" narration framing

States:

- opening close bell: appears near player silhouette/left side
- orbit: follows fixed path left, front, right/back, above
- follow: moves to target positions
- gaze: steadier and brighter when being looked at
- acquisition: pulls green light inward and links to pad
- ending: appears ahead in forest

Shake animation:

- if the player shakes the pad, the screen bell should visibly shake in the same
  rhythm
- green sound rings or short strokes emit from the bell
- keep rings local, not full-screen

### Rain View

Purpose: show cocktail-effect masking and world pressure.

Representation:

- deep blue floor-impact particles
- most rain activity near the ground/floor plane
- small splashes or droplets
- not a full-screen weather overlay

Wind:

- when wind intensity rises, rain particle velocity bends sideways
- use wind streaks only sparingly
- rain should look like it is being swept, not like generic snow or screen noise

Assist visibility:

- if rain volume is being reduced by assist, optionally reduce particle density
  slightly
- do not expose assist as a player-facing UI concept

### Tinnitus View

Purpose: show the wrong sound source without making it a normal monster.

Representation:

- violet/purple unstable orb or wound
- glitchy outline
- short radial tears
- flicker tied to sound intensity/glitch bursts
- thin high-frequency line patterns can pass through it

Cleanse:

- when pad pose approaches correct, object stabilizes or tightens
- while cleansing, surface tears reverse, collapse, or desaturate
- on resolve, emit one short echo ripple and fully disappear
- do not leave a stable residual object

Walls/noise near tinnitus:

- add simple cyan/white glitch planes along the sides of the tinnitus route
- they frame the path rather than becoming a full wall-escape gameplay section

### Boss Tinnitus View

Purpose: make the boss feel much larger than normal tinnitus.

Representation:

- larger violet/black sound mass
- low-frequency pulsing body
- intermittent electric/glitch cracks
- orbiting or embedded weak point
- scale should communicate event importance

Pattern feedback:

- first 2 seconds: weak point holds still
- moving section: weak point drifts slowly on fixed route
- on hit: boss body contracts or flashes briefly
- on failure: weak point snaps back and boss pulse reasserts
- left/right haptic direction may be visualized as a subtle directional streak

### Forest Ending View

Purpose: final contrast after dead sound world.

Representation:

- dark world opens into low-poly forest silhouettes
- green-gray or cool blue-green fog
- sparse tree trunks/leaves
- gentle particles like pollen or small leaves
- bird/forest sound should feel visible through subtle motion, not literal birds
  everywhere

Ending:

- after boss defeat, clear violet light
- short darkness/silence
- forest fades in
- familiar green bell appears ahead
- if player moves forward, fade out
- if player does not move, auto-end after 10 seconds

## Post-Processing Plan

Use URP Volume profiles per stage if the project uses URP. If not, implement the
closest equivalent with available post-processing.

Recommended profiles:

- `Observer_DefaultDeadWorld`
- `Observer_RainWind`
- `Observer_Tinnitus`
- `Observer_Boss`
- `Observer_Forest`

Effects to use:

- bloom for emissive bell/tinnitus
- vignette for focus and darkness
- film grain/noise for etched concept look
- color adjustments for desaturated dead world
- chromatic aberration only during tinnitus/boss, not constantly
- lens distortion very subtle, mainly boss/tinnitus
- depth of field only if it does not obscure gameplay objects

Avoid:

- constant heavy chromatic aberration
- full-screen white flashes
- effects that hide the bell or pad
- post-processing that makes the bottom tracking-safe area bright or busy

## Implementation Priority

Minimum viable observer display:

1. Build `ObserverDisplayRoot`.
2. Add main camera/world view with dark stage.
3. Add bottom layout panels and keep bottom center empty.
4. Add 16x8 LED matrix preview from logical LED buffer.
5. Add pad view from tracked pad pose.
6. Add bell view and bell shake animation.
7. Add rain floor particles and wind bend.
8. Add normal tinnitus view.
9. Add boss tinnitus view.
10. Add forest ending view.
11. Add stage/post-process switching.

If time is short, do not build a beautiful forest first. Build pad, bell, LED
preview, rain, tinnitus, and boss readability first.

## Separation From Device AI Work

The hardware/player-experience implementation remains the source of truth.

Observer display work should be assigned as a separate task stream:

- It consumes stage and tracking state.
- It mirrors light output.
- It visualizes objects already present in the final route.
- It never changes tracking, haptics, audio, or LED behavior.

Any new display-side code should be documented here or in the implementation log
below so the hardware/gameplay developer knows what was added.

## Implementation Log

### 2026-05-25

- Created this observer display plan.
- Locked required monitor elements: pad, bell, bell shake, rain, tinnitus, boss,
  forest, and 16x8 LED preview.
- Set `Docs/DISPLAY_EX.png` as the layout constraint.
- Decided bottom left should be a mixed spectator state / sound map panel, not
  only raw debug numbers.
- Set bottom center as camera-safe empty area for phone-based pad tracking.
