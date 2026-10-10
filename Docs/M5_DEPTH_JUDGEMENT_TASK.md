# Milestone 5 — Fixed Depth Judgement Task

## Scope

M5 implements the configurable two-target depth judgement task.

Per the current experiment decision, this task is independent from the physical board and does not use `BoardAnchor`. It is an outcome task presented at a fixed virtual reference configuration after Tetris.

## Current configurable defaults

All values remain in `ExperimentConfig.asset`:

- reference distance: 1.00 m
- total near/far separation: 0.06 m
- current target depths: 0.97 m and 1.03 m
- formal trials: 8
- practice trials: 3
- horizontal separation: 0.12 m
- target diameter at 1.00 m: 0.04 m

The exact depth difference remains a pilot parameter.

## Trial geometry

At the start of each trial:

1. Capture the current Main Camera pose.
2. Place two identical virtual spheres in world space.
3. Randomly assign the nearer target to left or right.
4. Keep both targets fixed in world space until the response.
5. Compensate target size by depth so apparent size is not an unintended monocular cue.

The targets do not follow the participant's head after trial onset.

## Input

Each trial asks:

`WHICH ONE IS FARTHER?`

The participant uses the existing Magic Leap XR controller ray to point at the target they judge to be farther away, then presses the trigger to select it.

Each target uses `XRSimpleInteractable`, reusing the same XR Interaction Toolkit / Magic Leap controller ray-selection path already validated for the experiment UI. There is no left/right button mapping to remember. The visible target size is unchanged, while the invisible sphere collider is enlarged for more forgiving ray selection on device.

## M5 device-test transition

For this milestone only, completion/early termination of the M4 Tetris test automatically:

1. hides Tetris renderers,
2. starts an 8-trial depth block using the currently selected C1/C2/C3 label,
3. writes a standalone test CSV session if no participant session already exists.

M6 will replace this milestone test transition with the formal experiment state machine.

## Logging

Each formal M5 trial logs:

- condition label
- phase
- trial number
- reference depth
- depth difference
- correct/closer side
- participant response
- correctness
- reaction time

Console output also prints the actual near/far depths for device validation.

## Device validation

1. Run the normal board calibration and START Tetris.
2. Press Menu to end Tetris early for testing.
3. Tetris should disappear and two spheres plus the prompt `WHICH ONE IS FARTHER?` should appear.
4. The spheres must remain visible and must not fall/move.
5. Point the controller ray at the sphere judged farther away and press trigger.
6. A new randomized trial appears immediately.
7. Complete all 8 trials.
8. Confirm the targets disappear and the console reports `M5 depth task complete`.
9. Verify the generated `M5_TEST_*_depth.csv` contains 8 rows.

Success means two targets are presented, the farther target is randomized left/right, ray+trigger selection works, reaction time and accuracy are recorded, and the task is independent from BoardAnchor.

## Validation status

Device validation completed on Magic Leap 2. The two targets were visible and selectable with controller ray + trigger, all 8 trials could be completed, and the enlarged invisible hit area made left/right target selection practical without changing the visual stimulus geometry.
