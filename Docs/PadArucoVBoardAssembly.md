# Pad ArUco V-Board Assembly

This is the first physical step for pad position tracking.

The goal is simple:

- attach a rigid `two-face ArUco board` to the front of the pad
- make sure the camera can still see at least one face when the pad yaws left or right

## Files

- Print sheet:
  [pad-aruco-v-board-print-sheet.svg](</C:/Bell Ringer/Docs/Printables/PadArucoBoard/pad-aruco-v-board-print-sheet.svg>)
- Assembly schematic:
  [pad-aruco-v-board-assembly-schematic.svg](</C:/Bell Ringer/Docs/Printables/PadArucoBoard/pad-aruco-v-board-assembly-schematic.svg>)
- Left marker only:
  [aruco-original-left-id-23.svg](</C:/Bell Ringer/Docs/Printables/PadArucoBoard/aruco-original-left-id-23.svg>)
- Right marker only:
  [aruco-original-right-id-47.svg](</C:/Bell Ringer/Docs/Printables/PadArucoBoard/aruco-original-right-id-47.svg>)

## Marker IDs

Use these exact IDs later in the tracker:

- left face: `23`
- right face: `47`

Dictionary:

- `DICT_ARUCO_ORIGINAL`

This matters. If the tracker later uses a different dictionary, detection will fail.

## What The User Must Prepare

The assistant cannot do these physical steps for the user.

The user needs:

- a printer
- matte white paper or matte sticker paper
- two rigid square plates, each exactly `60 mm x 60 mm`
- good plate materials:
  - `2 mm to 3 mm foam board`
  - `1 mm to 2 mm plastic sheet`
  - rigid thick card only if nothing else is available
- scissors or a precision knife
- ruler
- glue
- tape or hot glue for the join
- a way to attach the finished board to the pad front edge
- mounting options:
  - double-sided tape
  - removable adhesive
  - small custom bracket if preferred

## Print Settings

The user must print the sheet at:

- `100% scale`
- `actual size`
- `fit to page = off`

Do not use:

- automatic scaling
- shrink to fit
- borderless auto-resize

After printing, the user must measure the rulers on the print sheet:

- one ruler must be exactly `100 mm`
- one ruler must be exactly `50 mm`

If either ruler is wrong, stop and reprint correctly.

## Build Steps

### 1. Print the sheet

Print:

- [pad-aruco-v-board-print-sheet.svg](</C:/Bell Ringer/Docs/Printables/PadArucoBoard/pad-aruco-v-board-print-sheet.svg>)

### 2. Confirm the size

Measure the printed rulers.

The user should continue only if:

- the `100 mm` ruler is exactly `100 mm`
- the `50 mm` ruler is exactly `50 mm`

### 3. Cut the printed face tiles

Cut out:

- the `LEFT FACE / ID 23` square
- the `RIGHT FACE / ID 47` square

Each printed face tile should end up as:

- `60 mm x 60 mm`

Important size distinction:

- the full cut tile is `60 mm x 60 mm`
- the actual ArUco marker inside it is `50 mm x 50 mm`
- the remaining `5 mm` on each side is intentional white margin

### 4. Cut the rigid face plates

Cut two rigid backing plates:

- `60 mm x 60 mm`
- `60 mm x 60 mm`

These must match the printed face size exactly.

### 5. Glue the prints to the rigid plates

Glue:

- `LEFT FACE / ID 23` to one rigid plate
- `RIGHT FACE / ID 47` to the other rigid plate

Important:

- keep the paper flat
- avoid bubbles
- do not wrinkle the black pattern
- do not laminate with glossy film

Glossy reflection will hurt detection.

### 6. Join the two face plates as a V

Use tape, glue, or a rigid center strip to join the two plates.

Target geometry:

- included angle near `90 degrees`
- the V opening faces the camera

This is the most important orientation rule.

The V must open toward the camera, not toward the user.

### 7. Attach the V-board to the pad

Mount the completed V-board:

- centered on the front edge of the pad
- near the charging-port side the user described
- as rigidly as possible

The board should not wobble relative to the pad.

If the board flexes, tracking quality drops.

## Later Software Size Setting

When the tracker is implemented later:

- use the ArUco dictionary `DICT_ARUCO_ORIGINAL`
- use the marker IDs `23` and `47`
- use the marker side length as `50 mm`, not `60 mm`

The `60 mm` size is the physical tile cut size.
The `50 mm` size is the black marker square used for pose estimation.

## What The User Should Check After Assembly

Before any software work, the user should manually inspect:

1. From the camera's point of view, the neutral pad pose should show both faces or at least a strong view of one face.
2. When the pad yaws left, one face should remain visible.
3. When the pad yaws right, the other face should remain visible.
4. The user's hand should not cover the faces during normal grip.
5. The board should not shake independently from the pad.

## If The User Notices A Problem

### Problem: marker disappears too easily when rotating

Try:

- increasing the V angle slightly toward `100 degrees`
- moving the board a little farther forward from the pad body
- raising the camera slightly

### Problem: marker looks too small on camera

Try:

- moving the camera closer
- increasing face size later to `70 mm x 70 mm`

Do not change the size yet unless the first camera test clearly fails.

### Problem: reflections make the black squares shine

Use:

- matte paper
- matte glue
- no glossy tape over the marker face

## Why This Design Was Chosen

This design is intentionally conservative.

It uses:

- two rigid faces
- one unique marker per face
- large marker area
- simple geometry

This is better than a single flat front marker because:

- it survives more yaw rotation
- it reduces total marker loss
- it stays simple enough for a first robust implementation

## Next Step

After the user prints and assembles this board, the next implementation step is:

- PC-side camera capture and marker detection
