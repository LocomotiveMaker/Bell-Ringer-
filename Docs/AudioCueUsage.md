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
