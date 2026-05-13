# Audio Cue Usage

This document records chosen sound assets and intended gameplay timing so other agents can preserve the design intent.

## Project Sound Direction

The game depends on high-quality sound because the player often has closed eyes.

Primary tone:

- quiet
- mysterious
- dreamlike
- spacious
- sensory-first rather than UI-like

Avoid sounds that feel like generic UI alerts, school bells, obvious church ambience, or busy realistic environments unless explicitly marked as temporary.

## Bell Cues

Selected on 2026-05-13 after listening.

Semantic copies for implementation:

- `Assets/Audio/Curated/Bell/Selected`

| Asset | Source Folder | Intended Timing | Design Role |
| --- | --- | --- | --- |
| `bigsoundbank_cc0_2116` | `Assets/Audio/Processed/Bell` | When the player finds the bell | Closing punctuation / arrival confirmation |
| `bigsoundbank_cc0_2117` | `Assets/Audio/Processed/Bell` | When the player finds the bell | Closing punctuation / arrival confirmation |
| `bigsoundbank_cc0_0292` | `Assets/Audio/Processed/Bell` | Bell calling from far away | Distant guidance call |
| `bigsoundbank_cc0_0293` | `Assets/Audio/Processed/Bell` | Bell calling from far away | Distant guidance call |
| `bigsoundbank_cc0_0294` | `Assets/Audio/Processed/Bell` | Stronger call after the player fails to find the bell repeatedly, starting around the fourth assist | Strong reorientation cue |
| `bigsoundbank_cc0_2114` | `Assets/Audio/Processed/Bell` | Initial orbiting bell around the player | Opening spatial orientation cue |
| `gimi_license_ambient_soundscape_glitch_bells` | `Assets/Audio/Processed/Bell` | Briefly while the bell moves | Transitional movement texture |
| `gimi_license_ambient_soundscape_glitch_bells` | `Assets/Audio/Processed/Bell` | When the player finally obtains the bell | Bell acquisition texture / transformation layer |

## Rain Cue Target

Rain should not feel like a ceiling or sky source.

Use two families:

- Wide rain bed: broad floor-impact rain over a very open, flat, empty plane.
- Close droplets: local one-shots for drops hitting the floor, bell, pad, shoulder, or near-body surfaces.

Rain should support spatial emptiness. Avoid dense forest canopy, roof, city, window, cafe, thunderstorm, or cozy indoor rain unless explicitly needed for contrast.

Close droplets may trigger tactile interpretation: controller vibration, shoulder contact, or bell-surface contact.

## Rain Candidates

Collected on 2026-05-13. These are not final selections yet.

Analysis bundle:

- `tools/audio-lab/output/batch/RainCandidates`

Unity-ready converted files:

- `Assets/Audio/Processed/Rain`

First-listen shortlist:

- `Assets/Audio/Curated/Rain/Bed`
- `Assets/Audio/Curated/Rain/CloseDrops`

### Wide Rain Bed Candidates

Listen for a broad, flat, open-plane floor impact. The bed should feel spatially wide but not like a roof, window, forest canopy, street, or thunderstorm.

| Listen Order | Asset | Intended Use |
| --- | --- | --- |
| 1 | `01_bigsoundbank_rain_on_concrete_floor.wav` | Primary floor-impact rain bed candidate |
| 2 | `02_bigsoundbank_rain_puddle_floor.wav` | Wet floor / puddle-heavy alternate bed |
| 3 | `03_mixkit_rain_long_loop.wav` | Smooth loop candidate for broad rain layer |
| 4 | `04_mixkit_light_rain_loop_long.wav` | Softer light-rain bed |
| 5 | `05_mixkit_light_rain_loop_wide.wav` | Shorter wide-loop candidate |
| 6 | `06_mixkit_light_rain_atmosphere.wav` | Gentle atmosphere alternate |
| 7 | `07_gimi_long_rain_bed_alt.wav` | Long rain-bed alternate; verify Gimi license before final use |

### Close Droplet Candidates

Listen for local, tactile impacts that can be placed near the player or attached to the bell/pad.

| Listen Order | Asset | Intended Use |
| --- | --- | --- |
| 1 | `01_gimi_drop_water_close.wav` | Close single/drop cluster; verify Gimi license before final use |
| 2 | `02_mixkit_heavy_rain_drops.wav` | Stronger near-floor droplets |
| 3 | `03_mixkit_rain_splashing_floor.wav` | Floor splashing / nearby ground hits |
| 4 | `04_mixkit_rain_on_umbrella_body_surface.wav` | Body/shoulder/pad surface feeling |
| 5 | `05_mixkit_heavy_rain_metal_surface_pad_bell.wav` | Bell or controller surface hits; use sparingly |
| 6 | `06_bigsoundbank_metal_water_surface.wav` | Metallic/wet surface alternate |
| 7 | `07_bigsoundbank_hard_surface_drops_alt.wav` | Hard-surface drop alternate |

