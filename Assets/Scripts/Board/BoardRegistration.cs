using System;
using MagicLeap.OpenXR.Features.MarkerUnderstanding;
using Unity.XR.CoreUtils;
using UnityEngine;
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

        [Tooltip("Measured physical side length of the QR marker in metres.")]
        [Min(0.01f)]
        [SerializeField] private float markerSizeMeters = 0.12f;

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
        public Transform BoardAnchor => boardAnchor;

        private MagicLeapMarkerUnderstandingFeature markerFeature;
        private MarkerDetector markerDetector;
        private bool loggedFirstRegistration;

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

            if (hideBoardUntilFirstDetection)
                boardAnchor.gameObject.SetActive(false);

            MarkerDetectorSettings settings = new();
            settings.MarkerDetectorProfile = detectorProfile;
            settings.MarkerType = MarkerType.QR;
            settings.QRSettings.EstimateQRLength = false;
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

            Debug.Log(
                $"M1 board registration ready. Waiting for QR '{targetQrText}' " +
                $"({markerSizeMeters:F3} m).");
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

                ApplyMarkerPose(data.MarkerPose.Value);
                IsMarkerVisible = true;
                HasRegisteredBoard = true;

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

        private void OnDestroy()
        {
            if (markerFeature != null && markerDetector != null)
                markerFeature.DestroyMarkerDetector(markerDetector);
        }
    }
}
