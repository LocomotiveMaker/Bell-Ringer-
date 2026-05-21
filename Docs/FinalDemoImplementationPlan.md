# Bell Ringer Final Demo Implementation Plan

Last updated: 2026-05-21

This plan is based on:

- `Docs/FinalDemoDecisionLock.md`
- `Docs/FinalDemoFlowAudioIntegration.md`
- current test scenes and components under `Assets/Scripts`

If this document conflicts with older notes, use `FinalDemoDecisionLock.md` first,
then this implementation plan, then `FinalDemoFlowAudioIntegration.md`.

## Goal

Build one complete 5-7 minute playable demo before polishing individual
mechanics. The correct implementation strategy is not to perfect each isolated
test scene first. The correct strategy is:

1. Create the final scene shell and central data/tuning layer.
2. Connect every stage end-to-end with simple but working transitions.
3. Add audio, LED, haptic, and assist rules through shared routers.
4. Run full-flow closed-eye tests.
5. Tune weak sections aggressively.

The demo must be able to run from preflight to forest ending even if some
sections are visually or sonically rough on the first integration pass.

## Locked Route

The final demo route is:

1. Preflight / calibration.
2. Opening wake.
3. Bell orbit / sound-light binding.
4. Bell follow target 1.
5. Bell follow target 2 with rain/wind masking.
6. Bell gaze tutorial: 3 gaze successes, 2 moves.
7. Bell acquisition.
8. General tinnitus 1.
9. General tinnitus 2 with boss foreshadowing.
10. Boss approach.
11. Boss cleanse pattern 1.
12. Boss cleanse pattern 2.
13. Boss cleanse pattern 3.
14. Boss defeat / cut to forest.
15. Forest ending: follow final bell or auto-end after 10 seconds.

## Highest-Risk Items

These risks decide the implementation order:

- The user must be able to complete the route even if tracking is imperfect.
- Operator skip/force-complete must exist before deep tuning.
- Audio cues must be addressable by cue ID, not direct scene references.
- Timing and tolerances must be centralized because the user will tune them.
- Bell readability must survive rain/wind.
- Boss must be understandable: slow learned path, generous range, reset on miss.
- Player movement lock/unlock must be reliable.
- LED priority must prevent rain from burying bell or tinnitus.
- Haptics must not become noisy; they must communicate lock, progress, failure,
  and assist.

## Architecture

### Scene

Create a new scene:

- `Assets/Scenes/FinalDemo.unity`

Keep existing test scenes intact:

- `LightTextureTest.unity`
- `PadTrackingTest.unity`
- `TinnitusTest.unity`
- `GiantTinnitusTest.unity`
- `BellGazeTutorialTest.unity`
- `AudioLabTest.unity`
- `ClosedEyeCalibration.unity`

The test scenes remain validation tools. They should not be destructively merged.

### Core Runtime Objects

`FinalDemo.unity` should contain these root objects:

- `FinalDemoRoot`
- `PlayerRig`
- `WorldAudio`
- `WorldLight`
- `WorldHaptics`
- `ObserverView`
- `DebugOperatorPanel`

Minimum components:

- `FinalDemoDirector`
- `FinalDemoTuningProfile`
- `FinalDemoCueLibrary`
- `FinalDemoAudioRouter`
- `FinalDemoLightRouter`
- `FinalDemoHapticRouter`
- `FinalDemoInputStatus`
- `FinalDemoOperatorControls`

### Data First, Not Hardcoding

Create two central assets:

- `Assets/ScriptableObjects/FinalDemo/FinalDemoTuningProfile.asset`
- `Assets/ScriptableObjects/FinalDemo/FinalDemoCueLibrary.asset`

The scene should reference these two assets. Most values should be adjusted from
these assets, not from scattered MonoBehaviour fields.

## FinalDemoTuningProfile

This is required early because almost every section needs tuning.

Include at minimum:

