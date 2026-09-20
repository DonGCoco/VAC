using UnityEngine;

namespace VACExperiment
{
    public class VACController : MonoBehaviour
    {
        [SerializeField] private ExperimentConfig config;
        [SerializeField] private Transform viewer;
        [SerializeField] private Transform contentRoot;
        [SerializeField] private ApparentSizeController apparentSizeController;

        public VacCondition CurrentCondition { get; private set; }
        public float CurrentDistanceMeters { get; private set; }

        public void ApplyCondition(VacCondition condition)
        {
            if (config == null || viewer == null || contentRoot == null)
            {
                Debug.LogError("VACController is missing required references.");
                return;
            }

            CurrentCondition = condition;
            CurrentDistanceMeters = config.GetDistance(condition);

            // Place once relative to the viewer. The content does not follow the head afterward.
            Vector3 flatForward = Vector3.ProjectOnPlane(viewer.forward, Vector3.up).normalized;
            if (flatForward.sqrMagnitude < 0.001f)
                flatForward = viewer.forward.normalized;

            contentRoot.position = viewer.position + flatForward * CurrentDistanceMeters;

            if (apparentSizeController != null)
                apparentSizeController.ApplyForDistance(CurrentDistanceMeters);
        }
    }
}
