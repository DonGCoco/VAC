# Brightness diagnostic

Open the saved Pilot scene with Android selected, then use **VAC → Build Brightness Diagnostic APK**. Install `Builds/VAC-Brightness-Diagnostic.apk` and open **VAC Brightness Diagnostic**.

Face a uniform wall and set the viewing direction once with the trigger. Stay in the same seat and posture for all groups. The headset shows eight trials per group, with no distance labels.

| Group | Material | Depth difference |
| --- | --- | --- |
| 1 | Original Pilot | 0 cm |
| 2 | Original Pilot | Pilot setting (6 cm) |
| 3 | Shared opaque gray Unlit | 0 cm |
| 4 | Shared opaque gray Unlit | Pilot setting (6 cm) |

Compare the two balls, then press the trigger to hide them. On the answer screen, turn your head left or right to select the darker side, or keep it centered for equal brightness. The prompt shows the selection. Press the trigger again to save the answer, then face the original wall marker before the next comparison.

Each saved trial is appended to `brightness_*.csv` in the headset app's persistent-data directory. With the headset connected:

```bash
adb pull /sdcard/Android/data/com.vacexperiment.brightnessdiagnostic/files/ ./brightness-results/
```

Compare the darker side with the logged farther side in groups 2 and 4. Groups 1 and 3 check for a fixed side bias at equal depth. Group order is fixed for debugging; these runs are not formal experiment sessions.
