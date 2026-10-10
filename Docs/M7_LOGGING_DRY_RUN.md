# Milestone 7 — Logging and Dry Run

## What M7 now records

Every participant run creates a new timestamped session folder:

`Application.persistentDataPath/VACExperimentData/<participant>_<UTC timestamp>/`

This prevents a repeated dry run with the same participant ID from appending into an earlier CSV.

Each session contains:

- `session.csv`
  - participant
  - group and condition order
  - protocol/app/Unity version
  - device model / OS
  - development-build flag
  - focal distance
  - C1/C2/C3 distances
  - board tolerance
  - Tetris / warm-up / recovery settings
  - depth-task settings

- `events.csv`
  - participant / session / group / block / condition / sequence
  - flow events and UTC timestamps
  - focal / target / locked distance
  - target and locked VAC magnitude
  - questionnaire, recovery, development-skip, and completion events

- `blocks.csv`
  - one row per formal block at participant START
  - block / condition / sequence
  - focal distance
  - target distance
  - actual locked distance
  - target and actual locked VAC magnitude

- `tetris.csv`
  - one formal row per block
  - block / condition / A-B-C sequence
  - target / locked distance and VAC
  - duration, score, lines, lines/min, pieces, placement time, top-outs
  - time-weighted mean, minimum, and maximum actual head-to-board viewing distance during Tetris
  - VAC magnitude calculated from the mean viewing distance

- `depth_trials.csv`
  - participant / group / block / condition / sequence
  - exposure focal/target/locked distance and VAC
  - trial number
  - reference depth and near/far depths
  - correct farther side, response, correctness, reaction time

Warm-up Tetris and practice depth responses are intentionally excluded from formal performance CSV rows. Their progress is represented in `events.csv`.

VAC magnitude is stored in diopters as:

`abs(1 / focal_distance_m - 1 / vergence_distance_m)`

## Experimenter monitor

After participant assignment, the monitor shows:

`Logging · <session_id>`

This confirms that the formal session logger has started.

## Automatic export to the experimenter computer

The experimenter monitor now auto-exports the current session as soon as the headset reports `Complete`.

Requirements:

- the experimenter monitor is running
- the Magic Leap 2 is still visible to `adb` (USB or the same adb connection used for Build And Run)

The monitor copies only the completed session folder to:

`<repo>/CollectedData/<session_id>/`

For example:

`~/Desktop/VAC/CollectedData/P002_20261011_012345678/`

It then runs `Tools/validate_vac_session.py` automatically.

The browser reports:

- `Data export · COPYING TO COMPUTER…`
- then `Data export · SAVED · Validation PASS` or `FAIL`

The manual pull helper remains available only as a fallback:

```bash
bash Tools/pull_vac_data.sh
```

## Manual validation fallback

Automatic validation runs after export. If needed, it can still be rerun manually:

```bash
python3 Tools/validate_vac_session.py CollectedData/P002_YYYYMMDD_HHMMSSmmm
```

The validator checks:

- one session metadata row
- expected G1/G2/G3 condition order
- exactly three formal block rows
- A/B/C sequence assignment
- locked distance within the configured calibration tolerance
- exactly three formal Tetris rows
- actual viewing-distance samples in each Tetris block
- the configured number of formal depth trials in every block
- consistent session IDs
- final `ExperimentCompleted` event

A `DevelopmentRecoverySkipped` event is reported as a warning, not a failure, because it is valid for software dry runs but not formal participant collection.

## M7 device dry-run

Recommended next device validation:

1. Build the current branch to Magic Leap 2.
2. Use P002 / Auto so this dry run also exercises G2: C2 -> C3 -> C1.
3. Confirm the monitor shows `Logging · P002_<timestamp>`.
4. Run the full flow.
5. Use Menu to finish each Tetris exposure early for this software test.
6. Use `SKIP RECOVERY (DEV ONLY)` in a Development Build.
7. Complete all formal depth trials in all three blocks.
8. Reach `Complete`.
9. Confirm the monitor automatically shows `Data export · SAVED`.
10. Confirm automatic validation reports `PASS`.
11. Open `CollectedData/<session_id>/` and verify the five CSV files are present.

M7 is not considered device-validated until the auto-exported session returns `Validation PASS`.
