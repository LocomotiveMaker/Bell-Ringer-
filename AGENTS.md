# AGENTS.md

- Unity version is `2022.3.9f1`; keep changes compatible with this LTS version.
- Build for a strong MVP first: stable audio, light, vibration, and serial interaction matter more than content quantity.
- Prefer simple, reliable mechanics over fragile tracking-heavy systems.
- Keep player-facing design readable without normal vision; monitor visuals are secondary.
- Organize Unity C# under `Assets/Scripts/{Core,Hardware,Gameplay,Audio,Debug}` and place Arduino code under `arduino/`.
