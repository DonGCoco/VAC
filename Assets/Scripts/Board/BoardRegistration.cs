using System;
using System.Collections;
using MagicLeap.OpenXR.Features.MarkerUnderstanding;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;

namespace VACExperiment.Board
{
    /// <summary>
    /// Milestone 1 board registration using Magic Leap 2 OpenXR Marker Understanding.
    /// Detects one known QR marker and derives a configurable physical-board pose from it.
    /// </summary>
    public class BoardRegistration : MonoBehaviour
    {
        [Header("XR")]
        [Tooltip("XR Origin from the official Magic Leap ML Rig. If empty, it is found automatically.")]
        [SerializeField] private XROrigin xrOrigin;

        [Tooltip("World-space transform representing the physical board reference pose.")]
        [SerializeField] private Transform boardAnchor;

        [Header("QR marker")]
        [Tooltip("Exact text encoded in the QR marker attached to the physical board.")]
        [SerializeField] private string targetQrText = "VAC_BOARD_01";

        [Tooltip("Measured physical side length of the QR marker in metres, excluding the outer white margin. Used when automatic size estimation is disabled.")]
        [Min(0.01f)]
        [SerializeField] private float markerSizeMeters = 0.12f;

        [Tooltip("Development aid for temporary/screen-displayed QR markers. When enabled, Magic Leap estimates QR size instead of trusting Marker Size Meters. Disable for the formal board once the printed QR size is known exactly.")]
        [SerializeField] private bool estimateQrLength = false;

        [Tooltip("Accuracy is appropriate for board registration; change only if device testing requires it.")]
        [SerializeField] private MarkerDetectorProfile detectorProfile = MarkerDetectorProfile.Accuracy;

        [Header("Marker to board")]
        [Tooltip("Board-reference position relative to the QR marker coordinate frame, in metres.")]
        [SerializeField] private Vector3 markerToBoardPositionMeters = Vector3.zero;

        [Tooltip("Board-reference rotation relative to the QR marker coordinate frame.")]
        [SerializeField] private Vector3 markerToBoardEulerDegrees = Vector3.zero;

        [Header("Visibility")]
        [Tooltip("Hide the BoardAnchor hierarchy until the target QR marker has produced a valid pose.")]
        [SerializeField] private bool hideBoardUntilFirstDetection = true;

        public bool IsMarkerVisible { get; private set; }
        public bool HasRegisteredBoard { get; private set; }
        public bool IsPoseLocked { get; private set; }
        public Transform BoardAnchor => boardAnchor;
        public bool IsSpatialAnchorTracking =>
            spatialAnchor != null &&
            !spatialAnchor.pending &&
            spatialAnchor.trackingState == TrackingState.Tracking;
        public bool HasSpatialAnchorLockFailed { get; private set; }
        public string SpatialAnchorLockError { get; private set; }
        public float SecondsSinceMarkerSeen =>
            HasRegisteredBoard ? Time.unscaledTime - lastMarkerSeenTime : float.PositiveInfinity;

        public bool HasRecentMarkerObservation(float graceSeconds)
        {
            return HasRegisteredBoard &&
                   Time.unscaledTime - lastMarkerSeenTime <= Mathf.Max(0f, graceSeconds);
        }

        private MagicLeapMarkerUnderstandingFeature markerFeature;
        private MarkerDetector markerDetector;
        private bool loggedFirstRegistration;
        private float lastMarkerSeenTime = float.NegativeInfinity;
        private ARAnchorManager anchorManager;
        private ARAnchor spatialAnchor;
        private Coroutine spatialAnchorLockRoutine;
        private const float SpatialAnchorLoadTimeoutSeconds = 5f;
        private const float SpatialAnchorTrackingTimeoutSeconds = 5f;

        public void Configure(XROrigin origin, Transform anchor)
        {
            xrOrigin = origin;
            boardAnchor = anchor;
        }

        private void OnValidate()
        {
            if (xrOrigin == null)
                xrOrigin = FindAnyObjectByType<XROrigin>();
        }

