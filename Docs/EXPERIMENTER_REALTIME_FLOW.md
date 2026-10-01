# Realtime Experimenter / Participant Handoff

The distance-calibration UI is split by role.

## Experimenter

The experimenter must see the live distance while the participant is already wearing the Magic Leap 2. This avoids changing eye-to-board distance by calibrating on someone else and then handing over the headset.

Run:

`python3 Tools/ExperimenterMonitor/experimenter_monitor.py`

The script opens a local browser monitor. Headset and laptop should be on the same network.

The experimenter monitor receives live:

- C1 / C2 / C3
- target distance
- actual headset-to-board distance
- Too Close / Ready / Too Far
- spatial-anchor status
- Tetris running status

The experimenter can send:

- C1 / C2 / C3
- LOCK
- RESCAN

There is deliberately no remote START command.

## Participant

The participant wears the headset throughout calibration.

Formal handoff:

1. Experimenter selects the condition on the laptop.
2. Participant keeps wearing the headset.
3. Experimenter watches the live distance and verbally says closer/farther.
4. When the laptop monitor reports READY, experimenter says "do not move".
5. Experimenter clicks LOCK on the laptop.
6. Magic Leap creates/tracks the spatial anchor.
7. Only after the state becomes LOCKED does a simple START button appear in the participant headset.
8. Participant presses START with the ML2 controller.
9. START disappears and the task begins.

The participant does not see target distance, actual distance, QR state, condition controls, LOCK or RESCAN.

The old in-headset M2 calibration panel remains available only as a development fallback through the `createRuntimePanel` option; it is disabled by default in the scene.

## Network

The headset publishes status at 10 Hz over UDP.

Defaults:

- status port: 45555
- command port: 45556
- status destination: 255.255.255.255 (broadcast)

If the experiment network blocks broadcast, set `ExperimenterBridge.experimenterHost` in Unity to the laptop's IPv4 address. No experiment logic depends on a specific IP address.

Android `INTERNET` permission is included in the app manifest.
