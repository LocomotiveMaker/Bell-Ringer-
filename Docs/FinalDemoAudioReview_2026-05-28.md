# FinalDemo Audio Review 2026-05-28

This document records the current FinalDemo audio wiring status, the bell-move
audio follow-up, and the 3D model import recommendation for the pad and bell.

Primary references checked:

- `Assets/Scenes/FinalDemo.unity`
- `Assets/ScriptableObjects/FinalDemo/FinalDemoCueLibrary.asset`
- `Assets/Scripts/FinalDemo/FinalDemoDirector.cs`
- `Assets/Scripts/Audio/TinnitusAudioController.cs`
- `Packages/manifest.json`

## Summary

The current FinalDemo scene is using the correct `FinalDemoCueLibrary` and
`FinalDemoTuningProfile` assets.

However, the user's final sound selections are only partially reflected.

The current state is:

- some selected sounds are correctly connected
- some are connected but with the wrong default clip
- some exist in the project but are not used anywhere in `FinalDemo`
- the dedicated bell movement cue exists in the cue library, but it is not
  currently played by the stage logic

## Correctly Reflected Now

These are currently wired in `FinalDemoCueLibrary.asset` and are used by
`FinalDemoDirector.cs`.

### Rain

- `04_mixkit_light_rain_loop_long`
  - mapped as `Rain light bed`
- `02_mixkit_heavy_rain_drops`
  - mapped as `Rain close drops`
- `07_gimi_long_rain_bed_alt`
  - mapped as `Rain strong bed`

Current timing:

- rain loops start on rain-zone entry
- close drops are emitted during the rain stage

### Tinnitus

- `04_mixkit_horror_radio_signal_long`
  - currently the default `Tinnitus long glitch`
- `01_bigsoundbank_radio_frequency_sweep_healing`
  - currently the default `Tinnitus healing loop`
- `05_mixkit_fast_sci_fi_sweep`
  - currently the default `Tinnitus resolve`
- `02_mixkit_small_electric_glitch`
  - currently the default `Tinnitus burst`

### Boss

- `02_mixkit_heartbeat_boss_heavy_a`
  - currently the default `Boss base pulse`
- `12_mixkit_boss_electric_burst_b`
  - currently the default `Boss glitch burst`
- `13_mixkit_hard_surface_return`
  - currently the default `Boss hit`
- `05_mixkit_long_swell_whoosh`
  - currently the default `Boss defeat rise`
- `04_mixkit_air_swell_whoosh`
  - currently the default `Boss defeat air`
- `03_mixkit_soft_swoosh_cut`
  - currently the default `Transition soft cut`

### Bell / Interaction

- `02_mixkit_soft_bell_chime_clean_hit`
  - currently the default `Bell pad shake response`
- `11_mixkit_bright_magic_collision_long`
  - currently the default `Bell acquisition`

## Not Reflected Correctly Yet

These items need follow-up.

### Bell movement sound

The user's current decision is:

- replace bell movement sound with `mixkit_license_2675_distant_magic_bell_tone`

Current imported project asset for that candidate:

- `Assets/Audio/Curated/Crystal/DreamTone/02_mixkit_distant_magic_bell_tone.wav`

Current status:

- the dedicated cue `Bell movement texture` exists
- it currently points to:
  - default: `07_bell_movement_transition_gimi_glitch_bells`
  - alternate: `06_mixkit_magic_crystal_hit_c`
- but the cue is never actually played by `FinalDemoDirector`

Required follow-up:

- change the default `Bell movement texture` clip to
  `02_mixkit_distant_magic_bell_tone.wav`
- keep older movement candidates as alternates in the dropdown
- wire the cue into real movement playback logic

### Current bell movement logic status

The bell visual itself does move in some stages:

- orbit stage: bell position is updated continuously
- gaze stage move: bell position interpolates from start to target
- follow stage: bell target position is fixed per step, not continuously moved

But the movement sound is not currently following that path as a dedicated
spatialized moving source.

What currently happens:

- opening close bell uses `BellDistantCall`
- bell orbit also uses repeated `BellDistantCall` one-shots at the bell's current
  position
- bell gaze uses repeated `BellDistantCall`
- bell movement cue is not used

So the answer is:

- no, the current FinalDemo does not yet implement the intended logic
- the bell movement sound is not presently emitted from the moving bell object as
  a dedicated movement cue along the movement path

Why left/right separation may feel weak right now:

- the main bell cue in orbit/gaze is still a repeated call sound, not a dedicated
  movement texture
- the sound is emitted as discrete one-shots, not a continuously following moving
  source
- the follow stage teleports the bell target between fixed positions instead of
  sonifying the travel itself
- HRTF preview may be off, depending on runtime toggle and plugin state

Required implementation behavior:

- when the bell is moving, the movement cue should come from the bell object's
  current world position
