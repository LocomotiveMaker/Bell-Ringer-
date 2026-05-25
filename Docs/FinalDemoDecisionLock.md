# FinalDemo Decision Lock

Last updated: 2026-05-21

This document preserves the user's answers to the implementation questionnaire
and adds only implementation-level supplements. If this document conflicts with
older planning notes, this document is the current decision source.

Related audio-flow document:

- `Docs/FinalDemoFlowAudioIntegration.md`

## Highest-Level Decision

The priority is to implement the whole playable flow first, then rapidly improve
weak sections. The target is not a perfect isolated prototype of one mechanic.
The target is a complete 5-7 minute demo whose sensory flow can be evaluated end
to end.

Core route:

1. Opening.
2. Bell orbit / sensory orientation.
3. Bell follow with at least two targets.
4. Rain/wind masking section.
5. Bell gaze tutorial: 3 gaze successes, 2 moves.
6. Bell acquisition.
7. General tinnitus 1.
8. General tinnitus 2.
9. Boss tinnitus.
10. Forest ending.

If time runs short, reduce scope in this order:

1. Reduce boss pattern count or shorten boss tuning.
2. Simplify rain/wind by removing either rain or wind complexity.
3. Remove one general tinnitus.
4. Simplify forest ending follow.
5. Opening can be shortened, but should not be removed unless absolutely needed.

## Must-Have Tuning Architecture

Create one central tuning asset/component for all high-risk timing and difficulty
values. Suggested name:

- `FinalDemoTuningProfile`

It should include at least:

- opening duration
- opening silence duration
- bell orbit duration
- bell follow target count and target positions
- bell arrival radius, default `0.8m`
- bell assist timeout
- rain start zone and rain intensity curve
- rain auto-assist volume floor
- bell auto-assist gain curve
- gaze hold duration, default about `2.2s`
- gaze cone/base tolerance
- gaze assist widening speed and max width
- bell acquisition haptic strength/duration
- general tinnitus count, default `2`
- general tinnitus cleanse duration, default `4s`
- general tinnitus pose tolerances
- boss pattern count, default `3`
- boss pattern durations, default `8.0 / 8.5 / 9.0s`
- boss initial fixed duration, default `2s`
- boss pose tolerances
- boss assist widening after repeated failures
- ending auto-finish timer, default `10s`
- HRTF/spatializer mode toggle
- global skip/force-progress controls

The user expects to tune these values directly after implementation.

## Demo Scope

1. Target play time: 5-7 minutes. Implement nominal pacing around 6 minutes.
2. Route is locked as bell, rain/wind, gaze, two general tinnitus targets, boss,
   forest ending. Put simple wall/noise boundaries around the tinnitus area.
3. Cuts are allowed in the order listed above. Keep the whole route playable.
4. Add operator override controls. The player can struggle, but the operator must
   be able to skip or force-complete a stage when the demo would stall.

Recommended operator controls:

- start demo
- force next stage
- force complete current objective
- recenter head
- recenter pad
- reset current stage
- toggle assist level
- toggle HRTF/spatializer mode

## Preflight / Calibration

5. Preflight is the screen/state before the art experience starts. It must serve
   both operator debugging and audience-facing viewing.
6. Start should prefer all checks passing: `Recenter Head`, `Pad Center`,
   `ArUco valid`, `LED connected`, `vibration connected`.
7. Calibration failure should not hard-block the demo. Show warnings and allow
   operator override.
8. Starting the game by keyboard/mouse operator input is enough.

Implementation supplement:

- Use a clean observer layout with a debug overlay or side panel.
- Do not expose this UI to the player as a meaningful game interface.
- Use status colors and short labels. Avoid making the operator guess why start
  is disabled or degraded.

## Opening

9. Keep the structure: opening ambience, silence, close left bell.
10. Durations must be tunable. Default may start at `10s + 5s`, with shorter
    values such as `5s + 2s` available for exhibition pacing.
11. The first close bell should be near the left ear and may surprise the player
    slightly, but must not be unpleasant or jump-scare-like.
12. Player movement is locked during opening.

Implementation supplement:

- Treat opening as a controlled sensory onset.
- Do not allow walking or joystick drift to move the player before the bell world
  is established.

## Bell Orientation

