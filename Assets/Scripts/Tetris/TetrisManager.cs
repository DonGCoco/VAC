using UnityEngine;
using UnityEngine.Events;
using VACExperiment.Board;

namespace VACExperiment.Tetris
{
    /// <summary>
    /// Pilot Tetris: participant START, three-second countdown, then fixed-speed gravity.
    /// </summary>
    public class TetrisManager : MonoBehaviour
    {
        [SerializeField] private ExperimentConfig config;
        [SerializeField] private TetrisBoard board;
        [SerializeField] private TetrisSequenceManager sequenceManager;
        [SerializeField] private DataLogger dataLogger;
        [SerializeField] private BoardDistanceCalibration boardDistanceCalibration;

        [Header("Input")]
        [Tooltip("Briefly ignores gameplay input after START so the trigger used on the experimenter UI cannot place a piece.")]
        [SerializeField, Min(0f)] private float startInputGuardSeconds = 0.35f;

        [Header("Events")]
        public UnityEvent onSessionCompleted;
        public UnityEvent onSessionStarted = new();
        public UnityEvent onSessionCancelled = new();

        public bool IsRunning { get; private set; }
        public bool IsCountingDown { get; private set; }
        public bool IsBusy => IsRunning || IsCountingDown;
        public float CountdownRemainingSeconds { get; private set; }
        public int CountdownNumber => IsCountingDown ? Mathf.CeilToInt(CountdownRemainingSeconds) : 0;
        public float RemainingSeconds { get; private set; }
        public int Score => score;
        public int LinesCleared => linesCleared;
        public int PiecesPlaced => piecesPlaced;
        public TetrisSequenceId CurrentSequence => sequenceId;

        private VacCondition legacyCondition;
        private VacLevel vacLevel;
        private bool useLegacyConditionLogging;
        private TetrisSequenceId sequenceId;
        private TetrisPiece activePiece;
        private int sequenceIndex;

        private int score;
        private int linesCleared;
        private int piecesPlaced;
        private int topOuts;
        private float placementTimeTotal;
        private float currentPieceSpawnTime;
        private float sessionStartTime;
        private float inputEnabledTime;
        private float sessionDurationSeconds;
        private bool isTrainingSession;
        private float viewingDistanceWeightedTotal;
        private float viewingDistanceSampleSeconds;
        private float minViewingDistance;
        private float maxViewingDistance;
        private float countdownStartedAt;
        private float gravityElapsed;
        private TextMesh countdownText;
        private TextMesh statsText;
        public float FallIntervalSeconds => Mathf.Max(0.05f, config != null ? config.tetrisFallIntervalSeconds : 1f);

        // Compatibility entry point for the old two-condition flow.
        public void BeginSession(VacCondition newCondition, TetrisSequenceId newSequence)
        {
            if (IsBusy || !isActiveAndEnabled) return;
            legacyCondition = newCondition;
            useLegacyConditionLogging = true;
            isTrainingSession = false;
            BeginSessionInternal(newSequence, config != null ? config.tetrisDurationSeconds : 0f);
        }

        // New C1/C2/C3 experiment entry point.
        public void BeginSession(VacLevel newCondition, TetrisSequenceId newSequence)
        {
            if (IsBusy || !isActiveAndEnabled) return;
            vacLevel = newCondition;
            useLegacyConditionLogging = false;
            isTrainingSession = false;
            BeginSessionInternal(newSequence, config != null ? config.tetrisDurationSeconds : 0f);
        }

        public void BeginTrainingSession(VacLevel trainingCondition)
        {
            if (IsBusy || !isActiveAndEnabled) return;
            vacLevel = trainingCondition;
            useLegacyConditionLogging = false;
            isTrainingSession = true;
            BeginSessionInternal(
                TetrisSequenceId.T,
                config != null ? config.warmupTetrisDurationSeconds : 0f);
        }

