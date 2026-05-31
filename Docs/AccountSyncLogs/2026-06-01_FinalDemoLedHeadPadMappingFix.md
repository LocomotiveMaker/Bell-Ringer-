# FinalDemo LED/Head/Pad Mapping Fix - 2026-06-01

Superseded:
- The WS right-panel non-mirrored mapping, raised-pad LED mapping, and HeadTilt pitch-stillness change from this note were rolled back later on 2026-06-01.
- For the current WS mapping state, read `2026-06-01_FinalDemoWsMappingRecovery.md`.

Context:
- 사용자님이 WS 보드에서 한 빛이 좌/중앙으로 갈라지고, 상하 이동도 두 패널에서 서로 다른 방향으로 움직이는 중대 매핑 오류를 보고했다.
- 동시에 FinalDemo에서 머리 상하 회전, 패드 z 깊이, 들어올린 패드 LED 위치, 종/패드 외곽 glow가 충분히 보이지 않았다.

Implemented:
- Active VR/head sketch and serial template LED mapping were corrected:
  - `D6`/left panel remains column-major bottom-to-top.
  - `D7`/right panel is now column-major top-to-bottom without horizontal column reversal.
  - This restores the intended logical 16x8 board so Unity preview and physical board should move together.
- `HeadTiltInputProvider` no longer freezes physical pitch during stillness hold by default.
  - Yaw/roll stillness stabilization remains.
  - Head up/down should respond again without sacrificing the existing yaw path.
- `FinalDemoPadSceneVisual` now maps pad camera-space z through a signed depth range.
  - Close/far pad movement should be much more visible on the game monitor.
  - Pad scale also changes with depth.
- Raised-pad LED now uses the pad camera-space pose directly and maps to a lower-board local LED area.
  - It uses `padLightVisibleStartY=0.025` and `padLightVisibleMaxY=0.08` as the intended exhibition lift range.
  - The old view-local y path was removed from LED positioning because it pushed the light upward.
- Bell/pad halos were made larger/brighter and re-enabled every frame in case imported model presentation hides child renderers.

Validation:
- `BellRinger.Runtime.csproj` builds.
- `BellRinger.Editor.csproj` builds.
- `Arduino/HeadMpu9250LedBridge` compiles for `esp32:esp32:esp32s3`.
- `Arduino/BellRingerSerialTemplate` compiles for `esp32:esp32:esp32s3`.

QA focus:
- Re-upload `Arduino/HeadMpu9250LedBridge/HeadMpu9250LedBridge.ino` to the head/LED ESP32-S3 before judging the WS board mapping.
- In FinalDemo, move a bell light horizontally across the center: it should not split between far-left and center.
- Move a bell light vertically: both panels should move in the same logical screen direction.
- Recenter head, then look up/down: `Head virtual yaw/pitch` should show pitch changing and the first-person camera should pitch.
- Raise the pad near `y=0.08m`: amber/yellow LED should appear in the lower region, not upper-left.
- Move the pad closer/farther from the camera: the game pad model should change z position and scale noticeably.
- Bell and pad models should have a faint green/amber outer glow in FinalDemo.

Notebook compatibility:
- No serial port defaults, UDP ports, renderer assets, or scene object positions were changed.
- Hardware behavior changes require re-uploading the head/LED sketch; notebook compatibility remains the same after uploading the same sketch.
