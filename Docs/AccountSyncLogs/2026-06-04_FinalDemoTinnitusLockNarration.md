# FinalDemo Tinnitus Lock Narration - 2026-06-04

Context:
- User reported that in the first general tinnitus step, when the player approaches and the position gets locked, the narration
  `Take9-3_소리의 위치에, 패드를 가져다대세요._2026-05-26.wav`
  should play but currently does not.

Findings:
- The requested wav is already mapped in `FinalDemoCueLibrary.asset` to `NarrFindTinnitusPose` (`id: 29`).
- `LockGeneralTinnitusPoseCheck(...)` previously locked movement and reset pose progress but did not queue that narration.
- The same cue was only used later as a fallback hint after timeout, which is why it was missing at the lock moment.

Implemented:
- `Assets/Scripts/FinalDemo/FinalDemoDirector.cs`
  - In `LockGeneralTinnitusPoseCheck(...)`, when `stage == GeneralTinnitusOne`, queue `FinalDemoCueId.NarrFindTinnitusPose` immediately.

Validation:
- Requires runtime QA in `FinalDemo` to confirm the narration is heard when the first tinnitus lock engages.
- No cue remap or asset replacement was needed.