- opening ambience seconds
- opening silence seconds
- bell orbit seconds
- bell follow target positions
- bell arrival radius, default `0.8m`
- bell call interval
- bell assist timeout
- bell assist gain multiplier
- rain zone start position/radius
- rain intensity ramp seconds
- rain assist volume floor
- gaze hold seconds, default `2.2s`
- gaze cone degrees
- gaze assist delay seconds
- gaze assist max cone degrees
- bell acquisition haptic strength
- bell acquisition haptic seconds
- general tinnitus count, default `2`
- general tinnitus cleanse seconds, default `4s`
- general tinnitus target positions
- general tinnitus target pad poses
- general tinnitus pose tolerances
- boss pattern count, default `3`
- boss pattern durations, default `8.0 / 8.5 / 9.0`
- boss fixed start seconds, default `2s`
- boss pattern start poses
- boss pattern movement offsets or path points
- boss pose tolerances
- boss assist failure count threshold
- boss assist tolerance multiplier
- ending auto-finish seconds, default `10s`
- HRTF enabled default
- global skip enabled
- debug drawing enabled

Do not overbuild custom inspectors on the first pass. A normal ScriptableObject
inspector is enough.

## FinalDemoCueLibrary

Use cue IDs as the stable API. Scene logic should call cue IDs, not direct
AudioClip fields.

Minimum cue IDs:

- `BELL_OPENING_ORBIT`
- `BELL_MOVEMENT_TEXTURE`
- `BELL_DISTANT_CALL`
- `BELL_STRONG_ASSIST`
- `BELL_PAD_SHAKE_RESPONSE`
- `BELL_GAZE_SUCCESS`
- `BELL_ACQUISITION`
- `RAIN_LIGHT_BED`
- `RAIN_CLOSE_DROPS`
- `RAIN_STRONG_BED`
- `NARR_FOLLOW_BELL`
- `NARR_LOOK_BELL`
- `TINNITUS_LONG_GLITCH`
- `TINNITUS_BURST`
- `TINNITUS_POSE_LOCK`
- `TINNITUS_POSE_LOST`
- `TINNITUS_HEALING_LOOP`
- `TINNITUS_RESOLVE`
- `BOSS_BASE_PULSE`
- `BOSS_GLITCH_BURST`
- `BOSS_WEAKPOINT_MOVE`
- `BOSS_HIT`
- `BOSS_DEFEAT_RISE`
- `BOSS_DEFEAT_AIR`
- `TRANSITION_SOFT_CUT`
- `FOREST_BED`
- `FOREST_BELL`

Each cue entry should support:

- cue ID
- default clip
- alternate clips
- loop or one-shot
- spatialized or non-spatialized
- mixer group
- volume
- pitch
- min distance / max distance
- optional low-pass cutoff
- optional high-pass cutoff
- optional LED binding enum
- optional haptic binding enum

Do not implement full dropdown UX before the flow exists. Store alternates in an
array first. Inspector polish can follow.

## AudioMixer

Create a minimal AudioMixer from the start:

- Master
- Bell
- RainWind
- Tinnitus
- BossTinnitus
- Ambience
- Interaction
- Narration

Minimum required behavior:

- narration ducks RainWind and Tinnitus slightly
- bell remains readable during rain
- global master volume is adjustable
- HRTF/spatializer can be toggled or bypassed

If Steam Audio/HRTF becomes unstable, keep the toggle off and ship Unity default
3D audio for the demo.

## Shared Routers

### FinalDemoAudioRouter

Responsibilities:

- Play one-shot by cue ID at world position.
- Start/stop loops by cue ID with handle.
- Crossfade loop volume.
- Apply narration ducking.
- Apply bell assist gain.
- Stop all cues for emergency reset.

Avoid implementing a general audio engine. The router should only support this
demo's needs.

### FinalDemoLightRouter

Responsibilities:

- Own LED priority.
- Convert world target positions to 16x8 LED commands.
- Route bell point/ripple.
- Route rain floor band.
- Route tinnitus violet point/tear.
- Clear LEDs during resolve and stage reset.

