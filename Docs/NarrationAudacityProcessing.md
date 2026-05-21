# Narration Audacity Processing

Purpose: process Gemini TTS narration through Audacity so the voice fits Bell Ringer's quiet, closed-eye, sensory-first sound world.

Current chosen TTS direction:

- Gemini 2.5 Pro Preview TTS
- Voice: Despina
- Tone: bright
- Style: whisper
- Speed: natural
- Accent: unset

Current sample source files:

- `Assets/Audio/Generated Audio May 19, 2026 - 3_36AM.wav`
- `Assets/Audio/Generated Audio May 19, 2026 - 3_51AM.wav`
- `Assets/Audio/Generated Audio May 19, 2026 - 3_53AM.wav`

Audacity path:

- `C:\Program Files\Audacity\Audacity.exe`

## Automation Status

Audacity is not a reliable headless CLI processor in this local setup.

Audacity supports Macros and `mod-script-pipe` scripting, but scripting requires a running GUI instance and the scripting module to create named pipes. On this machine, the module exists, but pipe automation did not work reliably during testing. Because Audacity scripting also weakens local security, keep it disabled unless there is a specific reason to use it.

Use Audacity's GUI and save processing settings manually.

## Processing Goal

The narration should feel like:

- inside the player's head or slightly in front
- small and calm
- human and close, but not beside the ear
- clear under rain, bell, wall, and tinnitus layers
- not like a phone assistant
- not like a tutorial UI
- not like a dramatic narrator

Do not chase a big reverb sound. The best version should sound almost dry when soloed, with only enough space to stop it from feeling pasted on top of the game.

## Recommended Main Chain: Clear Mind-Front Voice

This is the main production candidate chain.

1. Open the Gemini WAV in Audacity.
2. `Ctrl+A` to select all audio.
3. Set Project Rate to `48000 Hz`.
4. Apply the effects below in order.

### 1. High-Pass Filter

Path:

- `Effect > EQ and Filters > High-Pass Filter`

Settings:

- Cutoff frequency: `85 Hz`
- Rolloff: `12 dB per octave`

Purpose:

- remove low rumble that will fight rain, hum, and wall noise
- keep the voice light and close

### 2. Filter Curve EQ

Path:

- `Effect > EQ and Filters > Filter Curve EQ`

Approximate curve:

| Frequency | Gain |
| --- | --- |
| 80 Hz | `-6 dB` |
| 150 Hz | `-2 dB` |
| 250 Hz | `-1.5 dB` |
| 800 Hz | `0 dB` |
| 2500 Hz | `+1 dB` |
| 4200 Hz | `+0.5 dB` |
| 8000 Hz | `-1 dB` |
| 12000 Hz | `-2 dB` |

Purpose:

- reduce chest/boxiness
- preserve Korean consonant clarity
- avoid sharp, artificial high-end

### 3. Compressor

Path:

- `Effect > Volume and Compression > Compressor`

Settings:

- Threshold: `-22 dB`
- Noise Floor: `-55 dB`
- Ratio: `2.2:1`
- Attack Time: `0.05 s`
- Release Time: `0.8 s`
- Make-up gain: off if available
- Compress based on peaks: off if available

Purpose:

- keep the quiet voice intelligible without making it sound loud
- reduce sudden syllable jumps

### 4. Reverb

Path:

- `Effect > Reverb`

Settings:

- Room Size: `12%`
- Pre-delay / Delay: `9 ms`
- Reverberance: `14%`
- Damping: `70%`
- Tone Low: `65%`
- Tone High: `45%`
- Wet Gain: `-20 dB`
- Dry Gain: `-1 dB`
- Stereo Width: `45%`
- Wet Only: off

Purpose:

- add a barely audible space around the voice
- imply "slightly in front / inside the head"
- avoid obvious echo

If you can clearly hear reverb as an effect, it is too much.

### 5. Limiter

Path:

- `Effect > Volume and Compression > Limiter`

Settings:

- Type: `Soft Limit`
- Input Gain: `0 dB`
- Limit to: `-3 dB`
- Hold: `10 ms`
- Apply Make-up Gain: off

Purpose:

- catch peaks without making the voice sound mastered or broadcast-like

### 6. Loudness Normalization

Path:

- `Effect > Volume and Compression > Loudness Normalization`

Settings:

- Perceived Loudness: `-20 LUFS`
- Treat mono as dual-mono: on if available

Purpose:

- keep all narration lines consistent
- leave Unity mixer room for final volume control

### 7. Fade Edges

Manually select and fade:

- first `20 ms`: Fade In
- last `80 ms`: Fade Out

Purpose:

- remove start/end clicks
- make line entry and exit less abrupt

## Alternate Chain: Slightly More Dreamlike

Use only if the main chain feels too dry.

Change only the Reverb settings:

- Room Size: `18%`
- Delay: `12 ms`
- Reverberance: `18%`
- Damping: `75%`
- Tone Low: `60%`
- Tone High: `40%`
- Wet Gain: `-17 dB`
- Dry Gain: `-1.5 dB`
- Stereo Width: `50%`

Reject this version if consonants blur or if the voice feels farther away than the bell.

## Bad Direction To Avoid

Avoid these:

- obvious cave reverb
- long echo tail
- chorus/phaser
- telephone/radio EQ
- heavy low-pass haze
- strong stereo widening
- noise reduction that creates watery artifacts

The player is navigating by sound. Narration can be soft, but it must never become vague.

## Export

Export from Audacity as:

- WAV
- 48 kHz
- mono if the narration will be positioned/mixed in Unity
- 16-bit PCM or 24-bit PCM

Suggested folder:

- `Assets/Audio/Processed/Narration/GeminiTtsAudacity`

Suggested names:

- `01_gemini_despina_clear_mind_front_audacity.wav`
- `02_gemini_despina_dream_soft_space_audacity.wav`

Copy selected listening candidates to:

- `Assets/Audio/Curated/Narration/GeminiTtsAudacity`

## In-Game Mixing Note

Use Unity only for playback and final bus balancing, not for creating the sound.

Recommended Unity mix target:

- narration bus starts low
- duck rain, wall noise, and tinnitus slightly while narration plays
- do not duck the bell too much, because bell is the main orientation source
- keep narration mostly center/non-spatial or very slightly forward
