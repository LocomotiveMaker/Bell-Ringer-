# Arduino Setup

- Default baud rate: `115200`
- Unity can target a fixed port with `BELL_RINGER_SERIAL_PORT=COM9`
- Unity can override baud with `BELL_RINGER_SERIAL_BAUD=115200`
- If no device is attached, Unity falls back to keyboard simulation by default

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
LED ripple cx=7.50 cy=3.50 radius=2.00 width=1.30 red=32 green=255 blue=96 level=0.16
LED wall cx=7.50 cy=3.50 w=9.00 h=4.00 red=128 green=128 blue=128 level=0.12 seed=123
LED rain cx=7.50 cy=1.50 w=15.00 h=3.00 red=120 green=171 blue=255 level=0.14 seed=42 phase=2.25
LED clear
```
