# Milestone 2 — Board Distance Calibration

## Scope

Milestone 2 adds only physical-board distance setup on top of the device-validated Milestone 1 registration.

It does not add Tetris, the depth task, questionnaire flow, recovery logic or final CSV logging.

## Current configurable defaults

The central `Assets/Config/ExperimentConfig.asset` contains:

- C1: `0.80 m`
- C2: `1.00 m`
- C3: `1.50 m`
- Ready tolerance: `±0.03 m`
- focal distance placeholder: `0.74 m`

These remain configurable so later pilot/supervisor changes do not require code edits.

## Measurement

Actual board distance is:

`distance(Main Camera, BoardAnchor)`

The Main Camera is used as the practical cyclopean-eye approximation. Once the physical marker-to-board offset is configured, `BoardAnchor` is the intended board/Tetris reference rather than the marker corner.

The calibration reports `Ready` only while the target QR marker is currently visible. This avoids accepting a stale BoardAnchor pose after the physical board has moved out of marker tracking.

## In-headset calibration panel

A minimal runtime panel is created in front of the headset:

- C1 button
- C2 button
- C3 button
- selected condition and target distance
- actual distance
- `TOO CLOSE`, `READY`, `TOO FAR`, or `SHOW QR MARKER`

The three condition buttons use the installed XR Interaction Toolkit `XRSimpleInteractable`, so the existing Magic Leap controller ray/select path is reused.

## Console validation logging

The component logs:

- condition selections,
- target and tolerance,
- state changes,
- measured distance when the state changes.

Formal experiment CSV logging remains a later milestone.

## Device validation checklist

1. Display/attach the `VAC_BOARD_01` QR and enter the measured QR size in `M1_BoardRegistration`.
2. Build and run on Magic Leap 2.
3. Confirm the M2 panel appears.
4. Point the existing controller ray at C1/C2/C3 and select each one.
5. Confirm the target changes to 0.80 / 1.00 / 1.50 m.
6. With the QR visible, move the board/headset distance:
   - closer than target minus tolerance -> `TOO CLOSE`
   - within tolerance -> `READY`
   - farther than target plus tolerance -> `TOO FAR`
7. Hide the QR or move it outside marker tracking and confirm `SHOW QR MARKER`.
