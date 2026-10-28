using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace VACExperiment
{
    /// <summary>
    /// M6 participant assignment for the three-condition within-subject experiment.
    ///
    /// Automatic counterbalancing:
    /// P001 -> G1 -> C1, C2, C3
    /// P002 -> G2 -> C2, C3, C1
    /// P003 -> G3 -> C3, C1, C2
    /// then repeats.
    ///
    /// Tetris sequence is tied to block number, not VAC condition:
    /// Block 1 -> A, Block 2 -> B, Block 3 -> C.
    /// </summary>
    public class ParticipantSession : MonoBehaviour
    {
        public string ParticipantId { get; private set; } = "";
        public int GroupNumber { get; private set; }
        public bool HasAssignment => GroupNumber >= 1 && GroupNumber <= 3;
        public int CurrentBlockIndex { get; private set; }

        public string GroupLabel => HasAssignment ? $"G{GroupNumber}" : "";
        public string ConditionOrderLabel =>
            HasAssignment
                ? $"{GetConditionForBlock(1)} > {GetConditionForBlock(2)} > {GetConditionForBlock(3)}"
                : "";

        public void Assign(string participantId, int groupOverride = 0)
        {
            if (string.IsNullOrWhiteSpace(participantId))
                throw new ArgumentException("Participant ID cannot be empty.");

            string trimmed = participantId.Trim();
            int participantNumber = ExtractParticipantNumber(trimmed);

            if (groupOverride < 0 || groupOverride > 3)
                throw new ArgumentOutOfRangeException(
                    nameof(groupOverride),
                    "Group override must be 0 (Auto), 1, 2, or 3.");

            ParticipantId = trimmed;
            GroupNumber = groupOverride == 0
                ? ((participantNumber - 1) % 3) + 1
                : groupOverride;

            CurrentBlockIndex = 0;

            Debug.Log(
                $"M6 participant assigned: id={ParticipantId}; group={GroupLabel}; " +
                $"order={ConditionOrderLabel}; sequences=A>B>C.");
        }

        public VacLevel GetConditionForBlock(int blockIndex)
        {
            if (!HasAssignment)
                throw new InvalidOperationException("No participant assignment exists.");

            if (blockIndex < 1 || blockIndex > 3)
                throw new ArgumentOutOfRangeException(nameof(blockIndex));

            return GroupNumber switch
            {
                1 => blockIndex switch
                {
                    1 => VacLevel.C1,
                    2 => VacLevel.C2,
                    _ => VacLevel.C3
                },
                2 => blockIndex switch
                {
                    1 => VacLevel.C2,
                    2 => VacLevel.C3,
                    _ => VacLevel.C1
                },
                _ => blockIndex switch
                {
                    1 => VacLevel.C3,
                    2 => VacLevel.C1,
                    _ => VacLevel.C2
                }
            };
        }

        public TetrisSequenceId GetSequenceForBlock(int blockIndex)
        {
            if (blockIndex < 1 || blockIndex > 3)
                throw new ArgumentOutOfRangeException(nameof(blockIndex));

            return blockIndex switch
            {
                1 => TetrisSequenceId.A,
                2 => TetrisSequenceId.B,
                _ => TetrisSequenceId.C
            };
        }

        public void SetCurrentBlock(int blockIndex)
        {
            if (!HasAssignment)
                throw new InvalidOperationException("No participant assignment exists.");

            if (blockIndex < 0 || blockIndex > 3)
                throw new ArgumentOutOfRangeException(nameof(blockIndex));

            CurrentBlockIndex = blockIndex;
        }

        private static int ExtractParticipantNumber(string participantId)
        {
            Match match = Regex.Match(participantId, @"(\d+)$");
            if (!match.Success)
            {
                throw new ArgumentException(
                    $"Participant ID '{participantId}' must end with a number, e.g. P001.");
            }

            int number = int.Parse(match.Groups[1].Value);
            if (number <= 0)
                throw new ArgumentException("Participant number must be greater than zero.");

            return number;
        }
    }
}
