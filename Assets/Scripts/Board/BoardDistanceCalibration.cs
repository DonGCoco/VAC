using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VACExperiment.Board
{
    public enum BoardDistanceState
    {
        WaitingForMarker,
        TooClose,
        Ready,
        TooFar,
        Locked
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
        public bool IsBoardLocked => boardRegistration != null && boardRegistration.IsPoseLocked;
        public float LockedDistanceMeters { get; private set; }

        private TextMesh conditionText;
        private TextMesh actualValueText;
        private GameObject actualWaitingObject;
        private GameObject actualValueObject;
        private GameObject waitingStateObject;
        private GameObject tooCloseStateObject;
        private GameObject readyStateObject;
        private GameObject tooFarStateObject;
        private GameObject lockedStateObject;
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
            if (boardRegistration != null && boardRegistration.IsPoseLocked)
                boardRegistration.UnlockBoardPose();

            CurrentCondition = level;
            LockedDistanceMeters = 0f;
            RefreshTargetDistance();
            LogConditionSelection();
        }

        public void LockBoardPose()
        {
            if (CurrentState != BoardDistanceState.Ready)
            {
                Debug.LogWarning(
                    "M2 board pose can only be locked while the selected condition is READY.");
                return;
            }

            if (!boardRegistration.HasRecentMarkerObservation(config.markerVisibilityGraceSeconds))
            {
                Debug.LogWarning(
                    "M2 board pose lock rejected because the QR marker is not currently reliable.");
                return;
            }

            if (!boardRegistration.LockCurrentBoardPose())
                return;

            LockedDistanceMeters = ActualDistanceMeters;
            CurrentState = BoardDistanceState.Locked;
            Debug.Log(
                $"M2 board distance locked: condition={CurrentCondition}; " +
                $"actual={LockedDistanceMeters:F3} m.");
        }

        public void RescanBoard()
        {
            boardRegistration.UnlockBoardPose();
            LockedDistanceMeters = 0f;
            CurrentState = BoardDistanceState.WaitingForMarker;
        }

        private void RefreshTargetDistance()
        {
            TargetDistanceMeters = config.GetTargetDistance(CurrentCondition);
        }

        private void RefreshMeasurement()
        {
            if (boardRegistration.IsPoseLocked)
            {
                ActualDistanceMeters = LockedDistanceMeters;
                CurrentState = BoardDistanceState.Locked;
                return;
            }

            if (!boardRegistration.HasRecentMarkerObservation(config.markerVisibilityGraceSeconds))
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
            Transform existingPanel = viewer.Find("M2_CalibrationPanel");
            if (existingPanel != null)
            {
                existingPanel.gameObject.SetActive(false);
                Destroy(existingPanel.gameObject);
            }

            GameObject panel = new("M2_CalibrationPanel");
            panel.transform.SetParent(viewer, false);
            panel.transform.localPosition = new Vector3(
                0f,
                panelVerticalOffsetMeters,
                panelDistanceMeters);
            panel.transform.localRotation = Quaternion.identity;

            conditionText = CreateText(
                panel.transform,
                "ConditionTarget",
                new Vector3(0f, 0.060f, 0f),
                0.0026f,
                40,
                "C1   Target 0.80 m");

            actualWaitingObject = CreateText(
                panel.transform,
                "ActualWaiting",
                new Vector3(0f, 0.030f, 0f),
                0.0026f,
                40,
                "Actual --").gameObject;

            actualValueText = CreateText(
                panel.transform,
                "ActualValue",
                new Vector3(0f, 0.030f, 0f),
                0.0026f,
                40,
                "Actual 0.000 m");
            actualValueObject = actualValueText.gameObject;

            waitingStateObject = CreateText(
                panel.transform,
                "StateWaiting",
                new Vector3(0f, 0.120f, 0f),
                0.0032f,
                42,
                "SHOW QR MARKER").gameObject;

            tooCloseStateObject = CreateText(
                panel.transform,
                "StateTooClose",
                new Vector3(0f, 0.120f, 0f),
                0.0032f,
                42,
                "TOO CLOSE").gameObject;

            readyStateObject = CreateText(
                panel.transform,
                "StateReady",
                new Vector3(0f, 0.120f, 0f),
                0.0032f,
                42,
                "READY").gameObject;

            tooFarStateObject = CreateText(
                panel.transform,
                "StateTooFar",
                new Vector3(0f, 0.120f, 0f),
                0.0032f,
                42,
                "TOO FAR").gameObject;

            lockedStateObject = CreateText(
                panel.transform,
                "StateLocked",
                new Vector3(0f, 0.120f, 0f),
                0.0032f,
                42,
                "LOCKED").gameObject;

            CreateButton(panel.transform, "C1", new Vector3(-0.10f, -0.025f, 0f), SelectC1);
            CreateButton(panel.transform, "C2", new Vector3(0f, -0.025f, 0f), SelectC2);
            CreateButton(panel.transform, "C3", new Vector3(0.10f, -0.025f, 0f), SelectC3);
            CreateButton(panel.transform, "LOCK", new Vector3(-0.055f, -0.070f, 0f), LockBoardPose);
            CreateButton(panel.transform, "RESCAN", new Vector3(0.055f, -0.070f, 0f), RescanBoard);

            RefreshStatusText();
        }

        private static TextMesh CreateText(
            Transform parent,
            string objectName,
            Vector3 localPosition,
            float characterSize,
            int fontSize,
            string initialText)
        {
            GameObject textObject = new(objectName);
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = localPosition;

            TextMesh textMesh = textObject.AddComponent<TextMesh>();
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.characterSize = characterSize;
            textMesh.fontSize = fontSize;
            textMesh.text = initialText;
            return textMesh;
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
            if (conditionText == null ||
                actualWaitingObject == null ||
                actualValueText == null ||
                actualValueObject == null ||
                waitingStateObject == null ||
                tooCloseStateObject == null ||
                readyStateObject == null ||
                tooFarStateObject == null ||
                lockedStateObject == null)
                return;

            conditionText.text =
                $"{CurrentCondition}   Target {TargetDistanceMeters:F2} m";

            bool waiting = CurrentState == BoardDistanceState.WaitingForMarker;
            actualWaitingObject.SetActive(waiting);
            actualValueObject.SetActive(!waiting);

            if (!waiting)
            {
                string prefix = CurrentState == BoardDistanceState.Locked ? "Locked" : "Actual";
                actualValueText.text = $"{prefix} {ActualDistanceMeters:F3} m";
            }

            waitingStateObject.SetActive(CurrentState == BoardDistanceState.WaitingForMarker);
            tooCloseStateObject.SetActive(CurrentState == BoardDistanceState.TooClose);
            readyStateObject.SetActive(CurrentState == BoardDistanceState.Ready);
            tooFarStateObject.SetActive(CurrentState == BoardDistanceState.TooFar);
            lockedStateObject.SetActive(CurrentState == BoardDistanceState.Locked);
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
