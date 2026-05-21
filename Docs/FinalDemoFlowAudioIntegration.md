# Bell Ringer Final Demo Flow / Audio Integration

Last updated: 2026-05-21

This document is the current planning source for the demo flow and sound usage.
It supersedes older flow notes in `AudioCueUsage.md` when there is a conflict.
`AudioCueUsage.md` remains useful as the candidate library and license/source note.

Use `Docs/FinalDemoDecisionLock.md` together with this document. The decision
lock records the latest questionnaire answers for scope, implementation priority,
assist rules, HRTF testing, observer screen, and final demo assembly.

## Core Rule

The player is assumed to have closed or blocked vision. The primary feedback
channels are spatial sound, near-eye LED light, controller vibration, head
rotation, and pad position/rotation.

The monitor view is for observers and debugging. It may show the bell, tinnitus,
walls, rain floor, and current state, but player decisions should remain based on
sound, light, and haptics.

## Hardware / Input Assumptions

- Head ESP32-S3: two LED panels and head IMU.
- Pad ESP32-S3 or existing pad path: pad IMU and vibration.
- Pad position: camera ArUco tracking.
- Pad yaw: prefer camera pose.
- Pad pitch/roll: use pad IMU.
- Head rotation: main camera/look input.
- Right-stick camera rotation on the pad: disabled for the main demo.
- Recenter controls are required before play: `Recenter Head` and pad pose center.

## Audio Selection Policy

Use a ScriptableObject or equivalent cue table with explicit cue IDs. If one cue
has older bell-selected sounds and a newly assigned sound, expose all as a
dropdown/list in the inspector. The default option must be the most recently
assigned sound in this document.

Do not delete older options from dropdowns unless the user explicitly rejects
them. Mark rejected or held sounds with status rather than silently removing them.

For implementation, each cue should support:

- default clip
- alternate clips
- loop / one-shot
- spatialized / non-spatialized
- volume range
- pitch range
- low-pass / high-pass if needed
- LED color/pattern binding
- haptic binding
- stage/state trigger

## Global Mix Direction

Keep the game quiet, mysterious, dreamlike, and spacious. Avoid music-like loops
during progression. Let silence and near-silence remain part of the language.

Suggested buses:

- `Bell`: clear, readable, mostly unmasked, green LED binding.
- `RainWind`: floor-wide rain and wind, duck slightly when narration plays.
- `Tinnitus`: uncomfortable but not painful, violet LED binding.
- `BossTinnitus`: larger low pulse, glitch, and heavy pressure.
- `Ambience`: very low open ground / air texture.
- `Interaction`: wall scan, contact, pad/tactile feedback.
- `Narration`: temporary Windows TTS first, later replace clips only.

## Color / Light Binding

- Bell: green.
- Rain: deep blue, mainly floor/bottom LED region. Looking down increases
  visibility; looking upward reduces it.
- Wall/noise plane: cyan or weak pale glitch/noise. Use as plane-like boundary
  information, not a bright object.
- Tinnitus: violet/purple is now preferred over red because closed-eye red,
  yellow, and white separation was weak.
- Pad: yellow can remain for debug or subtle pad binding, but avoid relying on it
  as the only closed-eye information channel.

Bell light should be minimal: a point at the sound position plus only 3-4 short
expanding cells. Do not use a large full ripple; it blurs under closed eyes.

## Stage Flow

### 0. Preflight / Calibration

Purpose: make the demo robust before the sensory experience begins.

Observer/debug UI should show:

- head IMU connected
- head LED connected
- pad IMU connected
- pad vibration connected
- camera/ArUco tracking valid
- current pad pose center
- current head forward center

No strong world audio should play here. If an audio check is needed, use a short
muted bell or a separate debug-only cue.

Narration is optional here and should be debug-only, not part of the artwork.

### 1. Opening Wake

Intent: the player wakes into a quiet sound world.

Sequence:

1. A faint bell-like opening ambience grows for about 10 seconds.
2. All sound drops into about 5 seconds of silence.
3. A close bell appears near the player's left ear.

Sound:

- Primary bell close/orbit source:
  `Assets/Audio/Curated/Bell/Selected/06_opening_orbit_bigsoundbank_2114.wav`
- Optional opening texture:
  `Assets/Audio/Curated/Bell/Selected/07_bell_movement_transition_gimi_glitch_bells.wav`

Mix:

