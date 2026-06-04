# FinalDemo Bell Hardware Revert - 2026-06-04

Context:
- User clarified that the earlier bell green-light request referred to the monitor preview, not the WS LED board.
- Requirement: keep the monitor bell bloom improvement, but return WS-board bell output to the state before that instruction.

Implemented:
- `Assets/Scripts/FinalDemo/FinalDemoLightRouter.cs`
  - Added a separate `bellHardwareColor` using the older WS-board green.
  - Restored the older direct hardware fallback commands:
    - bell point mini-ripple uses `SendLedRipple(...)` again
    - bell point/anchor pulse values reverted
    - bell wave hardware radius/core/width/contrast reverted
  - Left the logical preview rendering on the newer bloom-style bell frame.

Validation:
- Requires runtime QA in FinalDemo to confirm:
  - monitor preview still shows the softer bloom-style bell
  - WS board bell output matches the earlier pre-bloom-hardware behavior

