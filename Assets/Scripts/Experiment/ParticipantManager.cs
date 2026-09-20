using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace VACExperiment
{
    public enum VacCondition
    {
        Low,
        High
    }

    public enum TetrisSequenceId
    {
        A,
        B
    }

    [Serializable]
    public struct ConditionAssignment
    {
        public VacCondition condition1;
        public TetrisSequenceId sequence1;
        public VacCondition condition2;
        public TetrisSequenceId sequence2;
    }

    public class ParticipantManager : MonoBehaviour
    {
        public string CurrentParticipantId { get; private set; }
        public ConditionAssignment CurrentAssignment { get; private set; }

        public ConditionAssignment Assign(string participantId)
        {
            if (string.IsNullOrWhiteSpace(participantId))
                throw new ArgumentException("Participant ID cannot be empty.");

            CurrentParticipantId = participantId.Trim();
            int number = ExtractParticipantNumber(CurrentParticipantId);

            int pattern = number % 4;

            CurrentAssignment = pattern switch
            {
                1 => Make(VacCondition.Low,  TetrisSequenceId.A, VacCondition.High, TetrisSequenceId.B),
                2 => Make(VacCondition.High, TetrisSequenceId.A, VacCondition.Low,  TetrisSequenceId.B),
                3 => Make(VacCondition.Low,  TetrisSequenceId.B, VacCondition.High, TetrisSequenceId.A),
                _ => Make(VacCondition.High, TetrisSequenceId.B, VacCondition.Low,  TetrisSequenceId.A)
            };

            return CurrentAssignment;
        }

        private static ConditionAssignment Make(
            VacCondition c1, TetrisSequenceId s1,
            VacCondition c2, TetrisSequenceId s2)
        {
            return new ConditionAssignment
            {
                condition1 = c1,
                sequence1 = s1,
                condition2 = c2,
                sequence2 = s2
            };
        }

        private static int ExtractParticipantNumber(string participantId)
        {
            Match match = Regex.Match(participantId, @"(\d+)$");
            if (!match.Success)
                throw new ArgumentException(
                    $"Participant ID '{participantId}' must end with a number, e.g. P01.");

            int number = int.Parse(match.Groups[1].Value);
            if (number <= 0)
                throw new ArgumentException("Participant number must be greater than zero.");

            return number;
        }
    }
}
