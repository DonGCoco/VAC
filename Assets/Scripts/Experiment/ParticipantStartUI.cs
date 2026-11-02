using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using VACExperiment.Board;
using VACExperiment.Tetris;

namespace VACExperiment
{
    /// <summary>
    /// Participant-side UI for the handoff after experimenter calibration.
    /// Shows game-centre distance during calibration and the START handoff only.
    /// START appears only after the spatially anchored board is truly LOCKED.
    /// </summary>
    public class ParticipantStartUI : MonoBehaviour
    {
        [SerializeField] private BoardDistanceCalibration calibration;
        [SerializeField] private BoardRegistration boardRegistration;
        [SerializeField] private TetrisManager tetrisManager;
        [SerializeField] private FormalExperimentFlowController formalFlow;
        [SerializeField] private Transform viewer;

        [Header("Placement")]
        [SerializeField, Min(0.3f)] private float panelDistanceMeters = 0.70f;
        [SerializeField] private float panelVerticalOffsetMeters = -0.05f;

        private GameObject startPanel;
        private TextMesh distanceText;
        private bool startConsumedForCurrentLock;

        private void Start()
        {
            if (calibration == null)
                calibration = FindAnyObjectByType<BoardDistanceCalibration>();

            if (boardRegistration == null)
                boardRegistration = FindAnyObjectByType<BoardRegistration>();

            if (tetrisManager == null)
                tetrisManager = FindAnyObjectByType<TetrisManager>();

            if (formalFlow == null)
                formalFlow = FindAnyObjectByType<FormalExperimentFlowController>();

            if (viewer == null && Camera.main != null)
                viewer = Camera.main.transform;

            if (calibration == null || boardRegistration == null ||
                tetrisManager == null || viewer == null)
            {
                Debug.LogError(
                    "ParticipantStartUI requires calibration, board registration, TetrisManager and Main Camera.");
                enabled = false;
                return;
            }

            CreatePanel();
            startPanel.SetActive(false);
        }

        private void LateUpdate()
        {
            if (formalFlow == null)
                formalFlow = FindAnyObjectByType<FormalExperimentFlowController>();

            bool locked =
                calibration.CurrentState == BoardDistanceState.Locked &&
                boardRegistration.IsSpatialAnchorTracking;

            if (!locked)
                startConsumedForCurrentLock = false;

            bool flowAllowsStart =
                formalFlow == null || formalFlow.ParticipantStartAllowed;

            bool visible =
                locked &&
                flowAllowsStart &&
                !tetrisManager.IsRunning &&
                !startConsumedForCurrentLock;

            if (startPanel != null && startPanel.activeSelf != visible)
                startPanel.SetActive(visible);

            // Keep this extra near-field text out of Tetris, depth tasks and questionnaires.
            bool showDistance = !tetrisManager.IsRunning &&
                (formalFlow == null || formalFlow.ParticipantStartAllowed);
            if (distanceText != null)
            {
                distanceText.gameObject.SetActive(showDistance);
                distanceText.text = calibration.HasValidViewingDistance
                    ? $"Game distance {calibration.ActualDistanceMeters:F3} m"
                    : "Game distance --";
            }
        }

        private void CreatePanel()
        {
            Transform existing = viewer.Find("ParticipantStartPanel");
            if (existing != null)
                Destroy(existing.gameObject);

            startPanel = new GameObject("ParticipantStartPanel");
            startPanel.transform.SetParent(viewer, false);
            startPanel.transform.localPosition =
                new Vector3(0f, panelVerticalOffsetMeters, panelDistanceMeters);
            startPanel.transform.localRotation = Quaternion.identity;

            Transform oldDistance = viewer.Find("ParticipantGameDistance");
            if (oldDistance != null)
                Destroy(oldDistance.gameObject);

            GameObject distanceObject = new("ParticipantGameDistance");
            distanceObject.transform.SetParent(viewer, false);
            distanceObject.transform.localPosition =
                new Vector3(0f, panelVerticalOffsetMeters - 0.065f, panelDistanceMeters);
            distanceObject.transform.localRotation = Quaternion.identity;
            distanceText = distanceObject.AddComponent<TextMesh>();
            distanceText.anchor = TextAnchor.MiddleCenter;
            distanceText.alignment = TextAlignment.Center;
            distanceText.characterSize = 0.0023f;
            distanceText.fontSize = 36;
            distanceText.text = "Game distance --";
            distanceObject.SetActive(false);

            GameObject hintObject = new("ReadyHint");
            hintObject.transform.SetParent(startPanel.transform, false);
            hintObject.transform.localPosition = new Vector3(0f, 0.035f, 0f);

            TextMesh hint = hintObject.AddComponent<TextMesh>();
            hint.anchor = TextAnchor.MiddleCenter;
            hint.alignment = TextAlignment.Center;
            hint.characterSize = 0.0023f;
            hint.fontSize = 36;
            hint.text = "READY";

            GameObject button = GameObject.CreatePrimitive(PrimitiveType.Cube);
            button.name = "Participant_START_Button";
            button.transform.SetParent(startPanel.transform, false);
            button.transform.localPosition = Vector3.zero;
            button.transform.localScale = new Vector3(0.10f, 0.040f, 0.008f);

            MeshRenderer renderer = button.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.enabled = false;

            XRSimpleInteractable interactable = button.AddComponent<XRSimpleInteractable>();
            interactable.selectEntered.AddListener(_ => StartExperiment());

            GameObject labelObject = new("START_Label");
            labelObject.transform.SetParent(startPanel.transform, false);
            labelObject.transform.localPosition = Vector3.zero;

            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.0040f;
            label.fontSize = 52;
            label.text = "START";
        }

        private void StartExperiment()
        {
            if (startConsumedForCurrentLock)
                return;

            if (calibration.CurrentState != BoardDistanceState.Locked ||
                !boardRegistration.IsSpatialAnchorTracking)
                return;

            startConsumedForCurrentLock = true;
            startPanel.SetActive(false);

            if (formalFlow != null && formalFlow.HasActiveParticipant)
                formalFlow.HandleParticipantStart();
            else
                calibration.StartTetrisTest();
        }
    }
}