- Keep the first 10 seconds very soft.
- The 5-second silence must feel intentional; do not fill it with ambience.
- The left-ear bell should be close, but not a jump scare.

Light:

- Bell green point appears only with the close bell.
- The point may pulse once or twice, then become spatially tied to the sound.

### 2. Bell Orbit / Sound-Light Binding

Intent: teach that sound and light are one linked sense in this game.
The goal is not "look at the bell" yet. The goal is "the bell has a sound
position and a matching light position."

Sequence:

1. Bell starts from the left.
2. Bell moves to front.
3. Bell moves toward right/back.
4. Bell may pass above the player.
5. The player hears and sees that bell movement creates matching green light.

Sound:

- Main orbit:
  `Assets/Audio/Curated/Bell/Selected/06_opening_orbit_bigsoundbank_2114.wav`
- Movement texture dropdown:
  - `Assets/Audio/Curated/Bell/Selected/07_bell_movement_transition_gimi_glitch_bells.wav`
  - default: `Assets/Audio/Curated/Crystal/Collision/06_mixkit_magic_crystal_hit_c.wav`

Light:

- Green point follows the bell.
- On each bell attack, emit a short 3-4 cell ripple from the current bell point.

Haptics:

- None required yet.
- Do not add pad button cues here.

### 3. Bell Follow

Intent: the player starts following a moving sound.

Sequence:

1. Bell moves to a location ahead or off-axis.
2. Narration says to follow the bell.
3. Bell calls periodically.
4. Player walks toward the bell using spatial audio and green LED hints.
5. If the player is lost, pad shake can re-emphasize the bell.

Narration:

- Temporary line: "종소리를 따라, 천천히 이동하세요."
- Use this line once after the bell commits to its first destination.

Sound:

- Distant bell dropdown:
  - `Assets/Audio/Curated/Bell/Selected/03_distant_call_bigsoundbank_0292.wav`
  - default: `Assets/Audio/Curated/Bell/Selected/04_distant_call_bigsoundbank_0293.wav`
- Strong assist after repeated failure:
  `Assets/Audio/Curated/Bell/Selected/05_strong_assist_call_after_fourth_bigsoundbank_0294.wav`
- Pad shake / bell re-emphasis:
  - old bell options may remain in dropdown
  - default: `Assets/Audio/Curated/Crystal/Collision/02_mixkit_soft_bell_chime_clean_hit.wav`

Important interaction rule:

- Do not use pad button sound in the demo.
- Use pad shake when the player cannot hear or locate the bell.
- Pad shake should feel like the pad and bell are synchronized. The sound should
  be bell-like or crystal-bell-like, not a UI click.

Light:

- Green point appears at bell position on each call.
- Use short mini-ripple only.
- Avoid full-screen green wash.

Haptics:

- On pad shake, add a short soft vibration synchronized with the bell response.

### 4. Rain / Wind Cocktail Section

Intent: demonstrate cocktail-party listening. The bell remains the target, but
rain and wind make it harder to separate.

Sequence:

1. Start with light floor rain while bell is still readable.
2. Add close droplets.
3. Gradually introduce wind/rain-wind texture.
4. Bell continues to call from the target direction.
5. Player must keep separating the bell from the environment.

Early rain candidates:

- `Assets/Audio/Curated/Rain/Bed/04_mixkit_light_rain_loop_long.wav`
- `Assets/Audio/Curated/Rain/CloseDrops/02_mixkit_heavy_rain_drops.wav`

Rain-wind / stronger rain candidates:

- `Assets/Audio/Curated/Rain/Bed/07_gimi_long_rain_bed_alt.wav`
- `Assets/Audio/Curated/Rain/CloseDrops/03_mixkit_rain_splashing_floor.wav`
- `Assets/Audio/Curated/Rain/CloseDrops/04_mixkit_rain_on_umbrella_body_surface.wav`
- `Assets/Audio/Curated/Ambience/OpenGround/08_mixkit_wide_nature_floor_texture_a.wav`
- hold / check thunder risk:
  `Assets/Audio/Curated/Ambience/OpenGround/09_mixkit_wide_nature_floor_texture_b.wav`

Mix:

- Start with `04_mixkit_light_rain_loop_long` low and wide.
- Add `02_mixkit_heavy_rain_drops` as local one-shots.
- As the section intensifies, crossfade or layer in `07_gimi_long_rain_bed_alt`.
- Use `03_mixkit_rain_splashing_floor` for floor impact.
- Use `04_mixkit_rain_on_umbrella_body_surface` sparingly for body/pad/near hits.
- If `09_mixkit_wide_nature_floor_texture_b` contains thunder or a too-specific
  natural scene, reject it for the main demo.

