# Audio Lab

`tools/audio-lab` is a small Node workspace for Bell Ringer audio iteration.

It covers six jobs:

- analyze local audio files into feature reports
- normalize or convert source files into a lab-friendly WAV format
- batch-analyze folders of downloaded candidates
- create spectrogram PNGs without an external API
- write category scorecards as CSV
- convert candidate folders into Unity-ready WAV files
- call external providers for narration and sound-effect generation

## Why these tools

- `OpenAI`: narration TTS and speech-to-text for review notes
- `ElevenLabs`: text-to-sound effect generation
- `Meyda`: offline feature extraction in Node
- `ffmpeg-static`: deterministic audio conversion without a separate local install
- `music-metadata`: codec and duration inspection

Two tools from the earlier research list are intentionally not wired in:

- `Adobe Podcast Enhance`: useful product, but there is no clear public one-step API for the Podcast Enhance workflow, so this lab does not automate it
- `Essentia.js`: powerful, but old WebAssembly bindings plus AGPL licensing make it a poor default fit for this repo; add it later only if you need its higher-level MIR descriptors

## Setup

1. Install dependencies:

```powershell
npm.cmd install
```

2. Copy `.env.example` to `.env` and fill in the keys you have.

3. Run a health check:

```powershell
npm.cmd run doctor
```

## Commands

Analyze any supported audio file. Internally it converts to mono 48k WAV and writes a JSON report.

```powershell
npm.cmd run analyze -- --input ..\downloads\sample.wav
```

Analyze a whole folder, write per-file JSON reports, and write one category scorecard CSV.

```powershell
npm.cmd run batch -- --input ..\..\Assets\Audio\RawCandidates
```

Analyze a whole folder and also make spectrogram PNGs.

```powershell
npm.cmd run batch -- --input ..\..\Assets\Audio\RawCandidates --spectrograms
```

Analyze a whole folder, make spectrograms, and convert Unity-ready WAV copies in the same output bundle.

```powershell
npm.cmd run batch -- --input ..\..\Assets\Audio\RawCandidates --spectrograms --unity-import --normalize
```

Create a spectrogram PNG for one file.

```powershell
npm.cmd run spectrogram -- --input ..\downloads\sample.wav
```

Build or rebuild a CSV scorecard from a folder of analysis JSON files.

```powershell
npm.cmd run scorecard -- --input output\batch\RawCandidates-2026-05-12-120000\analysis
```

Prepare a file into a normalized mono WAV for game-side iteration.

```powershell
npm.cmd run prepare:audio -- --input ..\downloads\sample.mp3 --normalize
```

Convert a candidate folder into `Assets/Audio/Processed` as 48kHz mono WAV files.

```powershell
npm.cmd run unity:import -- --input ..\..\Assets\Audio\RawCandidates --normalize
```

Generate narration with OpenAI TTS from a JSON spec.

```powershell
npm.cmd run openai:tts -- --spec presets\openai-follow-bell-narration.json
```

Transcribe an audio file with OpenAI STT.

```powershell
npm.cmd run openai:stt -- --input ..\downloads\voice-note.wav --response-format json
```

Generate a sound effect with ElevenLabs from a JSON spec.

```powershell
npm.cmd run eleven:sfx -- --spec presets\elevenlabs-tinnitus-burst.json
```

List Korean-friendly Typecast voices after setting `TYPECAST_API_KEY`.

```powershell
npm.cmd run typecast:voices -- --model ssfm-v30 --gender female --age young_adult --output output\typecast-female-voices.json
```

Generate a Typecast narration sample after choosing a `voice_id`.

```powershell
npm.cmd run typecast:tts -- --voice-id tc_xxx --text "종소리를 따라, 천천히 이동하세요." --output ..\..\Assets\Audio\RawCandidates\Narration\ProviderTests\typecast-test.wav
```

List Korean female Supertone narration voices after setting `SUPERTONE_API_KEY`.

```powershell
npm.cmd run supertone:voices -- --language ko --gender female --use-case narration --output output\supertone-ko-female-voices.json
```

Generate a Supertone narration sample after choosing a `voice_id`.

```powershell
npm.cmd run supertone:tts -- --voice-id voice_xxx --text "종소리를 따라, 천천히 이동하세요." --output ..\..\Assets\Audio\RawCandidates\Narration\ProviderTests\supertone-test.wav
```

Generate NAVER CLOVA Voice narration samples after setting `NAVER_CLOVA_VOICE_API_KEY_ID` and `NAVER_CLOVA_VOICE_API_KEY`.

```powershell
npm.cmd run naver:tts -- --speaker vgoeun --text "종소리를 따라, 천천히 이동하세요." --output ..\..\Assets\Audio\RawCandidates\Narration\ProviderTests\naver-vgoeun-test.wav
```

Generate the Korean narration comparison batch. NAVER runs with NAVER keys. Typecast and Supertone also need selected voice IDs in `TYPECAST_VOICE_IDS` and `SUPERTONE_VOICE_IDS`.

```powershell
npm.cmd run narration:korean-test
```

## Output layout

- `output/analysis`: JSON feature reports
- `output/batch`: per-folder analysis bundles
- `output/prepared`: converted WAV files
- `output/scorecards`: CSV scorecards rebuilt from reports
- `output/spectrograms`: one-off spectrogram PNGs
- `output/openai`: narration audio and transcription files
- `output/elevenlabs`: generated sound effects
- `tmp`: transient WAV files used for analysis

## Report shape

The analyzer writes:

- original file metadata
- prepared WAV metadata
- frame statistics for RMS, spectral centroid, flatness, rolloff, spread, and zero-crossing rate
- derived scores for brightness, noisiness, instability, transient density, and harshness

These are heuristics for narrowing candidates. They are not a replacement for listening.

## Suggested no-API workflow

1. Download candidates into `Assets/Audio/RawCandidates/<Category>`.
2. Run `npm.cmd run batch -- --input ..\..\Assets\Audio\RawCandidates --spectrograms`.
3. Open `scorecard.csv` and sort by the scores that matter for the category.
4. Listen only to the narrowed set.
5. Run `npm.cmd run unity:import -- --input ..\..\Assets\Audio\RawCandidates\<Category> --output ..\..\Assets\Audio\Processed\<Category> --normalize` for selected files.

For Bell Ringer categories, `brightness` and `harshness` are useful for tinnitus and wall noise, `noisiness` helps separate rain/wind beds from clean bells, and `instability` helps find glitch-like candidates. The final pick still needs listening.
