using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VACExperiment.Tetris;

namespace VACExperiment
{
    public enum DepthPhase
    {
        Practice,
        Post
    }

    public enum ResponseSide
    {
        Left,
        Right
    }

    /// <summary>
    /// Fixed-depth judgement task.
    ///
    /// This task is deliberately independent from the physical BoardAnchor.
    /// Each trial is placed in world space from the participant's viewer pose at
    /// trial onset, then remains fixed until the response.
    /// </summary>
    public class DepthJudgmentManager : MonoBehaviour
    {
        [SerializeField] private ExperimentConfig config;
        [SerializeField] private Transform viewer;
        [SerializeField] private Transform leftTarget;
        [SerializeField] private Transform rightTarget;
        [SerializeField] private DataLogger dataLogger;

        [Header("M5 transition")]
        [SerializeField] private TetrisManager tetrisManager;
        [SerializeField] private ConditionController conditionController;
        [SerializeField] private bool autoStartAfterTetrisForMilestone5 = true;
        [SerializeField] private bool createStandaloneTestLog = true;

        [Header("Events")]
        public UnityEvent onBlockCompleted;

        public bool IsRunning { get; private set; }
        public VacLevel CurrentCondition { get; private set; }
        public int CurrentTrialNumber => IsRunning ? trialIndex + 1 : 0;
        public int TrialCount => closerSides?.Count ?? 0;

        private DepthPhase phase;
        private float referenceDepth;
        private List<ResponseSide> closerSides;
        private int trialIndex;
        private float stimulusOnsetTime;

        private Vector3 leftReferenceScale;
        private Vector3 rightReferenceScale;
        private Renderer[] tetrisRenderers;

        private void Awake()
        {
            ResolveReferences();
            EnsureTargets();

            if (leftTarget != null)
                leftReferenceScale = leftTarget.localScale;

            if (rightTarget != null)
                rightReferenceScale = rightTarget.localScale;

            SetTargetsVisible(false);
        }

        private void Start()
        {
            if (autoStartAfterTetrisForMilestone5 && tetrisManager != null)
                tetrisManager.onSessionCompleted.AddListener(HandleTetrisCompleted);
        }

        private void OnDestroy()
        {
            if (tetrisManager != null)
                tetrisManager.onSessionCompleted.RemoveListener(HandleTetrisCompleted);
        }

        private void ResolveReferences()
        {
            if (viewer == null && Camera.main != null)
                viewer = Camera.main.transform;

            if (tetrisManager == null)
                tetrisManager = FindAnyObjectByType<TetrisManager>();

            if (conditionController == null)
                conditionController = FindAnyObjectByType<ConditionController>();

            if (dataLogger == null)
                dataLogger = GetComponent<DataLogger>();
        }

        private void EnsureTargets()
        {
            if (config == null)
                return;

            if (leftTarget == null)
                leftTarget = CreateDefaultTarget("DepthTarget_Left");

            if (rightTarget == null)
                rightTarget = CreateDefaultTarget("DepthTarget_Right");
        }

        private Transform CreateDefaultTarget(string objectName)
        {
            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            target.name = objectName;
            target.transform.SetParent(transform, false);
            target.transform.localScale = Vector3.one * config.depthTargetDiameterMeters;

            Collider collider = target.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            return target.transform;
        }

        private void HandleTetrisCompleted()
        {
            if (!autoStartAfterTetrisForMilestone5)
                return;

            if (tetrisManager != null)
            {
                tetrisRenderers ??= tetrisManager.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer renderer in tetrisRenderers)
                    renderer.enabled = false;
            }

            VacLevel level = conditionController != null
                ? conditionController.CurrentCondition
                : VacLevel.C1;

            BeginFormal(level);
        }

        public void BeginPractice(VacLevel practiceCondition)
        {
            BeginBlock(practiceCondition, DepthPhase.Practice, config.practiceDepthTrials);
        }

        public void BeginFormal(VacLevel exposureCondition)
        {
            BeginBlock(exposureCondition, DepthPhase.Post, config.formalDepthTrials);
        }

        // Compatibility overloads for the old two-condition flow until M6 replaces it.
        public void BeginPractice(VacCondition practiceCondition)
        {
            BeginPractice(practiceCondition == VacCondition.Low ? VacLevel.C1 : VacLevel.C3);
        }

        public void BeginFormal(VacCondition exposureCondition)
        {
            BeginFormal(exposureCondition == VacCondition.Low ? VacLevel.C1 : VacLevel.C3);
        }

