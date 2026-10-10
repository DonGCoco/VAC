using System;
using UnityEngine;
using VACExperiment.Board;
using VACExperiment.Tetris;

namespace VACExperiment
{
    public enum FormalExperimentPhase
    {
        Idle,
        WarmupCalibration,
        WarmupTetris,
        WarmupDepthPractice,
        PreBlockQuestionnaire,
        BlockCalibration,
        FormalTetris,
        FormalDepth,
        PostBlockQuestionnaire,
        Recovery,
        Complete
    }

    /// <summary>
    /// M6 single-scene experiment state machine.
    ///
    /// Flow:
    /// participant assignment
    /// -> warm-up calibration at C2
    /// -> Tetris training sequence T
    /// -> depth practice
    /// -> pre-block questionnaire
    /// -> calibration / participant START
    /// -> formal Tetris
    /// -> formal depth task
    /// -> post questionnaire
    /// -> recovery (between blocks)
    /// -> repeat for three counterbalanced VAC conditions.
    ///
    /// Questionnaires remain external; the monitor provides explicit checkpoints.
    /// </summary>
    public class FormalExperimentFlowController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ExperimentConfig config;
        [SerializeField] private ParticipantSession participantSession;
        [SerializeField] private BoardDistanceCalibration calibration;
        [SerializeField] private TetrisManager tetrisManager;
        [SerializeField] private DepthJudgmentManager depthJudgmentManager;
        [SerializeField] private DataLogger dataLogger;

        public FormalExperimentPhase Phase { get; private set; } = FormalExperimentPhase.Idle;
        public string PhaseLabel => Phase.ToString();
        public int CurrentBlockIndex =>
            participantSession != null ? participantSession.CurrentBlockIndex : 0;
        public bool ParticipantStartAllowed =>
            Phase == FormalExperimentPhase.WarmupCalibration ||
            Phase == FormalExperimentPhase.BlockCalibration;
        public bool HasActiveParticipant =>
            participantSession != null && participantSession.HasAssignment;
        public float RecoveryRemainingSeconds =>
            Phase == FormalExperimentPhase.Recovery && config != null
                ? Mathf.Max(0f, config.minimumRecoverySeconds -
                    (Time.realtimeSinceStartup - recoveryStartTime))
                : 0f;
        public bool RecoveryMinimumSatisfied =>
            Phase == FormalExperimentPhase.Recovery &&
            RecoveryRemainingSeconds <= 0f;

        private float recoveryStartTime;
        private bool listenersAttached;

        private void Start()
        {
            ResolveReferences();
            AttachListeners();

            if (depthJudgmentManager != null)
                depthJudgmentManager.SetMilestone5AutoStartEnabled(false);
        }

        private void OnDestroy()
        {
            DetachListeners();
        }

        public void SetParticipantSession(ParticipantSession session)
        {
            participantSession = session;
        }

        public void AssignParticipant(string participantId, int groupOverride = 0)
        {
            ResolveReferences();
            AttachListeners();

            if (participantSession == null || calibration == null ||
                tetrisManager == null || depthJudgmentManager == null || dataLogger == null)
            {
                throw new InvalidOperationException(
                    "FormalExperimentFlowController is missing required references.");
            }

            if (Phase != FormalExperimentPhase.Idle &&
                Phase != FormalExperimentPhase.Complete &&
                Phase != FormalExperimentPhase.WarmupCalibration)
            {
                throw new InvalidOperationException(
                    $"Cannot assign a new participant during phase {Phase}.");
            }

            participantSession.Assign(participantId, groupOverride);
            participantSession.SetCurrentBlock(0);
            dataLogger.StartSession(participantSession.ParticipantId);

            dataLogger.LogEvent(
                "ParticipantAssigned",
                "",
                $"Group={participantSession.GroupLabel};Order={participantSession.ConditionOrderLabel}");

            depthJudgmentManager.SetMilestone5AutoStartEnabled(false);
            PrepareWarmupCalibration();
        }

        public void HandleParticipantStart()
        {
            ResolveReferences();

            if (!ParticipantStartAllowed)
            {
                Debug.LogWarning(
                    $"Participant START ignored during phase {Phase}.");
                return;
            }

            if (Phase == FormalExperimentPhase.WarmupCalibration)
            {
                SetPhase(FormalExperimentPhase.WarmupTetris, "WarmupStarted", VacLevel.C2);
                tetrisManager.BeginTrainingSession(VacLevel.C2);
                return;
            }

            if (Phase == FormalExperimentPhase.BlockCalibration)
            {
                int block = participantSession.CurrentBlockIndex;
                VacLevel condition = participantSession.GetConditionForBlock(block);
                TetrisSequenceId sequence = participantSession.GetSequenceForBlock(block);

                dataLogger.LogEvent(
                    "FormalBlockStarted",
                    condition.ToString(),
                    $"Block={block};Sequence={sequence}");

                SetPhase(FormalExperimentPhase.FormalTetris);
                tetrisManager.BeginSession(condition, sequence);
            }
        }

        public void ContinueAfterQuestionnaire()
        {
            ResolveReferences();

            if (Phase == FormalExperimentPhase.PreBlockQuestionnaire)
            {
                PrepareCurrentBlockCalibration();
                return;
            }

            if (Phase != FormalExperimentPhase.PostBlockQuestionnaire)
            {
                Debug.LogWarning(
                    $"Questionnaire continue ignored during phase {Phase}.");
                return;
            }

            int block = participantSession.CurrentBlockIndex;
            VacLevel condition = participantSession.GetConditionForBlock(block);
            dataLogger.LogEvent(
                "PostQuestionnaireCompleted",
                condition.ToString(),
                $"Block={block}");

            if (block >= 3)
            {
                calibration.RescanBoard();
                SetPhase(FormalExperimentPhase.Complete, "ExperimentCompleted");
                return;
            }

            calibration.RescanBoard();
            recoveryStartTime = Time.realtimeSinceStartup;
            SetPhase(FormalExperimentPhase.Recovery, "RecoveryStarted");
        }