Reject candidates with obvious thunder, city/traffic, window glass, roof shelter, thick forest canopy, or too much cozy indoor texture.

## Tinnitus / Glitch Cue Target

Collected on 2026-05-14. These are candidates, not final selections.

Analysis bundle:

- `tools/audio-lab/output/batch/TinnitusCandidates`

Unity-ready converted files:

- `Assets/Audio/Processed/Tinnitus`

First-listen shortlist:

- `Assets/Audio/Curated/Tinnitus/LongGlitch`
- `Assets/Audio/Curated/Tinnitus/BurstGlitch`
- `Assets/Audio/Curated/Tinnitus/HealingMotion`
- `Assets/Audio/Curated/Tinnitus/Resolve`
- `Assets/Audio/Curated/Tinnitus/Approach`
- `Assets/Audio/Curated/Tinnitus/Lock`

### Design Role

Tinnitus should feel like frequency plus glitch rather than a normal creature voice.

Unity may generate the persistent low/high frequency tones directly. These audio assets should provide texture, attack, confirmation, disorder, and resolution layers around those generated tones.

Use these families:

- `LongGlitch`: quiet long-term glitch/noise layer that can sit under the generated tinnitus tone.
- `BurstGlitch`: short, stronger irregular glitch one-shots that appear intermittently.
- `HealingMotion`: active sounds for cleansing while the pad vibrates and the tinnitus resists or stabilizes.
- `Resolve`: final punctuation when the tinnitus is fully healed and disappears.
- `Approach`: feedback as pad position/rotation gets closer to the correct target pose.
- `Lock`: click/chime/tack sound when pad position and rotation enter the valid cleanse pose.

### Long Glitch Candidates

Listen at low volume. Reject any file that is too painful, too busy, too musical, or too obviously radio/TV as a real-world object.

| Listen Order | Asset | Intended Use |
| --- | --- | --- |
| 1 | `01_bigsoundbank_transformer_low_long.wav` | Low electrical bed under generated tinnitus tone |
| 2 | `02_bigsoundbank_neon_transformer_buzz.wav` | Transformer-like continuous tension |
| 3 | `03_mixkit_terror_radio_frequency_long.wav` | Long corrupted frequency candidate |
| 4 | `04_mixkit_horror_radio_signal_long.wav` | Alternate long corrupted signal |
| 5 | `05_bigsoundbank_feedback_590hz_soft.wav` | Soft feedback layer; use carefully with generated tone |

### Burst Glitch Candidates

These should be one-shot irritants, not loops. They can fire when tinnitus intensity spikes.

| Listen Order | Asset | Intended Use |
| --- | --- | --- |
| 1 | `01_bigsoundbank_zapping_short_white_noise.wav` | Very short harsh white-noise tear |
| 2 | `02_mixkit_small_electric_glitch.wav` | Small electric glitch |
| 3 | `03_mixkit_electric_buzz_glitch.wav` | Stronger buzz-glitch hit |
| 4 | `04_mixkit_glitch_static.wav` | Static burst |
| 5 | `05_mixkit_digital_signal_interference.wav` | Digital interference burst |
| 6 | `06_mixkit_futuristic_glitch_robot.wav` | Characterful glitch; use only if not too robotic |

### Healing Motion Candidates

These support cleansing while the pad is vibrating. They may be layered under dynamic volume/pitch automation.

| Listen Order | Asset | Intended Use |
| --- | --- | --- |
| 1 | `01_bigsoundbank_radio_frequency_sweep_healing.wav` | Frequency sweep for tuning/cleansing |
| 2 | `02_mixkit_glitch_rewind_healing.wav` | Tinnitus being pulled backward or corrected |
| 3 | `03_mixkit_electric_whoosh_healing.wav` | Energetic cleanse motion |
| 4 | `04_mixkit_glitchy_synth_intro_rise.wav` | Rising or stabilizing cleanse layer |
| 5 | `05_mixkit_fast_sci_fi_sweep.wav` | Short fast cleanse transition |

### Resolve Candidates

These should feel like a final point, not a reward jingle.

