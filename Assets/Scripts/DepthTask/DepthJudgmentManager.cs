using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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

    public class DepthJudgmentManager : MonoBehaviour
    {
        [SerializeField] private ExperimentConfig config;
        [SerializeField] private Transform viewer;
        [SerializeField] private Transform leftTarget;
        [SerializeField] private Transform rightTarget;
        [SerializeField] private DataLogger dataLogger;

        [Header("Events")]
        public UnityEvent onBlockCompleted;

        public bool IsRunning { get; private set; }

        private VacCondition condition;
        private DepthPhase phase;
        private float referenceDepth;
        private List<ResponseSide> closerSides;
        private int trialIndex;
        private float stimulusOnsetTime;

        private Vector3 leftReferenceScale;
        private Vector3 rightReferenceScale;

        private void Awake()
        {
            if (leftTarget != null)
                leftReferenceScale = leftTarget.localScale;

            if (rightTarget != null)
                rightReferenceScale = rightTarget.localScale;
        }

        public void BeginPractice(VacCondition practiceCondition)
        {
            BeginBlock(practiceCondition, DepthPhase.Practice, config.practiceDepthTrials);
        }

        public void BeginFormal(VacCondition exposureCondition)
        {
            BeginBlock(exposureCondition, DepthPhase.Post, config.formalDepthTrials);
        }

        private void BeginBlock(VacCondition newCondition, DepthPhase newPhase, int trialCount)
        {
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

            condition = newCondition;
            phase = newPhase;

            // IMPORTANT: the depth-test reference distance is identical after both VAC conditions.
            // Otherwise the outcome task itself would change between conditions.
            referenceDepth = config.depthTaskReferenceDistanceMeters;

            closerSides = BuildBalancedOrder(trialCount);
            trialIndex = 0;
            IsRunning = true;

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

            if (phase != DepthPhase.Practice && dataLogger != null)
            {
                dataLogger.LogDepthTrial(
                    condition,
                    phase.ToString(),
                    trialIndex + 1,
                    referenceDepth,
                    config.depthDifferenceMeters,
                    correctSide.ToString(),
                    response.ToString(),
                    correct,
                    reactionTime);
            }

            trialIndex++;

            if (trialIndex >= closerSides.Count)
            {
                IsRunning = false;
                SetTargetsVisible(false);
                onBlockCompleted?.Invoke();
                return;
            }

            ShowTrial();
        }

        private void ShowTrial()
        {
            ResponseSide closerSide = closerSides[trialIndex];

            Vector3 flatForward = Vector3.ProjectOnPlane(viewer.forward, Vector3.up).normalized;
            if (flatForward.sqrMagnitude < 0.001f)
                flatForward = viewer.forward.normalized;

            Vector3 right = Vector3.Cross(Vector3.up, flatForward).normalized;
            float halfSeparation = config.targetHorizontalSeparationMeters * 0.5f;
            float halfDepthDifference = config.depthDifferenceMeters * 0.5f;

            float nearDepth = referenceDepth - halfDepthDifference;
            float farDepth = referenceDepth + halfDepthDifference;

            float leftDepth = closerSide == ResponseSide.Left ? nearDepth : farDepth;
            float rightDepth = closerSide == ResponseSide.Right ? nearDepth : farDepth;

            leftTarget.position =
                viewer.position + flatForward * leftDepth - right * halfSeparation;

            rightTarget.position =
                viewer.position + flatForward * rightDepth + right * halfSeparation;

            // Remove apparent-size as an unintended monocular cue.
            ApplyConstantAngularSize(leftTarget, leftReferenceScale, leftDepth);
            ApplyConstantAngularSize(rightTarget, rightReferenceScale, rightDepth);

            SetTargetsVisible(true);
            stimulusOnsetTime = Time.realtimeSinceStartup;
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
