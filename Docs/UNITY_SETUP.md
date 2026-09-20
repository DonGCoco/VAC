# Unity Scene Wiring

This document describes the minimum scene setup for the current framework.

## 1. Create ExperimentConfig

Create:

`Assets → Create → VAC Experiment → Experiment Config`

Do **not** treat the default Low/High distances as final experimental values. They are placeholders until the Magic Leap model and focal properties are confirmed.

Recommended pilot-editable fields:

- Low VAC Distance
- High VAC Distance
- Tetris Duration Seconds
- Formal Depth Trials
- Practice Depth Trials
- Depth Difference Meters
- Target Horizontal Separation Meters

## 2. Suggested scene hierarchy

```
VACExperiment
├── Managers
│   ├── ParticipantManager
│   ├── DataLogger
│   ├── ExperimentManager
│   ├── ExperimentFlowController
│   ├── VACController
│   ├── DepthJudgmentManager
│   ├── TetrisManager
│   ├── TetrisSequenceManager
│   └── KeyboardDebugInput
│
├── XR / Magic Leap Rig
│   └── ViewerCamera
│
├── VACContentRoot
│   └── TetrisBoard
│
└── DepthTargets
    ├── LeftTarget
    └── RightTarget
```

## 3. Tetris board

Add `TetrisBoard` to the TetrisBoard object.

The current fallback visual creates cubes automatically when no block prefab is assigned. This is useful for testing.

Add:

- `ApparentSizeController` to the VAC content root or board root,
- `VACController` to a manager object,
- the viewer/camera Transform,
- the VAC content root Transform.

The VAC controller places content once relative to the current viewer direction. It does not continuously follow head rotation.

## 4. Depth judgment

Create two simple target GameObjects.

Assign:

- Viewer = headset camera transform
- Left Target
- Right Target
- ExperimentConfig
- DataLogger

The manager creates a balanced Left/Right order. For 20 formal trials it will use 10 Left-closer and 10 Right-closer trials, then shuffle them.

## 5. Tetris

Create and assign:

- TetrisBoard
- TetrisSequenceManager
- TetrisManager
- ExperimentConfig
- DataLogger

Sequence A/B are deterministic 7-bag sequences generated from two Inspector seeds. They are repeatable across participants. Their actual difficulty equivalence must still be checked during pilot testing.

## 6. Desktop testing

Add `KeyboardDebugInput`.

During a Depth Judgment block:

- Left Arrow = answer Left
- Right Arrow = answer Right

During Tetris:

- Left/Right Arrow = move
- Up Arrow = rotate
- Down Arrow = soft drop
- Space = hard drop

This is only for Editor/desktop testing. Later, Magic Leap controller/input actions should call the same public methods.

## 7. Experiment flow

`ExperimentFlowController` automates the in-headset parts:

```
Pre depth → Tetris → Post depth → questionnaire pause
```

Questionnaires remain outside the headset.

Manual checkpoints remain for:

- baseline questionnaire,
- post-condition questionnaire,
- recovery.

## 8. Data

CSV files are created under:

`Application.persistentDataPath/VACExperimentData/`

Questionnaire data is stored separately outside Unity and merged using Participant ID.