Light:

- Rain is deep blue and floor-bound.
- Looking down should reveal more rain light.
- Looking upward should reduce rain visibility.
- Drops should be small, low, short-lived pulses.

Bell masking:

- Do not fully hide the bell. The section is about separation, not failure.
- Duck rain slightly for narration only.

### 5. Bell Gaze Tutorial

Intent: after following the bell, teach head rotation / gaze lock.

Sequence:

1. Rain/wind stops or drops sharply.
2. Bell appears near the player's forward view.
3. Player looks at the bell for a cumulative hold.
4. Looking away pauses progress but does not reset it.
5. Bell moves to another location.
6. Repeat until three successful looks are completed.
7. There are three gaze successes and two bell moves.

Sound:

- Use bell calls from the selected bell set.
- On each successful gaze:
  - `Assets/Audio/Curated/Bell/Selected/01_find_punctuation_bigsoundbank_2116.wav`
  - default alternate:
    `Assets/Audio/Curated/Bell/Selected/02_find_punctuation_bigsoundbank_2117.wav`
- Bell movement between gaze targets:
  - `Assets/Audio/Curated/Bell/Selected/07_bell_movement_transition_gimi_glitch_bells.wav`
  - default: `Assets/Audio/Curated/Crystal/Collision/06_mixkit_magic_crystal_hit_c.wav`

Narration:

- Optional first-time line: "빛과 소리가 만나는 곳을 바라보세요."
- Do not repeat this after every move unless testing proves players need it.

Light:

- Bell green point becomes stable enough to look at.
- Successful gaze can slightly tighten the green point and reduce ripple.

### 6. Bell Acquisition

Intent: the bell becomes the player's object and symbolic tool.

Trigger:

- After the third successful bell gaze.

Sound dropdown:

- `Assets/Audio/Curated/Bell/Selected/08_bell_acquisition_gimi_glitch_bells.wav`
- default: `Assets/Audio/Curated/Crystal/Collision/11_mixkit_bright_magic_collision_long.wav`

Mix:

- Acquisition should feel like transformation, not a cheerful reward.
- Keep the attack soft enough for closed-eye play.

Light:

- Green bell light gathers inward or stabilizes.
- Avoid a large flash.

Haptics:

- One clear but not violent vibration pulse.
- After this point, pad shake can imply bell-wave interaction.

### 7. General Tinnitus Setup

Intent: introduce tinnitus as a wrong high-frequency signal embedded in space.

Demo count:

- Minimum: 2 general tinnitus targets before the boss.
- After the second general tinnitus is cleansed, boss presence becomes much more
  obvious with heavier pulse and ambience.

General tinnitus sound identity:

- Thin high-frequency tone generated in Unity.
- Add beating, pitch wobble, and short glitch layers.
- It must be uncomfortable and urgent, but not painful.
- Do not leave a stable residual tone after cleanse.

Long tinnitus texture candidates:

- `Assets/Audio/Curated/Tinnitus/LongGlitch/03_mixkit_terror_radio_frequency_long.wav`
- default:
  `Assets/Audio/Curated/Tinnitus/LongGlitch/04_mixkit_horror_radio_signal_long.wav`

Strong glitch burst candidates:

- `Assets/Audio/Curated/Tinnitus/BurstGlitch/05_mixkit_digital_signal_interference.wav`
- `Assets/Audio/Curated/Tinnitus/BurstGlitch/04_mixkit_glitch_static.wav`
- `Assets/Audio/Curated/Tinnitus/BurstGlitch/03_mixkit_electric_buzz_glitch.wav`
- default:
  `Assets/Audio/Curated/Tinnitus/BurstGlitch/02_mixkit_small_electric_glitch.wav`

Light:

- Use violet/purple.
- When the player is within range and looking toward it, show a violet point.
- On intensity spikes, expand or tear the point briefly. Prefer irregular
  stretching/noise over symmetrical "black hole" widening if it reads more alive.

### 8. General Tinnitus Cleanse

Intent: use pad position and rotation, not head gaze.

Input:

- Required pose: position + yaw + pitch + roll.
- If the pad is inside the valid pose range, cleanse progresses for about 4 sec.
- If the pad exits range, progress pauses but does not reset.

