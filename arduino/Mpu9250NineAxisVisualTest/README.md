# MPU9250 Nine-Axis Visual Test

This is a fresh MPU9250 test sketch and a local browser visualizer. It reads
the MPU9250 directly with `Wire`, calibrates gyro bias and magnetometer
hard/soft iron error, then uses quaternion gyro integration with accelerometer
roll/pitch correction and magnetometer yaw-only correction. Yaw-only correction
keeps a bad magnetic sample from tipping the cube into the wrong axis.

## Wiring

```text
Uno 3.3V  -> MPU9250 VCC
Uno GND   -> MPU9250 GND
Uno A4    -> MPU9250 SDA
Uno A5    -> MPU9250 SCL
Uno GND   -> MPU9250 AD0/SDO
Uno 3.3V  -> MPU9250 NCS
```

MPU9250 logic is 3.3 V. Use a breakout that already has level shifting, or
make sure SDA/SCL are pulled up to 3.3 V, not 5 V.

## Upload

Upload `Mpu9250NineAxisVisualTest.ino` to an Arduino Uno at `115200` baud.

Startup sequence:

1. Keep the sensor still during `CAL,gyro,hold_still`.
2. If no magnetometer calibration is stored, rotate the module through every
   direction for 25 seconds during `CAL,mag,rotate_all_axes`.
3. After `INFO,orientation_initialized`, the sketch streams lines like:

```text
Q,0.999900,0.001000,0.002000,0.003000,YPR,12.50,-1.25,3.90,M,18.20,-31.40,42.10,G,0.000,0.000,0.000,F,54.2,MAP,0,CAL,1,MAG,1,STILL,1
```

## Visualizer

Open `visualizer.html` in Chrome or Edge. If the browser blocks Web Serial from
`file://`, serve this folder locally:

```powershell
cd "C:\Bell Ringer\arduino\Mpu9250NineAxisVisualTest"
python -m http.server 8765
```

Then open:

```text
http://localhost:8765/visualizer.html
```

Buttons:

- `Connect`: select the Uno serial port.
- `Mag Cal`: sends `c`; rotate all axes for 25 seconds. Calibration is saved to
  EEPROM.
- `Gyro Zero`: sends `g`; keep the sensor still while gyro bias is recalculated.
- `Reset Pose`: sends `r`; resets the current orientation from gravity and
  magnetometer.
- `Mag Axis`: sends `m`; changes the AK8963-to-MPU axis map, clears the saved
  magnetometer calibration, and requires a new `Mag Cal`.
- `Erase Cal`: sends `x`; removes saved magnetometer calibration.

## Fixing Bad Yaw

If rotating the board around the vertical yaw axis does not move yaw, moves only
a little, or leaks into roll/pitch, the magnetometer axes are probably not
aligned with the accel/gyro axes for that breakout board.

Use this loop:

1. Click `Mag Axis`.
2. Click `Mag Cal` and rotate the sensor through every orientation for the full
   countdown.
3. Click `Reset Pose`.
4. Keep the sensor roughly flat and rotate it 90 degrees around vertical.
5. If yaw is still wrong, repeat with the next `Mag Axis` map.

The correct map is the one where yaw changes strongly and in the expected
direction while roll/pitch stay mostly stable during a flat turn.

## Drift Notes

The filter corrects pitch/roll from gravity and yaw from the magnetometer, and
it slowly learns residual gyro bias while the board is stationary. Clean yaw
still requires a good magnetometer calibration and a magnetic environment
without strong nearby metal, speakers, motors, or current-carrying wires. If the
cube rotates slowly while stationary, redo `Gyro Zero`; if yaw is wrong or jumps
near objects, redo `Mag Cal` away from magnetic interference.
