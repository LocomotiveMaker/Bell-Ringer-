# Narration TTS Provider Setup

Purpose: generate high-quality Korean narration candidates for Bell Ringer without using a voice actor.

Current test sentence:

```text
종소리를 따라, 천천히 이동하세요.
```

Voice target:

- female, but not character-like
- small and comfortable, without exaggerated whispering
- protective guide rather than command voice
- low emotion, but not cold or machine-like
- perceived as inside the head or slightly in front of the player
- quiet in the mix, but intelligible through EQ, compression, and ducking

## Output Folder

Provider-generated samples should go here first:

```text
Assets/Audio/RawCandidates/Narration/ProviderTests
```

After listening, selected files can be copied to:

```text
Assets/Audio/Curated/Narration/ProviderTests
```

## NAVER CLOVA Voice

Use this as the first API-driven Korean TTS test because it has a Korean-specific voice list and direct WAV output.

Important: this does not use the NAVER Cloud Platform account-level `Access key ID` and `Secret key` from `My Account > Account and security management`. CLOVA Voice uses the `Client ID` and `Client Secret` issued after registering an `AI·NAVER API` application and enabling/selecting the CLOVA Voice service for that application.

Required `.env` values:

```text
NAVER_CLOVA_VOICE_API_KEY_ID=
NAVER_CLOVA_VOICE_API_KEY=
NAVER_CLOVA_VOICE_URL=https://naveropenapi.apigw.ntruss.com/tts-premium/v1/tts
```

Single sample:

```powershell
npm.cmd run naver:tts -- --speaker vgoeun --text "종소리를 따라, 천천히 이동하세요." --output ..\..\Assets\Audio\RawCandidates\Narration\ProviderTests\naver-vgoeun-test.wav
```

Recommended first speakers:

- `vgoeun`
- `vyuna`
- `vmikyung`
- `nkyunglee`
- `nminyoung`
- `njiwon`

If you receive `401 Authentication Failed`, the values are usually not the AI·NAVER API application's Client ID/Client Secret, or the `.env` file was not loaded from `tools/audio-lab`.

If you receive a quota-related error such as `429`, check whether the CLOVA Voice service is selected/enabled on the AI·NAVER API application.

Default tuning in the scripts:

- `volume=-2`
- `speed=2`
- `pitch=1`
- `alpha=-1`
- `emotion=0`
- `format=wav`
- `sampling-rate=48000`

## Typecast

Use this if NAVER still sounds synthetic or too service-like. Typecast requires selecting a `voice_id` first.

Required `.env` values:

```text
TYPECAST_API_KEY=
TYPECAST_TTS_MODEL=ssfm-v30
TYPECAST_VOICE_IDS=
```

List likely voices:

```powershell
npm.cmd run typecast:voices -- --model ssfm-v30 --gender female --age young_adult --output output\typecast-female-voices.json
```

Generate one sample:

```powershell
npm.cmd run typecast:tts -- --voice-id tc_xxx --text "종소리를 따라, 천천히 이동하세요." --output ..\..\Assets\Audio\RawCandidates\Narration\ProviderTests\typecast-test.wav
```

For batch tests, put comma-separated IDs in `.env`:

```text
TYPECAST_VOICE_IDS=tc_xxx,tc_yyy,tc_zzz
```

Then run:

```powershell
npm.cmd run narration:korean-test
```

## Supertone

Use this as another high-priority Korean-specialized option. Supertone requires selecting a `voice_id` first.

Required `.env` values:

```text
SUPERTONE_API_KEY=
SUPERTONE_TTS_MODEL=sona_speech_2
SUPERTONE_VOICE_IDS=
```

List likely voices:

```powershell
npm.cmd run supertone:voices -- --language ko --gender female --use-case narration --output output\supertone-ko-female-voices.json
```

Generate one sample:

```powershell
npm.cmd run supertone:tts -- --voice-id voice_xxx --text "종소리를 따라, 천천히 이동하세요." --output ..\..\Assets\Audio\RawCandidates\Narration\ProviderTests\supertone-test.wav
```

For batch tests, put comma-separated IDs in `.env`:

```text
SUPERTONE_VOICE_IDS=voice_xxx,voice_yyy,voice_zzz
```

Then run:

```powershell
npm.cmd run narration:korean-test
```

## Batch Command

The batch command tries all configured providers:

```powershell
cd "C:\Bell Ringer\tools\audio-lab"
npm.cmd run narration:korean-test
```

You can restrict providers:

```powershell
cd "C:\Bell Ringer\tools\audio-lab"
npm.cmd run narration:korean-test -- --providers naver
npm.cmd run narration:korean-test -- --providers typecast,supertone
```

If running from another folder, use `--prefix`:

```powershell
npm.cmd --prefix "C:\Bell Ringer\tools\audio-lab" run narration:korean-test -- --providers naver
```

Without provider keys, the command will skip generation and print the missing configuration.

## Troubleshooting

### `Could not read package.json`

If npm prints an error like:

```text
Could not read package.json: ... C:\Users\skagu\package.json
```

the command was run from the wrong folder. Run it from:

```text
C:\Bell Ringer\tools\audio-lab
```

or use `npm.cmd --prefix "C:\Bell Ringer\tools\audio-lab" ...`.

### NAVER `401 Authentication information are missing`

This means the request reached NAVER, but NAVER did not accept the authentication information.

Check these first:

- `NAVER_CLOVA_VOICE_API_KEY_ID` must be the CLOVA Voice application Client ID from NAVER Cloud Platform, not an account access key.
- `NAVER_CLOVA_VOICE_API_KEY` must be the matching CLOVA Voice application Client Secret.
- The CLOVA Voice service must be subscribed/enabled in NAVER Cloud Platform.
- The URL must match the platform:
  - public NCP: `https://naveropenapi.apigw.ntruss.com/tts-premium/v1/tts`
  - gov/public-sector NCP: `https://naveropenapi.apigw.gov-ntruss.com/tts-premium/v1/tts`
- Do not paste surrounding quotes or hidden spaces into `.env`.

## Production Rule

Do not ship the old Windows TTS samples. They were rejected because the synthetic voice quality breaks the sound-centered experience.

Generated narration must be tested over actual bell, rain, wall, and ambience layers before selection. A voice that sounds good solo may still fail if it becomes intrusive inside the game mix.