- that source should move as the bell moves
- the cue should be spatialized
- the movement cue should be separate from the bell call cue
- call cues can remain ponctuation, but the travel cue must carry motion

Recommended movement sound use:

- orbit stage:
  - low-volume moving bell texture follows the orbit path
- bell gaze relocation:
  - moving bell texture plays during the interpolation from old to new gaze point
- optional follow-stage reposition:
  - only if the bell itself visibly/sensibly relocates between follow targets

### Tinnitus long glitch

User-selected pair:

- `03_mixkit_terror_radio_frequency_long`
- `04_mixkit_horror_radio_signal_long`

Current status:

- only `04_mixkit_horror_radio_signal_long` is connected
- `03_mixkit_terror_radio_frequency_long` is unused in FinalDemo

Required follow-up:

- keep `04` as current default or switch after listening
- add `03` as alternate/default candidate in the dropdown if not already exposed

### Tinnitus resolve replacement

User decision:

- old `Tinnitus/Resolve` folder clips should not be used
- replacement pair:
  - `03_mixkit_electric_whoosh_healing`
  - `05_mixkit_fast_sci_fi_sweep`

Current status:

- `05_mixkit_fast_sci_fi_sweep` is correctly used as `Tinnitus resolve`
- `03_mixkit_electric_whoosh_healing` is not used in FinalDemo

Required follow-up:

- add `03_mixkit_electric_whoosh_healing` as alternate/default-selectable option

### Tinnitus healing during cleanse

User decision:

- default: `01_bigsoundbank_radio_frequency_sweep_healing`
- `02_mixkit_glitch_rewind_healing` is uncertain

Current status:

- `01` is assigned as `Tinnitus healing loop`
- but the dedicated cue is not explicitly started/stopped by FinalDemo stage logic
- the procedural `TinnitusAudioController` uses long/burst glitch clips directly,
  while cleanse progression mainly changes procedural synthesis state

Required follow-up:

- decide whether `TinnitusHealingLoop` should be an actual routed loop during
  valid cleanse hold
- if yes, start it on entering valid tolerance and stop it on resolve or loss

### Strong glitch family

User-selected group:

- `05_mixkit_digital_signal_interference`
- `04_mixkit_glitch_static`
- `03_mixkit_electric_buzz_glitch`
- `02_mixkit_small_electric_glitch`

Current status:

- only `02_mixkit_small_electric_glitch` is the current default `Tinnitus burst`
- the other three are not connected in FinalDemo

Required follow-up:

- expose all four as alternates in the same cue dropdown
- keep one as current default and make fast switching possible

### Boss family

User-selected group:

- `02_mixkit_heartbeat_boss_heavy_a`
- `01_mixkit_heartbeat_boss_low_a`
- `12_mixkit_boss_electric_burst_b`
- `16_mixkit_boss_glitch_noise_a_long`
- `09_mixkit_low_wall_scan_hit_a`
- `13_mixkit_hard_surface_return`
- `14_mixkit_deep_reveal_hit_big_risk`

Current status:

- used now:
  - `02_mixkit_heartbeat_boss_heavy_a`
  - `12_mixkit_boss_electric_burst_b`
  - `13_mixkit_hard_surface_return`
- not used now:
  - `01_mixkit_heartbeat_boss_low_a`
  - `16_mixkit_boss_glitch_noise_a_long`
  - `14_mixkit_deep_reveal_hit_big_risk`
- partially wired:
  - `09_mixkit_low_wall_scan_hit_a` is assigned as `Boss weakpoint move`
  - but `BossWeakpointMove` is not actually played during boss path movement

Required follow-up:

- expose `01` as alternate for boss base pulse
- expose `16` as alternate or additional loop layer for boss instability
- expose `14` as alternate heavy hit
- actually trigger `BossWeakpointMove` while the weak point is moving

### Wall contact

User-selected wall sounds:

- `06_mixkit_button_click_metal_tactile`
- `08_mixkit_mechanical_tick_alt`

Current status:

- these exist in the project
- they are not wired in FinalDemo cue playback

Required follow-up:

- add a small wall-contact cue family or map these into an existing interaction
  cue set

### Boss to forest transition

User-selected group:

- `05_mixkit_long_swell_whoosh`
- `04_mixkit_air_swell_whoosh`
- `06_mixkit_cinematic_rise_whoosh_a`

Current status:

- `05` and `04` are used
- `06` is unused

Required follow-up:

- if needed, add `06` as an alternate accent in the defeat chain

### Forest ending bed

User-selected pair:

- `01_mixkit_morning_birds_wide_long`
- `02_mixkit_forest_birds_ambience_long`

Current status:

- current default `Forest bed` is `02_mixkit_forest_birds_ambience_long`
- `01_mixkit_morning_birds_wide_long` is unused

Important timing issue:

- `ForestBed` is currently started during `OpeningAmbience`, not only in the
  forest ending

Required follow-up:

