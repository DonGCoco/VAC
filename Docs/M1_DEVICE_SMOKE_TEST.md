# Milestone 1 — Magic Leap 2 Device Smoke Test

Goal: prove the Unity/OpenXR/Magic Leap toolchain works on the real headset before adding experiment logic.

## Working rule for this project

During development, tunable experimental values must be visible rather than buried in code.

For Milestone 1, select **M1_SmokeTest_Cube** in the Hierarchy. The Inspector exposes:

- Viewer
- Distance Meters
- Vertical Offset Meters
- Rotation Offset Degrees
- Keep Apparent Angular Size
- Reference Distance Meters
- Reference Cube Size Meters
- Place Once On Start

The initial values are only test defaults. They are not final experimental values.

From Milestone 2 onward, study parameters that belong to the actual experiment should live in the central **ExperimentConfig** asset so the team can change them without editing C#.

## Acceptance criteria

Milestone 1 passes only when:

1. Unity opens the repository with Unity 6.2 (6000.2.15f1).
2. Magic Leap OpenXR settings validate without blocking errors.
3. `M1_SmokeTest.unity` builds and installs to the connected Magic Leap 2.
4. The app launches on-device.
5. A simple cube is visible in the headset.

Do not start Milestone 2 until all five are true.

## One-time Unity setup

1. Let Unity finish resolving packages.
2. Open **Window > Package Manager**.
3. Select the Magic Leap SDK package and import the **ML Rig & OpenXR Input** sample.
4. Configure the project for Magic Leap 2.
5. Verify Android/OpenXR configuration:
   - Android target
   - OpenXR provider enabled for Android
   - Magic Leap 2 feature group enabled
   - Magic Leap 2 Controller Interaction Profile enabled
   - Vulkan only
   - Minimum API Level 29
   - IL2CPP
   - x86-64
6. Run **Window > XR > OpenXR > Project Validation** and fix blocking errors.

## Create the smoke-test scene

After importing the ML Rig & OpenXR Input sample:

**VAC > Milestone 1 > Create Smoke Test Scene**

The helper creates the scene, official ML Rig, smoke-test cube and directional light.

Then select:

**Hierarchy > M1_SmokeTest_Cube**

and inspect the editable parameters before building.

For the first run, leave:

- Distance Meters = 1.0
- Keep Apparent Angular Size = enabled
- Reference Distance Meters = 1.0
- Reference Cube Size Meters = 0.25
- Place Once On Start = enabled

The cube is placed once when the app starts and remains world-fixed.

## Build to the device

Keep the Magic Leap 2 connected by USB and visible in Magic Leap Hub 3.

1. Switch the build target to Android if needed.
2. Confirm `M1_SmokeTest.unity` is enabled.
3. Choose **Build And Run**.
4. Put on the headset and confirm that the cube is visible.

## Stop condition

If the cube does not appear, do not continue to Milestone 2.

Capture the Unity Console, OpenXR Project Validation, Build window, and Hub connection state so the failure can be diagnosed.
