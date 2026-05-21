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

## Crystal / Dream / Open Ambience Cue Target

Collected on 2026-05-14. These are candidates, not final selections.

Analysis bundles:

- `tools/audio-lab/output/batch/CrystalCandidates`
- `tools/audio-lab/output/batch/AmbienceCandidates`

Unity-ready converted files:

- `Assets/Audio/Processed/Crystal`
- `Assets/Audio/Processed/Ambience`

First-listen shortlist:

- `Assets/Audio/Curated/Crystal/Collision`
- `Assets/Audio/Curated/Crystal/DreamTone`
- `Assets/Audio/Curated/Ambience/OpenGround`
- `Assets/Audio/Curated/Ambience/SubtleTexture`

### Design Role

These sounds are not assigned to fixed gameplay beats yet.

Crystal and dream sounds are motif candidates. They may later support bell acquisition, cleanse resolution, hidden transitions, light shimmer, or unusual material contact. Keep them secondary to the bell and tinnitus identities.

Open ambience is more important structurally. The game should feel like a huge empty land, but it should not use normal background music during progression. Use sparse environmental layers, very low wind/air beds, distant textural movements, and rare small ambient details instead.

Reject anything that feels like:

- obvious BGM
- melodic loop
- fantasy reward jingle
- UI notification
- indoor room tone
- city/cafe/traffic
- dense forest unless in the ending forest
- too clear a real-world location

### Open Ambience Mixing Notes

Use two layer types:

- `OpenGround`: long low-volume beds that create scale and emptiness.
- `SubtleTexture`: small randomized environmental details placed around the player at low volume.

The main field should remain sparse. A good baseline is one long `OpenGround` layer plus occasional `SubtleTexture` events. Do not fill every silence. Silence and near-silence are part of the game language.

For a wide empty land feeling:

- Keep most ambience non-directional or very wide.
- Put rare texture events far away or at the floor plane.
- Avoid constant tonal movement that reads as music.
- Let the bell remain the most readable intentional sound.

### Crystal Collision Candidates

These are for small crystal-like contacts, shimmer hits, or material accents. Use them quietly unless the design later assigns them to a major object.

| Listen Order | Asset | Intended Use |
| --- | --- | --- |
| 1 | `01_mixkit_crystal_chime_clean_hit.wav` | Clean crystal hit candidate |
| 2 | `02_mixkit_soft_bell_chime_clean_hit.wav` | Softer bell-crystal hybrid hit |
| 3 | `03_mixkit_resonating_metallic_hit.wav` | More physical resonant contact |
| 4 | `04_mixkit_magic_twinkle_hit_b.wav` | Bright magical crystal accent |
| 5 | `05_mixkit_magic_twinkle_hit_c.wav` | Short bright magical accent |
| 6 | `06_mixkit_magic_crystal_hit_c.wav` | Crystal/magic collision alternate |
| 7 | `07_mixkit_magic_sparkle_hit_b.wav` | Sparkle collision alternate |
| 8 | `08_mixkit_small_magic_resonance_hit.wav` | Small resonant accent |
| 9 | `09_mixkit_promise_bell_resonant_hit.wav` | Longer resonant bell/crystal tail |
| 10 | `10_mixkit_magic_sparkle_hit_a_long.wav` | Longer sparkle accent |
| 11 | `11_mixkit_bright_magic_collision_long.wav` | Longer bright collision; verify it is not too magical/cartoon-like |
| 12 | `12_mixkit_small_echo_chime_hit_a_ui_risk.wav` | Possible chime hit; high UI risk |
| 13 | `13_mixkit_small_echo_chime_hit_b_ui_risk.wav` | Possible chime hit; high UI risk |

### Dream Tone Candidates

These are transition or tail candidates, not background music. Prefer short appearances, fade-ins, or layered tails after meaningful actions.

