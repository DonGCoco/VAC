using UnityEngine;

namespace VACExperiment
{
    public enum ExperimentPhase
    {
        Idle,
        Training,
        BaselineQuestionnairePause,
        PreDepth,
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
        public int CurrentConditionIndex { get; private set; } = 0;

        private ConditionAssignment assignment;

        public void StartParticipant(string participantId)
        {
            assignment = participantManager.Assign(participantId);
            dataLogger.StartSession(participantId);
            CurrentConditionIndex = 0;
            SetPhase(ExperimentPhase.Training);
        }

        public void FinishTrainingAndPauseForBaselineQuestionnaire()
        {
            SetPhase(ExperimentPhase.BaselineQuestionnairePause);
        }

        public void BeginFirstCondition()
        {
            CurrentConditionIndex = 1;
            BeginCurrentCondition();
        }

        public void BeginSecondCondition()
        {
            CurrentConditionIndex = 2;
            BeginCurrentCondition();
        }

        public VacCondition GetCurrentCondition()
        {
            return CurrentConditionIndex == 1
                ? assignment.condition1
                : assignment.condition2;
        }

        public TetrisSequenceId GetCurrentSequence()
        {
            return CurrentConditionIndex == 1
                ? assignment.sequence1
                : assignment.sequence2;
        }

        public void MarkPreDepthStarted()
        {
            SetPhase(ExperimentPhase.PreDepth);
        }

        public void MarkTetrisStarted()
        {
            SetPhase(ExperimentPhase.Tetris);
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

        private void BeginCurrentCondition()
        {
            VacCondition condition = GetCurrentCondition();
            vacController.ApplyCondition(condition);
            dataLogger.LogEvent(
                "ConditionStarted",
                condition.ToString(),
                $"ConditionIndex={CurrentConditionIndex};Sequence={GetCurrentSequence()}");
            SetPhase(ExperimentPhase.PreDepth);
        }

        private void SetPhase(ExperimentPhase newPhase)
        {
            Phase = newPhase;
            string condition = CurrentConditionIndex > 0
                ? GetCurrentCondition().ToString()
                : "";

            dataLogger.LogEvent("PhaseChanged", condition, newPhase.ToString());
            Debug.Log($"Experiment phase: {newPhase}");
        }
    }
}