Priority:

1. Pad / critical interaction feedback
2. Bell
3. Tinnitus / boss
4. Wall/noise
5. Rain

Rain must yield when bell or tinnitus needs readability.

### FinalDemoHapticRouter

Responsibilities:

- Trigger bell assist pulse.
- Trigger bell acquisition pulse.
- Trigger tinnitus approach vibration.
- Trigger tinnitus lock pulse.
- Trigger tinnitus cleanse hum.
- Trigger boss tracking pulse.
- Trigger boss failure stop/unstable pulse.
- Trigger boss hit pulse.
- Stop all haptics on stage reset.

Priority:

1. Cleanse lock / cleanse progress
2. Boss failure / success
3. Bell assist
4. Environmental drops

Use left/right motor separation when possible. Fall back to whole-pad vibration.

## Stage Director

Create a single `FinalDemoDirector` with explicit states:

- `Preflight`
- `OpeningAmbience`
- `OpeningSilence`
- `OpeningCloseBell`
- `BellOrbit`
- `BellFollowOne`
- `BellFollowRain`
- `BellGaze`
- `BellAcquisition`
- `GeneralTinnitusOne`
- `GeneralTinnitusTwo`
- `BossApproach`
- `BossPatternOne`
- `BossPatternTwo`
- `BossPatternThree`
- `BossDefeat`
- `ForestEnding`
- `Complete`

Every state needs:

- enter
- tick
- exit
- force complete
- reset current state
- debug label

Do not hide flow in coroutines only. Coroutines are acceptable for timed audio
or fades, but the current state should remain visible and force-progressable.

## Operator Controls

Implement before polishing gameplay.

Required controls:

- Start demo
- Force next stage
- Force complete current objective
- Reset current stage
- Recenter head
- Recenter pad
- Toggle assist level
- Toggle HRTF/spatializer mode
- Stop all audio/haptics/LED

Required display:

- current stage
- elapsed stage time
- head IMU connected/fresh
- LED connected
- pad IMU connected/fresh
- ArUco valid
- vibration target found
- current assist level
- current objective progress
- current cue or loop status if practical

The operator panel is allowed to be visible on the monitor. The player should not
use it.

## Implementation Order

The safest order is below. Do not start with boss polish, advanced visuals, or
HRTF integration before the full route is playable.

### Bundle 1: Final Scene Skeleton

Goal: one scene can start, display state, and move through placeholder stages.

Implement:

- `FinalDemo.unity`
- `FinalDemoDirector`
- `FinalDemoTuningProfile`
- `FinalDemoOperatorControls`
- `FinalDemoInputStatus`
- `PlayerRig` using current head look and left-stick movement
- movement lock API
- skip/force-complete/reset controls

Acceptance:

- scene enters `Preflight`
- operator can start
- all stages can be force-advanced to `Complete`
- movement can be locked/unlocked
- C# build passes

### Bundle 2: Cue Library + Audio Router + Mixer

Goal: every future stage can request sound by cue ID.

Implement:

- `FinalDemoCueLibrary`
- cue entry type
- minimal AudioMixer groups
- `FinalDemoAudioRouter`
- cue handles for loops
- narration ducking hook
- load default cues from documented paths

Acceptance:

- operator can test representative cues from each bus
- bell one-shot spatializes
- rain loop starts/stops
- tinnitus loop starts/stops
- narration cue ducks rain/tinnitus
- C# build passes

### Bundle 3: Light + Haptic Router

Goal: feedback channels are centralized before gameplay wiring.

Implement:

- `FinalDemoLightRouter`
- bell point and mini-ripple command
- rain floor band command
- tinnitus/boss violet point command
- clear LED command
- `FinalDemoHapticRouter`
- bell assist pulse
- tinnitus lock/progress vibration
- boss hit/failure vibration

Acceptance:

- each feedback type can be tested from operator panel
- rain does not overwrite bell when both are active
- haptics stop on emergency stop/reset
- C# build passes

