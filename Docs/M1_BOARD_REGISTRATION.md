# Milestone 1 — Physical Board Registration

## Goal

Use the installed Magic Leap 2 OpenXR **Marker Understanding** feature to locate one QR marker on the physical board and derive a `BoardAnchor` pose.

No Tetris, VAC-condition logic, depth task, smoothing or spatial-anchor layer is added in this milestone.

## Marker used for the first device validation

- Type: QR
- Payload: `VAC_BOARD_01`
- Default configured side length: `0.12 m`
- Detector profile: `Accuracy`

The QR payload and measured physical size are Inspector parameters on `M1_BoardRegistration`. If the printed QR is not exactly 0.12 m wide, enter the measured value before testing.

## Required Magic Leap setting

Marker Understanding requires:

`com.magicleap.permission.MARKER_TRACKING`

Before the device build, verify it is enabled under:

`Edit > Project Settings > Magic Leap > Permissions / Manifest Settings`

The OpenXR **Magic Leap 2 Marker Understanding** feature is already enabled in this repository.

## Scene

Use:

`Assets/Scenes/M1_SmokeTest.unity`

Relevant hierarchy:

```
ML Rig
M1_BoardRegistration
BoardAnchor
└── M1_BoardTestCube
M1_DirectionalLight
```

`BoardRegistration` uses the official ML Rig's `XROrigin`, detects the target QR, converts the returned marker pose into the XR Origin frame, applies the configurable marker-to-board offset, and updates `BoardAnchor`.

## Marker-to-board offset

For the first validation, leave the offset at zero. The test cube should appear at the QR pose.

After marker pose tracking is confirmed, enter the measured position/rotation from the QR centre to the desired board reference point using:

- `Marker To Board Position Meters`
- `Marker To Board Euler Degrees`

These remain configurable because the final physical board dimensions and marker placement may change.

## Success criteria

Milestone 1 passes on the actual Magic Leap 2 only when:

1. The QR marker is detected.
2. A valid 6DoF marker pose is returned.
3. `BoardAnchor` is placed from that pose.
4. The test cube appears at the expected physical-board reference.
5. The cube remains registered to the board while the participant moves their head.

If the marker briefly leaves view after a valid registration, the last BoardAnchor pose is intentionally retained. No custom CV or smoothing is used.
