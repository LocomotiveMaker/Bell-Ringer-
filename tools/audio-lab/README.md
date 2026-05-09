# Audio Lab

`tools/audio-lab` is a small Node workspace for Bell Ringer audio iteration.

It covers three jobs:

- analyze local audio files into feature reports
- normalize or convert source files into a lab-friendly WAV format
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

Prepare a file into a normalized mono WAV for game-side iteration.

```powershell
npm.cmd run prepare:audio -- --input ..\downloads\sample.mp3 --normalize
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

## Output layout

- `output/analysis`: JSON feature reports
- `output/prepared`: converted WAV files
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