        private void BeginSessionInternal(TetrisSequenceId newSequence, float durationSeconds)
        {
            if (config == null || board == null || sequenceManager == null)
            {
                Debug.LogError("TetrisManager is missing required references.");
                return;
            }

            if (dataLogger == null)
                dataLogger = FindAnyObjectByType<DataLogger>();

            if (boardDistanceCalibration == null)
                boardDistanceCalibration = FindAnyObjectByType<BoardDistanceCalibration>();

            SetVisualsVisible(true);

            sequenceId = newSequence;
            sequenceIndex = 0;
            score = 0;
            linesCleared = 0;
            piecesPlaced = 0;
            topOuts = 0;
            placementTimeTotal = 0f;
            currentPieceSpawnTime = 0f;
            sessionStartTime = 0f;
            viewingDistanceWeightedTotal = 0f;
            viewingDistanceSampleSeconds = 0f;
            minViewingDistance = float.PositiveInfinity;
            maxViewingDistance = 0f;

            board.ClearBoard();
            sessionDurationSeconds = Mathf.Max(1f, durationSeconds);
            RemainingSeconds = sessionDurationSeconds;
            gravityElapsed = 0f;
            countdownStartedAt = Time.realtimeSinceStartup;
            CountdownRemainingSeconds = 3f;
            IsCountingDown = true;
            ShowCountdown();
            Debug.Log($"Pilot Tetris countdown: sequence={sequenceId}; 3,2,1; exposure timer not started.");
        }

        private void Update()
        {
            AdvanceSession(Time.realtimeSinceStartup, Time.unscaledDeltaTime);
        }

        private void AdvanceSession(float now, float deltaSeconds)
        {
            if (IsCountingDown)
            {
                // Recheck the existing spatial lock throughout countdown.
                if (boardDistanceCalibration != null &&
                    boardDistanceCalibration.CurrentState != BoardDistanceState.Locked)
                {
                    CancelSession();
                    return;
                }
                CountdownRemainingSeconds = Mathf.Max(0f, 3f - (now - countdownStartedAt));
                if (CountdownRemainingSeconds > 0f)
                {
                    ShowCountdown();
                    return;
                }
                IsCountingDown = false;
                HideCountdown();
                sessionStartTime = now;
                inputEnabledTime = Time.unscaledTime + startInputGuardSeconds;
                IsRunning = true;
                SpawnNextPiece();
                ShowStats();
                onSessionStarted?.Invoke();
                Debug.Log($"Pilot Tetris running: sequence={sequenceId}; fallInterval={FallIntervalSeconds:F3}s (candidate).");
                return; // No gravity or sampling charged to the countdown frame.
            }
            if (!IsRunning)
                return;

            RemainingSeconds = Mathf.Max(
                0f,
                sessionDurationSeconds - (now - sessionStartTime));

            SampleViewingDistance();

            if (RemainingSeconds <= 0f)
            {
                EndSession();
                return;
            }
            AdvanceGravity(deltaSeconds);
        }

        private void AdvanceGravity(float deltaSeconds)
        {
            gravityElapsed += Mathf.Max(0f, deltaSeconds);
            while (IsRunning && activePiece != null && gravityElapsed >= FallIntervalSeconds)
            {
                gravityElapsed -= FallIntervalSeconds;
                var fallingPiece = activePiece;
                if (!fallingPiece.StepDown() || activePiece != fallingPiece)
                    break; // SpawnNextPiece resets elapsed; never catch up a new piece.
            }
        }

        private void ShowCountdown()
        {
            if (countdownText == null && Camera.main != null)
            {
                var label = new GameObject("PilotTetrisCountdown");
                label.transform.SetParent(Camera.main.transform, false);
                label.transform.localPosition = new Vector3(0f, 0f, 0.7f);
                countdownText = label.AddComponent<TextMesh>();
                countdownText.anchor = TextAnchor.MiddleCenter;
                countdownText.alignment = TextAlignment.Center;
                countdownText.fontSize = 64;
                countdownText.characterSize = 0.012f;
            }
            if (countdownText != null)
            {
                countdownText.gameObject.SetActive(true);
                countdownText.text = CountdownNumber.ToString();
            }
        }