13. Bell orbit is automatic, without player input.
14. The orbit path can be fixed: left, front, right/back, above.
15. No success/failure condition is needed. Advance after a timed sequence.
16. Bell LED rule: keep the green point and sound-linked waveform, but improve
    scale by distance. Near bell can create a stronger/larger local pulse; far
    bell should create a smaller/weaker pulse. The waveform should appear only
    when the bell sounds.

Implementation supplement:

- "Larger" should not mean a full-screen wash. Keep it localized and readable
  under closed eyes.
- Use distance/intensity to modulate brightness, pulse radius, and decay.

## Bell Follow

17. Player movement uses the left joystick.
18. Use at least two bell targets.
19. Arrival radius starts at `0.8m`, but must be tunable.
20. Add a timer-based assist if the player cannot find the bell.
21. Pad shake assist should be triggered by the player shaking the pad, not only
    automatically.
22. Pad button sound is not strictly banned, but it is not used in the demo.

Implementation supplement:

- Bell follow should support both automatic assist and player-triggered pad shake.
- If the player is lost, increase bell readability before skipping:
  1. increase bell gain
  2. reduce rain/wind masking if active
  3. play minimal narration if needed
  4. let operator force progression

## Rain / Wind

23. Rain/wind starts on entering a specific zone.
24. The objective remains following the bell.
25. Rain/wind is a difficulty and cocktail-effect layer, not a hard failure
    mechanic.
26. If it becomes too hard, use assist: lower rain volume, raise bell volume, and
    optionally trigger narration.
27. Rain LED stays floor/bottom based. Do not cover the whole display.
28. Wall/noise planes can be added simply around the tinnitus section rather than
    as a full standalone wall phase.

Implementation supplement:

- Rain should be readable as floor impact.
- Bell green must remain readable through rain/wind.
- Rain assist should be automatic and tunable, not a one-off hardcoded volume cut.

## Bell Gaze

29. Bell gaze starts after rain/wind has stopped or dropped sharply.
30. Lock structure: 3 gaze successes and 2 bell moves.
31. Each gaze starts around `2.2s`; make it tunable.
32. Looking away pauses progress, but does not reset it.
33. Disable gaze scoring while the bell is moving. Re-enable after movement ends.
34. If gaze fails for too long, widen the valid gaze range gradually rather than
    moving the bell more frontally.

Implementation supplement:

- Gaze assist should be invisible or subtle.
- Do not punish small head jitter.
- The goal is learning the sound/light relation, not precise FPS aiming.

## Bell Acquisition

35. Bell acquisition should feel like the player gained a real ability, not only
    a symbolic cutscene.
36. Haptic pulse should be synchronized to the acquisition sound and tunable.
37. Add a short silence or transition after acquisition before general tinnitus.

Implementation supplement:

- After acquisition, enable the pad/bell interaction language: pad shake,
  cleanse feedback, and sound-linked vibration.
- This does not need a separate tutorial if the first tinnitus encounter teaches
  it clearly.

## General Tinnitus

38. Start with exactly 2 general tinnitus targets, but keep count tunable.
39. Both can share the same base difficulty. Allow tolerances to be narrowed for
    later tuning.
40. Player should walk to find them. Place them generally forward and use side
    walls/noise planes to frame the route.
41. Tinnitus LED appears only when the player looks/head-turns toward it.
42. Tinnitus pad poses are fixed presets.
43. Cleanse duration starts at `4s`, tunable.
44. Exiting the valid pose pauses progress and preserves accumulated progress.
45. Use the documented replacement resolve: do not use `Tinnitus/Resolve`; default
    to `HealingMotion/05_mixkit_fast_sci_fi_sweep.wav`.

Implementation supplement:

- The second tinnitus should foreshadow the boss more strongly after cleanse:
  heavier pulse, larger ambience, and clearer boss presence.
- Do not leave residual tinnitus after resolve, because that confuses completion.

## Boss Tinnitus

46. Boss appears after both general tinnitus targets.
47. Lock player movement on boss entry.
48. Use 3 patterns: `8.0s`, `8.5s`, `9.0s`, all tunable.
49. Each pattern uses `2s` fixed initial cleanse, then slow movement tracking.
50. Failure resets boss pattern progress to 0 and returns the weak point to the
    start. The route stays fixed so the player can learn it.
51. Between patterns, play a hit/transition without interrupting the flow. The
    next movement direction should be reflected in haptics. If the next weak
    point path moves right, emphasize right vibration; if left, emphasize left.