Approach / exact / lost feedback:

- Near correct pose should increase vibration and optional approach sound.
- Exact valid pose uses:
  `Assets/Audio/Curated/Narration/Cue/11_mixkit_narration_soft_ui_risk.wav`
- Leaving valid pose after lock uses:
  `Assets/Audio/Curated/Narration/Cue/05_mixkit_narration_tone_d.wav`

These two files are named as narration cues, but current design assigns them as
tinnitus pose feedback. Keep them very low and reject later if they read as UI.

Healing-in-progress sound:

- default:
  `Assets/Audio/Curated/Tinnitus/HealingMotion/01_bigsoundbank_radio_frequency_sweep_healing.wav`
- hold / uncertain alternate:
  `Assets/Audio/Curated/Tinnitus/HealingMotion/02_mixkit_glitch_rewind_healing.wav`

Resolve:

- Do not use any clip in `Assets/Audio/Curated/Tinnitus/Resolve` for now.
- All previous `Tinnitus/Resolve` candidates are rejected for the current demo.
- Resolve replacement dropdown:
  - `Assets/Audio/Curated/Tinnitus/HealingMotion/03_mixkit_electric_whoosh_healing.wav`
  - default:
    `Assets/Audio/Curated/Tinnitus/HealingMotion/05_mixkit_fast_sci_fi_sweep.wav`

Completion behavior:

- Remove tinnitus sound immediately after resolve.
- Add strong echo/release if needed, then a short silence.
- Clear violet LED completely.
- Do not leave a stable "healed tinnitus" tone.

Haptics:

- Near pose: vibration grows gradually.
- Enter valid range: one strong vibration pulse.
- During cleanse: slower, weaker humming vibration than current implementation.
- Completion: short release pulse, then stop.

### 9. Boss Foreshadowing

Intent: make the boss feel present before direct encounter.

Before boss:

- Very low heartbeat/pulse can appear occasionally during general tinnitus.
- After the second general tinnitus is cleansed, boss sound grows clearly.
- The sound should feel larger and more spatial than normal tinnitus.

Ambience around tinnitus/boss:

- `Assets/Audio/Curated/Ambience/OpenGround/03_mixkit_broad_environment_bed_a.wav`
- `Assets/Audio/Curated/Ambience/OpenGround/07_mixkit_open_ground_texture_a.wav`

General open ambience before heavy sections:

- `Assets/Audio/Curated/Ambience/OpenGround/01_mixkit_wide_breeze_long.wav`
- `Assets/Audio/Curated/Ambience/OpenGround/05_mixkit_open_ground_texture_b_very_low.wav`

### 10. Boss Tinnitus Approach

Intent: entering the boss area changes the rules.

Trigger:

- Player reaches the boss tinnitus area.

Gameplay:

- Lock player movement.
- Left joystick should not move the player.
- The player does not solve this by walking or head-gaze.
- The player tracks weak points with pad pose.

Boss base sound:

- `Assets/Audio/Curated/Tinnitus/Boss/02_mixkit_heartbeat_boss_heavy_a.wav`
- default alternate:
  `Assets/Audio/Curated/Tinnitus/Boss/01_mixkit_heartbeat_boss_low_a.wav`

Boss glitch / instability:

- `Assets/Audio/Curated/Tinnitus/Boss/12_mixkit_boss_electric_burst_b.wav`
- `Assets/Audio/Curated/Tinnitus/Boss/16_mixkit_boss_glitch_noise_a_long.wav`

Light:

- Violet boss mass, larger than normal tinnitus.
- Stronger irregular tearing/pulsing.
- Avoid red if it becomes indistinct under closed eyes.

### 11. Boss Cleanse Pattern

Intent: three learned weak-point traces, not one random chase.

Pattern count:

- Pattern 1: about 8.0 sec total.
- Pattern 2: about 8.5 sec total.
- Pattern 3: about 9.0 sec total.
- Each pattern uses the same mechanic but different initial and movement target
  positions.

Each pattern:

1. First 2 sec: cleanse at initial sound/weak-point pose.
2. Remaining time: sound object moves very slowly along a fixed path.
3. Player follows that path with pad pose.
4. Allowed range during movement should be generous.

Failure:

- Unlike general tinnitus, boss progress resets if the pad exits range.
- Reset applies during the first 2 sec and the moving portion.
- The weak point returns to the first position and cleanse time returns to 0.
- The movement path does not change, so the player can learn the route.

