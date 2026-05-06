# Pad MPU9250 Bring-Up

This document is for the very first pad IMU bring-up step:

- wire `MPU9250 -> Uno`
- verify the sensor answers on I2C
- print raw accel/gyro values to Serial

The bring-up sketch is:

- [Mpu9250BringUp.ino](</C:/Bell Ringer/Arduino/Mpu9250BringUp/Mpu9250BringUp.ino>)

## Important Assumption

This wiring assumes a common `MPU9250 breakout board`.

Typical labels are:

- `VCC` or `3V3`
- `GND`
- `SDA`
- `SCL`
- `AD0` or `SDO`
- `CS`, `NCS`, `CSB`, or `NSS`
- optional `INT`
- optional `FSYNC`
- optional `EDA` / `ECL` or `AUX_DA` / `AUX_CL`

## Safest Power Rule

Unless the breakout board explicitly says `VIN` or `5V` is allowed, the safest default is:

- power it from `Uno 3.3V`

The raw MPU-9250 chip is a `3.3V device`.

So for the first bring-up, do **not** assume `5V` is safe.

## Exact Wiring

Use this wiring first.

### Required lines

- `Uno 3.3V` -> `MPU9250 VCC` or `3V3`
- `Uno GND` -> `MPU9250 GND`
- `Uno SDA` or `A4` -> `MPU9250 SDA`
- `Uno SCL` or `A5` -> `MPU9250 SCL`

The Arduino Uno official pinout says I2C/TWI is on:

- `A4 or SDA`
- `A5 or SCL`

### Address pin

- `MPU9250 AD0` or `SDO` -> `GND`

This makes the I2C address:

- `0x68`

If the board is instead tied high, the address becomes:

- `0x69`

The sketch checks both `0x68` and `0x69`, but for first bring-up the recommended wiring is still:

- `AD0 -> GND`

### SPI-disable pin

If the board has a pin labeled:

- `CS`
- `NCS`
- `CSB`
- `NSS`

connect it to:

- `3.3V`

Reason:

- we are using `I2C`, not `SPI`
- tying `CS` high avoids accidental SPI selection

### Leave these unconnected for now

- `INT`
- `FSYNC`
- `EDA` / `ECL`
- `AUX_DA` / `AUX_CL`

They are not needed for raw accel/gyro bring-up.

## Wiring Summary Table

| Uno | MPU9250 |
|---|---|
| `3.3V` | `VCC` or `3V3` |
| `GND` | `GND` |
| `SDA` or `A4` | `SDA` |
| `SCL` or `A5` | `SCL` |
| `GND` | `AD0` or `SDO` |
| `3.3V` | `CS` / `NCS` / `CSB` / `NSS` |

## If The Breakout Has `VIN` Instead Of `VCC`

Some modules expose:

- `VIN`
- `GND`
- `SDA`
- `SCL`

If the board clearly documents that `VIN` accepts `5V`, then `Uno 5V -> VIN` may be valid.

But if the board labeling is not clear, the safer choice is:

- stop and confirm the exact module marking first

For this reason, the safest universal first instruction is still:

- use the module's `3.3V` input if available

## Upload

Build the sketch:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Build-ArduinoSketch.ps1 -SketchPath Arduino\Mpu9250BringUp
```

Then upload it from Arduino IDE or `arduino-cli` as the user prefers.

## What The Sketch Does

On startup it:

1. starts Serial at `115200`
2. starts I2C at `400kHz`
3. scans the I2C bus
4. checks `0x68` and `0x69`
5. reads `WHO_AM_I`
6. initializes accel/gyro
7. prints raw values every `20ms`

## Expected Serial Output

Success looks like this:

```text
[MPU9250] bring-up start
[MPU9250] I2C device found at 0x68
[MPU9250] Found candidate at 0x68 WHO_AM_I=0x71
[MPU9250] initialized at 0x68
[MPU9250] printing raw accel/gyro every 20 ms
raw ax=... ay=... az=... gx=... gy=... gz=... tempC=...
```

## What The User Should Physically Observe

When the board lies flat and still:

- one accel axis should sit near `+1g` or `-1g`
- the other two accel axes should be closer to `0g`
- gyro values should hover near `0`

When the user rotates the board by hand:

- gyro values should change sharply
- accel values should shift with orientation

## If It Fails

### Case 1: no device at `0x68` or `0x69`

Check in this order:

1. `VCC`
2. `GND`
3. `SDA`
4. `SCL`
5. `AD0`
6. `CS`

Most first failures are one of those six lines.

### Case 2: I2C device is found but init fails

Likely causes:

- unstable power
- wrong module voltage
- wrong pin labeling

### Case 3: values are frozen

Likely causes:

- bad SDA/SCL contact
- module browning out
- wrong power pin choice

## Next Step After Success

Once the user sees stable raw numbers, the next step is:

- orientation fusion
- yaw/pitch/roll output
- recenter support