| Listen Order | Asset | Intended Use |
| --- | --- | --- |
| 1 | `01_mixkit_ambient_magic_tail_long.wav` | Long dreamlike tail candidate |
| 2 | `02_mixkit_distant_magic_bell_tone.wav` | Distant bell/dream hybrid |
| 3 | `03_mixkit_soft_spell_tone_a.wav` | Soft dream tone |
| 4 | `04_mixkit_dream_magic_transition_a.wav` | Short transition shimmer |
| 5 | `05_mixkit_soft_magic_whoosh_tone.wav` | Soft whoosh-tone transition |
| 6 | `06_mixkit_mystic_rise_tone.wav` | Rising dream transition |
| 7 | `07_mixkit_slow_magic_tone.wav` | Slower magic tone |
| 8 | `08_mixkit_shimmering_magic_tail.wav` | Shimmer tail layer |
| 9 | `09_mixkit_sparkle_tail_layer.wav` | Sparkle tail layer |
| 10 | `10_mixkit_airy_magic_swell.wav` | Airy swell; overlaps with ending transition language |
| 11 | `11_mixkit_dream_magic_transition_b.wav` | Alternate short dream transition |
| 12 | `12_mixkit_ethereal_magic_swell_bright.wav` | Bright alternate; reject if too fantasy-like |
| 13 | `13_mixkit_sci_fi_ambient_tone_alt.wav` | Sci-fi alternate; reject if it weakens the organic bell identity |

### Open Ground Ambience Candidates

These are broad environmental beds for scale. Listen very quietly first; if a candidate only works at high volume, it probably does not fit.

| Listen Order | Asset | Intended Use |
| --- | --- | --- |
| 1 | `01_mixkit_wide_breeze_long.wav` | Primary open-air candidate |
| 2 | `02_mixkit_open_air_wind_long.wav` | Alternate open-air wind candidate |
| 3 | `03_mixkit_broad_environment_bed_a.wav` | Low broad environment bed |
| 4 | `04_mixkit_broad_environment_bed_b.wav` | Low broad environment alternate |
| 5 | `05_mixkit_open_ground_texture_b_very_low.wav` | Very low/quiet open ground candidate |
| 6 | `06_mixkit_low_cinematic_space_a_music_risk.wav` | Large low space; reject if it feels like score |
| 7 | `07_mixkit_open_ground_texture_a.wav` | Open texture alternate |
| 8 | `08_mixkit_wide_nature_floor_texture_a.wav` | Floor/nature texture alternate |
| 9 | `09_mixkit_wide_nature_floor_texture_b.wav` | Floor/nature texture alternate |
| 10 | `10_mixkit_broad_environment_bed_c_bright.wav` | Brighter broad bed; likely too active unless mixed very low |

### Subtle Ambience Texture Candidates

Use these as rare small events or very low secondary layers. They should add air and scale without becoming gameplay instructions.

| Listen Order | Asset | Intended Use |
| --- | --- | --- |
| 1 | `01_mixkit_small_environment_texture_f_long.wav` | Long subtle texture candidate |
| 2 | `02_mixkit_small_environment_texture_e_long.wav` | Long alternate texture |
| 3 | `03_mixkit_small_environment_texture_g.wav` | Low/mid texture candidate |
| 4 | `04_mixkit_small_environment_texture_p.wav` | Subtle texture alternate |
| 5 | `05_mixkit_small_environment_texture_l.wav` | Subtle texture alternate |
| 6 | `06_mixkit_small_environment_texture_d.wav` | Shorter/cleaner texture |
| 7 | `07_mixkit_small_environment_texture_b.wav` | Subtle texture alternate |
| 8 | `08_mixkit_small_environment_texture_j.wav` | Moderate texture alternate |
| 9 | `09_mixkit_small_environment_texture_m_short.wav` | Short ambient detail |
| 10 | `10_mixkit_small_environment_texture_k_short.wav` | Short ambient detail |
| 11 | `11_mixkit_small_environment_texture_i_bright.wav` | Brighter texture; reject if attention-grabbing |
| 12 | `12_mixkit_small_environment_texture_n_bright.wav` | Brighter texture; reject if attention-grabbing |

## Interaction / Missing Cue Candidate Target

Collected on 2026-05-16. These are candidates, not final selections.

This section covers seven sound families that were identified as likely gaps after the main bell, rain, tinnitus, forest, wind, crystal, and ambience collections.

Analysis bundles:

- `tools/audio-lab/output/batch/InteractionFeedbackCandidates`
- `tools/audio-lab/output/batch/BossTinnitusCandidates`
- `tools/audio-lab/output/batch/MovementBodyCandidates`
- `tools/audio-lab/output/batch/TransitionCutCandidates`
- `tools/audio-lab/output/batch/NarrationCueCandidates`

