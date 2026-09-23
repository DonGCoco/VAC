# Magic Leap 2 Setup for the VAC Experiment

The target headset is **Magic Leap 2** and the project should use the **OpenXR** workflow.

## 1. Development environment

Current project target:

- Unity 6.3 LTS
- Android Build Support
- Android SDK & NDK Tools
- OpenJDK
- Magic Leap Unity SDK
- Unity OpenXR Plugin
- OpenXR enabled for Android

The Magic Leap Project Setup Tool is the easiest way to apply the required project settings.

For Unity 6, make sure **Application Entry Point = Activity only** and GameActivity is disabled.

## 2. Recommended Android / XR settings

Before the first device build, verify:

- OpenXR provider enabled under Android
- Magic Leap feature group / Magic Leap 2 Support enabled
- Magic Leap 2 Controller Interaction Profile enabled if using the controller
- Vulkan as the graphics API
- Minimum API Level 29
- IL2CPP scripting backend
- x86-64 target architecture

Use OpenXR Project Validation before building.

## 3. Optical focal plane vs Focus Distance

Do **not** use Magic Leap Focus Distance / Stereo Convergence as the VAC manipulation.

Magic Leap 2 documentation describes XR content as being viewed through a fixed optical focal plane. The rendering Focus Distance setting is for stabilization / registration and does not physically move that optical focal plane.

VAC should therefore be manipulated by changing **virtual geometry depth**:

- Low VAC = one virtual vergence distance
- High VAC = another virtual vergence distance

The exact optical focal distance used for the study must still be confirmed before calculating the final conflict magnitudes in diopters.

## 4. Display Zone

Magic Leap 2's default near Display Zone boundary is 0.37 m.

Do not change this setting between conditions.

Avoid choosing a formal stimulus distance below the current safe/approved setup simply to create a larger conflict. Final distances must be agreed with the supervisor and checked in the pilot.

## 5. Scene setup

Use the Magic Leap XR Origin / ML Rig camera as the viewer reference.

Recommended structure:

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
│   └── TetrisSequenceManager
├── XR Origin / ML Rig
│   └── Main Camera
├── VACContentRoot
│   └── TetrisBoard
└── DepthTargets
    ├── LeftTarget
    └── RightTarget
~~~

Assign Main Camera as the viewer in VACController and DepthJudgmentManager.

## 6. Controller input

The experiment only needs a small set of actions.

Tetris:

- Left
- Right
- Rotate
- Soft drop
- Hard drop

Depth judgment:

- Left response
- Right response

The experiment code already exposes public methods for these actions. The Magic Leap input layer only needs to bind OpenXR actions to them.

## 7. Spatial behavior

The Tetris content should be positioned once at condition start and then remain spatially fixed.

Do not continuously parent the board to the headset camera.

## 8. Constant apparent size

When virtual Tetris depth changes, board world scale must change proportionally so the board subtends approximately the same visual angle.

The post-exposure depth task also compensates target size according to target depth so that participants cannot solve it simply by selecting the apparently larger target.

## 9. Questionnaires

All SSQ / QoE questionnaires are completed outside the headset.

Current flow:

~~~
Initial SSQ
→ Warm-up
→ Pre-Condition 1 SSQ
→ Condition 1 Tetris
→ Depth task
→ Post-Condition 1 SSQ
→ Recovery
→ Pre-Condition 2 SSQ
→ Condition 2 Tetris
→ Depth task
→ Post-Condition 2 SSQ
~~~

## 10. Remaining study decisions

Still to confirm before formal data collection:

- optical focal distance used for the study
- exact Low VAC virtual distance
- exact High VAC virtual distance
- whether both conditions remain on the same side of the focal plane
- final Tetris exposure duration
- final depth-task reference depth and depth difference
- final number of depth trials
- recovery rule after pilot testing
- QoE questionnaire requested by Martin
