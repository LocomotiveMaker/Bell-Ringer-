# Pad MPU9250 Integration Plan

## 1. Scope

This plan is for the `pad MPU9250`, not the head unit.

Current position tracking is already good enough to move forward:

- ArUco V-board works
- phone camera path works
- Unity UDP receiver works
- position is stable enough for the next stage

So the next priority is:

- add `pad rotation`
- merge `vision position + MPU rotation`

## 2. Final Architecture

Use this split:

- `vision tracker` owns `pad position`
- `vision tracker` also owns `pad yaw` when two-marker pose is available
- `MPU9250` owns `pad pitch/roll`
- `MPU yaw` is fallback/debug only
- `Unity` owns the final merged pose

This is still the correct architecture for the pad.

For the head path, do **not** chase real yaw with the MPU9250.
Use a separate `head tilt` provider:

- `physical head pitch -> virtual pitch`
- `physical head roll -> virtual yaw`
- `physical head yaw` ignored

## 3. Important Design Decision

Do **not** try to reuse the LED board serial path as-is for the pad IMU.

Reason:

- the current LED hardware path already uses its own serial connection
- the pad MPU will be on a separate Uno
- position tracker already arrives over UDP
- mixing all of this into one existing hardware bridge would create avoidable coupling

Recommended structure:

- `PadTracker` process keeps sending position over UDP
- `PadImuReceiver` in Unity reads pad rotation from a separate COM port
- `PadPoseProvider` merges both into one pad pose

The existing telemetry naming can still be reused conceptually:

- `handYaw`
- `handPitch`
- `handRoll`

But the receiver should be separate from the LED bridge.

## 4. Rotation Strategy

The fastest correct path is:

### Phase A: pad pitch/roll from 6-axis IMU, yaw from camera

Use:

- gyroscope
- accelerometer

And add:

- manual recenter

This gives:

- stable pad pitch
- stable pad roll
- yaw with no long-term drift while camera pose is available

This is the best implementation because:

- it is much simpler
- it avoids immediate magnetometer calibration pain
- it prevents bad IMU yaw from polluting the other axes

### Phase B: magnetometer experiments only if needed

Use the MPU9250 magnetometer later only if:

- the camera yaw path is unavailable for the final pad build
- a short fallback yaw is still needed

Do not make the head path depend on magnetometer yaw.

## 5. Arduino-Side Plan

The Uno on the pad should do this:

1. initialize MPU9250 over I2C
2. read accel + gyro at a stable loop rate
3. run sensor fusion
4. expose rotation over USB serial
5. support recenter command

Recommended output rate:

- target `100 Hz`
- acceptable first milestone `60 Hz`

Recommended serial format:

```text
wy=12.4,wp=-5.1,wr=3.8,btn=0
```

Or if the implementation is simpler:

```text
0,0,0,12.4,-5.1,3.8,0
```

The second format matches the current telemetry field order better.

For pad-only integration, the cleanest mapping is:

- `headYaw/headPitch/headRoll = 0`
- `handYaw/handPitch/handRoll = pad rotation`
- `buttonPressed = recenter or test button`

## 6. Unity-Side Plan

Add three pieces:

### A. Pad IMU serial receiver

Responsibilities:

- open the pad Uno COM port
- read serial lines
- parse yaw/pitch/roll
- expose freshness, last update time, last raw line, and current rotation

This should be a separate component from `HardwareBridge`.

### B. Pad pose merger

Responsibilities:

- read position from [PadTrackingReceiver.cs](</C:/Bell Ringer/Assets/Scripts/Gameplay/PadTrackingReceiver.cs:1>)
- read rotation from the new IMU receiver
- create one final pose:
  - `position = vision`
  - `rotation = imu`

### C. Debug scene/controller updates

Show at least:

- position confidence
- position freshness
- IMU freshness
- yaw/pitch/roll
- merged pad object
- recenter state

## 7. Calibration Plan

Use two different calibration concepts.

### Position calibration

Already mostly covered by the camera path:

- camera fixed
- pad center reset

### Rotation calibration

Need a separate reset flow:

1. user places pad in neutral pose
2. user presses `Recenter`
3. current yaw/pitch/roll becomes gameplay zero

This is mandatory.

Do not rely on the raw MPU orientation matching Unity axes automatically.

Also expect one axis-alignment pass:

- swap axis order if needed
- invert signs if needed
- confirm `yaw`, `pitch`, `roll` match real movement

## 8. Acceptance Criteria

The MPU phase is complete enough to continue when all of these are true:

1. pad pitch forward/back changes correctly in Unity
2. pad roll left/right changes correctly in Unity
3. pad yaw rotates consistently after recenter
4. merged pose object keeps vision position while following IMU rotation
5. brief IMU packet loss does not cause violent snapping
6. user can press one action to recenter rotation at any time

## 9. Recommended Implementation Order

### Stage 1: bring-up

- wire MPU9250 to Uno
- verify raw readings
- verify serial output

Success:

- serial monitor shows changing accel/gyro values

### Stage 2: fusion

- add orientation fusion
- output yaw/pitch/roll
- add recenter command

Success:

- serial values move correctly with hand rotation

### Stage 3: Unity receiver

- add dedicated COM receiver
- parse incoming lines
- show live yaw/pitch/roll in a test component

Success:

- Unity receives stable numbers from the pad Uno

### Stage 4: merge

- combine IMU rotation with current vision position
- drive one debug pad object

Success:

- one object in Unity follows both position and rotation together

### Stage 5: pad-mounted test

- mount V-board and MPU on the real pad
- test actual grip, occlusion, and recenter

Success:

- gameplay-like use feels coherent

## 10. Practical Work Bundles

These are the concrete work bundles to execute.

### Bundle 1: pad IMU hardware bring-up

- wire MPU9250 to pad Uno
- confirm power and I2C response
- print raw accel/gyro to serial

Deliverable:

- stable serial stream from the Uno

### Bundle 2: orientation firmware

- add fusion
- add yaw/pitch/roll output
- add recenter input/command

Deliverable:

- usable pad rotation serial protocol

### Bundle 3: Unity IMU receiver

- new `PadImuReceiver`
- COM config
- freshness / error reporting

Deliverable:

- Unity can show pad yaw/pitch/roll live

### Bundle 4: merged pose layer

- new `PadPoseProvider`
- merge UDP position and IMU rotation
- last-good handling

Deliverable:

- one merged pose for gameplay use

### Bundle 5: debug and operator flow

- recenter button
- status UI
- test scene updates

Deliverable:

- operator can recover alignment quickly during setup

## 11. What Should Not Be Done Yet

Do not do these before the first merged pose works:

- magnetometer hard/soft iron calibration workflow
- quaternion networking format
- dual-MPU head + pad merge in the same task
- replacing the current position tracker
- deep refactor of `HardwareBridge`

## 12. Final Recommendation

The next implementation should be:

1. actual pad-mounted ArUco test
2. pad MPU9250 serial bring-up
3. Unity IMU receiver
4. merged pad pose
5. only then decide whether yaw drift requires magnetometer work

That is the shortest path to a playable combined input system.