        private void BeginBlock(VacLevel newCondition, DepthPhase newPhase, int trialCount)
        {
            ResolveReferences();
            EnsureTargets();

            if (config == null || viewer == null || leftTarget == null || rightTarget == null)
            {
                Debug.LogError("DepthJudgmentManager is missing required references.");
                return;
            }

            if (config.depthDifferenceMeters >= config.depthTaskReferenceDistanceMeters * 2f)
            {
                Debug.LogError("Depth difference is too large for the configured reference distance.");
                return;
            }

            if (newPhase != DepthPhase.Practice &&
                createStandaloneTestLog &&
                dataLogger != null &&
                string.IsNullOrWhiteSpace(dataLogger.ParticipantId))
            {
                dataLogger.StartSession($"M5_TEST_{DateTime.Now:yyyyMMdd_HHmmss}");
            }

            CurrentCondition = newCondition;
            phase = newPhase;

            // Fixed outcome task: the same depths are used after every VAC block.
            referenceDepth = config.depthTaskReferenceDistanceMeters;

            closerSides = BuildBalancedOrder(trialCount);
            trialIndex = 0;
            IsRunning = true;

            Debug.Log(
                $"M5 depth task started: condition={CurrentCondition}; phase={phase}; " +
                $"trials={trialCount}; near={GetNearDepth():F3} m; far={GetFarDepth():F3} m; " +
                "reference=viewer-at-trial-onset; BoardAnchor=unused.");

            ShowTrial();
        }

        public void SubmitLeft()
        {
            Submit(ResponseSide.Left);
        }

        public void SubmitRight()
        {
            Submit(ResponseSide.Right);
        }

        private void Submit(ResponseSide response)
        {
            if (!IsRunning)
                return;

            float reactionTime = Time.realtimeSinceStartup - stimulusOnsetTime;
            ResponseSide correctSide = closerSides[trialIndex];
            bool correct = response == correctSide;

            float nearDepth = GetNearDepth();
            float farDepth = GetFarDepth();

            if (phase != DepthPhase.Practice && dataLogger != null)
            {
                dataLogger.LogDepthTrial(
                    CurrentCondition,
                    phase.ToString(),
                    trialIndex + 1,
                    referenceDepth,
                    config.depthDifferenceMeters,
                    correctSide.ToString(),
                    response.ToString(),
                    correct,
                    reactionTime);
            }

            Debug.Log(
                $"M5 depth trial {trialIndex + 1}/{closerSides.Count}: " +
                $"near={nearDepth:F3} m; far={farDepth:F3} m; " +
                $"closer={correctSide}; response={response}; correct={correct}; " +
                $"rt={reactionTime:F3}s.");

            trialIndex++;

            if (trialIndex >= closerSides.Count)
            {
                IsRunning = false;
                SetTargetsVisible(false);
                Debug.Log("M5 depth task complete.");
                onBlockCompleted?.Invoke();
                return;
            }

            ShowTrial();
        }

        private void ShowTrial()
        {
            ResponseSide closerSide = closerSides[trialIndex];

            Vector3 forward = viewer.forward.normalized;
            Vector3 right = viewer.right.normalized;
            float halfSeparation = config.targetHorizontalSeparationMeters * 0.5f;

            float nearDepth = GetNearDepth();
            float farDepth = GetFarDepth();

            float leftDepth = closerSide == ResponseSide.Left ? nearDepth : farDepth;
            float rightDepth = closerSide == ResponseSide.Right ? nearDepth : farDepth;

            // Capture the viewer pose once at trial onset. The targets are world-fixed
            // for the rest of that trial and do not follow subsequent head movement.
            leftTarget.position =
                viewer.position + forward * leftDepth - right * halfSeparation;

            rightTarget.position =
                viewer.position + forward * rightDepth + right * halfSeparation;

            ApplyConstantAngularSize(leftTarget, leftReferenceScale, leftDepth);
            ApplyConstantAngularSize(rightTarget, rightReferenceScale, rightDepth);

            SetTargetsVisible(true);
            stimulusOnsetTime = Time.realtimeSinceStartup;
        }

        private float GetNearDepth()
        {
            return referenceDepth - config.depthDifferenceMeters * 0.5f;
        }

        private float GetFarDepth()
        {
            return referenceDepth + config.depthDifferenceMeters * 0.5f;
        }

        private void ApplyConstantAngularSize(
            Transform target,
            Vector3 authoredScale,
            float targetDepthMeters)
        {
            float referenceDistance = config.targetReferenceScaleDistanceMeters;
            float scaleFactor = targetDepthMeters / referenceDistance;
            target.localScale = authoredScale * scaleFactor;
        }

        private void SetTargetsVisible(bool visible)
        {
            if (leftTarget != null)
                leftTarget.gameObject.SetActive(visible);

            if (rightTarget != null)
                rightTarget.gameObject.SetActive(visible);
        }

        private static List<ResponseSide> BuildBalancedOrder(int count)
        {
            var list = new List<ResponseSide>(count);
            int leftCount = count / 2;
            int rightCount = count / 2;

            if (count % 2 != 0)
            {
                if (UnityEngine.Random.value < 0.5f)
                    leftCount++;
                else
                    rightCount++;
            }

            for (int i = 0; i < leftCount; i++)
                list.Add(ResponseSide.Left);

            for (int i = 0; i < rightCount; i++)
                list.Add(ResponseSide.Right);

            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }

            return list;
        }
    }
}
