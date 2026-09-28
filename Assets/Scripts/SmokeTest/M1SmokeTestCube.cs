using System.Collections;
using UnityEngine;

namespace VACExperiment.SmokeTest
{
    /// <summary>
    /// Milestone 2 real-device validation helper.
    ///
    /// This test uses a flat panel rather than the authored cube shape when
    /// "Use Flat Panel For Validation" is enabled. The panel's world size is
    /// calculated from a requested visual angle:
    ///
    ///     size = 2 * distance * tan(angle / 2)
    ///
    /// This is the exact geometric relationship for a planar target centered
    /// on the view direction. The target is placed once in a world-locked
    /// reference frame and alternates between two configurable depths.
    ///
    /// All values that may be tuned during validation are exposed in Inspector.
    /// </summary>
    public class M1SmokeTestCube : MonoBehaviour
    {
        [Header("Viewer")]
        [Tooltip("Magic Leap headset camera. If empty, Camera.main is used at runtime.")]
        [SerializeField] private Transform viewer;

        [Header("Milestone 2 — test distances")]
        [Tooltip("First validation distance in metres. Not a final VAC condition.")]
        [Min(0.05f)]
        [SerializeField] private float distanceAMeters = 0.80f;

        [Tooltip("Second validation distance in metres. Not a final VAC condition.")]
        [Min(0.05f)]
        [SerializeField] private float distanceBMeters = 1.20f;

        [Tooltip("Which validation distance is used first.")]
        [SerializeField] private bool startAtDistanceA = true;

        [Header("Constant visual angle")]
        [Tooltip("Use a thin rectangular panel for the visual-angle check. Recommended for Milestone 2 because the final Tetris stimulus is flat.")]
        [SerializeField] private bool useFlatPanelForValidation = true;

        [Tooltip("Target vertical visual angle in degrees. The program computes world-space height from this value.")]
        [Range(1f, 40f)]
        [SerializeField] private float targetVerticalVisualAngleDegrees = 14f;

        [Tooltip("Target width / height. 1 = square. Tetris can later use its actual board aspect ratio.")]
        [Min(0.05f)]
        [SerializeField] private float targetAspectRatio = 1f;

        [Tooltip("Thickness of the validation panel in metres.")]
        [Min(0.0005f)]
        [SerializeField] private float panelThicknessMeters = 0.005f;

        [Header("Placement — editable in Inspector")]
        [Tooltip("Vertical offset from the captured eye position in metres.")]
        [SerializeField] private float verticalOffsetMeters = 0f;

        [Tooltip("Additional rotation after facing the captured viewer direction. Keep at zero for the visual-angle validation.")]
        [SerializeField] private Vector3 rotationOffsetDegrees = Vector3.zero;

        [Header("XR pose capture")]
        [Tooltip("Wait briefly for OpenXR head tracking to settle before capturing the initial viewer pose.")]
        [Min(0f)]
        [SerializeField] private float poseCaptureDelaySeconds = 0.75f;

        [Tooltip("Hide the target until the initial tracked viewer pose has been captured.")]
        [SerializeField] private bool hideUntilPoseCaptured = true;

        [Header("Automatic device validation")]
        [Tooltip("Automatically alternate between Distance A and B. This lets Milestone 2 be tested before controller input is implemented.")]
        [SerializeField] private bool autoSwitchDistances = true;

        [Tooltip("Seconds to remain at each distance before switching.")]
        [Min(0.25f)]
        [SerializeField] private float autoSwitchIntervalSeconds = 3f;

        [Header("Runtime state (read only in play mode)")]
        [SerializeField] private bool currentlyAtDistanceA = true;
        [SerializeField] private float currentDistanceMeters;
        [SerializeField] private float currentWorldHeightMeters;
        [SerializeField] private float currentWorldWidthMeters;

        private Vector3 capturedViewerPosition;
        private Vector3 capturedViewerForward;
        private bool viewerPoseCaptured;
        private float autoSwitchTimer;
        private Renderer targetRenderer;

        public float CurrentDistanceMeters => currentDistanceMeters;

        public void Configure(Transform viewerTransform)
        {
            viewer = viewerTransform;
        }

        private IEnumerator Start()
        {
            targetRenderer = GetComponent<Renderer>();

            if (hideUntilPoseCaptured && targetRenderer != null)
                targetRenderer.enabled = false;

            if (poseCaptureDelaySeconds > 0f)
                yield return new WaitForSecondsRealtime(poseCaptureDelaySeconds);

            CaptureViewerPose();

            currentlyAtDistanceA = startAtDistanceA;
            ApplyCurrentDistance();

            if (targetRenderer != null)
                targetRenderer.enabled = true;

            autoSwitchTimer = 0f;
        }

        private void Update()
        {
            if (!viewerPoseCaptured || !autoSwitchDistances)
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
                    "M2VisualAngleTest: No viewer assigned and no Main Camera found.");
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
                return;

            currentDistanceMeters = Mathf.Max(0.05f, distanceMeters);

            transform.position =
                capturedViewerPosition
                + capturedViewerForward * currentDistanceMeters
                + Vector3.up * verticalOffsetMeters;

            transform.rotation =
                Quaternion.LookRotation(-capturedViewerForward, Vector3.up)
                * Quaternion.Euler(rotationOffsetDegrees);

            float halfAngleRadians =
                0.5f * targetVerticalVisualAngleDegrees * Mathf.Deg2Rad;

            currentWorldHeightMeters =
                2f * currentDistanceMeters * Mathf.Tan(halfAngleRadians);

            currentWorldWidthMeters =
                currentWorldHeightMeters * targetAspectRatio;

            if (useFlatPanelForValidation)
            {
                transform.localScale = new Vector3(
                    currentWorldWidthMeters,
                    currentWorldHeightMeters,
                    panelThicknessMeters);
            }
            else
            {
                // Uniform-scale fallback. The exact visual-angle validation
                // should use the flat-panel mode above.
                transform.localScale =
                    Vector3.one * currentWorldHeightMeters;
            }

            Debug.Log(
                $"M2 constant-visual-angle test: " +
                $"{(currentlyAtDistanceA ? "A" : "B")}, " +
                $"distance={currentDistanceMeters:F3} m, " +
                $"visualAngle={targetVerticalVisualAngleDegrees:F2} deg, " +
                $"worldHeight={currentWorldHeightMeters:F3} m, " +
                $"worldWidth={currentWorldWidthMeters:F3} m, " +
                $"worldFixed=true");
        }
    }
}
