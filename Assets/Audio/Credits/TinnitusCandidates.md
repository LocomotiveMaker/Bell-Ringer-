# Tinnitus / Glitch Candidate Notes

Downloaded on 2026-05-14 for local listening and analysis.

## Folders

- Raw candidates: `Assets/Audio/RawCandidates/Tinnitus`
- Unity-ready WAV conversions: `Assets/Audio/Processed/Tinnitus`
- First-listen shortlist: `Assets/Audio/Curated/Tinnitus`
- Analysis bundle: `tools/audio-lab/output/batch/TinnitusCandidates`

## Design Intent

Tinnitus should read as unstable frequency plus glitch.

Unity may synthesize the main tinnitus tones. These assets are supporting layers for texture, spikes, tuning, lock, and resolution.

## Candidate Families

- `LongGlitch`: quiet long-term glitch/noise layer under generated tinnitus tones.
- `BurstGlitch`: short intense irregular spikes.
- `HealingMotion`: cleanse-in-progress movement, resistance, sweep, or stabilization.
- `Resolve`: final punctuation when tinnitus disappears.
- `Approach`: pad position/rotation nearing the valid pose.
- `Lock`: exact valid pose entered.

## Source Notes

- BigSoundBank files were downloaded from `https://bigsoundbank.com/UPLOAD/bwf-en/<id>.wav`.
- BigSoundBank pages and WAV metadata identify these sounds as CC0 / public domain.
- Mixkit files were downloaded from `https://assets.mixkit.co/active_storage/sfx/<id>/<id>-preview.mp3`.
- Mixkit states that its sound effects can be used in personal and commercial projects without attribution. Check the Mixkit license page before final release.

## First-Listen Order

Long glitch:

1. `Curated/Tinnitus/LongGlitch/01_bigsoundbank_transformer_low_long.wav`
2. `Curated/Tinnitus/LongGlitch/02_bigsoundbank_neon_transformer_buzz.wav`
3. `Curated/Tinnitus/LongGlitch/03_mixkit_terror_radio_frequency_long.wav`
4. `Curated/Tinnitus/LongGlitch/04_mixkit_horror_radio_signal_long.wav`
5. `Curated/Tinnitus/LongGlitch/05_bigsoundbank_feedback_590hz_soft.wav`

Burst glitch:

1. `Curated/Tinnitus/BurstGlitch/01_bigsoundbank_zapping_short_white_noise.wav`
2. `Curated/Tinnitus/BurstGlitch/02_mixkit_small_electric_glitch.wav`
3. `Curated/Tinnitus/BurstGlitch/03_mixkit_electric_buzz_glitch.wav`
4. `Curated/Tinnitus/BurstGlitch/04_mixkit_glitch_static.wav`
5. `Curated/Tinnitus/BurstGlitch/05_mixkit_digital_signal_interference.wav`
6. `Curated/Tinnitus/BurstGlitch/06_mixkit_futuristic_glitch_robot.wav`

Healing motion:

1. `Curated/Tinnitus/HealingMotion/01_bigsoundbank_radio_frequency_sweep_healing.wav`
2. `Curated/Tinnitus/HealingMotion/02_mixkit_glitch_rewind_healing.wav`
3. `Curated/Tinnitus/HealingMotion/03_mixkit_electric_whoosh_healing.wav`
4. `Curated/Tinnitus/HealingMotion/04_mixkit_glitchy_synth_intro_rise.wav`
5. `Curated/Tinnitus/HealingMotion/05_mixkit_fast_sci_fi_sweep.wav`

Resolve:

1. `Curated/Tinnitus/Resolve/01_mixkit_sci_fi_confirmation_clean.wav`
2. `Curated/Tinnitus/Resolve/02_mixkit_positive_interface_beep.wav`
3. `Curated/Tinnitus/Resolve/03_mixkit_page_forward_chime_echo.wav`
4. `Curated/Tinnitus/Resolve/04_mixkit_page_back_chime_echo.wav`

Approach:

1. `Curated/Tinnitus/Approach/01_mixkit_interface_hint_near.wav`
2. `Curated/Tinnitus/Approach/02_bigsoundbank_pure_beep_distance_test.wav`
3. `Curated/Tinnitus/Approach/03_mixkit_interface_option_select.wav`
4. `Curated/Tinnitus/Approach/04_mixkit_game_ui_tone.wav`
5. `Curated/Tinnitus/Approach/05_mixkit_sci_fi_reject_unstable.wav`

Lock:

1. `Curated/Tinnitus/Lock/01_mixkit_gear_metallic_lock.wav`
2. `Curated/Tinnitus/Lock/02_mixkit_gear_fast_lock_tap.wav`
3. `Curated/Tinnitus/Lock/03_mixkit_sci_fi_click.wav`
4. `Curated/Tinnitus/Lock/04_mixkit_electronic_lock_success_beeps.wav`
5. `Curated/Tinnitus/Lock/05_mixkit_computer_digital_lock.wav`
6. `Curated/Tinnitus/Lock/06_mixkit_cool_interface_click_tone.wav`
