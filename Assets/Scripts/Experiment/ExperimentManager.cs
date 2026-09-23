using UnityEngine;

namespace VACExperiment
{
    public enum ExperimentPhase
    {
        Idle,
        InitialQuestionnairePause,
        Training,
        PreConditionQuestionnairePause,
        Tetris,
        PostDepth,
        PostConditionQuestionnairePause,
        Recovery,
        Complete
    }

    public class ExperimentManager : MonoBehaviour
    {
        [SerializeField] private ParticipantManager participantManager;
        [SerializeField] private DataLogger dataLogger;
        [SerializeField] private VACController vacController;

        public ExperimentPhase Phase { get; private set; } = ExperimentPhase.Idle;
        public int CurrentConditionIndex { get; private set; }

        private ConditionAssignment assignment;

        public void StartParticipant(string participantId)
        {
            assignment = participantManager.Assign(participantId);
            dataLogger.StartSession(participantId);
            CurrentConditionIndex = 0;
            SetPhase(ExperimentPhase.InitialQuestionnairePause);
        }

        public void BeginTraining()
        {
            SetPhase(ExperimentPhase.Training);
        }

        public void FinishTrainingAndPrepareFirstCondition()
        {
            CurrentConditionIndex = 1;
            SetPhase(ExperimentPhase.PreConditionQuestionnairePause);
        }

        public void PrepareSecondConditionAfterRecovery()
        {
            CurrentConditionIndex = 2;
            SetPhase(ExperimentPhase.PreConditionQuestionnairePause);
        }

        public void BeginCurrentConditionAfterQuestionnaire()
        {
            if (CurrentConditionIndex < 1 || CurrentConditionIndex > 2)
            {
                Debug.LogError("No formal condition is prepared.");
                return;
            }

            VacCondition condition = GetCurrentCondition();
            vacController.ApplyCondition(condition);

            dataLogger.LogEvent(
                "ConditionStarted",
                condition.ToString(),
                $"ConditionIndex={CurrentConditionIndex};Sequence={GetCurrentSequence()}");

            SetPhase(ExperimentPhase.Tetris);
        }

        public VacCondition GetCurrentCondition()
        {
            if (CurrentConditionIndex == 1)
                return assignment.condition1;

            if (CurrentConditionIndex == 2)
                return assignment.condition2;

            throw new System.InvalidOperationException("No formal condition is active.");
        }

        public TetrisSequenceId GetCurrentSequence()
        {
            if (CurrentConditionIndex == 1)
                return assignment.sequence1;

            if (CurrentConditionIndex == 2)
                return assignment.sequence2;

            throw new System.InvalidOperationException("No formal condition is active.");
        }

        public void MarkPostDepthStarted()
        {
            SetPhase(ExperimentPhase.PostDepth);
        }

        public void PauseForPostConditionQuestionnaire()
        {
            SetPhase(ExperimentPhase.PostConditionQuestionnairePause);
        }

        public void BeginRecovery()
        {
            SetPhase(ExperimentPhase.Recovery);
        }

        public void CompleteExperiment()
        {
            SetPhase(ExperimentPhase.Complete);
        }

        private void SetPhase(ExperimentPhase newPhase)
        {
            Phase = newPhase;

            string condition = "";
            if (CurrentConditionIndex == 1)
                condition = assignment.condition1.ToString();
            else if (CurrentConditionIndex == 2)
                condition = assignment.condition2.ToString();

            dataLogger.LogEvent("PhaseChanged", condition, newPhase.ToString());
            Debug.Log($"Experiment phase: {newPhase}");
        }
    }
}
