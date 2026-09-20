using UnityEngine;

namespace VACExperiment.Tetris
{
    public class TetrisBoard : MonoBehaviour
    {
        [SerializeField, Min(4)] private int width = 10;
        [SerializeField, Min(8)] private int height = 20;
        [SerializeField, Min(0.001f)] private float cellSize = 0.04f;
        [SerializeField] private Transform blockPrefab;

        private Transform[,] grid;

        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;

        private void Awake()
        {
            grid = new Transform[width, height];
        }

        public Vector2Int SpawnPosition => new(width / 2 - 1, height - 2);

        public bool IsValid(Vector2Int piecePosition, Vector2Int[] cells)
        {
            foreach (Vector2Int cell in cells)
            {
                Vector2Int p = piecePosition + cell;

                if (p.x < 0 || p.x >= width || p.y < 0)
                    return false;

                // Cells above the board are allowed while spawning.
                if (p.y >= height)
                    continue;

                if (grid[p.x, p.y] != null)
                    return false;
            }

            return true;
        }

        public bool HasCellsAboveBoard(Vector2Int piecePosition, Vector2Int[] cells)
        {
            foreach (Vector2Int cell in cells)
            {
                if ((piecePosition + cell).y >= height)
                    return true;
            }

            return false;
        }

        public Transform CreateVisualBlock(Transform parent)
        {
            Transform block;
            if (blockPrefab != null)
            {
                block = Instantiate(blockPrefab, parent);
            }
            else
            {
                GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fallback.name = "TetrisBlock";
                block = fallback.transform;
                block.SetParent(parent, false);
            }

            block.localScale = Vector3.one * (cellSize * 0.92f);
            return block;
        }

        public void PositionVisual(Transform block, Vector2Int boardCell)
        {
            block.localPosition = new Vector3(
                (boardCell.x - (width - 1) * 0.5f) * cellSize,
                (boardCell.y - (height - 1) * 0.5f) * cellSize,
                0f);
        }

        public int LockPiece(TetrisPiece piece)
        {
            Vector2Int[] absoluteCells = piece.GetAbsoluteCells();
            Transform[] visuals = piece.DetachVisualBlocks();

            for (int i = 0; i < absoluteCells.Length; i++)
            {
                Vector2Int p = absoluteCells[i];

                if (p.y >= height)
                {
                    Destroy(visuals[i].gameObject);
                    continue;
                }

                visuals[i].SetParent(transform, false);
                PositionVisual(visuals[i], p);
                grid[p.x, p.y] = visuals[i];
            }

            return ClearCompletedLines();
        }

        public void ClearBoard()
        {
            if (grid == null)
                grid = new Transform[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (grid[x, y] != null)
                        Destroy(grid[x, y].gameObject);

                    grid[x, y] = null;
                }
            }
        }

        private int ClearCompletedLines()
        {
            int cleared = 0;

            for (int y = 0; y < height; y++)
            {
                if (!IsLineFull(y))
                    continue;

                ClearLine(y);
                ShiftRowsDown(y);
                y--;
                cleared++;
            }

            return cleared;
        }

        private bool IsLineFull(int y)
        {
            for (int x = 0; x < width; x++)
            {
                if (grid[x, y] == null)
                    return false;
            }

            return true;
        }

        private void ClearLine(int y)
        {
            for (int x = 0; x < width; x++)
            {
                if (grid[x, y] != null)
                    Destroy(grid[x, y].gameObject);

                grid[x, y] = null;
            }
        }

        private void ShiftRowsDown(int clearedRow)
        {
            for (int y = clearedRow + 1; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Transform block = grid[x, y];
                    if (block == null)
                        continue;

                    grid[x, y - 1] = block;
                    grid[x, y] = null;
                    PositionVisual(block, new Vector2Int(x, y - 1));
                }
            }
        }
    }
}