| Listen Order | Asset | Intended Use |
| --- | --- | --- |
| 1 | `01_mixkit_sci_fi_confirmation_clean.wav` | Clean final confirmation |
| 2 | `02_mixkit_positive_interface_beep.wav` | Short simple punctuation |
| 3 | `03_mixkit_page_forward_chime_echo.wav` | Slightly echoing disappearance |
| 4 | `04_mixkit_page_back_chime_echo.wav` | Alternate echoing disappearance |

### Approach Candidates

These can be driven by lock amount. As the pad gets closer to the correct position/rotation, increase volume, repetition rate, filter openness, or add layers.

| Listen Order | Asset | Intended Use |
| --- | --- | --- |
| 1 | `01_mixkit_interface_hint_near.wav` | Near-correct hint |
| 2 | `02_bigsoundbank_pure_beep_distance_test.wav` | Pure tone test for distance/pose feedback |
| 3 | `03_mixkit_interface_option_select.wav` | Subtle approach feedback |
| 4 | `04_mixkit_game_ui_tone.wav` | Stronger approach feedback |
| 5 | `05_mixkit_sci_fi_reject_unstable.wav` | Wrong/unstable pose warning, not primary success cue |

### Lock Candidates

These are for the exact moment when pad position and rotation enter the valid cleanse pose.

| Listen Order | Asset | Intended Use |
| --- | --- | --- |
| 1 | `01_mixkit_gear_metallic_lock.wav` | Physical locked-in feel |
| 2 | `02_mixkit_gear_fast_lock_tap.wav` | Faster physical tap |
| 3 | `03_mixkit_sci_fi_click.wav` | Clean non-metallic lock |
| 4 | `04_mixkit_electronic_lock_success_beeps.wav` | Strong lock confirmation |
| 5 | `05_mixkit_computer_digital_lock.wav` | Digital lock alternate |
| 6 | `06_mixkit_cool_interface_click_tone.wav` | Softer click-tone alternate |

Reject candidates that feel like normal UI, phone notifications, arcade pickups, obvious robots, or musical reward jingles.

## Light Wind / Forest Ending Cue Target

Collected on 2026-05-14. These are candidates, not final selections.

Analysis bundles:

- `tools/audio-lab/output/batch/WindCandidates`
- `tools/audio-lab/output/batch/BirdCandidates`
- `tools/audio-lab/output/batch/ForestEndingCandidates`

Unity-ready converted files:

- `Assets/Audio/Processed/Wind`
- `Assets/Audio/Processed/Birds`
- `Assets/Audio/Processed/ForestEnding`

First-listen shortlist:

- `Assets/Audio/Curated/Wind/Light`
- `Assets/Audio/Curated/ForestEnding/ForestBed`
- `Assets/Audio/Curated/Birds/AmbientBirds`
- `Assets/Audio/Curated/ForestEnding/TransitionRise`
- `Assets/Audio/Curated/ForestEnding/EndingPunctuation`

### Ending Sequence Intent

After the boss tinnitus is cleansed, play a rapid rising/approaching transition that feels wide and grand, as if sound is gathering from the open world toward the player.

Recommended structure:

- Boss tinnitus cleanse completes.
- `TransitionRise` swells quickly from wide space toward the player.
- Cut to a short silence or near-silence.
- The sound world suddenly changes into a cool, wide forest.
- `ForestBed`, subtle `AmbientBirds`, and very low `LightWind` play for roughly 10 seconds.
- A bell sounds in front of the player.
- The player follows the bell.
- At the final bell point, play `EndingPunctuation`.
- Narration follows immediately after the punctuation sound to say the game has ended.

The forest should not feel dense, cute, or busy. It should feel spacious, cool, peaceful, and slightly unreal after the tinnitus section. Birds can be loud in the source file if they are placed and mixed quietly in Unity.

Strong wind does not need a separate primary collection yet. The current plan is to start from rain-only, then gradually raise a rain-wind layer already found in the rain candidates to strengthen the cocktail-effect section.

### Light Wind Candidates

Use as a low-volume environmental layer in the ending forest, or as a soft movement layer before stronger rain-wind. Reject anything that sounds like a storm, interior draft, desert scene, or cinematic trailer whoosh.

| Listen Order | Asset | Intended Use |
| --- | --- | --- |
| 1 | `01_mixkit_breeze_through_trees_long_soft.wav` | Primary light wind bed candidate; long and soft |
| 2 | `02_mixkit_wind_blowing_open_air_long.wav` | Open-air alternate bed |
| 3 | `03_mixkit_soft_breeze_trees_short_layer.wav` | Shorter rustle/breeze layer |
| 4 | `04_mixkit_soft_cold_wind_short_gust.wav` | Short cool gust accent |
| 5 | `05_mixkit_wide_windy_air_short_gust.wav` | Short wide-air gust accent |