Unity-ready converted files:

- `Assets/Audio/Processed/Interaction`
- `Assets/Audio/Processed/Tinnitus/Boss`
- `Assets/Audio/Processed/Movement`
- `Assets/Audio/Processed/Transition`
- `Assets/Audio/Processed/Narration/Cue`

First-listen shortlist:

- `Assets/Audio/Curated/Interaction/PadFeedback`
- `Assets/Audio/Curated/Interaction/WallScan`
- `Assets/Audio/Curated/Interaction/WallContactEscape`
- `Assets/Audio/Curated/Tinnitus/Boss`
- `Assets/Audio/Curated/Movement/Body`
- `Assets/Audio/Curated/Transition/Cut`
- `Assets/Audio/Curated/Narration/Cue`

Files in these folders are numbered in intended listening order. Files with `risk` in the name are not rejected yet, but should be treated as likely failures unless they solve a specific implementation problem.

### Pad Feedback Candidates

Use these for controller-related tactile feedback outside tinnitus cleansing:

- pad button press
- pad shake detected
- pad reconnecting with the sound world
- positional lock beginning
- failed or lost pad tracking, if needed

The pad should not sound like a normal UI menu. Prefer physical, small, dry, tactile clicks over bright game sounds.

Reject if the cue feels like:

- phone or app UI
- arcade selection
- reward jingle
- too futuristic for the bell world
- too loud or attention-grabbing

First-listen folder:

- `Assets/Audio/Curated/Interaction/PadFeedback`

### Wall Scan Candidates

Use these for the bell-wave scan response when the player shakes the bell and the world returns information about nearby walls.

The scan response should feel like information coming back from space. It can be more abstract than a literal wall hit, but should still communicate distance, mass, and pressure.

Suggested implementation:

- Far wall: quieter, darker, longer tail.
- Near wall: louder, shorter, more pressure.
- Very close wall: add subtle distortion or noise layer.
- Direction should come from spatial placement, not from a busy stereo file.

Reject if the cue feels like:

- explosion
- weapon impact
- cinematic trailer hit
- obvious metal object unless the wall theme becomes metallic

First-listen folder:

- `Assets/Audio/Curated/Interaction/WallScan`

### Wall Contact / Escape Candidates

Use these for touching, rubbing, scraping, or forcing through a wall-like surface.

This family is important because it gives the player proof that their action is changing the world. It can be layered with wall noise and pad vibration.

Suggested implementation:

- Light contact: short grit/scrape.
- Continuous rubbing: looping or repeated scrape grains.
- Wall weakening: add stone movement or low shifting layer.
- Escape moment: stop scrape, then use a short cut/air-release cue from `Transition/Cut`.

Reject if the cue feels like:

- normal door opening
- realistic construction/rocks instead of abstract wall
- too metallic unless the wall becomes explicitly metallic

First-listen folder:

- `Assets/Audio/Curated/Interaction/WallContactEscape`

### Boss Tinnitus Candidates

Use these only for the boss tinnitus, not normal tinnitus.

The boss should have a separate identity:

- heartbeat-like low pulse
- heavy unstable body
- strong glitch/electric bursts
- larger perceived size
- movement while being cleansed

Suggested layering:

- Base: heartbeat or low pulse loop.
- Instability: intermittent glitch/electric burst.
- Cleansing: increase burst rate, pitch motion, and distortion while the pad stays aligned.
- Final defeat: transition to the already collected boss-cleanse-to-forest transition.

Reject if the cue feels like:

- literal monster voice
- cheesy horror sting
- painful sustained high tone
- normal small tinnitus asset just made louder

First-listen folder:

- `Assets/Audio/Curated/Tinnitus/Boss`

### Movement / Body Candidates

Use these very carefully. The project is abstract and sensory-first, so realistic footsteps may make the world too literal.

Best use cases:

- extremely low-volume body movement while walking
- cloth/controller handling near the player
- subtle floor contact when movement starts or stops
- controller motion gesture feedback, if pad-specific clicks are too UI-like