- remove `ForestBed` from the opening stage
- reserve it for the forest ending
- expose `01` and `02` as ending-bed alternates

### Held or unused optional sounds

These are currently not used in FinalDemo:

- `12_mixkit_ethereal_magic_swell_bright`
- `08_mixkit_wide_nature_floor_texture_a`
- `09_mixkit_wide_nature_floor_texture_b`
- `01_mixkit_wide_breeze_long`
- `05_mixkit_open_ground_texture_b_very_low`
- `03_mixkit_broad_environment_bed_a`
- `07_mixkit_open_ground_texture_a`
- `10_mixkit_wide_transition_cut`

If they should remain quickly swappable, they need to be exposed in cue
alternates or in a dedicated ambience/optional-cue group.

### Tinnitus pose lock / pose lost mismatch

User-selected cues:

- exact match: `11_mixkit_narration_soft_ui_risk`
- matched then lost: `05_mixkit_narration_tone_d`

Current status:

- exact match is not using `11_mixkit_narration_soft_ui_risk`
- pose lost is not using `05_mixkit_narration_tone_d`

Current mapping:

- pose lock: `02_mixkit_gear_fast_lock_tap`
- pose lost: `05_mixkit_sci_fi_reject_unstable`

Required follow-up:

- replace these defaults or at least expose the user-selected pair as alternates

## Immediate Audio TODO

The following items should be treated as explicit follow-up work for FinalDemo:

1. Replace bell movement default with
   `Assets/Audio/Curated/Crystal/DreamTone/02_mixkit_distant_magic_bell_tone.wav`.
2. Actually play `BellMovementTexture` from the moving bell object's current
   world position during bell relocation.
3. Stop using `ForestBed` during opening.
4. Wire `BossWeakpointMove` into the moving part of boss patterns.
5. Expose unused selected alternates in cue dropdowns:
   - `03_mixkit_terror_radio_frequency_long`
   - `03_mixkit_electric_whoosh_healing`
   - `04_mixkit_glitch_static`
   - `05_mixkit_digital_signal_interference`
   - `03_mixkit_electric_buzz_glitch`
   - `01_mixkit_heartbeat_boss_low_a`
   - `16_mixkit_boss_glitch_noise_a_long`
   - `14_mixkit_deep_reveal_hit_big_risk`
   - `06_mixkit_cinematic_rise_whoosh_a`
   - `01_mixkit_morning_birds_wide_long`
6. Decide whether `TinnitusHealingLoop` should be a real loop during cleanse
   instead of remaining an unused cue assignment.
7. Add wall contact cue usage if walls are part of the general tinnitus section.

## 3D Model Import Recommendation

### Current Unity import reality in this project

`Packages/manifest.json` does not include a glTF/GLB/USD importer package.

That means:

- `FBX`: safe
- `OBJ`: safe
- `glTF` / `GLB`: not currently guaranteed to import correctly
- `USDZ`: not currently appropriate for this pipeline

### Recommended download choice

For the pad:

- download `FBX` with `2k` textures

For the bell:

- download `OBJ` with `2k` textures

Reason:

- both are natively safe for the current Unity setup
- only two hero props are involved, so `2k` is acceptable
- post-processing is camera-level and will still affect the rendered models
- the main risk is not post-processing; it is import compatibility and material
  hookup

If performance becomes an issue later, the first fallback should be `1k`
textures, not switching formats.

### Recommended project folders

Recommended import targets:

- `Assets/Art/FinalDemo/Models/Pad/Source`
- `Assets/Art/FinalDemo/Models/Bell/Source`
- textures can remain beside the model files or under:
  - `Assets/Art/FinalDemo/Models/Pad/Textures`
  - `Assets/Art/FinalDemo/Models/Bell/Textures`

Recommended prefab targets after import:

- `Assets/Art/FinalDemo/Prefabs/Pad`
- `Assets/Art/FinalDemo/Prefabs/Bell`

### Important note about "putting the files in a folder"

There is no current folder-drop auto-binding system for these models.

Putting the files into the project will import them, but it will not
automatically replace the placeholders in `FinalDemo`.

Current scene linkage uses:

- `FinalDemoSceneReferences.padVisual`
- `FinalDemoSceneReferences.bellVisual`
- fallback bell placeholder:
  `FinalDemo_BellPlaceholder`

So the actual application step is:

1. import the model files into the recommended folders
2. make prefabs if needed
3. assign the imported bell model to `bellVisual`
4. assign the imported pad model to `padVisual`
5. or replace the current placeholder objects in `FinalDemo.unity`

### Fastest safe path

Fastest safe path for the current project:

1. Pad: import `FBX 2k`
2. Bell: import `OBJ 2k`
3. place them under the folders listed above
4. replace the scene visuals manually or by a small editor patch

If "drop files and auto-apply" is required later, that needs a dedicated editor
tool or a naming-convention importer. That tool does not exist yet in the
current project.
