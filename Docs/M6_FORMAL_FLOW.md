# Milestone 6 — Formal Experiment Flow

## Current integrated flow

M6 now owns the single-scene formal experiment flow while reusing the already validated M2/M4/M5 modules.

1. Assign participant ID.
2. Auto-assign G1/G2/G3 (or explicit override).
3. Warm-up calibration at the configured training condition (default C2).
4. Participant presses START in-headset.
5. Warm-up Tetris uses sequence T for the configurable warm-up duration (default 180 s).
6. Warm-up depth practice uses the configured practice trial count (default 3).
7. External Pre-SSQ checkpoint for Block 1.
8. Formal board calibration for the block's VAC condition.
9. LOCK -> participant START.
10. Formal Tetris uses A/B/C by block number.
11. Formal depth judgement.
12. External Post-SSQ + QoE checkpoint.
13. Between Blocks 1/2 and 2/3, minimum recovery (default 300 s), with experimenter confirmation after the minimum has elapsed.
14. Repeat through all three blocks.
15. After Block 3 Post-SSQ + QoE, flow enters Complete.

There is no initial SSQ before warm-up.

## Counterbalancing

- G1: C1 -> C2 -> C3
- G2: C2 -> C3 -> C1
- G3: C3 -> C1 -> C2

Tetris sequence is tied to block number:

- Block 1 -> A
- Block 2 -> B
- Block 3 -> C

Training always uses sequence T.

## Participant-side interaction

The participant sees START only when the formal flow is in a calibration phase and the board is truly LOCKED/tracking.

Warm-up and formal blocks therefore use the same handoff:

experimenter guides distance -> READY -> LOCK -> participant START.

## Experimenter monitor

The monitor now shows:

- participant ID
- group and condition order
- flow phase
- warm-up vs formal block number
- Tetris sequence
- condition / target / actual distance
- anchor state
- questionnaire checkpoints
- recovery countdown and recovery-complete action

Manual C1/C2/C3 controls remain visible only as a development fallback and are disabled once a participant is assigned.

## Device validation plan

For the first M6 device test:

1. Use a private network/hotspot that allows headset-laptop communication.
2. Assign P001 with Group Auto.
3. Confirm warm-up uses sequence T.
4. For speed, use the controller Menu button to end warm-up Tetris early instead of waiting the full configured duration.
5. Complete all practice depth trials.
6. Confirm monitor reaches PreBlockQuestionnaire for Block 1 and shows G1 order C1 -> C2 -> C3.
7. Click PRE-SSQ DONE -> CALIBRATION.
8. Calibrate/LOCK, press participant START, then use Menu to end formal Tetris early for this software test.
9. Complete formal depth trials and confirm PostBlockQuestionnaire.
10. For end-to-end testing only, temporarily reduce minimumRecoverySeconds locally; restore the formal value before pilot collection.
11. Confirm Block 2 uses C2 + B, Block 3 uses C3 + C, then Complete.

Do not mark M6 validated until the full three-block device flow has been completed once.
