using UnityEngine;

namespace VACExperiment.SmokeTest
{
    /// <summary>
    /// Milestone 1/2 real-device validation helper.
    ///
    /// Milestone 1:
    /// - prove that a world-fixed cube can be rendered on Magic Leap 2.
    ///
    /// Milestone 2:
    /// - switch between two configurable virtual distances,
    /// - keep the cube world-fixed relative to the viewer pose captured at startup,
    /// - optionally compensate world-space scale so apparent angular size stays constant.
    ///
    /// All test parameters are intentionally exposed in the Inspector.
    /// </summary>
    public class M1SmokeTestCube : MonoBehaviour
    {
        [Header("Viewer")]
        [Tooltip("Magic Leap headset camera. If empty, Camera.main is used at runtime.")]
        [SerializeField] private Transform viewer;

        [Header("Milestone 2 — test distances")]
        [Tooltip("First test distance in metres. This is a validation value, not a final VAC condition.")]
        [Min(0.05f)]
        [SerializeField] private float distanceAMeters = 0.80f;

        [Tooltip("Second test distance in metres. This is a validation value, not a final VAC condition.")]
        [Min(0.05f)]
        [SerializeField] private float distanceBMeters = 1.20f;

        [Tooltip("Which test distance is used when the app starts.")]
        [SerializeField] private bool startAtDistanceA = true;

        [Header("Placement — editable in Inspector")]
        [Tooltip("Vertical offset from the captured eye position in metres.")]
        [SerializeField] private float verticalOffsetMeters = 0.0f;

        [Tooltip("Additional cube rotation after it is placed facing the captured viewer direction.")]
        [SerializeField] private Vector3 rotationOffsetDegrees = new Vector3(0f, 25f, 0f);

        [Header("Apparent size — editable in Inspector")]
        [Tooltip("If enabled, cube world size changes proportionally with distance so angular size stays constant.")]
        [SerializeField] private bool keepApparentAngularSize = true;

        [Tooltip("Distance at which Reference Cube Size is defined.")]
        [Min(0.05f)]
        [SerializeField] private float referenceDistanceMeters = 1.0f;

        [Tooltip("Cube world size at the reference distance, in metres.")]
        [Min(0.001f)]
        [SerializeField] private float referenceCubeSizeMeters = 0.25f;

        [Header("Automatic device validation")]
        [Tooltip("If enabled, the cube alternates between Distance A and B automatically. Useful before controller input is implemented.")]
        [SerializeField] private bool autoSwitchDistances = true;

        [Tooltip("Seconds to remain at each distance before switching.")]
        [Min(0.25f)]
        [SerializeField] private float autoSwitchIntervalSeconds = 3.0f;

        [Header("Runtime state (read only in play mode)")]
        [SerializeField] private bool currentlyAtDistanceA = true;
        [SerializeField] private float currentDistanceMeters;

        private Vector3 capturedViewerPosition;
        private Vector3 capturedViewerForward;
        private bool viewerPoseCaptured;
        private float autoSwitchTimer;

        public float CurrentDistanceMeters => currentDistanceMeters;

        public void Configure(Transform viewerTransform)
        {
            viewer = viewerTransform;
        }

        private void Start()
        {
            CaptureViewerPose();

            currentlyAtDistanceA = startAtDistanceA;
            ApplyCurrentDistance();

            autoSwitchTimer = 0f;
        }

        private void Update()
        {
            if (!autoSwitchDistances)
                return;

            autoSwitchTimer += Time.unscaledDeltaTime;

            if (autoSwitchTimer >= autoSwitchIntervalSeconds)
            {
                autoSwitchTimer = 0f;
                ToggleDistance();
            }
        }

        [ContextMenu("Capture Viewer Pose")]
        public void CaptureViewerPose()
        {
            Transform activeViewer = viewer;

            if (activeViewer == null && Camera.main != null)
                activeViewer = Camera.main.transform;

            if (activeViewer == null)
            {
                Debug.LogError(
                    "M1SmokeTestCube: No viewer assigned and no Main Camera found.");
                viewerPoseCaptured = false;
                return;
            }

            capturedViewerPosition = activeViewer.position;

            Vector3 flatForward =
                Vector3.ProjectOnPlane(activeViewer.forward, Vector3.up).normalized;

            if (flatForward.sqrMagnitude < 0.001f)
                flatForward = activeViewer.forward.normalized;

            capturedViewerForward = flatForward;
            viewerPoseCaptured = true;

            Debug.Log(
                $"M2 viewer pose captured at {capturedViewerPosition}, " +
                $"forward={capturedViewerForward}");
        }

        [ContextMenu("Apply Distance A")]
        public void ApplyDistanceA()
        {
            currentlyAtDistanceA = true;
            ApplyCurrentDistance();
        }

        [ContextMenu("Apply Distance B")]
        public void ApplyDistanceB()
        {
            currentlyAtDistanceA = false;
            ApplyCurrentDistance();
        }

        [ContextMenu("Toggle Distance")]
        public void ToggleDistance()
        {
            currentlyAtDistanceA = !currentlyAtDistanceA;
            ApplyCurrentDistance();
        }

        private void ApplyCurrentDistance()
        {
            float targetDistance =
                currentlyAtDistanceA ? distanceAMeters : distanceBMeters;

            ApplyDistance(targetDistance);
        }

        private void ApplyDistance(float distanceMeters)
        {
            if (!viewerPoseCaptured)
                CaptureViewerPose();

            if (!viewerPoseCaptured)
                return;

            currentDistanceMeters = Mathf.Max(0.05f, distanceMeters);

            transform.position =
                capturedViewerPosition
                + capturedViewerForward * currentDistanceMeters
                + Vector3.up * verticalOffsetMeters;

            transform.rotation =
                Quaternion.LookRotation(-capturedViewerForward, Vector3.up)
                * Quaternion.Euler(rotationOffsetDegrees);

            float cubeSize = referenceCubeSizeMeters;

            if (keepApparentAngularSize)
            {
                if (referenceDistanceMeters <= 0f)
                {
                    Debug.LogError(
                        "M1SmokeTestCube: Reference distance must be > 0.");
                    return;
                }

                cubeSize *= currentDistanceMeters / referenceDistanceMeters;
            }

            transform.localScale = Vector3.one * cubeSize;

            Debug.Log(
                $"M2 virtual-depth test: " +
                $"{(currentlyAtDistanceA ? "A" : "B")}, " +
                $"distance={currentDistanceMeters:F3} m, " +
                $"cubeSize={cubeSize:F3} m, " +
                $"worldFixed=true");
        }
    }
}
