using UnityEngine;

namespace VACExperiment.Tetris
{
    public enum Tetromino
    {
        I, J, L, O, S, T, Z
    }

    public static class TetrominoLibrary
    {
        public static Vector2Int[] GetCells(Tetromino type)
        {
            return type switch
            {
                Tetromino.I => new[]
                {
                    new Vector2Int(-1, 0), new Vector2Int(0, 0),
                    new Vector2Int(1, 0), new Vector2Int(2, 0)
                },
                Tetromino.J => new[]
                {
                    new Vector2Int(-1, 1), new Vector2Int(-1, 0),
                    new Vector2Int(0, 0), new Vector2Int(1, 0)
                },
                Tetromino.L => new[]
                {
                    new Vector2Int(1, 1), new Vector2Int(-1, 0),
                    new Vector2Int(0, 0), new Vector2Int(1, 0)
                },
                Tetromino.O => new[]
                {
                    new Vector2Int(0, 1), new Vector2Int(1, 1),
                    new Vector2Int(0, 0), new Vector2Int(1, 0)
                },
                Tetromino.S => new[]
                {
                    new Vector2Int(0, 1), new Vector2Int(1, 1),
                    new Vector2Int(-1, 0), new Vector2Int(0, 0)
                },
                Tetromino.T => new[]
                {
                    new Vector2Int(0, 1), new Vector2Int(-1, 0),
                    new Vector2Int(0, 0), new Vector2Int(1, 0)
                },
                Tetromino.Z => new[]
                {
                    new Vector2Int(-1, 1), new Vector2Int(0, 1),
                    new Vector2Int(0, 0), new Vector2Int(1, 0)
                },
                _ => new Vector2Int[0]
            };
        }
    }
}
