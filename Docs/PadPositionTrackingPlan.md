# Pad Position Tracking Plan

## Goal

Implement gamepad position tracking before the MPU9250 rotation sensor arrives.

This prototype should:

- measure pad position with a phone camera and a PC
- send live position data into Unity
- stay compatible with the later `MPU9250 = rotation`, `vision = position` split
- avoid overbuilding

## Short Answer

Yes. For a first position prototype, the user only needs:

- a phone that can stream live video to the PC
- a visible marker attached to the pad
- a PC-side tracker
- a Unity receiver

However, this is true only for `position-first prototype` scope.

It is not enough by itself for a final robust controller if the user also wants:

- reliable rotation in all occlusion cases
- fast motion under blur
- stable tracking when the pad is partially hidden

That is why the clean final split is:

- `vision` handles position
- `MPU9250` handles rotation

## Recommended Architecture

The most practical path is marker-based vision tracking.

### Hardware

- Attach a printed marker board to the pad.
- Fix the phone in space so it looks at the play area.
- Stream the phone camera to the PC.

### PC-side tracker

- Receive video from the phone.
- Detect the marker board with OpenCV ArUco.
- Estimate marker or board pose.
- Convert the result into a stable game-space position.
- Send the result to Unity over UDP.

### Unity-side receiver

- Receive UDP packets.
- Update a `PadPositionProvider` runtime object.
- Feed that position into debug UI and gameplay code.

### Final hybrid

Later, when the MPU9250 arrives:

- position comes from camera tracking
- rotation comes from MPU9250
- Unity merges both into one controller pose

## Why This Path Is Best

This is better than phone-IMU-only tracking for the current project because:

- the user already plans to dedicate MPU9250 to orientation
- camera position tracking does not drift like IMU integration
- ArUco is a standard, documented, low-risk prototype path
- the phone does not need to be attached to the pad

This is better than full SLAM or ARCore-first integration because:

- it is much faster to build
- debugging is simpler
- the coordinate system is under the user's control
- failure cases are obvious: blur, occlusion, bad lighting, marker too small

## Marker Recommendation

Use an ArUco board, not a single tiny marker.

Recommended first prototype:

- 2 to 4 markers on one rigid flat board
- printed in black on matte white paper
- each marker side length around `4 cm to 6 cm`
- board mounted firmly on the pad

Why a board is better:

- more stable than one marker
- less pose jitter
- more robust when one corner is briefly hidden
- easier later to use `estimatePoseBoard`

## Phone Streaming Options

The tracker only needs one of these:

- phone feed exposed to the PC as a webcam device
- phone feed exposed as RTSP or MJPEG URL
- phone feed routed through OBS Virtual Camera

For fastest start, the easiest path is:

1. phone camera -> VDO.Ninja
2. VDO.Ninja -> OBS
3. OBS Virtual Camera -> OpenCV capture

This is not the lowest-latency final path, but it is fast to stand up.

If the user wants lower latency later, move to:

- direct RTSP or MJPEG ingest into OpenCV

## Coordinate Strategy

Do not start with full 3D gameplay coordinates.

Start with a controlled play-space contract:

- `x`: left/right on desk or play area
- `z`: forward/back from player
- `y`: fixed or lightly derived

Recommended first version:

- treat the pad as moving on an approximate 2D plane
- keep `y` fixed
- use only `x` and `z`

That gives a usable gameplay prototype much faster.

After that:

- enable full pose estimation with camera calibration
- add `y` only if the game actually needs vertical pad movement

## Development Phases

### Phase 1: Camera Feed

Success criteria:

- phone video appears on the PC
- PC-side script can read frames at runtime

Tasks:

- choose one streaming path
- lock phone position
- confirm stable frame rate and lighting

### Phase 2: Marker Detection

Success criteria:

- marker IDs are detected reliably
- tracked center moves correctly on screen

Tasks:

- print board
- detect ArUco markers
- draw debug corners and center

### Phase 3: Position Estimation

Success criteria:

- movement left/right and near/far maps correctly into numbers
- no major jitter while stationary

Tasks:

- calibrate camera if using metric pose
- estimate board pose
- add smoothing
- define world origin and scale

### Phase 4: Unity Integration

Success criteria:

- Unity receives live position
- debug object follows the pad

Tasks:

- add UDP receiver in Unity
- add runtime status text
- spawn a visible debug target in scene

### Phase 5: Merge With MPU9250

Success criteria:

- position from camera and rotation from MPU9250 coexist
- one combined pad pose is available to gameplay

Tasks:

- keep both data paths independent
- timestamp packets
- combine data in a single controller state object

## Data Contract

Use a tiny JSON packet first.

Example:

```json
{
  "t": 1710000000.125,
  "visible": true,
  "x": 0.18,
  "y": 0.00,
  "z": 0.42,
  "confidence": 0.93
}
```

Later add:

```json
{
  "yaw": 0.0,
  "pitch": 0.0,
  "roll": 0.0
}
```

but only after the MPU9250 path is ready.

## Practical Risks

### 1. Motion blur

Fast swinging can destroy marker detection.

Mitigation:

- use bright lighting
- use a larger marker board
- prefer higher shutter speed if the camera app allows it

### 2. Occlusion

The user's hand may cover the marker.

Mitigation:

- mount the board on the outer face of the pad
- use multiple markers on one board

### 3. Perspective distortion and lens distortion

This causes unstable metric pose.

Mitigation:

- calibrate the camera
- keep the phone fixed
- do not demand true centimeter accuracy in the first pass

### 4. Latency

Wireless video adds delay.

Mitigation:

- first accept it for prototype work
- later switch to lower-latency transport if needed

## Immediate Implementation Recommendation

The best next implementation step is:

1. fixed phone camera feed into PC
2. ArUco board on pad
3. Python OpenCV tracker
4. UDP JSON into Unity
5. Unity debug sphere that follows received position

This is the minimum useful system.

## What Not To Do First

Do not start with:

- SLAM
- full ARCore/ARKit app integration
- sensor fusion before position alone works
- 6DoF perfection
- phone-mounted tracking as the only source

Those are valid later, but they are wrong for the current milestone.

## Concrete Next Build Order

If the user wants to start implementation immediately, the build order should be:

1. create printable marker board image
2. create PC tracker script in Python
3. create Unity UDP receiver and debug scene object
4. verify live left/right and near/far motion
5. add smoothing and confidence handling
6. wait for MPU9250, then merge rotation

## References

- OpenCV ArUco marker detection and pose estimation:
  https://docs.opencv.org/trunk/d9/d6a/group__aruco.html
- OpenCV board and single-marker pose APIs:
  https://docs.opencv.org/4.x/d9/d6a/group__aruco.html
- OpenCV camera calibration:
  https://docs.opencv.org/4.x/dc/dbb/tutorial_py_calibration.html
- OpenCV `solvePnP` pose computation:
  https://docs.opencv.org/4.x/d5/d1f/calib3d_solvePnP.html
- VDO.Ninja phone camera into webcam workflow:
  https://docs.vdo.ninja/getting-started/mobile-phone-camera-into-webcam