### Bundle 4: Opening + Bell Orbit

Goal: first sensory sequence works from start without player input.

Implement:

- opening ambience fade
- opening silence
- close left bell
- fixed bell orbit path: left, front, right/back, above
- bell light follows sound position
- movement locked during opening
- automatic transition to Bell Follow

Acceptance:

- route plays without input until Bell Follow
- bell LED appears only on bell events
- operator can skip opening/orbit
- no rain/tinnitus audio leaks into opening

### Bundle 5: Bell Follow + Rain/Wind

Goal: player follows at least two bell targets, second with rain/wind.

Implement:

- two bell target positions
- arrival radius check, default `0.8m`
- periodic distant bell calls
- mandatory `NARR_FOLLOW_BELL`
- pad shake assist gesture/input if available
- timer-based bell assist fallback
- rain zone trigger
- rain bed and close droplet loop/events
- bell readability assist: raise bell, lower rain

Acceptance:

- player can reach target 1
- player can reach target 2 with rain active
- rain starts by zone entry
- bell remains readable
- operator can force arrival

### Bundle 6: Bell Gaze + Acquisition

Goal: head-gaze mechanic works inside the final route.

Implement:

- rain drops sharply before gaze
- bell appears near forward view
- 3 gaze successes, 2 moves
- progress pauses, does not reset, when looking away
- gaze cone widening assist
- gaze success cue
- bell movement cue
- acquisition cue
- acquisition haptic pulse
- short transition silence after acquisition

Acceptance:

- gaze uses head rotation, not right stick
- three successes complete the section
- assist prevents indefinite stall
- acquisition transitions to tinnitus section

### Bundle 7: General Tinnitus One + Two

Goal: normal tinnitus loop is playable twice in the final route.

Implement:

- two tinnitus target objects
- player walks to find each
- violet light only when looking/head-turned toward it
- generated tinnitus tone with long glitch layer
- pad pose preset per tinnitus
- 4s cleanse, pause not reset on miss
- approach vibration
- lock pulse
- slower cleanse hum than current prototype
- replacement resolve from `HealingMotion/05_mixkit_fast_sci_fi_sweep.wav`
- no old `Tinnitus/Resolve` cues
- boss foreshadowing after second cleanse

Acceptance:

- both targets can be found and cleansed
- leaving pose pauses but preserves progress
- resolve clears audio and LED
- boss ambience becomes noticeable after second cleanse

### Bundle 8: Boss Tinnitus

Goal: boss patterns are complete enough to evaluate under closed eyes.

Implement:

- boss area trigger
- player movement lock
- boss base pulse loop
- boss violet mass light
- three preset weak-point paths
- pattern durations `8.0 / 8.5 / 9.0`
- first 2s fixed pose, remaining slow movement
- failure reset to pattern start
- path remains fixed after failure
- tolerance widening after repeated failures
- directional haptic hint between patterns
- hit cue on pattern completion

Acceptance:

- boss cannot be solved by walking
- each pattern can be completed
- miss resets current pattern only
- route advances to boss defeat after pattern 3
- operator can skip any pattern

### Bundle 9: Boss Defeat + Forest Ending

Goal: the demo can finish with the intended release.

Implement:

- boss sound stop
- larger release/transition chain
- violet LED clear
- short silence
- forest bed fade in
- familiar bell ahead
- final follow or 10s auto-end
- no final narration
- final `Complete` state

Acceptance:

- boss defeat feels like release, not reward jingle
- forest appears after silence
- final bell can end the demo
- demo auto-ends if player does not move

### Bundle 10: Full-Flow QA + Tuning

Goal: make the complete route robust enough for 2026-05-26.

Implement/tune:

- reduce opening if too long
- tune bell volume/range
- tune rain masking and assist
- tune gaze cone and assist
- tune tinnitus tolerances
- tune boss tolerances and path speed
- tune haptic strength
- tune LED brightness for closed-eye comfort
- test HRTF toggle and decide default
- clean observer screen labels

