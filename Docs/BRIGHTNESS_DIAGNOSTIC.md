# Brightness diagnostic

Open the saved Pilot scene with Android selected, then use **VAC → Build Brightness Diagnostic APK**. Install `Builds/VAC-Brightness-Diagnostic.apk` and open **VAC Brightness Diagnostic**.

Face a uniform wall and set the viewing direction once with the trigger. Stay in the same seat and posture for all groups. The headset shows eight trials per group, with no distance labels.

| Group | Material | Depth difference |
| --- | --- | --- |
| 1 | Original Pilot | Pilot setting (6 cm) |
| 2 | Shared opaque gray Unlit | 0 cm |
| 3 | Shared opaque gray Unlit | Pilot setting (6 cm) |

Call out left darker, right darker, or equal before pressing the trigger. Have the operator record the response by group and trial number. Inspect the geometry log after the test to avoid revealing the far side.

```bash
adb logcat -d -s Unity | grep 'VACBrightnessDiagnostic group='
```

Compare the darker side with the logged farther side in groups 1 and 3. Group 2 checks for a fixed side bias at equal depth. Group order is fixed for debugging; these runs are not formal experiment sessions.