Prefer cloth shift and low body texture first. Use footsteps only if they are mixed quietly enough to feel like body presence rather than a walking simulator.

Reject if the cue feels like:

- clear shoe footsteps
- indoor floor
- character animation sound
- realistic scene that conflicts with the empty-land abstraction

First-listen folder:

- `Assets/Audio/Curated/Movement/Body`

### State Transition / Cut Candidates

Use these for scene or state transitions where silence is part of the design:

- rain to wall phase
- wall phase to tinnitus obstruction
- boss tinnitus cleanse to silence
- silence to forest
- narration entering after a major cue

These are not background transitions. They should be short structural edits in the sound world.

Suggested implementation:

- Use one short cut or pressure cue.
- Drop other layers sharply or with a very short duck.
- Let silence or near-silence carry the moment.
- Avoid stacking multiple cinematic risers unless the boss defeat explicitly needs scale.

Reject if the cue feels like:

- trailer transition
- magic spell
- horror jump scare
- obvious UI page transition

First-listen folder:

- `Assets/Audio/Curated/Transition/Cut`

### Narration Cue Candidates

Use only if narration needs an entry marker. Silence and ducking may be better than an audible cue.

Best use case:

- very quiet pre-narration tick or tone
- environmental layers duck slightly
- narration speaks
- cue does not repeat after every line unless needed for accessibility

Reject if the cue feels like:

- notification
- app assistant
- subtitle beep
- menu UI
- reward confirmation

First-listen folder:

- `Assets/Audio/Curated/Narration/Cue`

Recommended first test: try no audible narration cue, only sidechain/ducking. If players miss narration entry or are startled, test `01` through `04` at very low volume.

## Narration Voice Tone Samples

Generated on 2026-05-17 with local Windows TTS for direction testing only.

Voice:

- `Microsoft Heami Desktop`
- Korean female adult local TTS voice

Source sentence:

- `종의 소리를 따라 이동하세요.`

Tone target:

- female, but not character-like
- small and comfortable, but not an exaggerated whisper
- protective guide rather than command voice
- low emotion, but not cold or machine-like
- feels like it comes from inside the head or slightly in front of the player, not directly beside the ear
- starts quiet, but remains intelligible over gameplay audio through mixing, EQ, compression, and ducking

Generated files:

- `Assets/Audio/RawCandidates/Narration/VoiceSamples`
- `Assets/Audio/Processed/Narration/VoiceSamples`
- `Assets/Audio/Curated/Narration/VoiceSamples`

Analysis bundle:

- `tools/audio-lab/output/batch/NarrationVoiceSamples`

### Voice Sample Candidates

These samples are for tone direction only. Verify Microsoft voice licensing before using any generated file in a shipped build. If the tone direction is approved but quality is not enough, regenerate the final lines with a higher-quality TTS provider using this section as the voice brief.

Important update on 2026-05-17: the local `Microsoft Heami Desktop` samples were rejected. Do not use them as a quality target. They remain only as a record of the failed direction.

| Listen Order | Asset | Intended Difference |
| --- | --- | --- |
| 1 | `01_heami_soft_guiding_neutral.wav` | Baseline soft guide tone |
| 2 | `02_heami_soft_slower_comfort.wav` | Slower and more comfortable |
| 3 | `03_heami_clear_low_volume.wav` | Lower volume but slightly clearer pacing |
| 4 | `04_heami_front_mind_calm.wav` | Slight pause after `따라`; calmer front/head placement candidate |
| 5 | `05_heami_gentle_but_audible.wav` | Same tone as baseline but more audible |
| 6 | `06_heami_short_pause_soft.wav` | Stronger phrase separation; reject if too instructional |

Initial listening priority:

- Start with `01`, `02`, and `05`.
- Use `03` to test how quiet the narration can be before intelligibility suffers.
- Use `04` and `06` only if the phrase needs more separation for closed-eye play.

### High-Quality Korean TTS Provider Tests

Current replacement test sentence:

- `종소리를 따라, 천천히 이동하세요.`

Provider setup and commands:

- `Docs/NarrationTtsProviderSetup.md`

Target output folder:

- `Assets/Audio/RawCandidates/Narration/ProviderTests`

Preferred provider order for the next test:

1. NAVER CLOVA Voice
2. Typecast
3. Supertone