Acceptance:

- full route completes in 5-7 minutes
- operator can recover from stalls
- no section requires looking at monitor
- no painful sustained audio or harsh full LED flash
- C# build passes
- Arduino sketches unchanged unless explicitly needed

## Practical Daily Priority

With 2026-05-26 as the demo date, the work should be sequenced by playable
coverage:

### Day 1: Foundation and First Half

- Bundle 1
- Bundle 2
- Bundle 3
- Start Bundle 4

Target result:

- `FinalDemo.unity` exists.
- Cue/tuning/routers exist.
- Operator can run and skip placeholder route.
- Opening and bell orbit are starting to work.

### Day 2: Bell Route Complete

- Finish Bundle 4
- Bundle 5
- Bundle 6

Target result:

- Start through bell acquisition is playable.
- Rain and gaze are present, even if rough.

### Day 3: Tinnitus Route Complete

- Bundle 7
- Bundle 8 first playable pass

Target result:

- General tinnitus and boss are connected in the final scene.
- Boss has all three patterns, even if tuning is generous.

### Day 4: Ending and Full-Flow Run

- Finish Bundle 8
- Bundle 9
- First full closed-eye run

Target result:

- Full demo can reach ending.
- Operator recovery controls are proven.

### Day 5: Tuning / Safety / Presentation

- Bundle 10

Target result:

- 5-7 minute route.
- Reduced stalls.
- Presentable observer screen.
- Audio/LED/haptic levels safe enough for repeated exhibition testing.

## Scope Cut Rules

If the route is not complete, cut in this order:

1. Reduce boss patterns from 3 to 2.
2. Keep boss patterns but shorten durations.
3. Simplify rain/wind to one rain bed and one volume curve.
4. Remove second general tinnitus.
5. Simplify forest ending to timed sound fade and final bell.
6. Shorten opening.

Do not cut:

- preflight status
- operator force next/complete
- cue table
- tuning profile
- bell follow
- one tinnitus cleanse
- one boss pattern
- final completion state

## Code Reuse Strategy

Use existing tested components as references or adapters:

- `BellRingerSimpleMoveLookController` for movement/head look.
- `HeadImuReceiver` and `HeadTiltInputProvider` for head input.
- `PadTrackingReceiver`, `PadImuReceiver`, `PadPoseProvider` for pad pose.
- `PadPoseMatchEvaluator` for normal tinnitus and boss weak-point matching.
- `BellGazeTutorialController` as the base for final gaze logic.
- `GiantTinnitusEncounterController` as the base for boss weak-point tracking.
- `TinnitusAudioController` and `TinnitusLightPatternController` for normal
  tinnitus identity.
- `BellRingerSpatialLightTextureSampleController` rain projection code as a
  reference for rain floor light.
- `HardwareBridge` for LED output and telemetry.

Do not make `TinnitusTestController` the final game controller. It is useful as
a test harness, but `FinalDemoDirector` should own the final route.

## Implementation Notes

- Prefer simple explicit stage code over abstract quest/event frameworks.
- Prefer visible state and operator controls over hidden coroutines.
- Keep every stage force-completable.
- Keep all stage timings in `FinalDemoTuningProfile`.
- Keep all sound selection in `FinalDemoCueLibrary`.
- Keep existing test scenes stable.
- Avoid destructive refactors before the route is playable.
- Audio polish should follow cue routing, not precede it.
- HRTF should remain optional until proven stable on the actual demo machine.

## First Concrete Implementation Task

The first actual coding task should be:

1. Add `FinalDemoTuningProfile`.
2. Add `FinalDemoCueLibrary`.
3. Add `FinalDemoDirector` with all states and operator skip controls.
4. Add `FinalDemo.unity` containing the director and placeholder world objects.
5. Verify the scene can start, force-advance through every state, and finish.

This creates the spine. Every later feature attaches to that spine.
