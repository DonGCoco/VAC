using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VACExperiment
{
    public enum DepthPhase
    {
        Practice,
        Pre,
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

        public void BeginPractice(VacCondition practiceCondition)
        {
            BeginBlock(practiceCondition, DepthPhase.Practice, config.practiceDepthTrials);
        }

        public void BeginFormal(VacCondition formalCondition, DepthPhase formalPhase)
        {
            if (formalPhase == DepthPhase.Practice)
                throw new ArgumentException("Use BeginPractice for practice trials.");

            BeginBlock(formalCondition, formalPhase, config.formalDepthTrials);
        }

        private void BeginBlock(VacCondition newCondition, DepthPhase newPhase, int trialCount)
        {
            if (config == null || viewer == null || leftTarget == null || rightTarget == null)
            {
                Debug.LogError("DepthJudgmentManager is missing required references.");
                return;
            }

            condition = newCondition;
            phase = newPhase;
            referenceDepth = config.GetDistance(condition);
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
            Vector3 center = viewer.position + flatForward * referenceDepth;
            float halfSeparation = config.targetHorizontalSeparationMeters * 0.5f;

            float nearDepth = referenceDepth;
            float farDepth = referenceDepth + config.depthDifferenceMeters;

            Vector3 leftBase = viewer.position + flatForward *
                (closerSide == ResponseSide.Left ? nearDepth : farDepth) - right * halfSeparation;

            Vector3 rightBase = viewer.position + flatForward *
                (closerSide == ResponseSide.Right ? nearDepth : farDepth) + right * halfSeparation;

            leftTarget.position = leftBase;
            rightTarget.position = rightBase;

            SetTargetsVisible(true);
            stimulusOnsetTime = Time.realtimeSinceStartup;
        }

        private void SetTargetsVisible(bool visible)
        {
            if (leftTarget != null) leftTarget.gameObject.SetActive(visible);
            if (rightTarget != null) rightTarget.gameObject.SetActive(visible);
        }

        private static List<ResponseSide> BuildBalancedOrder(int count)
        {
            var list = new List<ResponseSide>(count);
            int leftCount = count / 2;
            int rightCount = count / 2;

            if (count % 2 != 0)
            {
                if (UnityEngine.Random.value < 0.5f) leftCount++;
                else rightCount++;
            }

            for (int i = 0; i < leftCount; i++) list.Add(ResponseSide.Left);
            for (int i = 0; i < rightCount; i++) list.Add(ResponseSide.Right);

            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }

            return list;
        }
    }
}
