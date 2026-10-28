using System;
using System.Collections.Generic;
using UnityEngine;

namespace VACExperiment.Tetris
{
    public class TetrisSequenceManager : MonoBehaviour
    {
        [Header("Deterministic 7-bag sequence seeds")]
        [SerializeField] private int trainingSeed = 65537;
        [SerializeField] private int sequenceASeed = 104729;
        [SerializeField] private int sequenceBSeed = 130363;
        [SerializeField] private int sequenceCSeed = 155921;
        [SerializeField, Min(14)] private int generatedSequenceLength = 256;

        private readonly Dictionary<TetrisSequenceId, List<Tetromino>> cache = new();

        public Tetromino GetPiece(TetrisSequenceId sequenceId, int index)
        {
            if (!cache.TryGetValue(sequenceId, out List<Tetromino> sequence))
            {
                sequence = GenerateSequence(GetSeed(sequenceId), generatedSequenceLength);
                cache[sequenceId] = sequence;
            }

            if (sequence.Count == 0)
                throw new InvalidOperationException("Tetris sequence is empty.");

            return sequence[index % sequence.Count];
        }

        public void Rebuild()
        {
            cache.Clear();
        }

        private int GetSeed(TetrisSequenceId sequenceId)
        {
            return sequenceId switch
            {
                TetrisSequenceId.T => trainingSeed,
                TetrisSequenceId.A => sequenceASeed,
                TetrisSequenceId.B => sequenceBSeed,
                TetrisSequenceId.C => sequenceCSeed,
                _ => sequenceASeed
            };
        }

        private static List<Tetromino> GenerateSequence(int seed, int length)
        {
            var rng = new System.Random(seed);
            var result = new List<Tetromino>(length);
            Tetromino[] bag =
            {
                Tetromino.I, Tetromino.J, Tetromino.L, Tetromino.O,
                Tetromino.S, Tetromino.T, Tetromino.Z
            };

            while (result.Count < length)
            {
                Tetromino[] shuffled = (Tetromino[])bag.Clone();

                for (int i = shuffled.Length - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
                }

                foreach (Tetromino piece in shuffled)
                {
                    if (result.Count >= length)
                        break;

                    result.Add(piece);
                }
            }

            return result;
        }
    }
}
