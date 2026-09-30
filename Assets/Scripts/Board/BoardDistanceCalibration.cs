using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VACExperiment.Board
{
    public enum BoardDistanceState
    {
        WaitingForMarker,
        TooClose,
        Ready,
        TooFar
    }

    /// <summary>
    /// Milestone 2 physical-board distance calibration.
    /// Measures the cyclopean-view approximation (Main Camera) to BoardAnchor distance,
    /// compares it with the selected C1/C2/C3 target, and exposes a minimal XRI-selectable
    /// in-headset calibration panel.
    /// </summary>
    public class BoardDistanceCalibration : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BoardRegistration boardRegistration;
        [SerializeField] private Transform boardAnchor;
        [SerializeField] private Transform viewer;
        [SerializeField] private ExperimentConfig config;

        [Header("Calibration")]
        [SerializeField] private VacLevel initialCondition = VacLevel.C1;

        [Header("Runtime panel")]
        [SerializeField] private bool createRuntimePanel = true;
        [Min(0.3f)]
        [SerializeField] private float panelDistanceMeters = 0.70f;
        [SerializeField] private float panelVerticalOffsetMeters = -0.18f;

        public VacLevel CurrentCondition { get; private set; }
        public BoardDistanceState CurrentState { get; private set; } = BoardDistanceState.WaitingForMarker;
        public float TargetDistanceMeters { get; private set; }
        public float ActualDistanceMeters { get; private set; }
        public bool IsReady => CurrentState == BoardDistanceState.Ready;

        private TextMesh statusText;
        private TextMesh stateText;
        private BoardDistanceState lastLoggedState = (BoardDistanceState)(-1);
        private VacLevel lastLoggedCondition = (VacLevel)(-1);

        private void Start()
        {
            if (boardRegistration == null)
                boardRegistration = FindAnyObjectByType<BoardRegistration>();

            if (boardAnchor == null && boardRegistration != null)
                boardAnchor = boardRegistration.BoardAnchor;

            if (viewer == null && Camera.main != null)
                viewer = Camera.main.transform;

            if (config == null)
            {
                Debug.LogError("BoardDistanceCalibration requires an ExperimentConfig asset.");
                enabled = false;
                return;
            }

            if (boardRegistration == null || boardAnchor == null || viewer == null)
            {
                Debug.LogError(
                    "BoardDistanceCalibration requires BoardRegistration, BoardAnchor and the ML Rig Main Camera.");
                enabled = false;
                return;
            }

            CurrentCondition = initialCondition;
            RefreshTargetDistance();

            if (createRuntimePanel)
                CreateRuntimePanel();

            LogConditionSelection();
        }

        private void Update()
        {
            RefreshMeasurement();
            RefreshStatusText();
            LogStateTransition();
        }

        public void SelectC1() => SelectCondition(VacLevel.C1);
        public void SelectC2() => SelectCondition(VacLevel.C2);
        public void SelectC3() => SelectCondition(VacLevel.C3);

        public void SelectCondition(VacLevel level)
        {
            CurrentCondition = level;
            RefreshTargetDistance();
            LogConditionSelection();
        }

        private void RefreshTargetDistance()
        {
            TargetDistanceMeters = config.GetTargetDistance(CurrentCondition);
        }

        private void RefreshMeasurement()
        {
            if (!boardRegistration.IsMarkerVisible)
            {
                CurrentState = BoardDistanceState.WaitingForMarker;
                ActualDistanceMeters = 0f;
                return;
            }

            ActualDistanceMeters = Vector3.Distance(viewer.position, boardAnchor.position);
            float delta = ActualDistanceMeters - TargetDistanceMeters;
            float tolerance = config.boardDistanceToleranceMeters;

            if (Mathf.Abs(delta) <= tolerance)
                CurrentState = BoardDistanceState.Ready;
            else if (delta < 0f)
                CurrentState = BoardDistanceState.TooClose;
            else
                CurrentState = BoardDistanceState.TooFar;
        }

        private void CreateRuntimePanel()
        {
            GameObject panel = new("M2_CalibrationPanel");
            panel.transform.SetParent(viewer, false);
            panel.transform.localPosition = new Vector3(
                0f,
                panelVerticalOffsetMeters,
                panelDistanceMeters);
            panel.transform.localRotation = Quaternion.identity;

            GameObject statusObject = new("StatusInfo");
            statusObject.transform.SetParent(panel.transform, false);
            statusObject.transform.localPosition = new Vector3(0f, 0.045f, 0f);

            statusText = statusObject.AddComponent<TextMesh>();
            statusText.anchor = TextAnchor.MiddleCenter;
            statusText.alignment = TextAlignment.Center;
            statusText.characterSize = 0.0026f;
            statusText.fontSize = 40;
            statusText.lineSpacing = 1.15f;
            statusText.text = "Waiting for marker";

            GameObject stateObject = new("DistanceState");
            stateObject.transform.SetParent(panel.transform, false);
            stateObject.transform.localPosition = new Vector3(0f, 0.125f, 0f);

            stateText = stateObject.AddComponent<TextMesh>();
            stateText.anchor = TextAnchor.MiddleCenter;
            stateText.alignment = TextAlignment.Center;
            stateText.characterSize = 0.0032f;
            stateText.fontSize = 42;
            stateText.text = "SHOW QR MARKER";

            CreateButton(panel.transform, "C1", new Vector3(-0.10f, -0.025f, 0f), SelectC1);
            CreateButton(panel.transform, "C2", new Vector3(0f, -0.025f, 0f), SelectC2);
            CreateButton(panel.transform, "C3", new Vector3(0.10f, -0.025f, 0f), SelectC3);
        }

        private static void CreateButton(
            Transform parent,
            string label,
            Vector3 localPosition,
            UnityEngine.Events.UnityAction callback)
        {
            GameObject button = GameObject.CreatePrimitive(PrimitiveType.Cube);
            button.name = $"M2_{label}_Button";
            button.transform.SetParent(parent, false);
            button.transform.localPosition = localPosition;
            button.transform.localScale = new Vector3(0.055f, 0.026f, 0.008f);

            MeshRenderer renderer = button.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.enabled = false;

            XRSimpleInteractable interactable = button.AddComponent<XRSimpleInteractable>();
            interactable.selectEntered.AddListener(_ => callback());

            GameObject labelObject = new($"{label}_Label");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = localPosition;

            TextMesh labelText = labelObject.AddComponent<TextMesh>();
            labelText.anchor = TextAnchor.MiddleCenter;
            labelText.alignment = TextAlignment.Center;
            labelText.characterSize = 0.0032f;
            labelText.fontSize = 42;
            labelText.text = label;
        }

        private void RefreshStatusText()
        {
            if (statusText == null || stateText == null)
                return;

            string stateLabel = CurrentState switch
            {
                BoardDistanceState.WaitingForMarker => "SHOW QR MARKER",
                BoardDistanceState.TooClose => "TOO CLOSE",
                BoardDistanceState.Ready => "READY",
                BoardDistanceState.TooFar => "TOO FAR",
                _ => "UNKNOWN"
            };

            string actualText = CurrentState == BoardDistanceState.WaitingForMarker
                ? "--"
                : $"{ActualDistanceMeters:F3} m";

            statusText.text =
                $"{CurrentCondition}   Target {TargetDistanceMeters:F2} m\n" +
                $"Actual {actualText}";

            stateText.text = stateLabel;
        }

        private void LogConditionSelection()
        {
            if (lastLoggedCondition == CurrentCondition)
                return;

            lastLoggedCondition = CurrentCondition;
            Debug.Log(
                $"M2 condition selected: {CurrentCondition}, " +
                $"target={TargetDistanceMeters:F3} m, " +
                $"tolerance=±{config.boardDistanceToleranceMeters:F3} m.");
        }

        private void LogStateTransition()
        {
            if (lastLoggedState == CurrentState)
                return;

            lastLoggedState = CurrentState;

            if (CurrentState == BoardDistanceState.WaitingForMarker)
            {
                Debug.Log("M2 board distance: waiting for target QR marker.");
                return;
            }

            Debug.Log(
                $"M2 board distance: {CurrentState}; " +
                $"condition={CurrentCondition}; " +
                $"target={TargetDistanceMeters:F3} m; " +
                $"actual={ActualDistanceMeters:F3} m.");
        }
    }
}