### Forest Bed Candidates

Listen for spaciousness first. The bed should support a sudden world-change after silence. Avoid anything that feels like a crowded jungle, nearby river focus, cozy park recording, or obvious location documentary unless it works as a subtle layer.

| Listen Order | Asset | Intended Use |
| --- | --- | --- |
| 1 | `01_mixkit_morning_birds_wide_long.wav` | Primary wide forest/morning bed candidate |
| 2 | `02_mixkit_forest_birds_ambience_long.wav` | Alternate long forest bed |
| 3 | `03_mixkit_birds_near_water_wide_alt.wav` | Wide alternate; reject if water becomes too specific |
| 4 | `04_mixkit_morning_garden_birds_alt.wav` | Softer morning alternate |
| 5 | `05_mixkit_forest_night_air_alt.wav` | Cooler/darker alternate |
| 6 | `06_mixkit_jungle_birds_wide_alt.wav` | More exotic alternate; likely too dense unless mixed very low |

### Ambient Bird Candidates

Use these as spatialized low-volume accents around the player, not as one loud front-facing source. Randomize position, interval, and volume so the forest feels populated without becoming busy.

| Listen Order | Asset | Intended Use |
| --- | --- | --- |
| 1 | `01_mixkit_distant_bird_call_long.wav` | Distant bird layer or rare long call |
| 2 | `02_mixkit_bird_singing_phrase_long.wav` | Longer phrase layer |
| 3 | `03_mixkit_small_birds_cluster.wav` | Small cluster near/side placement |
| 4 | `04_mixkit_bird_chirp_short_a.wav` | Short local chirp |
| 5 | `05_mixkit_morning_bird_phrase_alt.wav` | Alternate phrase |
| 6 | `06_mixkit_bird_singing_short_b.wav` | Short song accent |
| 7 | `07_mixkit_bird_tweet_short.wav` | Very short accent |
| 8 | `08_mixkit_bird_chirp_short_b.wav` | Very short accent |

### Boss Cleanse To Forest Transition Candidates

These are for the sound immediately after the boss tinnitus is defeated. The best version should feel like release, expansion, and the world rushing in, not a normal UI transition.

Layering idea:

- Use one longer riser as the main body.
- Add one deeper or brighter riser only for the last second.
- Optionally add `08_mixkit_impact_rise_short_tail.wav` at the cut to silence if it does not feel too trailer-like.

| Listen Order | Asset | Intended Use |
| --- | --- | --- |
| 1 | `01_mixkit_wide_transition_whoosh_long.wav` | Primary long transition candidate |
| 2 | `02_mixkit_bright_rising_whoosh.wav` | Bright rising release |
| 3 | `03_mixkit_deep_riser_whoosh.wav` | Deeper body layer |
| 4 | `04_mixkit_air_swell_whoosh.wav` | Airy swell layer |
| 5 | `05_mixkit_long_swell_whoosh.wav` | Shorter swell alternate |
| 6 | `06_mixkit_cinematic_rise_whoosh_a.wav` | Short cinematic alternate |
| 7 | `07_mixkit_cinematic_rise_whoosh_b.wav` | Very short bright alternate |
| 8 | `08_mixkit_impact_rise_short_tail.wav` | Tiny cut/impact accent only if needed |

### Final Ending Punctuation Candidates

This sound plays after the player follows the final forward bell and reaches it, immediately before the final narration. It should feel like a quiet full stop for the whole work, not like a game-clear jingle.

Best direction to test first:

- A small bell/chime attack.
- A soft airy or crystal tail.
- No cheerful melody.
- Enough decay to let the narration enter naturally afterward.

| Listen Order | Asset | Intended Use |
| --- | --- | --- |
| 1 | `01_gimi_soft_bell_air_final_long_tail.wav` | Primary candidate for soft final air/tail; verify Gimi license before final use |
| 2 | `02_mixkit_bell_of_promise_final_tail.wav` | Bell-like final point with longer tail |
| 3 | `03_bigsoundbank_2117_final_bell.wav` | Stronger bell punctuation; also used as bell-find candidate |
| 4 | `04_bigsoundbank_2116_final_bell.wav` | Alternate stronger bell punctuation; also used as bell-find candidate |
| 5 | `05_mixkit_relaxing_bell_chime_final.wav` | Short calm chime |
| 6 | `06_mixkit_crystal_chime_final.wav` | More crystalline alternate |
| 7 | `07_mixkit_soft_clean_confirmation_final.wav` | Clean confirmation alternate; reject if too UI-like |
| 8 | `08_mixkit_echo_chime_final.wav` | Echo chime alternate; reject if too UI-like |
