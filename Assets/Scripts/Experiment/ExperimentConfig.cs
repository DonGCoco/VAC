using UnityEngine;

namespace VACExperiment
{
    public enum VacLevel
    {
        C1,
        C2,
        C3
    }

    [CreateAssetMenu(fileName = "ExperimentConfig", menuName = "VAC Experiment/Experiment Config")]
    public class ExperimentConfig : ScriptableObject
    {
        [Header("Protocol")]
        [Tooltip("Human-readable study/protocol version stored with every M7 session.")]
        public string protocolVersion = "M7";

        [Header("VAC conditions (meters)")]
        [Tooltip("Optical focal distance of the Magic Leap 2 setup. Keep configurable until final confirmation.")]
        [Min(0.05f)] public float focalDistanceMeters = 0.74f;

        [Tooltip("C1 target viewing distance. Current implementation default; keep configurable for pilot/supervisor updates.")]
        [Min(0.05f)] public float c1DistanceMeters = 0.80f;

        [Tooltip("C2 target viewing distance. Current implementation default; keep configurable for pilot/supervisor updates.")]
        [Min(0.05f)] public float c2DistanceMeters = 1.00f;

        [Tooltip("C3 target viewing distance. Current implementation default; keep configurable for pilot/supervisor updates.")]
        [Min(0.05f)] public float c3DistanceMeters = 1.50f;

        [Tooltip("Allowed absolute difference between target and measured board distance for Ready.")]
        [Min(0.001f)] public float boardDistanceToleranceMeters = 0.03f;

        [Tooltip("How long a recent QR observation remains valid between Marker Understanding updates.")]
        [Min(0f)] public float markerVisibilityGraceSeconds = 0.50f;

        [Header("Condition visual geometry")]
        [Tooltip("Distance at which the authored stimulus local scale is treated as the reference visual size.")]
        [Min(0.05f)] public float visualAngleReferenceDistanceMeters = 1.0f;

        [Header("Tetris")]
        [Tooltip("Formal exposure duration. Final value should be fixed after pilot testing.")]
        [Min(1f)] public float tetrisDurationSeconds = 900f;

        [Tooltip("Warm-up condition. C2 is the current comfortable training default; keep configurable until the pilot.")]
        public VacLevel warmupCondition = VacLevel.C2;

        [Tooltip("Warm-up Tetris duration using training sequence T. Kept configurable until the pilot.")]
        [Min(10f)] public float warmupTetrisDurationSeconds = 180f;

        [Header("Flow")]
        [Tooltip("Minimum recovery interval between formal VAC blocks. The experimenter may wait longer.")]
        [Min(0f)] public float minimumRecoverySeconds = 300f;

        [Header("Depth judgment")]
        [Tooltip("Current depth-task reference distance. Kept separate from the physical-board calibration.")]
        [Min(0.05f)] public float depthTaskReferenceDistanceMeters = 1.0f;

        [Tooltip("Formal depth-judgement trials per block. Current candidate is 8.")]
        [Range(2, 40)] public int formalDepthTrials = 8;

        [Range(1, 10)] public int practiceDepthTrials = 3;

        [Tooltip("Total near/far separation around the reference depth. 0.06 m gives 0.97 / 1.03 m around a 1.00 m reference.")]
        [Min(0.001f)] public float depthDifferenceMeters = 0.06f;

        [Min(0.01f)] public float targetHorizontalSeparationMeters = 0.12f;

        [Tooltip("Target sphere diameter at targetReferenceScaleDistanceMeters.")]
        [Min(0.005f)] public float depthTargetDiameterMeters = 0.04f;

        [Tooltip("The target's authored scale is treated as correct at this distance.")]
        [Min(0.05f)] public float targetReferenceScaleDistanceMeters = 1.0f;

        // Temporary compatibility fields for the old two-condition code.
        // They stay hidden so Milestone 2 can move to C1/C2/C3 without breaking
        // ExperimentManager/VACController before those systems are replaced later.
        [HideInInspector] public float lowVacDistance = 1.0f;
        [HideInInspector] public float highVacDistance = 2.0f;

        public float GetTargetDistance(VacLevel level)
        {
            return level switch
            {
                VacLevel.C1 => c1DistanceMeters,
                VacLevel.C2 => c2DistanceMeters,
                VacLevel.C3 => c3DistanceMeters,
                _ => c1DistanceMeters
            };
        }

        public float GetDistance(VacCondition condition)
        {
            return condition == VacCondition.Low ? lowVacDistance : highVacDistance;
        }
    }
}
