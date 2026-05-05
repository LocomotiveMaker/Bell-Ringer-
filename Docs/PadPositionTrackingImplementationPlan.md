# Pad Position Tracking Implementation Plan

## 1. What This Plan Is Optimizing For

This plan reflects the user's stated priorities:

- first milestone is `live position tracking`, but not a throwaway prototype
- target quality is `directly usable for the real game`
- preferred error is `<= 5 cm`
- preferred latency is `as low as possible`, with `30 ms` as the desired ideal
- current hardware assumption is:
  - `Galaxy S21`
  - `fixed camera placement`
  - `wired connection preferred`
  - `MPU9250 arrives tomorrow and will own rotation later`

## 2. Hard Reality Check

Two targets here need to be separated:

### Position accuracy target

`<= 5 cm` is realistic in a fixed, calibrated setup if:

- the camera is calibrated
- the marker board is rigid
- the board is large enough
- lighting is acceptable
- the marker is visible

This is achievable.

### End-to-end latency target

`<= 30 ms` is not something I would promise with a `phone -> PC -> OpenCV -> Unity` stack.

The problem is not OpenCV itself. The problem is the entire chain:

- phone camera sensor exposure
- USB streaming / app encoding path
- frame acquisition on PC
- marker detection
- packet send
- Unity receive and render

For this hardware path, `30 ms` is an ideal target, not a reliable promise.

The realistic planning stance should be:

- target: `stable and low latency`
- expected first real result: `roughly 45 ms to 90 ms`
- push lower only after measurement

If later measurement shows that the phone path cannot get low enough, the correct fix is not more software complexity. The correct fix is switching the camera source to a low-latency USB camera.

## 3. Final Architecture Decision

The best architecture for the user's stated goals is:

- `camera vision = position`
- `MPU9250 = rotation`
- Unity merges them

For the current phase, implement only:

- camera-based pad position

For the next phase, add:

- MPU9250 rotation

This split is correct because:

- vision gives drift-free position
- MPU9250 gives orientation even when the marker is partly not visible
- the user's biggest risk, marker invisibility under rotation, is reduced when rotation is not dependent on the camera

## 4. The Main Risk and the Correct Answer

The user's most important concern was:

`What if the marker is not visible because the pad rotates away from the camera?`

This concern is valid.

A single flat front-facing marker is not enough for the final design.

### Recommended solution

Use a `small rigid multi-face marker board`, not one flat marker.

Recommended shape:

- a shallow `V` board attached to the front charging edge of the pad
- two visible faces angled outward
- optional third small top face if needed

### Recommended first board geometry

- left face: `5 x 5 cm`
- right face: `5 x 5 cm`
- angle between faces: about `70° to 100°`
- unique marker ID on each face
- rigid black/white printed plate mounted on plastic/foam board

### Why this works

- when one face rotates away, the other face often remains visible
- ArUco does not get “confused” if IDs are unique and board geometry is known
- later, the board pose can be estimated from whichever markers are visible

### Important detail

Do not put identical markers on multiple faces.

Use:

- unique IDs
- one known board layout
- one known 3D geometry model

That is standard practice.

## 5. Camera and Transport Decision

The user wants wired transport and performance.

### What should not be used for the real path

Do not build the real path around:

- VDO.Ninja
- OBS Virtual Camera
- browser relay

Those are fine for quick experiments, but not for the user's performance target.

### Recommended capture path

Use the phone as a USB video source with the least extra software layers possible.

Because the user's phone is `Galaxy S21`, I would not assume native USB webcam support exists and works well enough. The safer plan is:

- try `DroidCam` over `USB`
- capture directly in OpenCV from the resulting device or stream

If this path measures too slow, the fallback should be:

- replace the phone camera with a direct low-latency USB webcam

This is an important decision:

- software cannot guarantee sub-30 ms if the video source path is already too slow

## 6. Position Model

The user requested:

- high-resolution `x`
- high-resolution `y`
- coarse `z`

That fits the camera geometry well if the phone is placed almost in front of the user.

### Recommended pose output

Track the full 3D board pose internally:

- `x_cam`
- `y_cam`
- `z_cam`

But expose it to Unity as:

- precise `x`
- precise `y`
- smoothed / lower-confidence `z`

### Why

With a frontal camera:

- horizontal and vertical image motion are strong and stable
- depth is always weaker and noisier

So the correct design is:

- full 3D estimate in the tracker
- treat `z` as lower-confidence
- smooth `z` more aggressively than `x` and `y`

## 7. Workspace Assumptions from the User

Working range to design for:

- horizontal: about `+-35 cm`
- vertical: from desk surface upward to around `50 cm`
- forward/back: about `70 cm to 80 cm`

This means the camera must see a fairly large volume.

### Placement recommendation

Mount the camera:

- fixed
- centered in front of the user
- slightly above pad resting height
- far enough back to see the full play volume without wide-angle distortion becoming extreme

Practical starting distance:

- around `1.2 m to 1.8 m` from the pad rest area

The exact value should be chosen after checking frame coverage.

## 8. Coordinate System Decision

The user chose camera-based coordinates for now.

That is good for the first implementation.

### Recommended internal coordinate handling

- tracker outputs camera-space coordinates
- Unity stores them as pad camera-space position

### Recommended gameplay handling

Add a `center reset` action:

- current pad pose becomes gameplay center

This gives:

- stable internal math
- easy setup during exhibition
- flexibility later if the origin strategy changes

## 9. Calibration Strategy

The right split is:

### One-time or infrequent intrinsic calibration

Do this during development or installation:

