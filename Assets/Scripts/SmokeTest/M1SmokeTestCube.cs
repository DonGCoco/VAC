using UnityEngine;

namespace VACExperiment.SmokeTest
{
    /// <summary>
    /// Milestone 1/2 helper for verifying real-device placement.
    /// All values that we may tune during early testing are visible in the Inspector.
    /// The cube is positioned once and then remains world-fixed.
    /// </summary>
    public class M1SmokeTestCube : MonoBehaviour
    {
        [Header("Viewer")]
        [Tooltip("Magic Leap headset camera. If empty, Camera.main is used at runtime.")]
        [SerializeField] private Transform viewer;

        [Header("Placement — editable in Inspector")]
        [Tooltip("Virtual distance from the viewer in metres.")]
        [Min(0.05f)]
        [SerializeField] private float distanceMeters = 1.0f;

        [Tooltip("Vertical offset from eye level in metres.")]
        [SerializeField] private float verticalOffsetMeters = 0.0f;

        [Tooltip("Additional cube rotation after it is placed facing the viewer.")]
        [SerializeField] private Vector3 rotationOffsetDegrees = new Vector3(0f, 25f, 0f);

        [Header("Apparent size — editable in Inspector")]
        [Tooltip("If enabled, world-space cube size changes proportionally with viewing distance.")]
        [SerializeField] private bool keepApparentAngularSize = true;

        [Tooltip("Distance at which Reference Cube Size is defined.")]
        [Min(0.05f)]
        [SerializeField] private float referenceDistanceMeters = 1.0f;

        [Tooltip("Cube world size at the reference distance, in metres.")]
        [Min(0.001f)]
        [SerializeField] private float referenceCubeSizeMeters = 0.25f;

        [Header("Runtime")]
        [Tooltip("Place the cube once when the app starts. It will not follow the head afterward.")]
        [SerializeField] private bool placeOnceOnStart = true;

        public float DistanceMeters
        {
            get => distanceMeters;
            set => distanceMeters = Mathf.Max(0.05f, value);
        }

        public void Configure(Transform viewerTransform)
        {
            viewer = viewerTransform;
        }

        private void Start()
        {
            if (placeOnceOnStart)
                ApplyPlacement();
        }

        [ContextMenu("Apply Placement Now")]
        public void ApplyPlacement()
        {
            Transform activeViewer = viewer;

            if (activeViewer == null && Camera.main != null)
                activeViewer = Camera.main.transform;

            if (activeViewer == null)
            {
                Debug.LogError(
                    "M1SmokeTestCube: No viewer assigned and no Main Camera found.");
                return;
            }

            Vector3 forward = Vector3.ProjectOnPlane(activeViewer.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.001f)
                forward = activeViewer.forward.normalized;

            transform.position =
                activeViewer.position
                + forward * distanceMeters
                + Vector3.up * verticalOffsetMeters;

            // Face the viewer, then apply an optional rotation offset so the cube has visible depth.
            transform.rotation =
                Quaternion.LookRotation(-forward, Vector3.up)
                * Quaternion.Euler(rotationOffsetDegrees);

            float cubeSize = referenceCubeSizeMeters;

            if (keepApparentAngularSize)
            {
                if (referenceDistanceMeters <= 0f)
                {
                    Debug.LogError("M1SmokeTestCube: Reference distance must be > 0.");
                    return;
                }

                cubeSize *= distanceMeters / referenceDistanceMeters;
            }

            transform.localScale = Vector3.one * cubeSize;

            Debug.Log(
                $"M1 cube placed: distance={distanceMeters:F3} m, " +
                $"size={cubeSize:F3} m, world-fixed=true");
        }
    }
}
