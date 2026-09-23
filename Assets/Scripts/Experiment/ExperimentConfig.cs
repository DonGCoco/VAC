using UnityEngine;

namespace VACExperiment
{
    [CreateAssetMenu(fileName = "ExperimentConfig", menuName = "VAC Experiment/Experiment Config")]
    public class ExperimentConfig : ScriptableObject
    {
        [Header("VAC stimulus distances (meters)")]
        [Tooltip("Placeholder until pilot/supervisor confirmation.")]
        [Min(0.01f)] public float lowVacDistance = 1.0f;

        [Tooltip("Placeholder until pilot/supervisor confirmation.")]
        [Min(0.01f)] public float highVacDistance = 2.0f;

        [Header("Tetris")]
        [Tooltip("Formal exposure duration. Final value should be fixed after pilot testing.")]
        [Min(1f)] public float tetrisDurationSeconds = 900f;

        [Header("Depth judgment")]
        [Tooltip("Same reference depth is used after both VAC conditions.")]
        [Min(0.05f)] public float depthTaskReferenceDistanceMeters = 1.0f;

        [Tooltip("Pilot target is approximately 8–12 formal trials.")]
        [Range(2, 40)] public int formalDepthTrials = 10;

        [Range(1, 10)] public int practiceDepthTrials = 3;

        [Tooltip("Total near/far separation around the reference depth.")]
        [Min(0.001f)] public float depthDifferenceMeters = 0.03f;

        [Min(0.01f)] public float targetHorizontalSeparationMeters = 0.12f;

        [Tooltip("The target's authored scale is treated as correct at this distance.")]
        [Min(0.05f)] public float targetReferenceScaleDistanceMeters = 1.0f;

        public float GetDistance(VacCondition condition)
        {
            return condition == VacCondition.Low ? lowVacDistance : highVacDistance;
        }
    }
}
