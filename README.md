# VAC — Magic Leap Experiment Framework

Group 6 experimental codebase for studying whether different levels of vergence–accommodation conflict (VAC) affect:

1. general task performance (stationary Tetris),
2. stereoscopic depth-judgment performance, and
3. subjective visual discomfort (questionnaires are completed outside the headset).

## Current scope

This first version intentionally does **not** depend on Magic Leap 1 or Magic Leap 2 SDK-specific APIs. The exact headset model and optical focal properties are still to be confirmed.

The current framework provides:

- participant ID based counterbalancing,
- Low/High VAC distances exposed as Unity Inspector parameters,
- training vs. formal-experiment state,
- spatial placement for Low/High VAC content,
- automatic apparent-size compensation,
- balanced Left/Right depth-judgment trials,
- reaction-time and accuracy logging,
- CSV event/depth/Tetris summary logging,
- central experiment configuration.

The next implementation layer will add the stationary Tetris game and Magic Leap input/anchor integration.

## Planned experiment flow

```
Participant setup
→ Magic Leap setup
→ Training mode
   → Tetris training
   → 3–5 depth-judgment practice trials
→ Headset-off baseline questionnaire
→ Condition 1
   → Pre depth judgment
   → 15–20 min Tetris
   → Post depth judgment
→ Headset-off post-condition questionnaire
→ Recovery
→ Condition 2
   → Pre depth judgment
   → 15–20 min Tetris
   → Post depth judgment
→ Headset-off post-condition questionnaire
```

All questionnaires are completed **outside the headset** and matched later using Participant ID.

## Counterbalancing

The current participant assignment cycles through four combinations:

| Participant pattern | Condition 1 | Condition 2 |
|---|---|---|
| 1 mod 4 | Low + Sequence A | High + Sequence B |
| 2 mod 4 | High + Sequence A | Low + Sequence B |
| 3 mod 4 | Low + Sequence B | High + Sequence A |
| 0 mod 4 | High + Sequence B | Low + Sequence A |

Example: P01, P02, P03, P04.

## Unity setup

1. Create/open a Unity project that targets the confirmed Magic Leap device.
2. Copy/keep the scripts under `Assets/Scripts/`.
3. Create an `ExperimentConfig` asset via:
   `Assets → Create → VAC Experiment → Experiment Config`.
4. In the Inspector, set:
   - Low VAC Distance
   - High VAC Distance
   - Tetris Duration
   - Formal Depth Trials
   - Practice Depth Trials
   - Depth Difference
5. Add `ParticipantManager`, `DataLogger`, `VACController`, `DepthJudgmentManager`, and `ExperimentManager` to scene GameObjects and wire references.
6. Connect Magic Leap input later to the public Left/Right response methods and Tetris controls.

## Why Inspector parameters?

Final VAC distances are not known yet. By exposing them in the Inspector, pilot-test values can be changed without rewriting experiment logic. Example:

```
Low VAC Distance: 1.0 → 1.2
High VAC Distance: 2.0 → 2.5
```

The same applies to Tetris duration, depth difference, and trial counts.

## Data

CSV files are written under Unity's `Application.persistentDataPath`.

Formal depth trials contain:

- participant ID,
- VAC condition,
- Pre/Post phase,
- trial number,
- reference depth,
- depth difference,
- closer side,
- participant response,
- correctness,
- reaction time.

Tetris summaries will contain:

- participant ID,
- VAC condition,
- sequence ID,
- duration,
- score,
- lines cleared,
- pieces placed,
- average placement time,
- top-outs.

## Important

Before finalizing Low/High VAC distances, confirm:

- exact Magic Leap model,
- optical focal distance/focal planes,
- whether focal-plane switching occurs,
- comfortable virtual-depth range,
- whether the final experiment should keep both VAC levels on the same conflict-direction side of the focal plane.
