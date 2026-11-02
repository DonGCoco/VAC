# Game-centre viewing distance

## Definition and behaviour

`BoardDistanceCalibration.ActualDistanceMeters` is the Euclidean world-space distance
from the ML Rig Main Camera to `ConditionController.StimulusRoot.position`.
In `M1_SmokeTest`, StimulusRoot is `M4_TetrisRoot`: its origin is the centre of
the symmetric Tetris grid/frame. The current board offset is `(0, -0.40, 0)` metres.
The QR marker still registers BoardAnchor. Neither the marker position nor the
perpendicular distance to the board substitutes for game-centre distance.

READY compares this distance with the existing C1/C2/C3 targets and tolerance.
LOCK saves the same measurement. ExperimenterBridge forwards the same `actual_m`
and `locked_m`, and the existing formal-flow/logger path records them. Tetris
mean/min/max viewing distance now samples the game centre as well.
Missing/invalid game-centre geometry blocks READY. Lost marker observations before
lock, or an untracked spatial anchor after lock, produce no valid current distance.
No fallback to QR distance occurs.

The headset shows `Game distance ... m` during warm-up/block calibration and the
START handoff. It hides that extra near-field label during Tetris, depth tasks,
questionnaires and recovery. The computer Monitor uses the same distance.
This is a tracking-derived Main Camera approximation of viewing position, not
an independently measured eye position or a measurement from the headset shell.

## Size, data and compatibility

Nominal targets remain 0.8/1.0/1.5 m and tolerance remains ±0.03 m. Nominal
condition scaling still uses target/reference distance, applied once per condition.
The game remains fixed to the registered board and does not follow the viewer.
Do not claim exact constant visual angle under head motion or off-axis viewing.

The protocol version is `M7-game-center`. `session.csv` adds
`viewing_distance_reference=main_camera_to_game_center`. Existing numerical CSV
column names and bridge field names remain compatible. Old `M7` exports retain
their old anchor-distance meaning and must not be pooled as the same measurement.
The fixed post-exposure depth-task geometry is unchanged.

## Verification and device acceptance

Offline source checks: `python3 Tools/check_vac_handoff.py`.
An Editor geometry/state check that executes the actual calibration component is
available from `VAC / Check Game-Centre Distance` (see Editor script).
These do not replace a Unity build or ML2 device acceptance.

On ML2, select each condition and align viewing position with the game centre.
Compare the headset and Monitor game-distance values with an independent ruler
measurement using a consistent eye-position reference. Verify READY/TooClose/TooFar,
LOCK, marker loss, reacquisition and head motion after lock. Do not measure to the
QR marker or headset shell. Run a short dedicated test session and validate its
CSV folder with `Tools/validate_vac_session.py`; check the new distance-reference
metadata. Confirm the distance label disappears when either task begins.

Unity compilation, the Editor menu check and ML2 testing still need to run on a
machine with the project Unity version and the device. No new hardware accuracy
or participant results are claimed by this change.
