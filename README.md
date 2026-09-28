# VAC — Magic Leap 2 Experiment Framework

Group 5 experimental codebase for a within-subject study of vergence–accommodation conflict (VAC) using Magic Leap 2.

## Current status

**Active milestone: Milestone 1 — Device Smoke Test**

The Magic Leap 2 is connected to Magic Leap Hub 3 over USB. The repository is prepared for **Unity 6.2 (6000.2.15f1)**, which is the project baseline for Magic Leap 2 x86_64 support.

Do not move on to virtual-depth manipulation until a simple Unity/OpenXR cube has successfully built, launched, and appeared on the real headset.

See **Docs/M1_DEVICE_SMOKE_TEST.md** for the exact next steps.

## Study design

- Target device: Magic Leap 2
- Design: within-subject
- Formal sample target: 10 participants
- Pilot: 4 participants
- Conditions: Low VAC and High VAC
- Condition order: 50% Low → High, 50% High → Low
- VAC manipulation: virtual geometry depth, not Focus Distance / Stereo Convergence
- Low/High distances remain pilot/supervisor parameters

## Experimental software scope

Unity is responsible for:

- participant ID and counterbalancing,
- experimental state,
- stationary Tetris,
- fixed post-exposure depth judgement,
- timing,
- CSV logging,
- pausing at external questionnaire/recovery checkpoints.

SSQ and QoE remain outside the headset.

## Core controls

### Tetris

- 10 × 20 board
- left / right
- rotate
- soft drop
- hard drop
- line clearing
- deterministic sequences
- fixed-duration session

### Depth judgement

- same measurement task after both VAC conditions
- fixed reference depth across conditions
- configurable depth differences
- approximately 8 formal trials
- balanced/randomized left-right presentation
- constant apparent target size
- accuracy and reaction-time logging

## Repository structure

Existing experiment logic is under `Assets/Scripts/`.

Important documentation:

- `Docs/M1_DEVICE_SMOKE_TEST.md` — current device smoke test
- `Docs/BEFORE_FIRST_BUILD.md` — general pre-build checklist
- `Docs/UNITY_SETUP.md` — experiment scene wiring
- `Docs/MAGIC_LEAP_2_SETUP.md` — Magic Leap/OpenXR notes

## Development order

1. Device Smoke Test
2. Virtual Depth
3. Input
4. Depth Task
5. Tetris
6. Experiment Flow
7. Full Dry Run
8. Pilot

Each milestone must pass on the real Magic Leap 2 before the next one is treated as complete.
