# Milestone 5 — Fixed Depth Judgement Task

Pilot1 changes and current acceptance results: [PILOT1_IMPLEMENTATION.md](PILOT1_IMPLEMENTATION.md). The earlier `DEPTH_RECTANGLE_VALIDATION.md` is historical.

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

The spheres are visual stimuli only. Their colliders and `XRSimpleInteractable` components are disabled. Sphere position, depth, size, colour and depth compensation are unchanged.

Point the ML2 controller ray through an invisible left/right rectangle, then press the trigger to confirm which sphere is farther. The existing prompt shows `LEFT`, `RIGHT`, or `--`; pointing alone does not answer. No rectangle renderer, border or sphere highlight is added.

Pilot1 (`Pilot1-20261108`) replaces the fixed 6 cm gap. At trial onset, the plane captures the viewer pose at reference depth (currently 1 m). After existing size compensation, each actual world sphere centre/radius and rendered mesh determine its horizontal perspective silhouette on that plane. The mesh outline conservatively guards the ideal sphere tangent against tessellation differences. Left selection starts outside the left sphere's inward silhouette; right selection outside the right sphere's inward silhouette. Different depths produce separate, generally asymmetric boundaries, not 0.12 minus a fixed diameter. Geometry defaults remain candidates awaiting device acceptance:

- the entire interval between computed inner edges rejects input, with a conservative one-micrometre outward guard;
- outer x limit remains -0.38 / +0.38 m;
- height: 0.40 m; y from -0.20 to +0.20 m;
- outer edges are included within floating-point roundoff; parallel/back-facing/behind-origin/invalid intersections reject input.

The nominal rectangles and spheres stay world-fixed for the trial. A single fixed plane cannot align with both sphere depths for every displaced eye/controller. Selection uses XRI's actual current straight-ray origin and also vetoes the silhouette gap from that origin and the current eye. This conservatively shrinks selectable regions when needed, without moving base rectangles or expanding selection into the gap. Large motion can reject an otherwise outward point; exact edge coincidence for every pose is not claimed. Verify this on ML2 rather than increasing a constant gap. Invalid/behind-plane eye poses reject selection.

Development logs `Pilot depth geometry v2` record the plane, independent edges and radii each trial; `Pilot depth ray` records actual ray origin/direction and selection at most once per second. They are diagnostics, not answer rows or evidence that the reported pilot video/root cause was reproduced.

`MagicLeapDepthInput` is explicitly enabled on `M5_DepthTask` in `M1_SmokeTest`, with its manager assigned and `Controller Ray Origin` linked to the official ML Rig's `Controller` transform. That is the existing straight XRRayInteractor origin (no custom ray-origin override in the prefab). The manager also enables/adds an adapter at Start for other scenes. Auto-resolution only accepts one active XRI ray; scenes with multiple rays must explicitly assign the ML2 controller. Trigger and tracking actions come from the existing ML2 `Controller` action map; the rig's InputActionManager is unchanged.

The adapter consumes every fresh trigger press, even in the central gap or outside a rectangle. Holding the trigger, moving between rectangles while held, or holding it before block onset cannot submit another answer. After tracking loss (including missing position/rotation tracking), disable/re-enable, or a block start, it must observe release before another press can answer. The manager additionally rejects duplicate responses in the same frame. Other UI and Tetris input scripts are unchanged.

## Historical M5 device-test transition

The current saved scene uses the M6 formal controller: training countdown/game leads to 3 practice trials; each formal countdown/game leads to 8 formal trials after the existing gates. The standalone transition below is a legacy milestone reference, not an additional simultaneous listener in Pilot1.

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
- correct/farther side
- participant response
- correctness
- reaction time

Console output also prints the actual near/far depths for device validation.

## Device validation

1. Run calibration, training and depth practice with a dedicated test participant ID. Development early-end shortcuts may accelerate software checks; do not treat them as formal exposure data.
2. Verify the input-binding log names `Controller`; confirm the visible ray and response hint agree. Both spheres must match the previous visual geometry and cannot be clicked directly.
3. Point through the left/right rectangles without pressing: confirm LEFT/RIGHT, and -- in the centre gap, above/below, outside the width, or from behind. Move the head after trial onset: the rectangles and spheres must stay world-fixed for that trial.
4. Press in the gap, hold and move into either region: no response. Release then press inside a region: exactly one response. Hold and sweep between sides across the next trial: no further response.
5. Begin a block with the trigger already held: release and press again is required. Lose controller tracking: hint clears and no response; recover while held: no response until release/repress.
6. Complete 3 practice and 8 formal trials. Verify first response, correct/farther side, correctness and reaction time in the existing `depth_trials.csv`; formal sides remain balanced 4/4, practice remains 3, and practice logging policy is unchanged.
7. Complete all three experimental conditions and check subsequent questionnaires/recovery/completion and CSV export. Verify START, other UI and Tetris trigger behaviour are unaffected.

Reaction time continues to include judging, pointing and confirming. The timer, CSV schema, condition parameters, randomisation and trial counts are unchanged. Record the software/interaction version when analysing data; do not pool old sphere-click and rectangle-response runs without accounting for the input change.

## Validation status

The previous sphere-click device validation does not validate this rectangle implementation. The handoff itself had no Unity or ML2 acceptance result.

Run the real C# offline checks with `VAC/Checks/Depth rectangle response`, or batch `-executeMethod VACExperiment.EditorTools.VACDepthRectangleChecks.Run`. It checks the saved scene/controller/action bindings, sphere geometry and disabled colliders, rectangle boundaries, world-fixed geometry, hover text, trigger latch, tracking recovery, 3/8 trial completion and 4/4 formal balancing. Synthetic inputs in this check do not certify ML2 tracking, physical trigger timing, rendered layout or full experimental flow. Current integration results are recorded in `DEPTH_RECTANGLE_VALIDATION.md`.
