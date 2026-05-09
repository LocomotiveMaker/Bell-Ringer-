# Pad MPU9250 Hideakitai Comparison

This sketch is a comparison path for the pad IMU.

It keeps the Unity telemetry format used by `PadImuReceiver`, but replaces the custom AHRS code with the `hideakitai/MPU9250` library, which is based on Kris Winer's MPU9250 work.

Files:

- [PadMpu9250OrientationHideakitai.ino](</C:/Bell Ringer/Arduino/PadMpu9250OrientationHideakitai/PadMpu9250OrientationHideakitai.ino>)

## Why this exists

The ESP32 repository the user referenced is useful mainly because it treats orientation as `quaternion-first` and relies on a mature MPU9250 orientation path instead of ad-hoc glue code.

That repository is not directly portable to `Arduino Uno`:

- it targets `ESP32`
- it uses a different runtime shape
- its DMP-oriented approach is not a drop-in fit for the current Uno build

This comparison sketch applies the closest practical idea:

- switch to a mature MPU9250 library on AVR
- keep the existing Unity serial protocol
- compare real-world behavior against the custom sketch

## What the sketch does

- runs at `230400 baud`
- uses the hideakitai library's quaternion path
- keeps `wy/wp/wr`, `wqw/wqx/wqy/wqz`, `gx/gy/gz`, `st`, `mf/mc/mp`
- supports recenter and EEPROM calibration persistence

## Commands

- `r` or `recenter`
  - zero the current pose
- `status`
  - print connection and calibration state
- `agcal`
  - run accel/gyro calibration, then save to EEPROM
- `magcal` or `magcal_start`
  - run the full magnetometer calibration immediately, then save to EEPROM
- `magcal_stop`
  - ignored in this sketch
  - unlike the custom sketch, calibration is not split into start/finish phases
- `savecal`
  - save current calibration values to EEPROM
- `loadcal`
  - load EEPROM calibration values
- `clearcal` or `magcal_reset`
  - clear EEPROM calibration
- `fullcal`
  - accel/gyro calibration, then mag calibration

## Recommended test flow

1. Upload this sketch to the `COM10` Uno.
2. Start Unity `PadTrackingTest`.
3. Press `Reconnect IMU`.
4. Press `Reset Mag Cal` once if old custom-sketch expectations are mixed in.
5. Run `agcal` first with the pad fully still.
6. Run `magcal_start` and move the sensor widely on all axes until it finishes.
7. Press `Recenter IMU`.
8. Compare clockwise/counterclockwise behavior against the custom sketch.

## Notes

- `mf=9` is always reported because this library's AHRS path includes magnetometer data.
- `mp=1` means EEPROM calibration was loaded or saved.
- `magcal_start` blocks while calibration is running.
- This sketch is for comparison, not yet the default project IMU path.
