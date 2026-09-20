using UnityEngine;
using UnityEngine.Events;

namespace VACExperiment.Tetris
{
    public class TetrisManager : MonoBehaviour
    {
        [SerializeField] private ExperimentConfig config;
        [SerializeField] private TetrisBoard board;
        [SerializeField] private TetrisSequenceManager sequenceManager;
        [SerializeField] private DataLogger dataLogger;

        [Header("Drop timing")]
        [SerializeField, Min(0.05f)] private float normalDropInterval = 0.8f;
        [SerializeField, Min(0.02f)] private float softDropInterval = 0.08f;

        [Header("Events")]
        public UnityEvent onSessionCompleted;

        public bool IsRunning { get; private set; }
        public float RemainingSeconds { get; private set; }

        private VacCondition condition;
        private TetrisSequenceId sequenceId;
        private TetrisPiece activePiece;
        private int sequenceIndex;
        private float nextDropTime;
        private bool softDropHeld;

        private int score;
        private int linesCleared;
        private int piecesPlaced;
        private int topOuts;
        private float placementTimeTotal;
        private float currentPieceSpawnTime;
        private float sessionStartTime;

        public void BeginSession(VacCondition newCondition, TetrisSequenceId newSequence)
        {
            if (config == null || board == null || sequenceManager == null)
            {
                Debug.LogError("TetrisManager is missing required references.");
                return;
            }

            condition = newCondition;
            sequenceId = newSequence;
            sequenceIndex = 0;
            score = 0;
            linesCleared = 0;
            piecesPlaced = 0;
            topOuts = 0;
            placementTimeTotal = 0f;

            board.ClearBoard();
            RemainingSeconds = config.tetrisDurationSeconds;
            sessionStartTime = Time.realtimeSinceStartup;
            IsRunning = true;

            SpawnNextPiece();
        }

        private void Update()
        {
            if (!IsRunning)
                return;

            RemainingSeconds = Mathf.Max(
                0f,
                config.tetrisDurationSeconds - (Time.realtimeSinceStartup - sessionStartTime));

            if (RemainingSeconds <= 0f)
            {
                EndSession();
                return;
            }

            float interval = softDropHeld ? softDropInterval : normalDropInterval;

            if (Time.time >= nextDropTime && activePiece != null)
            {
                activePiece.StepDown();
                nextDropTime = Time.time + interval;
            }
        }

        public void MoveLeft()
        {
            if (IsRunning && activePiece != null)
                activePiece.MoveLeft();
        }

        public void MoveRight()
        {
            if (IsRunning && activePiece != null)
                activePiece.MoveRight();
        }

        public void RotateClockwise()
        {
            if (IsRunning && activePiece != null)
                activePiece.RotateClockwise();
        }

        public void HardDrop()
        {
            if (IsRunning && activePiece != null)
                activePiece.HardDrop();
        }

        public void SetSoftDrop(bool held)
        {
            softDropHeld = held;
        }

        public void LockCurrentPiece()
        {
            if (!IsRunning || activePiece == null)
                return;

            Vector2Int[] absolute = activePiece.GetAbsoluteCells();
            bool toppedOut = board.HasCellsAboveBoard(activePiece.Position,
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

        private void SpawnNextPiece()
        {
            Tetromino type = sequenceManager.GetPiece(sequenceId, sequenceIndex++);
            GameObject pieceObject = new($"ActivePiece_{type}");
            pieceObject.transform.SetParent(board.transform, false);

            activePiece = pieceObject.AddComponent<TetrisPiece>();
            activePiece.Initialize(board, this, type, board.SpawnPosition);

            // If the spawn position is already invalid, count a top-out and reset.
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
            nextDropTime = Time.time + normalDropInterval;
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

            dataLogger?.LogTetrisSummary(
                condition,
                sequenceId,
                duration,
                score,
                linesCleared,
                piecesPlaced,
                averagePlacementTime,
                topOuts);

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
