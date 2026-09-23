# Before the First Magic Leap 2 Build

Use this checklist before turning the repository into the device-tested Unity project.

## Repository hygiene

Do not commit generated Unity folders such as:

- Library
- Temp
- Obj
- Logs
- UserSettings
- Builds
- IDE-generated solution/project files

The repository .gitignore already excludes the main generated folders.

When a full Unity project is created locally, **do commit**:

- Assets
- Packages
- ProjectSettings

This is what makes the project reproducible on another machine.

## Unity version

For the current setup, use **Unity 6.3 LTS (6000.3.24f1)** consistently across the team if possible.

Avoid having different team members open and resave the project in different Unity major/minor versions unless necessary.

## Android modules

The Unity installation needs:

- Android Build Support
- Android SDK & NDK Tools
- OpenJDK

## Magic Leap SDK

Install the Magic Leap Unity SDK and configure OpenXR.

The SDK can be installed from the package downloaded by Magic Leap Hub or from the Magic Leap scoped registry.

Run the Magic Leap Project Setup Tool and then verify the settings manually.

## Unity 6 setting

For Unity 6:

**Project Settings → Player → Other Settings → Application Entry Point**

Use:

- Activity = enabled
- GameActivity = disabled

## Required build settings

Verify:

- Android target
- OpenXR enabled
- Magic Leap 2 Support enabled
- Magic Leap 2 Controller Interaction Profile enabled when controller input is added
- Vulkan only
- Minimum API Level 29
- IL2CPP
- x86-64

Run OpenXR Project Validation and resolve errors before building.

## Experimental values

Do not treat current distances as final.

Before formal testing, confirm:

- optical focal distance
- Low VAC distance
- High VAC distance
- final exposure duration
- depth-task settings
- recovery rule

Keep the Display Zone setting fixed across all participants and conditions.

## First technical milestone

Do not start with the full Tetris experiment.

First confirm on the actual Magic Leap 2 that the project can:

1. build and launch,
2. place one simple object at a known virtual distance,
3. switch between two virtual distances,
4. keep apparent angular size approximately constant,
5. receive controller input,
6. write a test CSV file.

After that works, wire in the complete Tetris and experiment flow.
