# FinalDemo Audio/Rain/Forest Pass - 2026-05-31

Context:
- User asked to implement bundle 1/2/3 immediately: Asset Store rain/forest prep, audio flow repair, and rain/forest environment replacement.
- Do not change authored bell follow positions, general tinnitus positions, boss position, or boss weakpoint path positions.

Completed:
- Opening ambience is now treated as a persistent bed from `OpeningAmbience` through `BossDefeat`.
- `ForestEnding` explicitly stops the opening ambience and fades in `ForestBed`.
- Rain audio now begins when `BellFollowRain` starts, including the relocation from the first bell target to the second bell target.
- Rain focus narration delay default is now 3 seconds after rain starts.
- Added delayed narration queue support.
- Added new cue ids and cue library entries for:
  - `NarrBellEscaped` -> `Take12-1_종이 달아났습니다. 다시 한번 따라가세요._2026-05-31.wav`
  - `NarrNoiseStillExists` -> `Take13-3_소음은 아직 존재합니다. 계속 앞으로 나아가세요._2026-05-31.wav`
  - `NarrBossAhead` -> `Take14-1_거대한 소음이 앞에 존재합니다._2026-05-31.wav`
- Added runtime broad rain presentation with `FinalDemoWorldRainEnvironment`.
- Added runtime forest presentation with `FinalDemoForestEnvironment`, using local `ObserverAssets/Forest` resources until the Asset Store package is imported.
- Added broad wet-looking world floor with `FinalDemoWorldFloorSurface`.
- Runtime startup removes legacy authoring objects:
  - `RainSkySheet`
  - `RainFogVolume`
  - `RainSkyParticles`
  - `RainGroundRippleParticles`
  - `ClearBlueSky_Forest_Authoring`
  - `BackWall_White_Authoring`
  - `LeftSoftWall_Authoring`
  - `RightSoftWall_Authoring`
  - `Ground_Grey_Authoring`
- `FinalDemoPolishTool` and `BellRingerFinalDemoSceneBuilder` were updated so future generated/polished scenes use the new runtime rain/forest/floor structure.

Asset Store status:
- The Rain Maker and Low Poly Environment Nature Free pages are reachable.
- Direct `.unitypackage` download is gated by Unity Asset Store login / Package Manager ownership and is not exposed as a plain public URL.
- Current implementation therefore uses existing local rain/forest resources and is ready to swap visuals after those packages are added through Unity Package Manager.

Validation:
- `BellRinger.Runtime.csproj` builds.
- `BellRinger.Editor.csproj` builds.
- Existing warnings remain from `PadTrackingReceiver.PadTrackingPacket` and `USG0001`.
- Unity batch scene polish did not complete because an existing Unity editor process was open; runtime cleanup still removes legacy objects during Play.

Notebook compatibility:
- No COM port, runtime path, or hardware default changes.
- The new rain/forest/floor systems are plain Unity particle/mesh runtime objects and should remain notebook-compatible.
