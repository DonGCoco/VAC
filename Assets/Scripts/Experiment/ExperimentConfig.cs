using UnityEngine;

namespace VACExperiment
{
    [CreateAssetMenu(fileName = "ExperimentConfig", menuName = "VAC Experiment/Experiment Config")]
    public class ExperimentConfig : ScriptableObject
    {
        [Header("VAC distances (meters)")]
        [Min(0.01f)] public float lowVacDistance = 1.0f;
        [Min(0.01f)] public float highVacDistance = 2.0f;

        [Header("Tetris")]
        [Min(1f)] public float tetrisDurationSeconds = 900f;

        [Header("Depth judgment")]
        [Min(2)] public int formalDepthTrials = 20;
        [Range(1, 10)] public int practiceDepthTrials = 4;
        [Min(0.001f)] public float depthDifferenceMeters = 0.03f;
        [Min(0.01f)] public float targetHorizontalSeparationMeters = 0.12f;

        public float GetDistance(VacCondition condition)
        {
            return condition == VacCondition.Low ? lowVacDistance : highVacDistance;
        }
    }
}