Selection rule: reject any voice that feels like a phone assistant, OS narrator, animated character, advertisement voice, or obvious synthetic TTS. The narration must be quiet and comfortable but still intelligible under game audio.

## Bell Similar Reference Search

Collected on 2026-05-19 after adding:

- `Assets/Audio/bell sound.wav`

The file was copied as the reference clip:

- `Assets/Audio/RawCandidates/BellSimilar/Reference/reference_bell_sound.wav`
- `Assets/Audio/Processed/BellSimilar/Reference/reference_bell_sound.wav`
- `Assets/Audio/Curated/BellSimilar/Closest/00_reference_bell_sound.wav`

Analysis bundle:

- `tools/audio-lab/output/analysis/bell-sound-new.json`
- `tools/audio-lab/output/spectrograms/bell-sound-new.png`
- `tools/audio-lab/output/batch/BellSimilarCandidates`

### Reference Audio Analysis

`bell sound.wav` is not a short single handbell. It behaves more like a long resonant bell/chime texture:

- duration: about `10.73s`
- format: `44.1kHz`, stereo, 16-bit PCM WAV
- brightness: `0.3241`
- noisiness: `0.0924`
- harshness: `0.3813`
- median RMS: `0.0029`
- spectral centroid mean: about `169.8`
- spectral rolloff mean: about `13477.6`

Spectrogram reading:

- several separated bell/chime attacks
- long horizontal harmonic partials
- soft noise/reverb bed under the partials
- not aggressive, not UI-like, not a dry close handbell

Design interpretation:

- good direction for a softer, more dreamlike bell identity
- likely better for final acquisition, distant calling, or transformation than for quick button feedback
- use as a reference for "long tail, soft metallic body, gentle repeated chime"

### Source Investigation

No reliable original source was found from the file itself.

Known facts:

- file name in project: `bell sound.wav`
- SHA256: `E9F705ABB8E83210A9818F002E5B4BB63D84EA787BC8B555FE282E76BB6A569F`
- embedded metadata was effectively empty except an empty `TXXX:Software` field
- web search for the exact SHA256 and file size did not identify a public source

Treat this file as source-unknown until the original download/generation source is recovered. Do not ship it unless the license/source is clarified.

### Similar Candidate Collection

New source folder:

- `Assets/Audio/RawCandidates/BellSimilar/BigSoundBank`

Unity-ready converted folder:

- `Assets/Audio/Processed/BellSimilar`

First-listen folders:

- `Assets/Audio/Curated/BellSimilar/Closest`
- `Assets/Audio/Curated/BellSimilar/LongChime`
- `Assets/Audio/Curated/BellSimilar/DreamChime`
- `Assets/Audio/Curated/BellSimilar/ResonantBell`

Use `Closest` first. It contains the reference file, the nearest BigSoundBank candidates by analysis distance, and the previously collected closest local bell candidates.

### Closest Candidates

Listen in order and compare against `00_reference_bell_sound.wav`.

| Listen Order | Asset | Intended Check |
| --- | --- | --- |
| 0 | `00_reference_bell_sound.wav` | Source-unknown reference; do not ship until source is known |
| 1 | `01_bigsoundbank_2554_tibetan_bowl_like.wav` | Closest long resonant body by analysis |
| 2 | `02_bigsoundbank_2555_tibetan_bowl_like.wav` | Long resonant alternate |
| 3 | `03_bigsoundbank_2553_tibetan_bowl_like.wav` | Long resonant alternate |
| 4 | `04_bigsoundbank_3360_one_pendulum_chime.wav` | Pendulum chime, close duration/body |
| 5 | `05_bigsoundbank_3361_two_pendulum_chimes.wav` | Two-chime alternate |
| 6 | `06_bigsoundbank_2880_soft_bell_candidate.wav` | Soft bell candidate |
| 7 | `07_bigsoundbank_2881_soft_bell_candidate.wav` | Soft bell alternate |
| 8 | `08_bigsoundbank_2888_bronze_bell_candidate.wav` | Brighter bronze bell candidate |
| 9 | `09_bigsoundbank_0920_glockenspiel.wav` | Dreamlike metallic note cluster |
| 10 | `10_bigsoundbank_1569_grandfather_clock_hour.wav` | Clock-bell stack; reject if too clock-like |
| 11 | `11_bigsoundbank_1110_tibetan_bowl_alt.wav` | Long bowl/chime alternate |
| 12 | `12_bigsoundbank_1570_grandfather_clock_hour2.wav` | Clock-bell alternate; reject if too literal |
| 13 | `13_existing_mixkit_930_bell_of_promise.wav` | Existing local candidate, still numerically close |
| 14 | `14_existing_bigsoundbank_2117.wav` | Existing local candidate |
| 15 | `15_existing_bigsoundbank_2116.wav` | Existing local candidate |