- print a ChArUco or chessboard calibration sheet
- capture `15 to 25` frames
- solve camera intrinsics
- save camera matrix and distortion coefficients

This is required if the user truly wants the `<= 5 cm` target.

### Fast per-install extrinsic / play-space calibration

Do this at exhibition setup:

1. camera is fixed
2. place pad at center rest pose
3. press calibrate
4. Unity stores that as the neutral reference

This should take only a few seconds.

### Do not ask the exhibition visitor to do camera calibration

Only the operator should do setup calibration.

## 10. Tracking Algorithm Recommendation

Use this order:

### Detection

- detect ArUco markers
- refine corners if available

### Pose

- create a rigid board model with known marker locations
- compute board image points from visible markers
- solve pose with `solvePnP`

### Filtering

- keep last valid pose
- smooth position with asymmetric filtering
- smooth `z` more than `x` and `y`
- keep last valid pose when temporarily lost

### Loss behavior

The user requested `last position hold`.

So loss behavior should be:

- if tracking is briefly lost: keep last pose
- confidence drops but position does not snap away
- after longer loss duration: mark as stale internally

## 11. Recommended Software Components

### PC tracker app

Build a standalone Python tracker first.

Responsibilities:

- capture camera frames
- detect markers
- estimate pose
- smooth result
- send UDP packets to Unity
- show debug overlay window

### Unity runtime receiver

Responsibilities:

- receive UDP packets
- store current pad position
- show tracking state and confidence
- move a debug object in scene

### Later sensor merger

Responsibilities:

- read position from tracker
- read rotation from MPU9250 path
- combine into one pad pose

## 12. Packet Format

Use a minimal UDP JSON packet first.

Example:

```json
{
  "t": 1710000000.125,
  "visible": true,
  "confidence": 0.91,
  "x": 0.182,
  "y": 0.264,
  "z": 0.537
}
```

Later, after MPU9250 integration:

```json
{
  "t": 1710000000.125,
  "visible": true,
  "confidence": 0.91,
  "x": 0.182,
  "y": 0.264,
  "z": 0.537,
  "yaw": -12.4,
  "pitch": 5.7,
  "roll": 22.1
}
```

But do not add rotation to this path until the MPU is connected.

## 13. Concrete Performance Strategy

To maximize the chance of good latency and accuracy:

### Capture settings

- prefer `1080p`
- prioritize higher real frame rate over extreme resolution
- do not use 4K or 8K for the first implementation

Recommended starting point:

- `1920 x 1080`
- `60 fps`

Reason:

- enough pixels for markers
- better latency than ultra-high resolution
- easier CPU budget for OpenCV

### Marker sizing

Since the user can attach a plate:

- use a rigid board width around `10 cm to 12 cm`
- not a tiny single `5 cm` marker alone

### Lighting

- strong and even lighting helps more than algorithm tricks
- avoid reflective lamination
- matte print only

### Filtering

- use small smoothing on `x/y`
- use stronger smoothing on `z`
- do not overfilter or motion will lag

## 14. What I Recommend the User Build First

This is the exact order I recommend.

### Stage 1: Physical test rig

Build:

- phone fixed in place
- pad marker board attached
- one printed calibration board

Success condition:

- camera sees the full intended play volume

### Stage 2: PC tracker only

Build:

- Python OpenCV viewer
- marker detection overlay
- board center and confidence text

Success condition:

- user can move the pad and see a stable tracked position in the debug window

### Stage 3: Metric pose and calibration

Build:

- camera calibration save/load
- board pose estimation
- smoothing

Success condition:

- position noise is low enough and stationary error looks credible

### Stage 4: Unity integration

Build:

- UDP receiver
- debug sphere / gizmo
- runtime UI with visible, confidence, x/y/z, age

Success condition:

- Unity object follows the pad in real time

### Stage 5: Loss handling

Build:

- last-valid hold
- stale timeout
- confidence-based UI

Success condition:

- temporary marker loss does not cause ugly snapping

### Stage 6: MPU9250 merge

Build:

- rotation receiver
- combined pose object

Success condition:

- pad position from vision and pad rotation from MPU coexist cleanly

## 15. What I Would Not Do Yet

Do not start with:

- multiple independent single markers with no rigid board model
- SLAM
- ARCore
- trying to solve both vision position and rotation as the final system first
- exhibition auto-magic without calibration

These would slow the project down and reduce reliability.

## 16. Real Acceptance Criteria

The user asked for something close to real deployment, so the milestone should be:

### Milestone A

- stable live `x/y`
- usable coarse `z`
- marker board visible under expected pad orientations
- last-value hold when briefly lost
- Unity debug scene updates in real time

### Milestone B

- camera calibration saved
- tracking confidence exposed
- repeatable setup process written down
- rotation path merged with MPU9250

## 17. Final Recommendation

The correct next implementation is:

1. rigid multi-face ArUco board on the front of the pad
2. wired phone camera path to PC
3. Python OpenCV pose tracker
4. Unity UDP receiver and debug scene
5. later merge MPU9250 rotation

If the user wants the cleanest engineering path, this is what I would build now.

## 18. References

- OpenCV ArUco documentation:
  https://docs.opencv.org/4.x/d9/d6a/group__aruco.html
- OpenCV camera calibration:
  https://docs.opencv.org/4.x/dc/dbb/tutorial_py_calibration.html
- OpenCV solvePnP:
  https://docs.opencv.org/4.x/d5/d1f/calib3d_solvePnP.html
- DroidCam USB-capable Android webcam app listing:
  https://play.google.com/store/apps/details?id=com.dev47apps.droidcam
