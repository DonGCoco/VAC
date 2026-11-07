# Pilot1 implementation and acceptance — 2026-11-08

## Baseline and scope

Read `README.md`, `PROMPT.txt`, `SPEC.md`, `ACCEPTANCE.md` from `VAC_Pilot1_Handoff (2).zip`. It is a specification package, not a patch. No repository AGENTS.md was found. Initial project worktree was clean; other workspace handoffs, outputs and temporary projects were preserved and excluded from this commit.

Remote check: main was `05434205afc0e3db45af354d2dad44fea372ce17`. PR #5 remained a Draft on `codex/depth-rectangle-response` at `63a94b04e5eac88d043521c9b3307352f2bc4d1d`, the latest applicable rectangle baseline. New independent branch: `fix/pilot1-feedback`. This is a stacked change based on PR #5. PR #3 was not merged or imported; the existing distance definition was not changed.

Implemented version: `Pilot1-20261108`. Preserved C1/C2/C3=0.80/1.00/2.00 m, exposure=900 s, training=180 s, recovery minimum=300 s, existing questionnaire/LOCK/participant gates, T/A/B/C sequences, scoring, sphere positions/diameters/depth compensation, side randomisation, 3 practice/8 formal and response/RT task CSV meanings.

## 1. Participant START, countdown and automatic fall

Existing experimenter assignment/pre-questionnaire/calibration/LOCK prepares the participant START; no extra Monitor START bypass was added. Participant selection consumes that START once and enters `WarmupCountdown` or `FormalCountdown`. The headset displays 3, 2, 1 for three seconds. No first piece, gravity, gameplay input, exposure/placement clock or viewing-distance sample accrues during countdown. On completion, first spawn and exposure clock start together; `onSessionStarted` advances the formal phase and logs `WarmupStarted`/`FormalBlockStarted`. Formal calibration/block rows are written at this actual start, not during a cancelled countdown.

`ExperimentConfig.tetrisFallIntervalSeconds` is shared by training and all conditions. **1.0 s/cell is a temporary configurable candidate**, not user-approved final speed or a literature-established value. No score acceleration is implemented. Gravity uses the existing `StepDown` collision/locking path. Every spawn resets gravity elapsed; a delayed frame can settle its existing piece but cannot apply accumulated time to the next one. Rotation, left/right, bumper hard drop, clear/shift/score and top-out restart are preserved. Held START/bumper must be released after countdown before the gameplay adapter arms; the existing short input guard remains.

Repeated START cannot restart/replace a pending game. Disable, cancel, rescan or spatial LOCK loss during countdown clears pending state, hides countdown and does not emit a completed/depth transition. Pending formal cancellation returns to calibration. Aborting a running formal game records cancellation, returns the formal flow to Idle and requires assignment of a new session before recollection, retaining the aborted session rows. Normal running completion/development early-end retains the existing depth transition. The saved scene has no active legacy ExperimentFlowController; legacy callbacks also refuse to double-launch during an active M6 session.

Monitor now shows waiting for participant / countdown / running and the manager's countdown number, countdown seconds and exposure remaining. Condition changes are blocked while countdown/running. Countdown has no exposure debit. This was checked with synthetic status packets, not a live headset-network session.

## 2. Whole inter-sphere gap excluded

Each trial keeps the original spheres and trial frame. After applying existing angular-size compensation, the actual world centre and radius of each sphere are projected separately onto the common reference plane from the captured trial eye. The inward tangent of the left sphere defines its rectangle's right limit; the inward tangent of the right sphere defines its rectangle's left limit. The result accounts for unequal depth and radius, and is generally asymmetric. It does not reuse a fixed 6 cm gap, `0.12 - constant diameter`, or a symmetric approximation.

