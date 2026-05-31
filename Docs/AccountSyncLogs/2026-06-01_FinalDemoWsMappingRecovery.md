# FinalDemo WS Mapping Recovery - 2026-06-01

Context:
- A previous WS mapping change removed the right-panel horizontal mirror.
- On the physical 16x8 board this made one light split between the far-left area and the middle/right area, and vertical motion appeared to move in opposite directions between panels.

Implemented:
- Restored the D7/right-panel physical X mirror in both active sketches:
  - `Arduino/HeadMpu9250LedBridge/HeadMpu9250LedBridge.ino`
  - `Arduino/BellRingerSerialTemplate/BellRingerSerialTemplate.ino`
- Kept the stable physical convention:
  - D6/left panel: column-major, bottom-to-top.
  - D7/right panel: column-major, mirrored local X, top-to-bottom.
- Changed FinalDemo pad camera-space Z correction so front/back movement is no longer inverted:
  - `PadPoseProvider.invertCameraSpacePositionZ` default is now off.
  - `Assets/Scenes/FinalDemo.unity` also has `invertCameraSpacePositionZ: 0`.

Required QA:
- Re-upload `Arduino/HeadMpu9250LedBridge/HeadMpu9250LedBridge.ino` to the VR/head ESP32-S3 before judging the WS board.
- Sweep a small dot from logical x=0 to x=15 around y=3 or y=4. It should travel continuously from physical left to physical right without jumping or splitting.
- Move a bell light up/down. Both panels should agree on screen direction.
- Move the pad forward/back in FinalDemo. The game-space Z should now match the real pad direction.

Notebook compatibility:
- No serial port, baud rate, or dependency changes were made.
- The WS fix is firmware-side, so each machine uses the corrected mapping only after the head/LED sketch is uploaded.