Boss movement / hit feedback:

- When boss sound/weak point moves:
  `Assets/Audio/Curated/Interaction/WallScan/09_mixkit_low_wall_scan_hit_a.wav`
- When boss takes a hit:
  `Assets/Audio/Curated/Interaction/WallScan/13_mixkit_hard_surface_return.wav`
- Heavy hit alternate / risk:
  `Assets/Audio/Curated/Interaction/WallScan/14_mixkit_deep_reveal_hit_big_risk.wav`

Healing / resistance:

- Reuse general healing layer only if it remains readable:
  `Assets/Audio/Curated/Tinnitus/HealingMotion/01_bigsoundbank_radio_frequency_sweep_healing.wav`
- Layer with boss electric burst during instability:
  `Assets/Audio/Curated/Tinnitus/Boss/12_mixkit_boss_electric_burst_b.wav`

Haptics:

- Boss valid range: stronger than normal tinnitus.
- During successful tracking: continuous low pulse plus weaker high vibration.
- Failure: abrupt stop or short unstable vibration.
- Pattern completion: solid hit pulse.

### 12. Boss Defeat / Cut To Forest

Intent: the world releases from tinnitus pressure and opens into a new space.

Defeat sequence:

1. Boss tinnitus sound disappears.
2. Use larger resolve/release than general tinnitus.
3. Violet LED clears.
4. Short silence.
5. Transition swell leads into forest.

Boss defeat transition chain:

- Start:
  `Assets/Audio/Curated/ForestEnding/TransitionRise/05_mixkit_long_swell_whoosh.wav`
- Then move into:
  `Assets/Audio/Curated/ForestEnding/TransitionRise/04_mixkit_air_swell_whoosh.wav`
- Optional accent:
  `Assets/Audio/Curated/ForestEnding/TransitionRise/06_mixkit_cinematic_rise_whoosh_a.wav`
- Cut/silence support:
  `Assets/Audio/Curated/Transition/Cut/03_mixkit_soft_swoosh_cut.wav`

Hold / be careful:

- `Assets/Audio/Curated/Transition/Cut/10_mixkit_wide_transition_cut.wav`
  may be useful for a heavier transition, but hold for now because it may feel
  too cinematic or heavy.

Light:

- Do not use a harsh flash.
- Clear the boss light, let darkness/silence breathe, then introduce forest
  feeling gradually.

### 13. Ending Forest

Intent: after tinnitus, the player arrives in an alien but peaceful natural
space. There is no final narration. The ending relies on the player's learned
habit: hearing the bell and naturally following it.

Sequence:

1. After boss defeat, there is silence or near-silence.
2. Forest sound slowly appears.
3. A familiar bell from the beginning sounds ahead.
4. Player follows it without being told.
5. End on the feeling that the player now instinctively notices and follows the
   bell.

Forest bed:

- `Assets/Audio/Curated/ForestEnding/ForestBed/01_mixkit_morning_birds_wide_long.wav`
- default alternate:
  `Assets/Audio/Curated/ForestEnding/ForestBed/02_mixkit_forest_birds_ambience_long.wav`

Ending bell:

- Reuse the opening/familiar bell language:
  `Assets/Audio/Curated/Bell/Selected/06_opening_orbit_bigsoundbank_2114.wav`
- If a final point is needed, test old ending punctuation only as optional:
  `Assets/Audio/Curated/ForestEnding/EndingPunctuation/01_gimi_soft_bell_air_final_long_tail.wav`

Current status of `01_gimi_soft_bell_air_final_long_tail`:

- Hold.
- The early part might work if cut, but the source quality is currently judged
  poor. Do not make it the default.

No final narration:

- Do not play "the game has ended" narration in the current ending.
- This supersedes older notes that placed narration after ending punctuation.

## Wall / Collision Sound Use

The current demo flow does not center on a long wall escape phase, but wall/noise
planes may still exist as boundaries or brief obstacles.

Wall contact:

- `Assets/Audio/Curated/Interaction/PadFeedback/06_mixkit_button_click_metal_tactile.wav`
- `Assets/Audio/Curated/Interaction/PadFeedback/08_mixkit_mechanical_tick_alt.wav`

These are pad-feedback-folder files but are currently assigned as weak wall
contact ticks. Keep them very quiet. If they sound like UI or button press in
context, replace them with `WallContactEscape` candidates.

## Held / Optional Motif Sounds

These are not rejected, but they do not have a solid gameplay slot yet.

