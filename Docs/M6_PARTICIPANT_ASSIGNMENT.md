# Milestone 6 — Participant Assignment (Step 1)

This is the first M6 integration step. It adds participant ID entry and three-group counterbalancing without yet replacing the validated M2/M4/M5 task flow.

## Automatic groups

Participant IDs must end with a positive number.

- P001 -> G1 -> C1 > C2 > C3
- P002 -> G2 -> C2 > C3 > C1
- P003 -> G3 -> C3 > C1 > C2
- P004 -> G1 -> C1 > C2 > C3

The pattern repeats every three participants.

Tetris sequence is fixed by block number:

- Block 1 -> A
- Block 2 -> B
- Block 3 -> C

Therefore P005 is automatically:

- G2
- Block 1: C2 + A
- Block 2: C3 + B
- Block 3: C1 + C

## Experimenter monitor

The monitor now has:

- Participant ID input
- Group Auto / G1 / G2 / G3 override
- ASSIGN button
- assigned participant, group and condition order display

The manual C1/C2/C3 buttons remain temporarily as a development fallback while the rest of M6 is integrated.

## Test

1. Pull the latest branch and rebuild the headset app.
2. Start the experimenter monitor.
3. Enter P001, choose Group: Auto, click ASSIGN.
4. Confirm the monitor shows G1 and C1 > C2 > C3.
5. Repeat with P002 and P003.
6. Confirm P004 wraps back to G1.
7. Test one manual override, for example P005 + Override G3, and confirm G3 is shown.
8. Confirm existing C1/C2/C3, distance, LOCK and START behavior still works.

This step does not yet advance blocks automatically. The next M6 step will connect this participant assignment to the formal state machine and existing Tetris/depth modules.
