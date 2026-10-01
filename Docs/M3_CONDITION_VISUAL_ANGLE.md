# Milestone 3 — Condition Geometry and Constant Visual Angle

## Scope

Milestone 3 adds the C1/C2/C3 visual-geometry layer on top of the registered physical board.

It does not add the formal Tetris gameplay yet. The existing M1 white board stimulus is used as a proxy so the condition geometry can be validated independently.

## Geometry rule

The physical board / `BoardAnchor` is the only depth reference.

The stimulus root:

- is parented to `BoardAnchor`,
- has local Z = `0`,
- is never placed relative to the participant's current head pose,
- is scaled only when the condition changes.

This replaces the old head-relative VAC placement approach for the new experiment path.

## Constant visual angle

For visual angle `theta` at viewing distance `d`:

`S(d) = 2 d tan(theta / 2)`

If the authored stimulus size is the reference size at `d_ref`, then:

`S(d) / S(d_ref) = d / d_ref`

Therefore Milestone 3 uses:

`scaleFactor = targetDistance / visualAngleReferenceDistance`

The default reference distance is configurable in `ExperimentConfig.asset` and currently set to `1.00 m`.

With the current C1/C2/C3 defaults:

- C1 0.80 m -> scale factor 0.80
- C2 1.00 m -> scale factor 1.00
- C3 1.50 m -> scale factor 1.50

Scaling is applied once on condition selection, not every frame.

## Current proxy

The existing 6 cm M1 board test square is the temporary reference stimulus at 1.00 m.

Expected proxy widths are therefore:

- C1 -> 4.8 cm
- C2 -> 6.0 cm
- C3 -> 9.0 cm

This proxy is only for validating the condition geometry. In Milestone 4, the formal static Tetris root will replace it while using the same condition-scaling logic.

## VAC values

The controller also computes condition-level values for later logging:

- focal demand: `Df = 1 / focalDistance`
- vergence demand: `Dv = 1 / targetDistance`
- VAC: `Df - Dv`

These are diagnostic values only in M3; formal CSV logging remains a later milestone.

## Validation

On device:

1. Scan/register the board.
2. Select C1, C2 and C3.
3. Confirm the white proxy stays on the same registered board plane.
4. Confirm it changes size with factors 0.80 / 1.00 / 1.50.
5. Confirm changing condition does not move the stimulus toward/away from the board plane.
6. Confirm head motion does not trigger continuous rescaling.

Absolute physical board-distance accuracy remains the separate pending M2 hardware validation and must be completed with the final board and an independent physical measurement before pilot data collection.
