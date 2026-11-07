using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using VACExperiment.Board;
using VACExperiment.Tetris;

namespace VACExperiment.EditorTools
{
    // Uses existing batch/editor facilities, not an additional test framework.
    [InitializeOnLoad]
    public static class VACPilot1Checks
    {
        private const string Pending = "VAC.Pilot1.Checks.Pending", Finished = "VAC.Pilot1.Checks.Finished";
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        private static int checks;
        static VACPilot1Checks() => EditorApplication.playModeStateChanged += OnPlayMode;
        private static void Check(bool value, string name)
        {
            if (!value) throw new Exception("Pilot1 check failed: " + name);
            checks++;
        }
        private static void Set(object owner, string field, object value) => owner.GetType().GetField(field, Private).SetValue(owner, value);
        private static T Get<T>(object owner, string field) => (T)owner.GetType().GetField(field, Private).GetValue(owner);
        private static void Prop(object owner, string name, object value) => Set(owner, "<" + name + ">k__BackingField", value);
        private static void Call(object owner, string method, params object[] args) => owner.GetType().GetMethod(method, Private).Invoke(owner, args);

        [MenuItem("VAC/Checks/Pilot1 countdown gravity and depth")]
        public static void Run()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            VACDepthRectangleChecks.ExitAfterChecks = false;
            VACDepthRectangleChecks.Run();
            var registration = UnityEngine.Object.FindAnyObjectByType<BoardRegistration>();
            Check(Mathf.Approximately(registration.MarkerSizeMeters, .145f), "saved QR size");
            Check(!Get<bool>(registration, "estimateQrLength"), "fixed QR estimation");
            Check(UnityEngine.Object.FindObjectsByType<ExperimentFlowController>(FindObjectsSortMode.None).Length == 0,
                "no active legacy flow to double-start depth in saved scene");
            var sceneManager = UnityEngine.Object.FindAnyObjectByType<TetrisManager>();
            Check(Get<ExperimentConfig>(sceneManager, "config") != null && Get<TetrisBoard>(sceneManager, "board") != null &&
                Get<TetrisSequenceManager>(sceneManager, "sequenceManager") != null, "saved Tetris bindings");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SessionState.SetBool(Pending, true);
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayMode(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
            {
                SessionState.SetBool(Pending, false);
                bool success = false;
                try { CheckTetris(); success = true; }
                catch (Exception e) { Debug.LogException(e); }
                SessionState.SetBool("VAC.Pilot1.Checks.Success", success);
                SessionState.SetBool(Finished, true);
                EditorApplication.ExitPlaymode();
            }
            else if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Finished, false))
            {
                SessionState.SetBool(Finished, false);
                bool success = SessionState.GetBool("VAC.Pilot1.Checks.Success", false);
                if (success) Debug.Log("VAC_PILOT1_PLAY_CHECKS_PASS (synthetic timing/input in Unity Play Mode; not ML2 acceptance)");
                if (Application.isBatchMode) EditorApplication.Exit(success ? 0 : 1);
            }
        }

        private static void CheckTetris()
        {
            checks = 0;
            var root = new GameObject("Pilot1SyntheticFixture");
            var actionManager = root.AddComponent<InputActionManager>();
            actionManager.actionAssets = new System.Collections.Generic.List<InputActionAsset>
            { AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Samples/Magic Leap SDK/2.6.0/ML Rig & OpenXR Input/MagicLeapOpenXRInput.inputactions") };
            actionManager.EnableInput();
            var config = ScriptableObject.CreateInstance<ExperimentConfig>();
            var camera = new GameObject("SyntheticCamera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            var board = root.AddComponent<TetrisBoard>();
            var sequence = root.AddComponent<TetrisSequenceManager>();
            var game = root.AddComponent<TetrisManager>();
            Set(game, "config", config); Set(game, "board", board); Set(game, "sequenceManager", sequence);
            game.onSessionCompleted = new UnityEvent();
            int started = 0, completed = 0, cancelled = 0;
            game.onSessionStarted.AddListener(() => started++);
            game.onSessionCompleted.AddListener(() => completed++);
            game.onSessionCancelled.AddListener(() => cancelled++);
            void Tick(float t, float delta = 0) => Call(game, "AdvanceSession", t, delta);
            float Begin()
            {
                game.BeginTrainingSession(VacLevel.C2);
                return Get<float>(game, "countdownStartedAt");
            }
            try
            {
                Check(config.tetrisFallIntervalSeconds == 1f && config.c3DistanceMeters == 2f, "candidate speed and conditions");
                float start = Begin();
                Check(game.IsCountingDown && !game.IsRunning && game.CountdownNumber == 3, "START begins 3");
                game.HardDrop(); game.MoveLeft(); game.RotateClockwise();
                Check(game.PiecesPlaced == 0 && Get<TetrisPiece>(game, "activePiece") == null, "no countdown input/first piece");
                Check(Get<float>(game, "currentPieceSpawnTime") == 0 && Get<float>(game, "viewingDistanceSampleSeconds") == 0,
                    "no placement time or distance sampling during wait/countdown");
                game.BeginSession(VacLevel.C3, TetrisSequenceId.C);
                Check(game.CurrentSequence == TetrisSequenceId.T && Get<float>(game, "countdownStartedAt") == start,
                    "repeat START cannot restart or replace session");
                // Sample just after each second boundary, beyond float clock ULPs.
                Tick(start + 1.01f); Check(game.CountdownNumber == 2 && game.RemainingSeconds == 180, "2 and full exposure remaining");
                Tick(start + 2.01f); Check(game.CountdownNumber == 1 && !game.IsRunning, "1 without gameplay");
                Tick(start + 3.01f);
                Check(game.IsRunning && !game.IsCountingDown && started == 1 && game.RemainingSeconds == 180,
                    "countdown ends once then exposure starts");
                Check(Mathf.Approximately(Get<float>(game, "sessionStartTime"), start + 3.01f) && Get<TetrisPiece>(game, "activePiece") != null,
                    "exposure clock and first piece start after 321");
                var active = Get<TetrisPiece>(game, "activePiece"); int spawnY = active.Position.y;
                Call(game, "AdvanceGravity", .5f); Check(active.Position.y == spawnY, "half interval no fall");
                Call(game, "AdvanceGravity", .5f); Check(active.Position.y == spawnY - 1, "one interval one cell");
                Set(game, "inputEnabledTime", 0f);
                game.MoveLeft(); game.MoveRight(); Check(active.Position.x == board.SpawnPosition.x, "left/right preserved");
                game.RotateClockwise();
                game.HardDrop(); Check(game.PiecesPlaced == 1 && Get<float>(game, "gravityElapsed") == 0, "hard drop locks and resets gravity");
                active = Get<TetrisPiece>(game, "activePiece"); spawnY = active.Position.y;
                Call(game, "AdvanceGravity", .5f); Check(active.Position.y == spawnY, "new piece does not inherit gravity");
                int before = game.PiecesPlaced;
                Call(game, "AdvanceGravity", 100f);
                Check(game.PiecesPlaced == before + 1 && Get<TetrisPiece>(game, "activePiece").Position == board.SpawnPosition,
                    "gravity collision locks once; next piece stays at spawn even after a long frame");

                board.ClearBoard();
                var grid = Get<Transform[,]>(board, "grid");
                for (int x = 4; x < board.Width; x++)
                { grid[x, 0] = board.CreateVisualBlock(board.transform); board.PositionVisual(grid[x, 0], new Vector2Int(x, 0)); }
                var upper = board.CreateVisualBlock(board.transform); grid[9, 1] = upper; board.PositionVisual(upper, new Vector2Int(9, 1));
                UnityEngine.Object.Destroy(Get<TetrisPiece>(game, "activePiece").gameObject);
                var linePiece = new GameObject("LineClearFixture").AddComponent<TetrisPiece>();
                linePiece.transform.SetParent(root.transform, false);
                linePiece.Initialize(board, game, Tetromino.I, new Vector2Int(1, 0)); Set(game, "activePiece", linePiece);
                game.LockCurrentPiece();
                Check(game.LinesCleared == 1 && game.Score == 100 && grid[9, 0] == upper && upper.localPosition.z == 0,
                    "clear line, scoring and shift rows preserved");
                UnityEngine.Object.Destroy(Get<TetrisPiece>(game, "activePiece").gameObject);
                var topPiece = new GameObject("TopOutFixture").AddComponent<TetrisPiece>(); topPiece.transform.SetParent(root.transform, false);
                topPiece.Initialize(board, game, Tetromino.I, new Vector2Int(4, board.Height + 2)); Set(game, "activePiece", topPiece);
                game.LockCurrentPiece();
                Check(Get<int>(game, "topOuts") == 1 && Get<TetrisPiece>(game, "activePiece").Position == board.SpawnPosition,
                    "top-out clears and respawns");
                game.CancelSession(); Check(!game.IsBusy && completed == 0, "abort does not signal completion/depth");
                start = Begin(); game.enabled = false; Tick(start + 30);
                Check(!game.IsBusy && started == 1 && completed == 0, "disable cancels countdown without ghost start");
                game.enabled = true; start = Begin(); game.EndSessionEarly(); Tick(start + 30);
                Check(!game.IsBusy && completed == 0, "early end during countdown cancels without completion");
                start = Begin(); Tick(start + 3.01f); game.EndSessionEarly(); game.EndSessionEarly();
                Check(completed == 1, "running end completes exactly once");

                var input = root.AddComponent<MagicLeapTetrisInput>(); // no ML rig in synthetic fixture; poll gate directly
                Call(input, "UpdateInputGate", false, true, false);
                Call(input, "UpdateInputGate", true, true, false);
                Check(!Get<bool>(input, "gameplayArmed"), "held START trigger cannot arm gameplay after countdown");
                Call(input, "UpdateInputGate", true, false, false);
                Check(Get<bool>(input, "gameplayArmed"), "release arms gameplay");
                Call(input, "UpdateInputGate", false, false, false);
                Check(!Get<bool>(input, "gameplayArmed"), "tracking/countdown gate resets");

                var calibration = root.AddComponent<BoardDistanceCalibration>();
                var registration = root.AddComponent<BoardRegistration>();
                Set(calibration, "boardRegistration", registration);
                Set(game, "boardDistanceCalibration", calibration);
                var session = root.AddComponent<ParticipantSession>(); session.Assign("PILOT1_SYNTHETIC001");
                var flow = root.AddComponent<FormalExperimentFlowController>();
                Set(flow, "config", config); Set(flow, "participantSession", session); Set(flow, "calibration", calibration); Set(flow, "tetrisManager", game);
                Prop(flow, "Phase", FormalExperimentPhase.PreBlockQuestionnaire);
                flow.HandleParticipantStart(); Check(!game.IsBusy, "questionnaire cannot be bypassed");
                Prop(flow, "Phase", FormalExperimentPhase.WarmupCalibration);
                flow.HandleParticipantStart(); Check(!game.IsBusy, "unlocked calibration cannot be bypassed");
                Prop(calibration, "CurrentState", BoardDistanceState.Locked); Prop(registration, "IsPoseLocked", true);
                flow.HandleParticipantStart(); Check(game.IsCountingDown && flow.Phase == FormalExperimentPhase.WarmupCountdown,
                    "training participant START enters countdown after existing gates");
                game.onSessionStarted.AddListener(() => Call(flow, "HandleTetrisStarted"));
                game.onSessionCancelled.AddListener(() => Call(flow, "HandleTetrisCancelled"));
                Prop(calibration, "CurrentState", BoardDistanceState.WaitingForMarker);
                Tick(Get<float>(game, "countdownStartedAt") + 1);
                Check(!game.IsBusy && flow.Phase == FormalExperimentPhase.WarmupCalibration, "lost lock cancels and returns to preparation");
                Check(config.formalDepthTrials == 8 && config.practiceDepthTrials == 3 && config.targetHorizontalSeparationMeters == .12f,
                    "depth protocol preserved");
                // Create a clearly labelled synthetic session, never reopen or
                // rewrite a prior participant folder, and inspect the actual writer.
                var logger = root.AddComponent<DataLogger>();
                logger.StartSession("PILOT1_SYNTHETIC_" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"),
                    "G1", "C1 > C2 > C3", config);
                string metadata = File.ReadAllText(Path.Combine(logger.SessionFolder, "session.csv"));
                Check(metadata.Contains("fall_interval_s") && metadata.Contains("start_mechanism") &&
                    metadata.Contains("qr_size_scope") && metadata.Contains("depth_interaction_version"), "new version metadata columns");
                Check(metadata.Contains("Pilot1-20261108") && metadata.Contains("sphere-silhouette-rectangles-v2") &&
                    metadata.Contains("participant-START-321") && metadata.Contains("unverified"), "actual metadata values and QR caveat");
                Set(flow, "dataLogger", logger); Set(game, "dataLogger", logger);
                session.SetCurrentBlock(1);
                Prop(flow, "Phase", FormalExperimentPhase.BlockCalibration);
                Prop(calibration, "CurrentState", BoardDistanceState.Locked);
                Prop(calibration, "LockedDistanceMeters", .8f);
                flow.HandleParticipantStart();
                Check(flow.Phase == FormalExperimentPhase.FormalCountdown && game.IsCountingDown && game.RemainingSeconds == 900,
                    "formal participant START uses full duration after existing gates");
                Check(File.ReadAllLines(Path.Combine(logger.SessionFolder, "blocks.csv")).Length == 1,
                    "countdown does not emit a started/calibrated formal block row");
                Tick(Get<float>(game, "countdownStartedAt") + 3.01f);
                Check(flow.Phase == FormalExperimentPhase.FormalTetris && game.IsRunning && game.FallIntervalSeconds == 1,
                    "formal countdown transitions once; same configured gravity");
                Check(File.ReadAllLines(Path.Combine(logger.SessionFolder, "blocks.csv")).Length == 2,
                    "formal block row written at actual game start");
                game.CancelSession();
                Check(flow.Phase == FormalExperimentPhase.Idle && !game.IsBusy,
                    "aborted running formal trial requires a fresh session, preserving old rows");
                string evidence = Path.GetFullPath(Path.Combine(Application.dataPath,
                    "../../../outputs/pilot1-20261108/synthetic-session"));
                Directory.CreateDirectory(evidence);
                foreach (string file in Directory.GetFiles(logger.SessionFolder, "*.csv"))
                    File.Copy(file, Path.Combine(evidence, Path.GetFileName(file)), true);
                Debug.Log($"VAC_PILOT1_TETRIS_CHECKS_PASS count={checks}; real Unity Play Mode, synthetic elapsed time/input");
            }
            finally
            {
                game.CancelSession();
                UnityEngine.Object.Destroy(root); UnityEngine.Object.Destroy(camera.gameObject); UnityEngine.Object.Destroy(config);
            }
        }
    }
}
