# VAC — Magic Leap 2 Experiment Framework

Group 5 experimental codebase for studying whether different levels of vergence–accommodation conflict (VAC) affect:

1. visual / oculomotor discomfort,
2. post-exposure depth-judgment performance, and
3. Tetris task-specific performance.

## Study design

- Target device: **Magic Leap 2**
- Design: **within-subject**
- Formal sample: **10 participants**
- Pilot: **4 participants**
- Conditions: **Low VAC** and **High VAC**
- Counterbalancing: 5 participants Low → High, 5 participants High → Low
- Primary symptom outcome: change in **SSQ Oculomotor score**
- Primary depth outcome: **depth-judgment accuracy**

The exact Low/High virtual distances are deliberately configurable rather than hard-coded. Final distances, exposure duration, depth-task difficulty, and recovery procedure are pilot parameters.

## Current formal flow

~~~
Consent + demographics
→ Initial SSQ (outside headset)
→ Warm-up / familiarisation
→ Pre-Condition 1 SSQ (outside headset)
→ Condition 1: Tetris
→ Short post-exposure depth-judgment task
→ Post-Condition 1 SSQ (outside headset)
→ Recovery
→ Pre-Condition 2 SSQ (outside headset)
→ Condition 2: Tetris
→ Short post-exposure depth-judgment task
→ Post-Condition 2 SSQ (outside headset)
~~~

Questionnaires remain outside the headset and are matched to Unity data using Participant ID.

## Current framework

The repository currently provides:

- participant-ID based condition/sequence counterbalancing,
- Low/High virtual distances exposed as Inspector parameters,
- one-time spatial placement of the Tetris board,
- automatic apparent-size compensation when viewing distance changes,
- a playable baseline Tetris implementation,
- deterministic Sequence A/B generation,
- timed Tetris performance logging,
- a short post-exposure depth-judgment task,
- constant apparent target size in the depth task,
- balanced Left/Right closer-target trials,
- reaction-time and accuracy logging,
- CSV event/depth/Tetris logging,
- desktop keyboard input for Editor testing.

Magic Leap 2 OpenXR controller bindings and the final Unity scene still need to be wired and device-tested.

## Important experimental controls

### Tetris board

The board is placed at the selected virtual depth once at condition start and remains world-fixed. It should not remain head-locked.

When viewing distance changes, the board is scaled proportionally so that its apparent angular size remains approximately constant.

### Depth task

The depth task uses the **same reference depth in both VAC conditions** so that the test itself does not introduce a second condition difference.

The two targets are scaled according to their individual depths so that apparent target size cannot be used as an easy monocular cue.

### VAC manipulation

Do not use Magic Leap Focus Distance / Stereo Convergence as the VAC manipulation. The manipulation is the virtual geometry depth of the stimulus.

The exact optical focal distance and final VAC magnitudes in diopters must be confirmed before formal data collection.

## Counterbalancing

The participant assignment cycles through four condition/sequence combinations:

| Participant pattern | Condition 1 | Condition 2 |
|---|---|---|
| 1 mod 4 | Low + Sequence A | High + Sequence B |
| 2 mod 4 | High + Sequence A | Low + Sequence B |
| 3 mod 4 | Low + Sequence B | High + Sequence A |
| 0 mod 4 | High + Sequence B | Low + Sequence A |

For P01–P10 this gives 5 Low→High and 5 High→Low participants.

## Data

CSV files are written under Unity's Application.persistentDataPath.

Depth-trial rows include:

- participant ID,
- exposure condition,
- trial number,
- reference depth,
- depth difference,
- closer side,
- participant response,
- correctness,
- reaction time.

Tetris summaries include:

- participant ID,
- VAC condition,
- sequence ID,
- duration,
- score,
- lines cleared,
- pieces placed,
- average placement time,
- top-outs.

## Setup

See:

- **Docs/BEFORE_FIRST_BUILD.md** — what to check before opening/building the project
- **Docs/UNITY_SETUP.md** — current Unity hierarchy and component wiring
- **Docs/MAGIC_LEAP_2_SETUP.md** — Magic Leap 2 / OpenXR setup notes

## Still to confirm

Before formal testing:

- exact Magic Leap 2 optical focal distance used for the study,
- Low VAC virtual distance,
- High VAC virtual distance,
- final Tetris exposure duration,
- final depth-task difference and number of trials,
- final recovery rule after the pilot,
- the QoE questionnaire requested by Martin.
