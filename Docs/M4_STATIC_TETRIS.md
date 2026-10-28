# Milestone 4 — Static Self-Paced Tetris

## Scope

Milestone 4 replaces the M1 white proxy with a playable, static/self-paced Tetris board registered to the physical board.

It does not add the depth task or full three-block experiment flow yet.

## Geometry

The Tetris root replaces the old white proxy as the M3 stimulus root.

- Tetris root is a direct child of `BoardAnchor`.
- Tetris root local Z is `0`.
- Block centers use local Z = `0`.
- Fallback block depth is only `0.002 m` at the 1.0 m authored reference and scales with the whole M3 stimulus.
- C1/C2/C3 still use M3 constant-visual-angle scaling.

The authored board is 10 x 20 cells with a configurable 0.02 m cell size at the 1.0 m reference distance.

## Static/self-paced interaction

There is no gravity and no automatic falling.

A new piece remains at the spawn row until the participant acts. The participant may move and rotate it freely, then explicitly hard-drop it to place it.

Current Magic Leap 2 controller mapping uses the official Magic Leap OpenXR sample action map:

- trackpad click left side -> move left
- trackpad click right side -> move right
- trigger -> rotate clockwise
- bumper -> hard drop / place
- menu -> end the current M4 test and return to the experimenter panel

The old keyboard component is kept compile-compatible, but soft/automatic drop is intentionally ignored.

## Deterministic sequences

The sequence enum now includes:

- T: training
- A
- B
- C

All use deterministic fixed-seed 7-bag generation. Every bag contains exactly one of each tetromino, so piece counts are balanced across sequences while order differs. Final sequence difficulty validation remains an experiment-design/pilot question rather than a runtime randomization step.

## M4 experimenter test flow

The existing calibration panel now includes `START`.

1. Select C1/C2/C3.
2. Register the board and reach READY.
3. LOCK and wait for LOCKED.
4. Select START.
5. The experimenter panel hides.
6. Static Tetris begins on Sequence A for this M4 device test.
7. Use the ML2 controller to move, rotate and place pieces.
8. Press the menu button to end the test and restore the experimenter panel.

The full participant/condition sequence selection is deferred to the later experiment-flow milestone.

## Device validation checklist

- START is rejected unless the board is LOCKED.
- START hides the experimenter panel and shows Tetris on the registered board.
- The board does not fall or animate downward without participant input.
- Trackpad left/right moves the current piece.
- Trigger rotates the current piece.
- Bumper places the current piece by hard drop.
- A new deterministic piece appears after placement.
- Completed lines clear.
- Tetris remains co-planar with the registered board.
- C1/C2/C3 continue to change the full Tetris visual size without changing its depth plane.
- Menu ends the test and restores the experimenter panel.


## Revised experimenter handoff

The in-headset calibration controls are no longer the default experiment flow.

The participant wears the headset while the experimenter watches a separate laptop monitor with the live target/actual distance and Ready state. The experimenter gives verbal closer/farther instructions, clicks LOCK when Ready, and only then does START appear inside the headset for the participant.

See `Docs/EXPERIMENTER_REALTIME_FLOW.md`.


## Device validation status

Validated on Magic Leap 2:

- START enters the static Tetris test.
- The active piece does not fall automatically.
- Trackpad left/right movement works.
- Trigger rotation works.
- Bumper hard drop / placement works.
- A new piece appears after placement.
- The task remains attached to the registered board plane.
- C1/C2/C3 continue to change the full Tetris visual size without moving it in depth.
- The M4 interaction path is accepted for continued integration.

Milestone 4 is therefore accepted for implementation/device behavior. Formal participant flow and experimenter-side distance control are validated separately.