For observer-relative horizontal centre x, forward depth z, sphere radius r and plane depth d, the ideal tangent projection is `d * (x*z ± r*sqrt(x*x+z*z-r*r)) / (z*z-r*r)`, plus the observer's plane-x offset. Left inward uses +; right inward uses −. The implementation also projects the actual rendered mesh vertices and conservatively intersects that outline with the ideal sphere envelope: tessellation must not leave a small visible gap selectable. World vertices are cached once per trial; input frames reuse the cache for observer projections. Default world radii come from compensated scale. Invalid/behind-plane/overlapping/missing-mesh projection rejects input. Inner boundaries include a 1 μm outward numerical guard, never tolerance extending into the gap. Outer extent ±0.38 m and height ±0.20 m are retained as layout candidates and explicitly saved in the scene.

**Parallax limit and correction:** one world-fixed plane cannot exactly coincide with the visible edges of two different-depth spheres for every moving head and controller position. The nominal rectangles remain fixed throughout the trial. A further silhouette-gap veto from the actual controller-ray origin and current eye prevents a ray crossing the plane outside the trial gap but still passing through the visual inter-sphere gap. This conservatively shrinks valid response space under motion; it does not move the base regions or arbitrarily increase a constant gap. An outward point can be rejected after large motion, and a pose beyond the plane rejects all input. Exact moving-view edge coincidence is not claimed; this needs ML2 acceptance.

The enabled saved adapter stays bound to the official ML Rig Controller/XRI ray. It uses XRI's current ray origin (including an initialised ray-origin child) and rejects inactive/non-straight rays or missing position/rotation tracking. Existing fresh-press latch and same-frame guard remain. Spheres have no clickable colliders/interactables, no new colour/scale/highlight, and no visible response panels. `Pilot depth geometry v2` logs trial edges/radii/plane; development-only `Pilot depth ray` logs actual origin/direction/selection at most once per second. These are diagnostic logs, not CSV answer rows. The reported pilot video was not provided or analysed, so its failure/root cause is not claimed as reproduced.

## 3. QR size and unchanged sphere separation

Code default, `M1_SmokeTest.unity`, and Editor scene generation use **0.145 m**; size estimation stays disabled. BoardRegistration passes this value to the actual SDK `QRSettings.QRLength`, which MarkerDetector forwards as OpenXR `MarkerLength`. Installed SDK 2.6.0 describes it as QR marker length in metres; its local comments do not resolve the physical white-border measurement convention for this print.

**Physical measurement scope remains UNVERIFIED:** record pattern-only width and border/paper width separately. Do not claim distance accuracy until the correct scope is confirmed and the marker is freshly RESCANned/ruler-validated. Old locked anchors are not valid calibration evidence after this change. Two-sphere centre separation is still **0.12 m**. No global 0.12 replacement or scene-scale compensation was performed.

Actual/READY/locked/CSV viewing distance remains the baseline Main Camera-to-BoardAnchor-centre distance, distinct from eye-to-game-centre and plane-normal distances. Existing geometry diagnostics remain for comparison. This QR change is not a game-centre distance-definition change or proof of corrected physical depth.

## Versioning and old data

The config asset and code default explicitly use `protocolVersion=Pilot1-20261108`. New session.csv columns record `software_revision`, `tetris_mode`, `fall_interval_s`, `start_mechanism`, actual `qr_size_m`, `qr_size_scope` (unverified) and `depth_interaction_version=sphere-silhouette-rectangles-v2`. Task CSV response/RT schema and export mechanism are preserved. Every run still creates a fresh session directory; old exports are not rewritten, backfilled or corrected. Do not directly pool first-pilot static/sphere-click/fixed-gap data with this version.

## Executed software checks