### Listening Criteria

Keep candidates that:

- have a long, soft, emotionally clean decay
- feel mysterious or dreamlike without becoming fantasy UI
- can sit in quiet ambience without sounding like a notification
- have a clear attack but not a harsh attack
- feel like a guiding object, not a reward jingle

Reject candidates that:

- sound like a clock, hotel counter bell, phone alert, or UI confirmation
- are too musical or recognizably melodic
- are too bright to repeat often
- are too dry and close
- have unclear licensing or source

### Gemini TTS Narration Processing

Current best direction as of 2026-05-19:

- Google AI Studio / Gemini 2.5 Pro Preview TTS
- Voice: Despina
- Tone: bright
- Style: whisper
- Speed: natural
- Accent: unset

Prompt direction:

- adult Korean female guide
- soft, calm, natural, grounded
- protective guide, not a character
- low and warm emotion
- clear articulation
- not a phone assistant, advertisement, audiobook, anime character, or tutorial voice

Generated source:

- `Assets/Audio/Generated Audio May 19, 2026 - 3_53AM.wav`

Processed candidates:

- `Assets/Audio/Curated/Narration/GeminiTts`

Listening order:

| Listen Order | Asset | Intended Use |
| --- | --- | --- |
| 1 | `01_gemini_353am_clear_warm_space.wav` | Primary processed candidate; warm and clear, no obvious echo |
| 2 | `02_gemini_353am_mind_front_soft_echo.wav` | Slight head/front-space echo; use if it does not blur consonants |
| 3 | `04_gemini_353am_dry_mix_reference.wav` | Dry reference for Unity-side mixer effects |
| 4 | `03_gemini_353am_dream_hazy_test.wav` | Hazy/dreamlike test; reject if words become less immediate |

Processing rule:

- Keep narration mostly dry and intelligible.
- Do not bake strong reverb into the file.
- Prefer subtle EQ/compression in the WAV and handle final placement with Unity AudioMixer.
- If the voice feels too close to the ear, use Unity-side spatial placement slightly in front of the listener before adding more echo.

### Gemini TTS Working Candidate

Added on 2026-05-19.

Source file:

- `Assets/Audio/Generated Audio May 19, 2026 - 3_53AM.wav`

Generation settings that produced the first acceptable direction:

- model: Gemini 2.5 Pro Preview TTS
- voice: Despina
- voice tone: bright
- style: whisper
- speed: natural
- accent: unset

Prompt structure:

- scene: closed-eye player in a vast quiet acoustic space; bell ahead; narrator is a calm guide inside the head or slightly in front
- sample context: sensory-first audio game; protective, intimate, calm, non-theatrical adult Korean woman
- transcript: `종소리를 따라, 천천히 이동하세요.`

Post-processed candidates:

- `Assets/Audio/Processed/Narration/GeminiTts`
- `Assets/Audio/Curated/Narration/GeminiTts`

| Listen Order | Asset | Intended Difference |
| --- | --- | --- |
| 1 | `01_gemini_353am_clear_warm_space.wav` | Most conservative processing; clarity first, slight warmth/space |
| 2 | `02_gemini_353am_mind_front_soft_echo.wav` | Slightly more front-of-mind echo and softness |
| 3 | `03_gemini_353am_dream_hazy_test.wav` | Haziest test; reject if intelligibility drops |

Processing note:

- The original file was not overwritten.
- Effects were applied with `ffmpeg-static`, not Audacity, because command-line batch processing is more reliable for repeatable variants.
- Avoid heavy reverb. Narration must remain clearer than the ambient bed, rain, wall noise, and tinnitus layers.
