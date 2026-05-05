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
LED field red=32 green=255 blue=96 level=0.08
LED ripple cx=7.50 cy=3.50 radius=2.00 width=1.30 red=32 green=255 blue=96 level=0.16
LED pulse cx=7.50 cy=3.50 radius=3.20 core=0.65 width=1.10 red=5 green=255 blue=31 level=0.09 contrast=1.80
LED wall cx=7.50 cy=3.50 w=9.00 h=4.00 red=5 green=217 blue=255 level=0.08 seed=123 density=0.45 contrast=1.85
LED rain cx=7.50 cy=0.50 w=16.00 h=2.00 red=5 green=20 blue=255 level=0.07 seed=42 phase=2.25 density=0.45 contrast=1.85
LED tinnitus cx=7.50 cy=3.50 core=0.65 tear=2.50 axisX=1.00 axisY=0.00 red=117 green=13 blue=255 level=0.09 seed=9 instability=0.60 smear=1.35 contrast=1.85
LED clear
```
