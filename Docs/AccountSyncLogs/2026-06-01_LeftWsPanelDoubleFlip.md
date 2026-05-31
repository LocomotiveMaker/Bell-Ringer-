# Left WS Panel Double Flip - 2026-06-01

Context:
- After rolling back the pad-light-related changes, the sample scene showed the left WS panel behaving as if both horizontal and vertical axes were reversed.
- The right panel already used the mirrored physical column plus top-to-bottom Y mapping.

Implemented:
- Updated the left/D6 panel mapping in both active sketches:
  - `Arduino/HeadMpu9250LedBridge/HeadMpu9250LedBridge.ino`
  - `Arduino/BellRingerSerialTemplate/BellRingerSerialTemplate.ino`
- Left/D6 now maps `localX/localY` to `(7 - localX) * 8 + (7 - localY)`.
- Right/D7 was left unchanged.

Required QA:
- Re-upload `Arduino/HeadMpu9250LedBridge/HeadMpu9250LedBridge.ino` to the VR/head ESP32-S3.
- In the sample scene, place a dot on the left panel and move it right/up. The physical left panel should now move right/up instead of left/down.
- Then sweep across the center seam. If the two panels now disagree at the seam, only the panel-side convention should be adjusted next, not the Unity light mapper.

Notebook compatibility:
- No Unity scene, port, baud, or dependency settings were changed.
- This is a firmware mapping change and only applies after uploading the corrected sketch.
