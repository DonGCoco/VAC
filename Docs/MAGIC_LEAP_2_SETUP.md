# Magic Leap 2 Setup for the VAC Experiment

The target headset for this project is **Magic Leap 2**.

## 1. Recommended Unity workflow

Use the current Magic Leap **OpenXR** workflow.

Magic Leap's current documentation recommends:

- Unity 2022.3 LTS or later
- Android Build Support
- Android SDK & NDK Tools
- OpenJDK
- Magic Leap Unity SDK
- Unity OpenXR Plugin 1.10.0 or later
- OpenXR enabled for Android
- Magic Leap 2 Support feature enabled
- Magic Leap 2 Controller Interaction Profile enabled if the controller is used

The Magic Leap Project Setup Tool can configure most required project settings automatically.

## 2. Important distinction: optical focal plane vs Unity Focus Distance

For this experiment, **do not use the Magic Leap Focus Distance / Stereo Convergence API as the VAC manipulation**.

Magic Leap 2 uses a **single fixed optical focal plane** for virtual content.

The Unity/OpenXR "Focus Distance" / "Stereo Convergence" setting is used for visual stabilization / content registration. It changes rendering/reprojection behavior; it does not physically move the headset's optical focal plane.

Our VAC manipulation should therefore be implemented by changing the **virtual geometry depth** of the stimulus:

- Low VAC: virtual content placed at one vergence distance
- High VAC: virtual content placed at another vergence distance

while accommodation remains determined by the headset's fixed optical focal plane.

This is what the existing `VACController` is designed to do.

## 3. Do not hard-code the final experimental distances yet

The current distances in `ExperimentConfig` are placeholders.

Before formal data collection we still need to confirm the intended Low/High VAC values with the supervisor and pilot test them.

A number around 0.74 m is reported in recent research as the Magic Leap 2 focal plane, but the current Magic Leap developer documentation we checked describes a fixed focal plane without publishing that exact numerical value on the VAC page. Therefore, do not treat 0.74 m as a final experimental constant until it is confirmed for our study.

## 4. Display Zone

Magic Leap 2 uses a Display Zone near boundary to protect users from uncomfortable near content.

The default near boundary is 0.37 m.

On supported OS versions it can be adjusted down to 0.25 m, but Magic Leap warns that closer content can increase discomfort.

For this study, we should avoid changing this system setting between conditions. Keep it fixed for all participants.

## 5. Scene setup

Use the Magic Leap ML Rig / XR Origin sample as the headset rig.

Recommended structure:

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
│   └── TetrisSequenceManager
│
├── ML Rig / XR Origin
│   └── Main Camera
│
├── VACContentRoot
│   └── TetrisBoard
│
└── DepthTargets
    ├── LeftTarget
    └── RightTarget
```

Assign the ML Rig's Main Camera transform as the `viewer` reference in:

- `VACController`
- `DepthJudgmentManager`

## 6. Controller input

Use the Magic Leap 2 Controller Interaction Profile through OpenXR.

The experiment only needs a small control set:

### Tetris

- Left / Right
- Rotate
- Soft drop
- Hard drop

### Depth judgment

- Left response
- Right response

The experiment logic already exposes public methods for these actions. The ML2 input layer only needs to bind OpenXR input actions to those methods.

## 7. Spatial behavior

The content should be positioned once relative to the participant at the start of a condition and then remain spatially fixed.

Do not continuously parent the Tetris board to the headset camera.

This reduces the chance of introducing an additional head-locked visual cue into the experiment.

## 8. Constant apparent size

When virtual depth changes, world-space scale must also change so the Tetris board subtends approximately the same visual angle in Low and High VAC conditions.

The existing `ApparentSizeController` performs this proportional scaling.

This is important because otherwise Tetris performance could differ simply because one board appears larger.

## 9. Questionnaires

All questionnaires are completed **outside the headset**.

Recommended order:

```
Participant information
→ Baseline questionnaire BEFORE putting on the headset
→ Magic Leap 2 setup
→ Short in-headset training
→ Condition 1
→ Headset-off post-condition questionnaire
→ Recovery
→ Condition 2
→ Headset-off post-condition questionnaire
```

A short verbal comfort/visibility check may be performed after headset setup and training, but it is not part of the formal questionnaire dataset.

## 10. Remaining study decisions

Still to confirm before formal data collection:

- exact Low VAC vergence distance
- exact High VAC vergence distance
- whether both conditions remain on the same side of the optical focal plane
- formal Depth Judgment reference depth and disparity
- Tetris duration after pilot testing
- number of formal Depth Judgment trials