        public void ContinueAfterRecovery()
        {
            if (Phase != FormalExperimentPhase.Recovery)
            {
                Debug.LogWarning(
                    $"Recovery continue ignored during phase {Phase}.");
                return;
            }

            if (!RecoveryMinimumSatisfied)
            {
                Debug.LogWarning(
                    $"Recovery minimum not satisfied. Remaining={RecoveryRemainingSeconds:F1}s.");
                return;
            }

            int nextBlock = participantSession.CurrentBlockIndex + 1;
            participantSession.SetCurrentBlock(nextBlock);
            SetPhase(
                FormalExperimentPhase.PreBlockQuestionnaire,
                "PreQuestionnaireReady",
                participantSession.GetConditionForBlock(nextBlock));
        }

        private void PrepareWarmupCalibration()
        {
            calibration.SelectCondition(VacLevel.C2);
            SetPhase(
                FormalExperimentPhase.WarmupCalibration,
                "WarmupCalibrationReady",
                VacLevel.C2);
        }

        private void PrepareCurrentBlockCalibration()
        {
            int block = participantSession.CurrentBlockIndex;
            if (block < 1 || block > 3)
            {
                Debug.LogError($"Invalid formal block index {block}.");
                return;
            }

            VacLevel condition = participantSession.GetConditionForBlock(block);
            calibration.SelectCondition(condition);

            dataLogger.LogEvent(
                "PreQuestionnaireCompleted",
                condition.ToString(),
                $"Block={block};Sequence={participantSession.GetSequenceForBlock(block)}");

            SetPhase(FormalExperimentPhase.BlockCalibration);
        }

        private void HandleTetrisCompleted()
        {
            if (Phase == FormalExperimentPhase.WarmupTetris)
            {
                tetrisManager.SetVisualsVisible(false);
                SetPhase(
                    FormalExperimentPhase.WarmupDepthPractice,
                    "WarmupDepthPracticeStarted",
                    VacLevel.C2);
                depthJudgmentManager.BeginPractice(VacLevel.C2);
                return;
            }

            if (Phase != FormalExperimentPhase.FormalTetris)
                return;

            int block = participantSession.CurrentBlockIndex;
            VacLevel condition = participantSession.GetConditionForBlock(block);

            tetrisManager.SetVisualsVisible(false);
            SetPhase(FormalExperimentPhase.FormalDepth);
            depthJudgmentManager.BeginFormal(condition);
        }

        private void HandleDepthCompleted()
        {
            if (Phase == FormalExperimentPhase.WarmupDepthPractice)
            {
                calibration.RescanBoard();
                participantSession.SetCurrentBlock(1);
                SetPhase(
                    FormalExperimentPhase.PreBlockQuestionnaire,
                    "WarmupCompleted",
                    participantSession.GetConditionForBlock(1));
                return;
            }

            if (Phase != FormalExperimentPhase.FormalDepth)
                return;

            int block = participantSession.CurrentBlockIndex;
            VacLevel condition = participantSession.GetConditionForBlock(block);
            SetPhase(
                FormalExperimentPhase.PostBlockQuestionnaire,
                "PostQuestionnaireReady",
                condition);
        }

        private void ResolveReferences()
        {
            if (config == null)
                config = Resources.FindObjectsOfTypeAll<ExperimentConfig>().Length > 0
                    ? Resources.FindObjectsOfTypeAll<ExperimentConfig>()[0]
                    : null;

            if (participantSession == null)
                participantSession = FindAnyObjectByType<ParticipantSession>();

            if (calibration == null)
                calibration = FindAnyObjectByType<BoardDistanceCalibration>();

            if (tetrisManager == null)
                tetrisManager = FindAnyObjectByType<TetrisManager>();

            if (depthJudgmentManager == null)
                depthJudgmentManager = FindAnyObjectByType<DepthJudgmentManager>();

            if (dataLogger == null)
                dataLogger = FindAnyObjectByType<DataLogger>();
        }

        private void AttachListeners()
        {
            if (listenersAttached)
                return;

            if (tetrisManager == null || depthJudgmentManager == null)
                return;

            tetrisManager.onSessionCompleted.AddListener(HandleTetrisCompleted);
            depthJudgmentManager.onBlockCompleted.AddListener(HandleDepthCompleted);
            listenersAttached = true;
        }

        private void DetachListeners()
        {
            if (!listenersAttached)
                return;

            if (tetrisManager != null)
                tetrisManager.onSessionCompleted.RemoveListener(HandleTetrisCompleted);

            if (depthJudgmentManager != null)
                depthJudgmentManager.onBlockCompleted.RemoveListener(HandleDepthCompleted);

            listenersAttached = false;
        }

        private void SetPhase(
            FormalExperimentPhase newPhase,
            string eventName = "PhaseChanged",
            VacLevel? condition = null)
        {
            Phase = newPhase;
            string conditionText = condition.HasValue
                ? condition.Value.ToString()
                : "";

            dataLogger?.LogEvent(
                eventName,
                conditionText,
                $"Phase={newPhase};Block={CurrentBlockIndex}");

            Debug.Log(
                $"M6 phase: {newPhase}; participant=" +
                $"{(participantSession != null ? participantSession.ParticipantId : "")}; " +
                $"block={CurrentBlockIndex}.");
        }
    }
}
