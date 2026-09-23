# Unity Scene Wiring

This document describes the minimum scene setup for the revised Magic Leap 2 VAC protocol.

## 1. Create ExperimentConfig

Create an ExperimentConfig asset from:

**Assets → Create → VAC Experiment → Experiment Config**

The current numeric values are placeholders. Final values must be frozen after the pilot.

Pilot-editable fields:

- Low VAC Distance
- High VAC Distance
- Tetris Duration Seconds
- Depth Task Reference Distance
- Formal Depth Trials
- Practice Depth Trials
- Depth Difference
- Target Horizontal Separation
- Target Reference Scale Distance

The revised plan expects roughly **8–12 formal depth trials**; the code currently defaults to 10.

## 2. Suggested hierarchy

~~~
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
├── Magic Leap 2 XR Origin / ML Rig
│   └── Main Camera
│
├── VACContentRoot
│   └── TetrisBoard
│
└── DepthTargets
    ├── LeftTarget
    └── RightTarget
~~~

## 3. Tetris / VAC content

Add:

- TetrisBoard to the board object
- ApparentSizeController to VACContentRoot or the board root
- VACController to a manager object

Assign:

- ExperimentConfig
- Viewer = headset Main Camera transform
- Content Root = VACContentRoot
- ApparentSizeController

VACController positions the content once when a formal condition begins. It does not continuously follow the participant's head.

The apparent-size controller scales the board proportionally with distance.

## 4. Depth judgment

Create two visually identical target GameObjects.

Assign to DepthJudgmentManager:

- ExperimentConfig
- Viewer = headset Main Camera
- Left Target
- Right Target
- DataLogger

The formal depth task is performed **after Tetris only**.

The task uses the same Depth Task Reference Distance after Low and High VAC. Near/far targets are placed symmetrically around that reference depth.

The manager also scales each target according to its individual depth. This removes apparent target size as an easy cue.

For the default 10 trials, five trials will be Left-closer and five Right-closer, shuffled.

## 5. Tetris

Create and assign:

- TetrisBoard
- TetrisSequenceManager
- TetrisManager
- ExperimentConfig
- DataLogger

Sequence A/B are deterministic 7-bag sequences generated from two Inspector seeds. Their difficulty equivalence should be checked in the pilot.

## 6. Desktop testing

Add KeyboardDebugInput.

Depth task:

- Left Arrow = answer Left
- Right Arrow = answer Right

Tetris:

- Left / Right Arrow = move
- Up Arrow = rotate
- Down Arrow = soft drop
- Space = hard drop

These controls are for Editor testing only. Magic Leap OpenXR controller actions should later call the same public methods.

## 7. Revised experiment flow

The supervised/manual checkpoints are intentional because SSQ is completed outside the headset.

~~~
Begin participant
→ Initial SSQ pause
→ ContinueAfterInitialQuestionnaire
→ Warm-up / familiarisation
→ FinishTraining
→ Pre-Condition 1 SSQ pause
→ ContinueAfterPreConditionQuestionnaire
→ Tetris Condition 1
→ Post-exposure depth task
→ Post-Condition 1 SSQ pause
→ Recovery
→ ContinueAfterRecovery
→ Pre-Condition 2 SSQ pause
→ ContinueAfterPreConditionQuestionnaire
→ Tetris Condition 2
→ Post-exposure depth task
→ Post-Condition 2 SSQ pause
→ Complete
~~~

## 8. Data

CSV files are created under:

**Application.persistentDataPath/VACExperimentData/**

Questionnaire data is stored separately and merged using Participant ID.

Before formal collection, run a full dry run and verify that participant ID, condition order, sequence ID, Tetris summary, depth accuracy, and reaction time are all written correctly.
