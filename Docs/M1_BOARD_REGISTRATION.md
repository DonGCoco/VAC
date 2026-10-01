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


## Device validation — PASSED (2026-09-29)

Validated on a physical Magic Leap 2.

Validation setup:

- QR payload: `VAC_BOARD_01`
- QR displayed on an iPhone for the smoke test
- Measured test marker size entered as `0.05 m`
- Marker-to-board offset left at zero
- Detector profile: `Accuracy`

Observed result:

- The QR was detected.
- The test block appeared at the QR pose.
- The test block remained registered to the QR while the wearer translated left/right and viewed it from an oblique angle.
- The block did not follow head movement.
- When the QR/object moved outside the Magic Leap display field of view, the virtual block was no longer visible; this was treated as a display-FOV effect rather than registration loss because registration remained stable when the QR stayed inside the display region.

Milestone 1 therefore passes the physical-device registration criterion.

### Android manifest note

Unity 6.2 generated a custom manifest containing both `UnityPlayerActivity` and `UnityPlayerGameActivity`, which caused Gradle to fail because `BaseUnityGameActivityTheme` was unavailable for this Magic Leap build.

The committed manifest intentionally keeps **Activity only** and includes:

`com.magicleap.permission.MARKER_TRACKING`

Do not re-add `UnityPlayerGameActivity` for this project unless the Magic Leap platform requirements change.


## VAC co-planarity requirement

For this experiment, the virtual stimulus plane must match the physical board plane in depth.

The M1 test object is therefore centred at `BoardAnchor local Z = 0`. No hidden forward/backward test offset is allowed.

During development with no final physical board, the QR plane may temporarily stand in for the board plane. Once the real board exists, only the configurable marker-to-board transform is updated. The later Tetris root must remain co-planar with `BoardAnchor` by default.