- `git diff --check`: PASS.
- Existing offline handoff check updated for 0.145 m QR and the configurable fall candidate: PASS for 15 code/asset numeric values and original geometry/LOCK checks. It reports **−76.5 mm** default maximum-scale game/QR-pattern clearance (overlap), excluding white border. The larger QR and existing 40 cm offset/maximum scale therefore still need a physical layout/FOV decision; this implementation did not silently alter the layout/conditions.
- Actual Unity C# depth checks: **95 PASS**, including both rendered-mesh silhouette comparisons, far-side swaps, full projected gap sweeps, edge-outward selection, world-fixed geometry, controller offset, head motion, fresh-press/tracking recovery, 3/8 completion and 4/4 formal balancing.
- Actual Unity **Play Mode** Tetris/flow/metadata checks: **36 PASS**, including training/formal countdown gates, repeated start, no countdown input/sample/first piece, actual-start timing, gravity/collision/new-piece reset, hard drop, movement, clear/score/shift, top-out, disable/cancel/late-tick ghost protection, held-trigger arm gate, questionnaire/LOCK gating, formal-row timing, cancellation/new-session handling and real metadata writer.
- Actual Monitor embedded JavaScript executed under Node with synthetic status packets: waiting, 3/2/1, running exposure time, countdown button locks, questionnaire and recovery PASS. No physical network/Monitor connection was certified.
- Actual DataLogger synthetic session.csv parsed independently with Python CSV: new columns align; version, fall=1, QR=0.145, depth version and 3/8 counts PASS. Synthetic fixture IDs are explicitly prefixed `PILOT1_SYNTHETIC`; these are not participant/ML2 data.
- Validation environment: installed Unity **6000.3.24f1**, ML SDK **2.6.0**, XRI **3.0.9**, existing Input System. Project records **6000.2.15f1**, unavailable here: that exact-version verification is pending. Checks/build use a separate local copy; unrelated Packages/ProjectSettings upgrades are not submitted.
- Android Development build: **Succeeded, errors=0; exit code 0**, IL2CPP/x86-64/Vulkan. Final local APK outside checkout: `../outputs/pilot1-20261108/VAC-Pilot1.apk`, **61,026,540 bytes**; SHA-256 `733B06420F14AF7099151073545649F7A65AC42D88A78920EDE467CF4A954545`. A local validation-only package ID `com.DefaultCompany.VAC.Pilot1Validation` avoids overwriting existing headset apps/data; the repository's application ID is unchanged.

Local evidence outside checkout: `../outputs/pilot1-20261108/unity-pilot-checks.log`, `unity-depth-checks.log`, `unity-build.log` and `synthetic-session/session.csv`. Synthetic metadata was generated by the actual logger; it is not ML2 participant evidence. SDK obsolete-API and Unity Magic Leap architecture deprecation warnings remain; the build has no reported errors.

Reproduce: Unity menu `VAC/Checks/Pilot1 countdown gravity and depth` (or batch `-executeMethod VACExperiment.EditorTools.VACPilot1Checks.Run`); `node Tools/check_pilot1_monitor.mjs`; `python Tools/check_vac_handoff.py`. The Unity check protects unsaved editor scenes, then uses an isolated Play Mode fixture and synthetic elapsed time/input; successful checks are not hardware acceptance.

## ML2 — all pending (no attached device)

ADB found no attached ML2 at implementation time. None of these items is marked passed:

1. Install/launch current APK with a dedicated new test ID, permissions and correct visible/controller ray binding.
2. Existing experimenter preparation → participant START → visible 3/2/1; timing/input/first piece inactive until completion; held trigger; continuous gravity/rotation/hard drop/collision; disable/rescan/early cancellation.
3. Sweep the entire visual inter-sphere region: -- and no answer/CSV row/trial advance, both far-side assignments. Check each inward visual edge, outer/vertical bounds, head motion, multiple controller offsets and the conservative parallax-veto limit. Track loss/recovery and held sweeps must require release/repress.
4. Fresh RESCAN using the 14.5 cm print after resolving border convention. Use one agreed eye reference for ruler measurements at configured conditions; separately record QR-centre, plane-normal and game-centre estimates. No legacy anchor reuse or arbitrary Z fix.
5. All three conditions, 3 practice and 8 formal each, questionnaires/recovery, Tetris→depth, finish and hotspot export. Inspect 24 actual formal depth rows, correct sides/responses/RT and new version metadata with the existing session validator. A structural CSV PASS alone is not distance/geometry validity.
6. Confirm game/QR clearance, physical board boundaries and usable FOV at maximum condition. Freeze fall interval only after the next pilot decision; record all parameter/layout changes with the new data version.
