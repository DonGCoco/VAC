using UnityEngine;
using VACExperiment.Board;

namespace VACExperiment
{
    /// <summary>
    /// Milestone 3 condition geometry.
    ///
    /// BoardAnchor defines stimulus depth. This controller never places content
    /// relative to the current head pose. It keeps the stimulus co-planar with
    /// BoardAnchor and applies constant-visual-angle scaling once per condition.
    /// </summary>
    public class ConditionController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ExperimentConfig config;
        [SerializeField] private BoardRegistration boardRegistration;
        [SerializeField] private Transform stimulusRoot;

        [Header("In-plane placement")]
        [Tooltip("Configurable X/Y offset on the registered board plane. No Z offset is permitted.")]
        [SerializeField] private Vector2 stimulusOffsetMeters = Vector2.zero;

        [Tooltip("Optional rotation within the registered board plane.")]
        [SerializeField] private float stimulusInPlaneRotationDegrees = 0f;

        public VacLevel CurrentCondition { get; private set; }
        public float TargetDistanceMeters { get; private set; }
        public float ScaleFactor { get; private set; } = 1f;
        public float FocalDiopters { get; private set; }
        public float VergenceDiopters { get; private set; }
        public float VacDiopters { get; private set; }

        private Vector3 referenceLocalScale;
        private bool referenceScaleCached;

        private void Awake()
        {
            ResolveReferences();
            CacheReferenceScale();
        }

        private void ResolveReferences()
        {
            if (boardRegistration == null)
                boardRegistration = FindAnyObjectByType<BoardRegistration>();
        }

        private void CacheReferenceScale()
        {
            if (stimulusRoot == null || referenceScaleCached)
                return;

            referenceLocalScale = stimulusRoot.localScale;
            referenceScaleCached = true;
        }

        public void ApplyCondition(VacLevel level)
        {
            ResolveReferences();
            CacheReferenceScale();

            if (config == null || boardRegistration == null || stimulusRoot == null)
            {
                Debug.LogError(
                    "ConditionController requires ExperimentConfig, BoardRegistration and StimulusRoot.");
                return;
            }

            Transform boardAnchor = boardRegistration.BoardAnchor;
            if (boardAnchor == null)
            {
                Debug.LogError("ConditionController cannot apply condition without BoardAnchor.");
                return;
            }

            float referenceDistance = config.visualAngleReferenceDistanceMeters;
            float targetDistance = config.GetTargetDistance(level);

            if (referenceDistance <= 0f || targetDistance <= 0f)
            {
                Debug.LogError("Condition distances must be greater than zero.");
                return;
            }

            if (stimulusRoot.parent != boardAnchor)
                stimulusRoot.SetParent(boardAnchor, false);

            stimulusRoot.localPosition =
                new Vector3(stimulusOffsetMeters.x, stimulusOffsetMeters.y, 0f);
            stimulusRoot.localRotation =
                Quaternion.Euler(0f, 0f, stimulusInPlaneRotationDegrees);

            // S(d) = 2 d tan(theta/2). Relative to an authored reference at d_ref,
            // S(d) / S(d_ref) = d / d_ref.
            // Apply once per condition; never rescale continuously from head motion.
            ScaleFactor = targetDistance / referenceDistance;
            stimulusRoot.localScale = referenceLocalScale * ScaleFactor;

            CurrentCondition = level;
            TargetDistanceMeters = targetDistance;

            FocalDiopters = 1f / config.focalDistanceMeters;
            VergenceDiopters = 1f / targetDistance;
            VacDiopters = FocalDiopters - VergenceDiopters;

            Debug.Log(
                $"M3 condition applied: {level}; target={targetDistance:F3} m; " +
                $"reference={referenceDistance:F3} m; scale={ScaleFactor:F3}; " +
                $"Df={FocalDiopters:F3} D; Dv={VergenceDiopters:F3} D; " +
                $"VAC={VacDiopters:F3} D; stimulusLocalZ={stimulusRoot.localPosition.z:F4} m.");
        }
    }
}
