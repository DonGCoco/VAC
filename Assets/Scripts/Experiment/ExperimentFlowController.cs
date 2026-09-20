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

        private DepthPhase activeDepthPhase;

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

        // Can be called by a simple UI button.
        public void BeginParticipantFromInspector()
        {
            experimentManager.StartParticipant(participantId);
        }

        public void FinishTraining()
        {
            experimentManager.FinishTrainingAndPauseForBaselineQuestionnaire();
        }

        // Call after the participant completes the baseline questionnaire outside the headset.
        public void ContinueAfterBaselineQuestionnaire()
        {
            experimentManager.BeginFirstCondition();
            StartPreDepth();
        }

        // Call after the post-condition questionnaire is finished outside the headset.
        public void ContinueAfterPostConditionQuestionnaire()
        {
            if (experimentManager.CurrentConditionIndex == 1)
            {
                experimentManager.BeginRecovery();
            }
            else
            {
                experimentManager.CompleteExperiment();
            }
        }

        // Call after recovery is judged sufficient.
        public void ContinueAfterRecovery()
        {
            experimentManager.BeginSecondCondition();
            StartPreDepth();
        }

        public void StartDepthPractice()
        {
            depthJudgmentManager.BeginPractice(VacCondition.Low);
        }

        private void StartPreDepth()
        {
            activeDepthPhase = DepthPhase.Pre;
            experimentManager.MarkPreDepthStarted();
            depthJudgmentManager.BeginFormal(
                experimentManager.GetCurrentCondition(),
                DepthPhase.Pre);
        }

        private void StartPostDepth()
        {
            activeDepthPhase = DepthPhase.Post;
            experimentManager.MarkPostDepthStarted();
            depthJudgmentManager.BeginFormal(
                experimentManager.GetCurrentCondition(),
                DepthPhase.Post);
        }

        private void HandleDepthBlockCompleted()
        {
            if (activeDepthPhase == DepthPhase.Pre)
            {
                experimentManager.MarkTetrisStarted();
                tetrisManager.BeginSession(
                    experimentManager.GetCurrentCondition(),
                    experimentManager.GetCurrentSequence());
            }
            else if (activeDepthPhase == DepthPhase.Post)
            {
                experimentManager.PauseForPostConditionQuestionnaire();
            }
        }

        private void HandleTetrisCompleted()
        {
            StartPostDepth();
        }
    }
}
