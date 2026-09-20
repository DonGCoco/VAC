using System.Collections.Generic;
using UnityEngine;

namespace VACExperiment.Tetris
{
    public class TetrisPiece : MonoBehaviour
    {
        private TetrisBoard board;
        private TetrisManager manager;
        private Tetromino type;
        private Vector2Int position;
        private Vector2Int[] cells;
        private readonly List<Transform> visualBlocks = new(4);

        public Tetromino Type => type;
        public Vector2Int Position => position;

        public void Initialize(
            TetrisBoard newBoard,
            TetrisManager newManager,
            Tetromino newType,
            Vector2Int spawnPosition)
        {
            board = newBoard;
            manager = newManager;
            type = newType;
            position = spawnPosition;
            cells = TetrominoLibrary.GetCells(type);

            visualBlocks.Clear();
            for (int i = 0; i < cells.Length; i++)
                visualBlocks.Add(board.CreateVisualBlock(transform));

            RefreshVisuals();
        }

        public bool MoveLeft() => TryMove(Vector2Int.left);
        public bool MoveRight() => TryMove(Vector2Int.right);

        public bool StepDown()
        {
            if (TryMove(Vector2Int.down))
                return true;

            manager.LockCurrentPiece();
            return false;
        }

        public void HardDrop()
        {
            while (TryMove(Vector2Int.down))
            {
            }

            manager.LockCurrentPiece();
        }

        public bool RotateClockwise()
        {
            if (type == Tetromino.O)
                return true;

            Vector2Int[] rotated = new Vector2Int[cells.Length];

            for (int i = 0; i < cells.Length; i++)
            {
                Vector2Int c = cells[i];
                rotated[i] = new Vector2Int(c.y, -c.x);
            }

            Vector2Int[] kicks =
            {
                Vector2Int.zero,
                Vector2Int.left,
                Vector2Int.right,
                Vector2Int.left * 2,
                Vector2Int.right * 2
            };

            foreach (Vector2Int kick in kicks)
            {
                if (!board.IsValid(position + kick, rotated))
                    continue;

                cells = rotated;
                position += kick;
                RefreshVisuals();
                return true;
            }

            return false;
        }

        public Vector2Int[] GetAbsoluteCells()
        {
            Vector2Int[] result = new Vector2Int[cells.Length];

            for (int i = 0; i < cells.Length; i++)
                result[i] = position + cells[i];

            return result;
        }

        public Transform[] DetachVisualBlocks()
        {
            Transform[] result = visualBlocks.ToArray();
            visualBlocks.Clear();
            return result;
        }

        private bool TryMove(Vector2Int offset)
        {
            Vector2Int candidate = position + offset;
            if (!board.IsValid(candidate, cells))
                return false;

            position = candidate;
            RefreshVisuals();
            return true;
        }

        private void RefreshVisuals()
        {
            for (int i = 0; i < visualBlocks.Count; i++)
            {
                Vector2Int absolute = position + cells[i];
                board.PositionVisual(visualBlocks[i], absolute);
            }
        }
    }
}