        private void HideCountdown()
        {
            if (countdownText != null) countdownText.gameObject.SetActive(false);
        }

        private void ShowStats()
        {
            if (statsText == null)
            {
                var label = new GameObject("TetrisStats");
                label.transform.SetParent(board.transform, false);
                label.transform.localPosition = new Vector3(
                    0f, -board.Height * board.CellSize * 0.5f - board.CellSize, 0f);
                statsText = label.AddComponent<TextMesh>();
                statsText.anchor = TextAnchor.UpperCenter;
                statsText.alignment = TextAlignment.Center;
                statsText.fontSize = 48;
                statsText.characterSize = 0.004f;
            }

            var viewer = Camera.main;
            if (viewer != null)
            {
                // TextMesh faces its local -Z direction; keep it in the board plane.
                bool viewerOnPositiveSide = Vector3.Dot(
                    viewer.transform.position - board.transform.position, board.transform.forward) > 0f;
                statsText.transform.localRotation = viewerOnPositiveSide
                    ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
            }

            UpdateStats();
            statsText.gameObject.SetActive(true);
        }

        private void UpdateStats()
        {
            if (statsText != null)
                statsText.text = $"Board resets: {topOuts}    Lines cleared: {linesCleared}";
        }

        private void HideStats()
        {
            if (statsText != null)
                statsText.gameObject.SetActive(false);
        }

        public void CancelSession()
        {
            if (!IsBusy) return;
            IsCountingDown = false;
            IsRunning = false;
            CountdownRemainingSeconds = 0f;
            RemainingSeconds = 0f;
            gravityElapsed = 0f;
            HideCountdown();
            HideStats();
            if (activePiece != null) Destroy(activePiece.gameObject);
            activePiece = null;
            if (dataLogger != null && dataLogger.HasSession)
                dataLogger.LogEvent("TetrisCancelled", vacLevel.ToString(), "No completion transition; pending countdown cancelled or running task aborted.");
            onSessionCancelled?.Invoke();
        }

        private void OnDisable() => CancelSession();

        private void OnDestroy()
        {
            if (countdownText != null) Destroy(countdownText.gameObject);
            if (statsText != null) Destroy(statsText.gameObject);
        }

        private void SampleViewingDistance()
        {
            if (isTrainingSession ||
                boardDistanceCalibration == null ||
                !boardDistanceCalibration.IsBoardLocked)
                return;

            float distance = boardDistanceCalibration.ActualDistanceMeters;
            float dt = Mathf.Max(0f, Time.unscaledDeltaTime);

            if (distance <= 0f || dt <= 0f)
                return;

            viewingDistanceWeightedTotal += distance * dt;
            viewingDistanceSampleSeconds += dt;
            minViewingDistance = Mathf.Min(minViewingDistance, distance);
            maxViewingDistance = Mathf.Max(maxViewingDistance, distance);
        }

        public void MoveLeft()
        {
            if (CanAcceptInput())
                activePiece.MoveLeft();
        }

        public void MoveRight()
        {
            if (CanAcceptInput())
                activePiece.MoveRight();
        }

        public void RotateClockwise()
        {
            if (CanAcceptInput())
                activePiece.RotateClockwise();
        }

        public void HardDrop()
        {
            if (CanAcceptInput())
                activePiece.HardDrop();
        }

        // Kept only so the old keyboard debug component still compiles.
        // Bumper hard drop and fixed automatic gravity are the pilot controls.
        public void SetSoftDrop(bool held)
        {
        }

        public void EndSessionEarly()
        {
            if (IsCountingDown) { CancelSession(); return; }
            if (IsRunning)
                EndSession();
        }

        public void SetVisualsVisible(bool visible)
        {
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
                renderer.enabled = visible;
        }

