# Depth rectangle integration — 2026-11-05

## Integration scope

- Read handoff `README.md`, then `PROMPT.txt`; no repository AGENTS.md was found.
- Handoff base: `6c763f6d36c1baf72a3167f0af72de83ecd72a33` (PR #4 head).
- Latest main at integration: `05434205afc0e3db45af354d2dad44fea372ce17`, which includes merged PR #4 and the later Unity settings restoration. The two depth scripts have no subsequent conflicting change. `git apply --check` passed; applied `depth_rectangle_response.patch` once, without replacement-file copying.
- Branch: `codex/depth-rectangle-response`, based on latest main because `vac-distance`/PR #4 is merged. The previous local branch was clean and remains available. No reset, force push or unrelated Unity-settings cleanup was used.
- Adaptations beyond the patch: explicitly enable/bind the input in the saved scene to the ML Rig Controller/XRI ray origin; require valid position and rotation tracking plus an active origin; clear/suppress input on invalid tracking; compare boundary equality with float roundoff tolerance and reject invalid intersections; add actual Unity C# checks and update M5 documentation.
- Rectangle dimensions are handoff candidates, not literature-validated or device-accepted dimensions. No visible rectangles/highlights were added.

## Preserved behaviour

The experiment config asset, configuration defaults, randomisation method, logger and formal-flow scripts are unchanged relative to main. This includes main's existing C3=2.00 m, 3 practice trials and 8 formal trials. No old C3 value was restored.

Source-body comparisons passed for `BuildBalancedOrder`, `ApplyConstantAngularSize`, `GetNearDepth`, `GetFarDepth`, `CreateDefaultTarget`, and `BeginBlock`. The sphere-position/scale/onset part of `ShowTrial` is unchanged; the response plane is the addition. In `Submit`, everything from reaction-time calculation through CSV call and trial progression is unchanged; only the duplicate/disabled guard was added.

The single fresh-press latch is shared across consecutive trials: an invalid press is consumed, a held trigger cannot answer subsequent trials, block onset/invalid tracking/disable resets to requiring release. Sphere colliders and existing XRSimpleInteractable components are disabled. UI and Tetris input scripts are untouched.

## Checks completed

- Patch applicability and `git diff --check`: PASS.
- Existing `Tools/check_vac_handoff.py`: PASS for its existing checks. Its maximum-scale board/QR clearance report is **-64 mm** for current main defaults (overlap); this is pre-existing geometry, not a rectangle regression, and remains a physical-layout concern.
- Unity script compilation and `VACExperiment.EditorTools.VACDepthRectangleChecks.Run`: PASS, **41 checks**, Unity exit code 0.
- Checks run actual production C# in Unity with synthetic input, not a Python model. Coverage: saved input enabled and correct manager/controller/XRI/action bindings; disabled sphere colliders; unchanged sphere geometry; left/right/gap/outer/vertical/parallel/back-facing/invalid ray cases; plane and targets world-fixed after head movement; LEFT/RIGHT/-- prompt; held trigger at start; one press advancing one trial; held sweep; gap press then entering a region; release/repress; tracking loss/recovery; same-frame duplicate rejection; 3/8 completion and 4/4 balancing.
- Validation used an isolated project copy with installed **Unity 6000.3.24f1**, ML SDK **2.6.0**, XRI **3.0.9**. The repository still records **6000.2.15f1**, which is not installed here; exact recorded-version compilation remains pending. Packages/ProjectSettings were not changed in the submitted branch.

## Android and device status

Android Development APK build: **Succeeded, errors=0**, Unity exit code **0**, using IL2CPP/x86-64 and Vulkan. Local artifact: `outputs/depth-rectangle-20261105/VAC-depth-rectangle.apk` (41,684,674 bytes, outside the repository). The validation-only build uses package `com.DefaultCompany.VAC.DepthRectangleValidation` to preserve existing device applications/data; this identity change is confined to the local build helper, not the submitted project. Build warnings include the existing sample's obsolete FindObjectOfType API and Unity's Magic Leap x86-64 deprecation notice; they are not device acceptance failures or a reason to change the experimental settings here.

On 2026-11-05 ADB reported no attached ML2 both before and after the build. Installation, launch and physical-controller acceptance are therefore pending. Previous sphere-click or previous handoff application launches do not certify this version.

## Remaining ML2 acceptance

1. Install the current rectangle validation APK, grant tracking permissions, confirm startup binds `Controller`, and compare the visible ray with LEFT/RIGHT hover selection.
2. Confirm both visual spheres retain size, position, depth, colour; spheres cannot be selected; no panel/border/highlight is visible. Check world-fixed plane, gap and outer edges from actual head/controller poses.
3. Test held at onset; press in gap then move in; long hold and sweep across trials; release/repress; tracking loss, recovery while held, and disabled controller origin. Each physical press must submit at most once.
4. Complete 3 practice and 8 formal trials; inspect the existing CSV response/correct side/correctness/RT fields and stage transitions. Practice logging and random side rules must remain unchanged.
5. Run all three conditions, external questionnaire/recovery and export flow; test START, other UI and Tetris trigger input. Record this interaction version when comparing old/new data. Physical trigger timing, rendered visibility, full experimental flow and real CSV output are not covered by synthetic Unity checks.
