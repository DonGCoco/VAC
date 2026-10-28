using UnityEngine;

namespace VACExperiment.Tetris
{
    public class TetrisBoard : MonoBehaviour
    {
        [SerializeField, Min(4)] private int width = 10;
        [SerializeField, Min(8)] private int height = 20;
        [SerializeField, Min(0.001f)] private float cellSize = 0.02f;
        [SerializeField, Min(0.0005f)] private float blockThicknessMeters = 0.002f;
        [SerializeField] private Transform blockPrefab;

        [Header("Board frame")]
        [SerializeField] private bool showBoardFrame = true;
        [SerializeField, Min(0.0005f)] private float frameBarWidthMeters = 0.002f;

        private Transform[,] grid;
        private Transform frameRoot;
        private Mesh planarMesh;

        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;

        private void Awake()
        {
            grid = new Transform[width, height];

            if (showBoardFrame)
                CreateBoardFrame();
        }

        public Vector2Int SpawnPosition => new(width / 2 - 1, height - 2);

        public bool IsValid(Vector2Int piecePosition, Vector2Int[] cells)
        {
            foreach (Vector2Int cell in cells)
            {
                Vector2Int p = piecePosition + cell;

                if (p.x < 0 || p.x >= width || p.y < 0)
                    return false;

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
                GameObject fallback = CreatePlanarVisual();
                fallback.name = "TetrisBlock";

                Collider collider = fallback.GetComponent<Collider>();
                if (collider != null)
                    Destroy(collider);

                block = fallback.transform;
                block.SetParent(parent, false);
            }

            block.localScale = new Vector3(
                cellSize * 0.92f,
                cellSize * 0.92f,
                blockPrefab != null ? blockThicknessMeters : 1f);

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

        private void CreateBoardFrame()
        {
            if (frameRoot != null)
                return;

            GameObject root = new("BoardFrame");
            frameRoot = root.transform;
            frameRoot.SetParent(transform, false);
            frameRoot.localPosition = Vector3.zero;

            float boardWidth = width * cellSize;
            float boardHeight = height * cellSize;
            float halfWidth = boardWidth * 0.5f;
            float halfHeight = boardHeight * 0.5f;

            CreateFrameBar(
                "FrameLeft",
                new Vector3(-halfWidth - frameBarWidthMeters * 0.5f, 0f, 0f),
                new Vector3(frameBarWidthMeters, boardHeight + frameBarWidthMeters * 2f, 1f));

            CreateFrameBar(
                "FrameRight",
                new Vector3(halfWidth + frameBarWidthMeters * 0.5f, 0f, 0f),
                new Vector3(frameBarWidthMeters, boardHeight + frameBarWidthMeters * 2f, 1f));

            CreateFrameBar(
                "FrameBottom",
                new Vector3(0f, -halfHeight - frameBarWidthMeters * 0.5f, 0f),
                new Vector3(boardWidth, frameBarWidthMeters, 1f));

            CreateFrameBar(
                "FrameTop",
                new Vector3(0f, halfHeight + frameBarWidthMeters * 0.5f, 0f),
                new Vector3(boardWidth, frameBarWidthMeters, 1f));
        }

        private void CreateFrameBar(string name, Vector3 localPosition, Vector3 localScale)
        {
            GameObject bar = CreatePlanarVisual();
            bar.name = name;

            Collider collider = bar.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            bar.transform.SetParent(frameRoot, false);
            bar.transform.localPosition = localPosition;
            bar.transform.localRotation = Quaternion.identity;
            bar.transform.localScale = localScale;
        }

        // Both sides share the same geometric plane. Duplicated vertices keep
        // opposite normals separate; no front/back thickness or Z offset remains.
        private GameObject CreatePlanarVisual()
        {
            if (planarMesh == null)
            {
                planarMesh = new Mesh { name = "VAC_CoplanarDoubleSidedQuad" };
                Vector3 a = new(-0.5f, -0.5f, 0f);
                Vector3 b = new(0.5f, -0.5f, 0f);
                Vector3 c = new(0.5f, 0.5f, 0f);
                Vector3 d = new(-0.5f, 0.5f, 0f);
                planarMesh.vertices = new[] { a, b, c, d, a, b, c, d };
                planarMesh.uv = new[]
                {
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up,
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up
                };
                planarMesh.triangles = new[] { 0, 2, 1, 0, 3, 2, 4, 5, 6, 4, 6, 7 };
                planarMesh.RecalculateNormals();
                planarMesh.RecalculateBounds();
            }

            // Retain Unity's default primitive material, matching existing visuals.
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
            visual.GetComponent<MeshFilter>().sharedMesh = planarMesh;
            return visual;
        }

        private void OnDestroy()
        {
            if (planarMesh != null)
                Destroy(planarMesh);
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
