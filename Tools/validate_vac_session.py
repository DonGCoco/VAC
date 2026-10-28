#!/usr/bin/env python3
import argparse
import csv
import sys
from pathlib import Path

EXPECTED_ORDERS = {
    "G1": ["C1", "C2", "C3"],
    "G2": ["C2", "C3", "C1"],
    "G3": ["C3", "C1", "C2"],
}
EXPECTED_SEQUENCES = ["A", "B", "C"]

def read_rows(path: Path):
    if not path.exists():
        raise FileNotFoundError(path)
    with path.open(newline="", encoding="utf-8-sig") as f:
        return list(csv.DictReader(f))

def as_float(value):
    try:
        return float(value)
    except (TypeError, ValueError):
        return None

def as_int(value):
    try:
        return int(value)
    except (TypeError, ValueError):
        return None

def main():
    parser = argparse.ArgumentParser(
        description="Validate one M7 VAC session folder after an ML2 dry run."
    )
    parser.add_argument("session_folder", type=Path)
    args = parser.parse_args()
    folder = args.session_folder

    errors = []
    warnings = []

    try:
        session_rows = read_rows(folder / "session.csv")
        event_rows = read_rows(folder / "events.csv")
        block_rows = read_rows(folder / "blocks.csv")
        depth_rows = read_rows(folder / "depth_trials.csv")
        tetris_rows = read_rows(folder / "tetris.csv")
    except FileNotFoundError as exc:
        print(f"FAIL: missing {exc.args[0]}")
        return 1

    if len(session_rows) != 1:
        errors.append(f"session.csv should contain exactly 1 data row; found {len(session_rows)}")
        session = session_rows[0] if session_rows else {}
    else:
        session = session_rows[0]

    participant = session.get("participant_id", "")
    group = session.get("group", "")
    expected_order = EXPECTED_ORDERS.get(group)
    formal_trials = as_int(session.get("formal_depth_trials")) or 0
    tolerance = as_float(session.get("board_tolerance_m"))

    if not participant:
        errors.append("participant_id is missing in session.csv")
    if expected_order is None:
        errors.append(f"unexpected group '{group}'")

    ordered_blocks = sorted(
        block_rows,
        key=lambda row: as_int(row.get("block")) or 999,
    )

    if len(ordered_blocks) != 3:
        errors.append(f"blocks.csv should contain exactly 3 formal blocks; found {len(ordered_blocks)}")

    for index, row in enumerate(ordered_blocks[:3], start=1):
        block = as_int(row.get("block"))
        condition = row.get("condition", "")
        sequence = row.get("sequence", "")
        if block != index:
            errors.append(f"expected block {index}; found {block}")
        if expected_order and condition != expected_order[index - 1]:
            errors.append(
                f"block {index}: expected condition {expected_order[index - 1]}, found {condition}"
            )
        if sequence != EXPECTED_SEQUENCES[index - 1]:
            errors.append(
                f"block {index}: expected sequence {EXPECTED_SEQUENCES[index - 1]}, found {sequence}"
            )

        target = as_float(row.get("target_distance_m"))
        locked = as_float(row.get("locked_distance_m"))
        if target is None or locked is None:
            errors.append(f"block {index}: missing target/locked distance")
        elif tolerance is not None and abs(locked - target) > tolerance + 0.002:
            errors.append(
                f"block {index}: locked distance {locked:.3f} m is outside "
                f"target {target:.3f} m ± tolerance {tolerance:.3f} m"
            )

    if len(tetris_rows) != 3:
        errors.append(f"tetris.csv should contain exactly 3 formal rows; found {len(tetris_rows)}")
    else:
        for index, row in enumerate(sorted(
            tetris_rows,
            key=lambda r: as_int(r.get("block")) or 999,
        ), start=1):
            if as_int(row.get("block")) != index:
                errors.append(f"tetris row {index}: wrong block")
            if row.get("sequence") != EXPECTED_SEQUENCES[index - 1]:
                errors.append(f"tetris block {index}: wrong sequence {row.get('sequence')}")
            if not row.get("mean_viewing_distance_m"):
                errors.append(f"tetris block {index}: mean viewing distance was not logged")

    if formal_trials > 0:
        for block in range(1, 4):
            rows = [r for r in depth_rows if as_int(r.get("block")) == block]
            if len(rows) != formal_trials:
                errors.append(
                    f"depth block {block}: expected {formal_trials} trials; found {len(rows)}"
                )
            trial_numbers = sorted(as_int(r.get("trial")) for r in rows)
            if rows and trial_numbers != list(range(1, formal_trials + 1)):
                errors.append(f"depth block {block}: trial numbering is incomplete")

    events = [row.get("event", "") for row in event_rows]
    if "ExperimentCompleted" not in events:
        errors.append("events.csv has no ExperimentCompleted event")
    if "DevelopmentRecoverySkipped" in events:
        warnings.append(
            "development recovery skip was used; acceptable for software dry-run, not formal collection"
        )

    session_ids = {
        row.get("session_id", "")
        for rows in (event_rows, block_rows, depth_rows, tetris_rows)
        for row in rows
    }
    session_ids.discard("")
    if len(session_ids) > 1 or (
        session_ids and session.get("session_id") not in session_ids
    ):
        errors.append("CSV files do not agree on session_id")

    print(f"Session: {session.get('session_id', folder.name)}")
    print(f"Participant: {participant or '—'} · Group: {group or '—'}")
    print(f"Blocks: {len(block_rows)} · Tetris rows: {len(tetris_rows)} · Depth rows: {len(depth_rows)}")

    for warning in warnings:
        print(f"WARN: {warning}")

    if errors:
        for error in errors:
            print(f"FAIL: {error}")
        print(f"\nRESULT: FAIL ({len(errors)} issue(s))")
        return 1

    print("\nRESULT: PASS")
    return 0

if __name__ == "__main__":
    sys.exit(main())
