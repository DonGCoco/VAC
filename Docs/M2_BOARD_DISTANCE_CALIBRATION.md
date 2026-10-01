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


## Formal physical-board setup

The phone-displayed 5 cm QR is only a near-range smoke test. It is not the formal marker for the 1.5 m condition.

For the physical experiment board:

- Print one high-contrast QR with payload `VAC_BOARD_01`.
- Attach it rigidly and flat to the same physical board that defines the Tetris plane.
- Use a known measured QR side length. For a maximum viewing distance of 1.5 m, use at least 0.15 m; a slightly larger marker is preferable if the board has room.
- Enter the measured black/white QR square side length, excluding the outer white margin, into `Marker Size Meters`.
- Place the QR outside the active Tetris area if possible, then measure the QR-centre-to-board-reference offset and enter it in `Marker To Board Position Meters` / `Marker To Board Euler Degrees`.

The QR therefore defines the physical board transform; no separate plane-detection system is required for this milestone.

## Lock / rescan flow

For each condition:

1. Move the physical board to the target distance.
2. Keep the QR visible while the operator aligns the board until the M2 panel reports `READY`.
3. Select `LOCK`.
4. `BoardAnchor` is frozen at the validated pose and no longer follows noisy subsequent QR pose updates.
5. Run the condition.
6. Before moving the board for the next condition, select `RESCAN` or select the next C1/C2/C3 condition. This unlocks `BoardAnchor` and resumes marker-based registration.

This matches the intended setup sequence: move board -> detect -> validate distance -> establish/update board anchor -> lock block -> run.


## Proxy testing before the final board exists

Milestone 2 does not require the final physical board geometry.

During development, the QR itself may temporarily act as the board reference:

- keep `Marker To Board Position Meters = (0,0,0)`,
- keep `Marker To Board Euler Degrees = (0,0,0)`,
- use any rigid/stationary surface or sufficiently large screen to hold/display the QR,
- enter the QR's measured physical side length.

When the real board is available later, only the marker size and marker-to-board offset/rotation need to be measured and configured. The registration and distance-calibration logic does not change.

After `LOCK`, the BoardAnchor pose is frozen, but the displayed `Actual` viewing distance remains live because the headset/participant can still move relative to the locked board.
