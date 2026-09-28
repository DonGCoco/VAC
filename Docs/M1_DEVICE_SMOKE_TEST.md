# Milestone 1 — Magic Leap 2 Device Smoke Test

Goal: prove the Unity/OpenXR/Magic Leap toolchain works on the real headset before adding experiment logic.

## Acceptance criteria

Milestone 1 passes only when:

1. Unity opens the repository with Unity 6.3 LTS (6000.3.24f1).
2. Magic Leap OpenXR settings validate without blocking errors.
3. `M1_SmokeTest.unity` builds and installs to the connected Magic Leap 2.
4. The app launches on-device.
5. A simple cube is visible in the headset.

Do not start Milestone 2 until all five are true.

## One-time Unity setup

The repository now contains:

- `Packages/manifest.json` with Magic Leap SDK 2.6.0, OpenXR, XR Plug-in Management, XR Interaction Toolkit and Input System.
- `ProjectSettings/ProjectVersion.txt` pinned to Unity 6000.3.24f1.
- an Editor helper that creates the smoke-test scene without hand-writing Unity scene YAML.

After opening the project:

1. Let Unity finish resolving packages.
2. Open **Window > Package Manager**.
3. Select the Magic Leap SDK package and import the **ML Rig & Inputs** sample.
4. Configure the project for Magic Leap 2. The official recommended route is the Magic Leap Project Setup Tool.
5. For Unity 6, verify **Project Settings > Player > Other Settings > Application Entry Point**:
   - Activity: enabled
   - GameActivity: disabled
6. Verify Android/OpenXR configuration:
   - Android target
   - OpenXR provider enabled for Android
   - Magic Leap feature group enabled
   - Magic Leap 2 Support enabled
   - Magic Leap 2 Controller Interaction Profile enabled
   - Vulkan only
   - Minimum API Level 29
   - IL2CPP
   - x86-64
7. Run **Window > XR > OpenXR > Project Validation** and fix blocking errors.

## Create the smoke-test scene

After importing the ML Rig & Inputs sample:

**VAC > Milestone 1 > Create Smoke Test Scene**

The helper will:

- create a new scene,
- instantiate the official Magic Leap ML Rig prefab,
- set the rig camera near clip to 0.25 m,
- create one cube at (0, 0, 1 m),
- set the cube rotation to (0, 65, 0),
- set cube scale to 0.25 m,
- add a directional light,
- save the scene as `Assets/Scenes/M1_SmokeTest.unity`,
- put the scene first in the build list.

This mirrors Magic Leap's simple OpenXR smoke-test structure while avoiding hand-authored scene YAML.

## Build to the device

Keep the Magic Leap 2 connected by USB and visible in Magic Leap Hub 3.

In Unity:

1. Switch the build target to Android if needed.
2. Confirm only `M1_SmokeTest.unity` is enabled for the smoke test.
3. Choose **Build And Run**.
4. If Unity warns that the Android SDK is outdated, choose **Use Highest Installed**.
5. Put on the headset and confirm that the cube is visible.

## Stop condition

If the cube does not appear, do not continue to Virtual Depth, controller input, Tetris or the experiment flow.

Capture:

- Unity Console errors,
- OpenXR Project Validation,
- the Build/Build Profile window,
- and the Magic Leap Hub device connection state.

Fix Milestone 1 first.
