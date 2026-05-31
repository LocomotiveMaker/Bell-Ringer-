# Arduino Setup

- VR/head ESP32-S3 upload target: `arduino/HeadMpu9250LedBridge/HeadMpu9250LedBridge.ino`
  - Combines WS2812B LED command handling and MPU9250 head telemetry on one COM port.
  - LED data pins use header `D6` = `GPIO9`, `D7` = `GPIO10`.
  - LED physical mapping is a logical 16x8 board: `D6` is the left 8x8 panel and `D7` is the right 8x8 panel. Both current panels map local X/Y through a physical column mirror and top-to-bottom Y correction.
  - Do not use `HeadMpu9250Tilt.ino` for the VR/head final wiring; it is IMU-only and ignores LED commands.
- Pad ESP32-S3 upload target: `arduino/PadMpu9250Orientation/PadMpu9250Orientation.ino`
  - Sends pad IMU pose telemetry at `230400`. Gamepad vibration is handled by Unity through the connected PC gamepad, not by this Arduino sketch.
- VR/head bridge baud rate: `115200`
- Dedicated pad IMU baud rate: `230400`
- Unity can target a fixed head/LED port with `BELL_RINGER_SERIAL_PORT=COM40`
- Unity can override baud with `BELL_RINGER_SERIAL_BAUD=115200`
- If no device is attached, Unity falls back to keyboard simulation by default
- Current input philosophy:
  - `head`: VR/head bridge sends `hy/hp/hr`; Unity decides which axes drive the virtual camera.
  - `pad`: camera owns `position + yaw`, IMU owns `pitch/roll`

Expected telemetry line format:

```text
hy=0,hp=0,hr=0,wy=0,wp=0,wr=0,btn=0
```

- `hy/hp/hr`: head yaw, pitch, roll
- `wy/wp/wr`: hand yaw, pitch, roll
- `btn`: `0/1`

Recognized Unity debug commands:

```text
PING
OUT vib=0.50 lr=255 lg=180 lb=64 pulse=0.75
LED fill b=31
LED field red=32 green=255 blue=96 level=0.08
LED ripple cx=7.50 cy=3.50 radius=2.00 width=1.30 red=32 green=255 blue=96 level=0.16
LED pulse cx=7.50 cy=3.50 radius=3.20 core=0.65 width=1.10 red=5 green=255 blue=31 level=0.09 contrast=1.80
LED wall cx=7.50 cy=3.50 w=9.00 h=4.00 red=5 green=217 blue=255 level=0.08 seed=123 density=0.45 contrast=1.85
LED rain cx=7.50 cy=0.50 w=16.00 h=2.00 red=5 green=20 blue=255 level=0.07 seed=42 phase=2.25 density=0.45 contrast=1.85
LED tinnitus cx=7.50 cy=3.50 core=0.65 tear=2.50 axisX=1.00 axisY=0.00 red=117 green=13 blue=255 level=0.09 seed=9 instability=0.60 smear=1.35 contrast=1.85
LED clear
```