        private void Start()
        {
            if (xrOrigin == null)
                xrOrigin = FindAnyObjectByType<XROrigin>();

            if (xrOrigin == null || boardAnchor == null)
            {
                Debug.LogError("BoardRegistration requires an XR Origin and BoardAnchor.");
                enabled = false;
                return;
            }

            if (string.IsNullOrEmpty(targetQrText))
            {
                Debug.LogError("BoardRegistration target QR text cannot be empty.");
                enabled = false;
                return;
            }

            markerFeature = OpenXRSettings.Instance?.GetFeature<MagicLeapMarkerUnderstandingFeature>();
            if (markerFeature == null || !markerFeature.enabled)
            {
                Debug.LogError(
                    "Magic Leap 2 Marker Understanding OpenXR Feature is missing or disabled.");
                enabled = false;
                return;
            }

            anchorManager = xrOrigin.GetComponent<ARAnchorManager>();
            if (anchorManager == null)
                anchorManager = xrOrigin.gameObject.AddComponent<ARAnchorManager>();

            if (hideBoardUntilFirstDetection)
                boardAnchor.gameObject.SetActive(false);

            MarkerDetectorSettings settings = new();
            settings.MarkerDetectorProfile = detectorProfile;
            settings.MarkerType = MarkerType.QR;
            settings.QRSettings.EstimateQRLength = estimateQrLength;
            if (!estimateQrLength)
                settings.QRSettings.QRLength = markerSizeMeters;

            markerDetector = markerFeature.CreateMarkerDetector(settings);
            if (markerDetector == null)
            {
                Debug.LogError(
                    "Could not create Magic Leap QR marker detector. " +
                    "Verify MARKER_TRACKING permission and OpenXR Marker Understanding settings.");
                enabled = false;
                return;
            }

            string sizeMode = estimateQrLength
                ? "automatic QR size estimation"
                : $"fixed QR size {markerSizeMeters:F3} m";

            Debug.Log(
                $"M1 board registration ready. Waiting for QR '{targetQrText}' " +
                $"using {sizeMode}.");
        }

        private void Update()
        {
            IsMarkerVisible = false;

            if (markerFeature == null || markerDetector == null)
                return;

            markerFeature.UpdateMarkerDetectors();

            if (markerDetector.Status != MarkerDetectorStatus.Ready)
                return;

            for (int i = 0; i < markerDetector.Data.Count; i++)
            {
                MarkerData data = markerDetector.Data[i];

                if (!string.Equals(data.MarkerString, targetQrText, StringComparison.Ordinal))
                    continue;

                if (!data.MarkerPose.HasValue)
                    continue;

                if (!IsPoseLocked)
                    ApplyMarkerPose(data.MarkerPose.Value);

                IsMarkerVisible = true;
                HasRegisteredBoard = true;
                lastMarkerSeenTime = Time.unscaledTime;

                if (!loggedFirstRegistration)
                {
                    loggedFirstRegistration = true;
                    Debug.Log(
                        $"M1 board registered from QR '{targetQrText}'. " +
                        $"BoardAnchor position={boardAnchor.position}, rotation={boardAnchor.rotation.eulerAngles}.");
                }

                return;
            }
        }

        private void ApplyMarkerPose(Pose markerPose)
        {
            Transform originTransform = xrOrigin.CameraFloorOffsetObject != null
                ? xrOrigin.CameraFloorOffsetObject.transform
                : xrOrigin.transform;

            Vector3 markerWorldPosition =
                originTransform.TransformPoint(markerPose.position);

            Quaternion markerWorldRotation =
                originTransform.rotation * markerPose.rotation;

            Vector3 boardWorldPosition =
                markerWorldPosition +
                markerWorldRotation * markerToBoardPositionMeters;

            Quaternion boardWorldRotation =
                markerWorldRotation *
                Quaternion.Euler(markerToBoardEulerDegrees);

            boardAnchor.SetPositionAndRotation(
                boardWorldPosition,
                boardWorldRotation);

            if (!boardAnchor.gameObject.activeSelf)
                boardAnchor.gameObject.SetActive(true);
        }

