using UnityEngine;
using VACExperiment.Tetris;

namespace VACExperiment
{
    public class ExperimentFlowController : MonoBehaviour
    {
        [Header("Participant")]
        [SerializeField] private string participantId = "P01";

        [Header("Managers")]
        [SerializeField] private ExperimentManager experimentManager;
        [SerializeField] private DepthJudgmentManager depthJudgmentManager;
        [SerializeField] private TetrisManager tetrisManager;

        private void OnEnable()
        {
            if (depthJudgmentManager != null)
                depthJudgmentManager.onBlockCompleted.AddListener(HandleDepthBlockCompleted);

            if (tetrisManager != null)
                tetrisManager.onSessionCompleted.AddListener(HandleTetrisCompleted);
        }

        private void OnDisable()
        {
            if (depthJudgmentManager != null)
                depthJudgmentManager.onBlockCompleted.RemoveListener(HandleDepthBlockCompleted);

            if (tetrisManager != null)
                tetrisManager.onSessionCompleted.RemoveListener(HandleTetrisCompleted);
        }

        // Step 1: creates the participant session, then pauses for the initial SSQ.
        public void BeginParticipantFromInspector()
        {
            experimentManager.StartParticipant(participantId);
        }

        // Call after the initial SSQ is completed outside the headset.
        public void ContinueAfterInitialQuestionnaire()
        {
            experimentManager.BeginTraining();
        }

        // Call after the supervised 3–5 minute warm-up/familiarisation.
        public void FinishTraining()
        {
            experimentManager.FinishTrainingAndPrepareFirstCondition();
        }

        // Call after the pre-condition SSQ is completed.
        public void ContinueAfterPreConditionQuestionnaire()
        {
            experimentManager.BeginCurrentConditionAfterQuestionnaire();

            tetrisManager.BeginSession(
                experimentManager.GetCurrentCondition(),
                experimentManager.GetCurrentSequence());
        }

        // Call after the post-condition SSQ is completed.
        public void ContinueAfterPostConditionQuestionnaire()
        {
            if (experimentManager.CurrentConditionIndex == 1)
                experimentManager.BeginRecovery();
            else
                experimentManager.CompleteExperiment();
        }

        // Call only after the recovery criterion is satisfied.
        // This pauses for the Condition 2 pre-condition SSQ.
        public void ContinueAfterRecovery()
        {
            experimentManager.PrepareSecondConditionAfterRecovery();
        }

        public void StartDepthPractice()
        {
            depthJudgmentManager.BeginPractice(VacCondition.Low);
        }

        private void HandleTetrisCompleted()
        {
            experimentManager.MarkPostDepthStarted();
            depthJudgmentManager.BeginFormal(experimentManager.GetCurrentCondition());
        }

        private void HandleDepthBlockCompleted()
        {
            experimentManager.PauseForPostConditionQuestionnaire();
        }
    }
}