        public void LockCurrentPiece()
        {
            if (!IsRunning || activePiece == null)
                return;

            Vector2Int[] absolute = activePiece.GetAbsoluteCells();
            bool toppedOut = board.HasCellsAboveBoard(
                activePiece.Position,
                GetRelativeCells(activePiece, absolute));

            int clearedThisPiece = board.LockPiece(activePiece);
            placementTimeTotal += Time.realtimeSinceStartup - currentPieceSpawnTime;
            piecesPlaced++;

            score += ScoreForLines(clearedThisPiece);
            linesCleared += clearedThisPiece;

            Destroy(activePiece.gameObject);
            activePiece = null;

            if (toppedOut)
            {
                topOuts++;
                board.ClearBoard();
            }

            SpawnNextPiece();
            UpdateStats();
        }

        private bool CanAcceptInput()
        {
            return IsRunning &&
                   activePiece != null &&
                   Time.unscaledTime >= inputEnabledTime;
        }

        private void SpawnNextPiece()
        {
            Tetromino type = sequenceManager.GetPiece(sequenceId, sequenceIndex++);
            GameObject pieceObject = new($"ActivePiece_{type}");
            pieceObject.transform.SetParent(board.transform, false);

            activePiece = pieceObject.AddComponent<TetrisPiece>();
            activePiece.Initialize(board, this, type, board.SpawnPosition);

            if (!board.IsValid(activePiece.Position, TetrominoLibrary.GetCells(type)))
            {
                topOuts++;
                Destroy(activePiece.gameObject);
                activePiece = null;
                board.ClearBoard();
                SpawnNextPiece();
                return;
            }

            currentPieceSpawnTime = Time.realtimeSinceStartup;
            gravityElapsed = 0f;
        }

        private void EndSession()
        {
            IsRunning = false;
            HideStats();

            if (activePiece != null)
            {
                Destroy(activePiece.gameObject);
                activePiece = null;
            }

            float duration = Time.realtimeSinceStartup - sessionStartTime;
            float averagePlacementTime = piecesPlaced > 0
                ? placementTimeTotal / piecesPlaced
                : 0f;

            if (!isTrainingSession)
            {
                if (useLegacyConditionLogging)
                {
                    dataLogger?.LogTetrisSummary(
                        legacyCondition,
                        sequenceId,
                        duration,
                        score,
                        linesCleared,
                        piecesPlaced,
                        averagePlacementTime,
                        topOuts);
                }
                else
                {
                    float? meanViewingDistance =
                        viewingDistanceSampleSeconds > 0f
                            ? viewingDistanceWeightedTotal / viewingDistanceSampleSeconds
                            : (float?)null;
                    float? minimumViewingDistance =
                        viewingDistanceSampleSeconds > 0f
                            ? minViewingDistance
                            : (float?)null;
                    float? maximumViewingDistance =
                        viewingDistanceSampleSeconds > 0f
                            ? maxViewingDistance
                            : (float?)null;

                    dataLogger?.LogTetrisSummary(
                        vacLevel,
                        sequenceId,
                        duration,
                        score,
                        linesCleared,
                        piecesPlaced,
                        averagePlacementTime,
                        topOuts,
                        meanViewingDistance,
                        minimumViewingDistance,
                        maximumViewingDistance);
                }
            }

            Debug.Log(
                $"Pilot Tetris ended: condition={vacLevel}; sequence={sequenceId}; " +
                $"duration={duration:F1}s; score={score}; lines={linesCleared}; " +
                $"pieces={piecesPlaced}; topOuts={topOuts}.");

            onSessionCompleted?.Invoke();
        }

        private static int ScoreForLines(int count)
        {
            return count switch
            {
                1 => 100,
                2 => 300,
                3 => 500,
                4 => 800,
                _ => 0
            };
        }

        private static Vector2Int[] GetRelativeCells(
            TetrisPiece piece,
            Vector2Int[] absoluteCells)
        {
            Vector2Int[] relative = new Vector2Int[absoluteCells.Length];
            for (int i = 0; i < absoluteCells.Length; i++)
                relative[i] = absoluteCells[i] - piece.Position;

            return relative;
        }
    }
}