- `Assets/Audio/Curated/Crystal/DreamTone/09_mixkit_sparkle_tail_layer.wav`
- `Assets/Audio/Curated/Crystal/DreamTone/07_mixkit_slow_magic_tone.wav`
- `Assets/Audio/Curated/Crystal/DreamTone/02_mixkit_distant_magic_bell_tone.wav`
- `Assets/Audio/Curated/Crystal/DreamTone/10_mixkit_airy_magic_swell.wav`
- `Assets/Audio/Curated/Crystal/DreamTone/13_mixkit_sci_fi_ambient_tone_alt.wav`

Notes:

- `12_mixkit_ethereal_magic_swell_bright.wav` is assigned to pad shake tests.
- `13_mixkit_sci_fi_ambient_tone_alt.wav` is interesting but currently has no
  clear use.

## Temporary Narration Plan

The final narration should not use rejected low-quality Windows TTS. However,
for demo assembly it is acceptable to generate temporary Windows narration and
replace only the audio clips later.

Implementation rule:

- Every narration line must be referenced by cue ID, not by direct clip path.
- Temporary files should live in:
  `Assets/Audio/Curated/Narration/WindowsTemp`
- Final files should later replace the clips behind the same cue IDs.

Temporary files generated on 2026-05-21 with `Microsoft Heami Desktop`:

- `Assets/Audio/Curated/Narration/WindowsTemp/01_NARR_FOLLOW_BELL_windows_temp.wav`
- `Assets/Audio/Curated/Narration/WindowsTemp/02_NARR_PAD_SHAKE_ASSIST_windows_temp.wav`
- `Assets/Audio/Curated/Narration/WindowsTemp/03_NARR_LOOK_BELL_windows_temp.wav`
- `Assets/Audio/Curated/Narration/WindowsTemp/04_NARR_FIND_TINNITUS_POSE_windows_temp.wav`
- `Assets/Audio/Curated/Narration/WindowsTemp/05_NARR_HOLD_POSE_windows_temp.wav`
- `Assets/Audio/Curated/Narration/WindowsTemp/06_NARR_BOSS_TRACK_windows_temp.wav`

These are placeholder implementation assets only. Do not use them as the final
voice quality target.

Recommended temporary lines:

| Cue ID | Text | Trigger | Required |
| --- | --- | --- | --- |
| `NARR_FOLLOW_BELL` | `종소리를 따라, 천천히 이동하세요.` | Bell first moves to follow target | Yes |
| `NARR_PAD_SHAKE_ASSIST` | `소리가 흐려지면, 패드를 가볍게 흔들어 보세요.` | Player is lost or bell is masked | Optional |
| `NARR_LOOK_BELL` | `빛과 소리가 만나는 곳을 바라보세요.` | First gaze tutorial only | Optional |
| `NARR_FIND_TINNITUS_POSE` | `패드를 천천히 움직여, 떨림이 강해지는 곳을 찾으세요.` | First normal tinnitus | Optional |
| `NARR_HOLD_POSE` | `그 자리에 머물러 주세요.` | First valid cleanse pose | Optional |
| `NARR_BOSS_TRACK` | `움직이는 소리를 놓치지 마세요.` | Boss first moving phase | Optional |

Do not add final ending narration in the current plan.

Narration mix:

- Keep mostly dry and intelligible.
- Place center or slightly in front of the listener, not beside the ear.
- Duck rain, wall noise, and tinnitus slightly while narration plays.
- Do not add a notification cue by default.

## Minimum Demo Route

If time is short, implement this route first:

1. Opening wake.
2. Bell orbit / sound-light binding.
3. Bell follow.
4. Rain/wind cocktail masking.
5. Bell gaze tutorial: 3 gaze successes, 2 moves.
6. Bell acquisition.
7. General tinnitus 1.
8. General tinnitus 2.
9. Boss approach.
10. Boss cleanse pattern 1, 2, 3.
11. Boss defeat.
12. Silence to forest ending, no final narration.

## Development Priorities

1. Build the cue table and dropdown defaults from this document.
2. Make old and new bell-overlap clips selectable; default to the newest assigned
   clip.
3. Reject all old `Tinnitus/Resolve` candidates for the current cue table.
4. Implement temporary narration as replaceable cue IDs.
5. Validate the mix with eyes closed: bell readability, rain masking, tinnitus
   discomfort, and boss tracking must all be tested without looking at the
   monitor.