        public bool LockCurrentBoardPose()
        {
            if (!HasRegisteredBoard)
                return false;

            if (spatialAnchorLockRoutine != null)
                StopCoroutine(spatialAnchorLockRoutine);

            if (spatialAnchor != null)
            {
                Destroy(spatialAnchor);
                spatialAnchor = null;
            }

            HasSpatialAnchorLockFailed = false;
            SpatialAnchorLockError = string.Empty;

            // Freeze QR-driven pose updates immediately so the validated pose
            // cannot move while the official spatial-anchor subsystem becomes ready.
            IsPoseLocked = true;
            spatialAnchorLockRoutine = StartCoroutine(CreateSpatialAnchorWhenReady());

            Debug.Log(
                $"M2 board pose lock requested at position={boardAnchor.position}, " +
                $"rotation={boardAnchor.rotation.eulerAngles}. Waiting for XRAnchorSubsystem.");
            return true;
        }

        private IEnumerator CreateSpatialAnchorWhenReady()
        {
            float subsystemDeadline = Time.unscaledTime + SpatialAnchorLoadTimeoutSeconds;

            while (!IsSpatialAnchorSubsystemLoaded() &&
                   Time.unscaledTime < subsystemDeadline)
            {
                yield return null;
            }

            if (!IsSpatialAnchorSubsystemLoaded())
            {
                FailSpatialAnchorLock(
                    "XRAnchorSubsystem did not load within the timeout. " +
                    "Verify the Magic Leap Spatial Anchor OpenXR feature.");
                yield break;
            }

            if (anchorManager == null)
            {
                FailSpatialAnchorLock("ARAnchorManager is unavailable on the XR Origin.");
                yield break;
            }

            spatialAnchor = boardAnchor.GetComponent<ARAnchor>();
            if (spatialAnchor == null)
                spatialAnchor = boardAnchor.gameObject.AddComponent<ARAnchor>();

            Debug.Log(
                $"M2 spatial anchor component created; pending={spatialAnchor.pending}, " +
                $"trackingState={spatialAnchor.trackingState}.");

            float trackingDeadline = Time.unscaledTime + SpatialAnchorTrackingTimeoutSeconds;

            while (!IsSpatialAnchorTracking &&
                   Time.unscaledTime < trackingDeadline)
            {
                yield return null;
            }

            if (!IsSpatialAnchorTracking)
            {
                FailSpatialAnchorLock(
                    $"Spatial anchor did not reach Tracking. " +
                    $"pending={(spatialAnchor != null && spatialAnchor.pending)}, " +
                    $"trackingState={(spatialAnchor != null ? spatialAnchor.trackingState.ToString() : "missing")}.");
                yield break;
            }

            spatialAnchorLockRoutine = null;
            Debug.Log(
                $"M2 spatial anchor TRACKING at position={boardAnchor.position}, " +
                $"rotation={boardAnchor.rotation.eulerAngles}.");
        }

        private static bool IsSpatialAnchorSubsystemLoaded()
        {
            if (XRGeneralSettings.Instance == null ||
                XRGeneralSettings.Instance.Manager == null ||
                XRGeneralSettings.Instance.Manager.activeLoader == null)
                return false;

            return XRGeneralSettings.Instance.Manager.activeLoader
                .GetLoadedSubsystem<XRAnchorSubsystem>() != null;
        }

        private void FailSpatialAnchorLock(string reason)
        {
            HasSpatialAnchorLockFailed = true;
            SpatialAnchorLockError = reason;
            spatialAnchorLockRoutine = null;

            Debug.LogError($"M2 spatial anchor lock failed: {reason}");
        }

        public void UnlockBoardPose()
        {
            if (spatialAnchorLockRoutine != null)
            {
                StopCoroutine(spatialAnchorLockRoutine);
                spatialAnchorLockRoutine = null;
            }

            IsPoseLocked = false;
            HasSpatialAnchorLockFailed = false;
            SpatialAnchorLockError = string.Empty;

            if (spatialAnchor != null)
            {
                Destroy(spatialAnchor);
                spatialAnchor = null;
            }

            Debug.Log(
                "M2 board pose unlocked; spatial anchor removed and QR tracking can update BoardAnchor again.");
        }

        private void OnDestroy()
        {
            if (spatialAnchorLockRoutine != null)
            {
                StopCoroutine(spatialAnchorLockRoutine);
                spatialAnchorLockRoutine = null;
            }

            if (spatialAnchor != null)
            {
                Destroy(spatialAnchor);
                spatialAnchor = null;
            }

            if (markerFeature != null && markerDetector != null)
                markerFeature.DestroyMarkerDetector(markerDetector);
        }
    }
}