52. Add assist after repeated failures by widening allowed range.
53. Developer can create 3 preset paths. User will tune positions and movement
    values later.

Implementation supplement:

- Boss should be hard because of tracking pressure, not because of unclear rules.
- Keep movement slow and tolerance generous at first.
- Directional haptics should fall back to whole-pad vibration if left/right motor
  control is unavailable.

## Ending

54. Lock structure: big release, LED clear, short silence, forest.
55. In the forest ending, walking forward to the bell ends the demo. If the player
    does not move, auto-end after 10 seconds.
56. No final player-facing narration.
57. Ending condition: final bell arrival or 10-second auto-end, whichever happens
    first.

Implementation supplement:

- If the auto-end needs to be communicated, do it on the observer/debug screen or
  through a gentle fade, not through final narration.
- The player should feel that they now naturally follow the bell without being
  instructed.

## Narration

58. Temporary Windows TTS is acceptable. Final narration must be swappable by WAV
    replacement through cue IDs.
59. Mandatory narration can be only: `종소리를 따라, 천천히 이동하세요.`
60. Other explanatory lines may be included or removed at developer discretion.
61. Narration should come from front-center or head-front placement. Duck competing
    sounds while narration plays.

Implementation supplement:

- Do not bind narration clips directly in scene objects.
- Use narration cue IDs so Gemini or another final TTS can replace Windows files
  without code or scene changes.
- For the active 11-line curated narration set, the stage timing map, the
  no-interrupt queue rule, and the current pad pose correction notes, use
  `Docs/FinalDemoNarrationCueTimingAndPadCorrections.md`.

## Audio Implementation

62. Use cue-ID-based ScriptableObject tables. Avoid directly placing AudioClips in
    scene objects.
63. Keep old and new candidates in Inspector dropdowns. Defaults follow
    `FinalDemoFlowAudioIntegration.md`.
64. Because implementation speed is acceptable, create a minimal AudioMixer from
    the start. Keep it practical rather than overdesigned.
65. Test HRTF. Prefer a toggleable prototype with fallback to Unity default 3D
    audio. Decide after hands-on testing.

Recommended AudioMixer groups:

- Master
- Bell
- RainWind
- Tinnitus
- BossTinnitus
- Ambience
- Interaction
- Narration

HRTF supplement:

- HRTF itself is not necessarily paid or conceptually hard.
- The risk is integration, plugin compatibility, and mix verification under the
  actual headset/headphone setup.
- Steam Audio is a reasonable first test because its Unity integration exposes a
  spatializer plugin and has a built-in HRTF option.
- Keep HRTF behind a project setting or runtime toggle until proven stable.

Useful official references:

- Steam Audio Unity Getting Started:
  `https://valvesoftware.github.io/steam-audio/doc/unity/getting-started.html`
- Steam Audio Unity Source / HRTF:
  `https://valvesoftware.github.io/steam-audio/doc/unity/source.html`

## Light / Haptics Priority

66. Light priority is: pad, bell, tinnitus, wall, rain.
67. Bell green point must remain readable even during rain/wind.
68. Haptic priority is: cleanse lock/cleanse progress, boss failure/success, bell
    assist, environmental drops.
69. Use left/right motor separation if possible. Fall back to whole-pad vibration.

Implementation supplement:

- If high-priority light and rain overlap, rain should dim or yield.
- Pad and bell feedback should be the clearest player-action feedback.

## Observer Screen

70. Make the observer screen presentable if possible. Low-poly plus heavy
    post-processing is acceptable.
71. Showing player, bell, tinnitus, boss, and rain/wind state does not conflict
    with the game intent because the player cannot use the monitor.
72. Implement on the assumption that the player sees no UI.

Implementation supplement:

- Observer screen can be both attractive and useful:
  - clean world visualization
  - subtle debug overlay
  - current stage
  - tracking/connection status
  - skip/assist status

## Integration Method

73. A new `FinalDemo.unity` scene is acceptable.
74. Keep existing test scenes. Assemble verified components into `FinalDemo`.
75. Implementation priority is acceptable as:
    cue table, input/hardware state, Opening/Bell, Rain, Gaze, General Tinnitus,
    Boss, Ending.
76. Implement the full flow first, then aggressively improve weak sections.

Implementation supplement:

- Do not merge test scenes destructively.
- Treat test scenes as component validation scenes.
- `FinalDemo` should be the composed performance route.
