using UnityEngine;
using UnityEngine.Events;

namespace VACExperiment.Tetris
{
    /// <summary>
    /// Static/self-paced Tetris for the VAC experiment.
    /// Pieces never fall automatically. The participant positions/rotates the
    /// current piece, then explicitly hard-drops it to place it.
    /// </summary>
    public class TetrisManager : MonoBehaviour
    {
        [SerializeField] private ExperimentConfig config;
        [SerializeField] private TetrisBoard board;
        [SerializeField] private TetrisSequenceManager sequenceManager;
        [SerializeField] private DataLogger dataLogger;

        [Header("Input")]
        [Tooltip("Briefly ignores gameplay input after START so the trigger used on the experimenter UI cannot place a piece.")]
        [SerializeField, Min(0f)] private float startInputGuardSeconds = 0.35f;

        [Header("Events")]
        public UnityEvent onSessionCompleted;

        public bool IsRunning { get; private set; }
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

        // Compatibility entry point for the old two-condition flow.
        public void BeginSession(VacCondition newCondition, TetrisSequenceId newSequence)
        {
            legacyCondition = newCondition;
            useLegacyConditionLogging = true;
            isTrainingSession = false;
            BeginSessionInternal(newSequence, config != null ? config.tetrisDurationSeconds : 0f);
        }

        // New C1/C2/C3 experiment entry point.
        public void BeginSession(VacLevel newCondition, TetrisSequenceId newSequence)
        {
            vacLevel = newCondition;
            useLegacyConditionLogging = false;
            isTrainingSession = false;
            BeginSessionInternal(newSequence, config != null ? config.tetrisDurationSeconds : 0f);
        }

        public void BeginTrainingSession(VacLevel trainingCondition)
        {
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

            SetVisualsVisible(true);

            sequenceId = newSequence;
            sequenceIndex = 0;
            score = 0;
            linesCleared = 0;
            piecesPlaced = 0;
            topOuts = 0;
            placementTimeTotal = 0f;

            board.ClearBoard();
            sessionDurationSeconds = Mathf.Max(1f, durationSeconds);
            RemainingSeconds = sessionDurationSeconds;
            sessionStartTime = Time.realtimeSinceStartup;
            inputEnabledTime = Time.unscaledTime + startInputGuardSeconds;
            IsRunning = true;

            SpawnNextPiece();

            Debug.Log(
                $"M4 static Tetris started: sequence={sequenceId}; " +
                $"mode=self-paced/no-gravity.");
        }

        private void Update()
        {
            if (!IsRunning)
                return;

            RemainingSeconds = Mathf.Max(
                0f,
                sessionDurationSeconds - (Time.realtimeSinceStartup - sessionStartTime));

            if (RemainingSeconds <= 0f)
                EndSession();
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
        // Static Tetris intentionally has no soft/automatic drop.
        public void SetSoftDrop(bool held)
        {
        }

        public void EndSessionEarly()
        {
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
        }

        private void EndSession()
        {
            IsRunning = false;

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
                    dataLogger?.LogTetrisSummary(
                        vacLevel,
                        sequenceId,
                        duration,
                        score,
                        linesCleared,
                        piecesPlaced,
                        averagePlacementTime,
                        topOuts);
                }
            }

            Debug.Log(
                $"M4 static Tetris ended: condition={vacLevel}; sequence={sequenceId}; " +
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
