# Realtime Experimenter / Participant Handoff

The distance-calibration UI is split by role.

## Experimenter

The experimenter must see the live distance while the participant is already wearing the Magic Leap 2. This avoids changing eye-to-board distance by calibrating on someone else and then handing over the headset.

Run:

`python3 Tools/ExperimenterMonitor/experimenter_monitor.py`

The script opens a local browser monitor. Headset and laptop should be on the same local network.

No laptop IP is stored in the Unity scene and no per-computer rebuild is required.

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

1. Experimenter starts the monitor on the laptop.
2. Monitor automatically discovers the Magic Leap 2 on the local network.
3. Experimenter selects the condition on the laptop.
4. Participant keeps wearing the headset.
5. Experimenter watches the live distance and verbally says closer/farther.
6. When the laptop monitor reports READY, experimenter says "do not move".
7. Experimenter clicks LOCK on the laptop.
8. Magic Leap creates/tracks the spatial anchor.
9. Only after the state becomes LOCKED does a simple START button appear in the participant headset.
10. Participant presses START with the ML2 controller.
11. START disappears and the task begins.

The participant does not see target distance, actual distance, QR state, condition controls, LOCK or RESCAN.

The old in-headset M2 calibration panel remains available only as a development fallback through the `createRuntimePanel` option; it is disabled by default in the scene.

## Zero-config discovery

The laptop initiates discovery.

Every 0.75 seconds the monitor sends:

`VAC_MONITOR_DISCOVER:45555`

to UDP command port `45556`.

When the headset receives that packet, it reads the sender IP directly from the UDP packet and pairs with that laptop. It then unicasts live status to the sender on UDP `45555`.

Therefore:

- laptop IP does not need to be known in advance,
- changing Wi-Fi does not require changing Unity,
- changing experimenter computers does not require rebuilding the APK,
- the same headset build can be used by different team members.

Experiment commands are accepted only from the currently paired monitor IP.

## Network requirements and fallback

Automatic discovery requires a local network that allows device-to-device UDP broadcast.

Some guest, enterprise, and phone-hotspot networks may isolate clients. No application-level auto-discovery protocol can cross a network that blocks all local device communication.

If broadcast discovery is blocked but direct client-to-client traffic is allowed, run:

`python3 Tools/ExperimenterMonitor/experimenter_monitor.py --headset-ip HEADSET_IP`

For example:

`python3 Tools/ExperimenterMonitor/experimenter_monitor.py --headset-ip 172.20.10.13`

This fallback sends the same discovery packet directly to the headset and still requires no Unity change or APK rebuild.

Android `INTERNET` permission is included in the app manifest.

## Device validation status

The earlier realtime handoff was validated with the Magic Leap 2 and experiment laptop.

The new zero-config discovery handshake still requires one device validation:

- run the monitor with no IP argument,
- confirm the page changes from `Searching for headset...` to `Headset connected`,
- confirm live distance updates,
- confirm C1/C2/C3 and LOCK still work,
- restart the monitor and confirm it automatically re-pairs.

If the current network blocks broadcast, validate the no-rebuild fallback with `--headset-ip`.
