using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using VACExperiment.Tetris;

namespace VACExperiment
{
    public enum DepthPhase
    {
        Practice,
        Post
    }

    public enum ResponseSide
    {
        Left,
        Right
    }

    /// <summary>
    /// Fixed-depth judgement task.
    ///
    /// This task is deliberately independent from the physical BoardAnchor.
    /// Each trial is placed in world space from the participant's viewer pose at
    /// trial onset, then remains fixed until the response.
    /// </summary>
    public class DepthJudgmentManager : MonoBehaviour
    {
        [SerializeField] private ExperimentConfig config;
        [SerializeField] private Transform viewer;
        [SerializeField] private Transform leftTarget;
        [SerializeField] private Transform rightTarget;
        [SerializeField] private DataLogger dataLogger;

        [Header("Interaction")]
        [Tooltip("World-fixed response-plane outer horizontal extent; inner edges derive from sphere silhouettes each trial.")]
        [SerializeField, Min(0.01f)] private float responseOuterExtentMeters = 0.38f;
        [SerializeField, Min(0.01f)] private float responseRegionHeightMeters = 0.40f;

        [Header("M5 transition")]
        [SerializeField] private TetrisManager tetrisManager;
        [SerializeField] private ConditionController conditionController;
        [SerializeField] private bool autoStartAfterTetrisForMilestone5 = true;
        [SerializeField] private bool createStandaloneTestLog = true;

        [Header("Events")]
        public UnityEvent onBlockCompleted;

        public bool IsRunning { get; private set; }
        public VacLevel CurrentCondition { get; private set; }
        public int CurrentTrialNumber => IsRunning ? trialIndex + 1 : 0;
        public int TrialCount => fartherSides?.Count ?? 0;

        private DepthPhase phase;
        private float referenceDepth;
        private List<ResponseSide> fartherSides;
        private int trialIndex;
        private float stimulusOnsetTime;

        private Vector3 leftReferenceScale;
        private Vector3 rightReferenceScale;
        private Renderer[] tetrisRenderers;
        private GameObject promptObject;
        private TextMesh promptText;
        private Vector3 responseOrigin, responseRight, responseUp, responseForward;
        private Vector3 trialEyePosition, leftSphereCenter, rightSphereCenter;
        private float leftSphereRadius, rightSphereRadius;
        private Vector3[] leftVisibleVertices, rightVisibleVertices;
        private float leftInnerEdge, rightInnerEdge;
        // A one-micrometre outward guard never expands selection into the gap.
        private const float ResponseEdgeGuardMeters = 0.000001f;
        private bool responseGeometryValid;
        public Vector2 ResponseInnerEdges => new(leftInnerEdge, rightInnerEdge);
        private int lastResponseFrame = -1;

        private void Awake()
        {
            ResolveReferences();
            EnsureTargets();

            if (leftTarget != null)
                leftReferenceScale = leftTarget.localScale;

            if (rightTarget != null)
                rightReferenceScale = rightTarget.localScale;

            ConfigureTargetInteraction(leftTarget, ResponseSide.Left);
            ConfigureTargetInteraction(rightTarget, ResponseSide.Right);
            CreatePrompt();

            SetTargetsVisible(false);
            SetPromptVisible(false);
        }

        private void Start()
        {
            var responseInput = GetComponent<MagicLeapDepthInput>();
            if (responseInput == null)
                responseInput = gameObject.AddComponent<MagicLeapDepthInput>();
            responseInput.enabled = true;
            if (autoStartAfterTetrisForMilestone5 && tetrisManager != null)
                tetrisManager.onSessionCompleted.AddListener(HandleTetrisCompleted);
        }

        private void OnDestroy()
        {
            if (tetrisManager != null)
                tetrisManager.onSessionCompleted.RemoveListener(HandleTetrisCompleted);
        }

        private void ResolveReferences()
        {
            if (viewer == null && Camera.main != null)
                viewer = Camera.main.transform;

            if (tetrisManager == null)
                tetrisManager = FindAnyObjectByType<TetrisManager>();

            if (conditionController == null)
                conditionController = FindAnyObjectByType<ConditionController>();

            if (dataLogger == null)
                dataLogger = GetComponent<DataLogger>();
        }

        private void EnsureTargets()
        {
            if (config == null)
                return;

            if (leftTarget == null)
                leftTarget = CreateDefaultTarget("DepthTarget_Left");

            if (rightTarget == null)
                rightTarget = CreateDefaultTarget("DepthTarget_Right");
        }

        private Transform CreateDefaultTarget(string objectName)
        {
            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            target.name = objectName;
            target.transform.SetParent(transform, false);
            target.transform.localScale = Vector3.one * config.depthTargetDiameterMeters;
            return target.transform;
        }

        private void ConfigureTargetInteraction(Transform target, ResponseSide side)
        {
            if (target == null)
                return;

            // Visual stimuli never receive input. Selection uses two independent,
            // invisible rectangles on a shared plane, with a central dead zone.
            foreach (var interactable in target.GetComponentsInChildren<XRSimpleInteractable>(true))
                interactable.enabled = false;
            foreach (var collider in target.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
        }

        public bool TryGetResponseSide(Ray ray, out ResponseSide side)
        {
            side = default;
            if (!IsRunning || !isActiveAndEnabled || !responseGeometryValid)
                return false;
            float denominator = Vector3.Dot(ray.direction, responseForward);
            // Accept only a forward ray into the trial's response plane.
            if (denominator <= 0.0001f)
                return false;
            float distance = Vector3.Dot(responseOrigin - ray.origin, responseForward) / denominator;
            if (distance <= 0f)
                return false;
            Vector3 offset = ray.GetPoint(distance) - responseOrigin;
            float x = Vector3.Dot(offset, responseRight);
            float y = Vector3.Dot(offset, responseUp);
            float halfHeight = Mathf.Max(0.01f, responseRegionHeightMeters) * 0.5f;
            float outerEdge = Mathf.Max(0.01f, responseOuterExtentMeters);
            float absX = Mathf.Abs(x), absY = Mathf.Abs(y);
            if (float.IsNaN(x) || float.IsNaN(y) || float.IsInfinity(x) || float.IsInfinity(y) ||
                (absY > halfHeight && !Mathf.Approximately(absY, halfHeight)) ||
                (absX > outerEdge && !Mathf.Approximately(absX, outerEdge)))
                return false;
            // The nominal rectangles are fixed for the trial. A controller can
            // originate far from the eye: its ray can cross this plane outside
            // the nominal gap yet still pass between the visible spheres.
            // Veto that parallax case using the actual ray origin and current eye.
            if (!TryProjectedGap(ray.origin, out float rayLeft, out float rayRight) ||
                viewer == null || !TryProjectedGap(viewer.position, out float eyeLeft, out float eyeRight))
                return false;
            float safeLeft = Mathf.Min(leftInnerEdge, Mathf.Min(rayLeft, eyeLeft));
            float safeRight = Mathf.Max(rightInnerEdge, Mathf.Max(rayRight, eyeRight));
            if (x <= safeLeft - ResponseEdgeGuardMeters) side = ResponseSide.Left;
            else if (x >= safeRight + ResponseEdgeGuardMeters) side = ResponseSide.Right;
            else return false;
            return true;
        }

        private bool TryProjectedGap(Vector3 observer, out float left, out float right)
        {
            left = right = 0f;
            float planeDepth = Vector3.Dot(responseOrigin - observer, responseForward);
            float observerX = Vector3.Dot(observer - responseOrigin, responseRight);
            if (!ProjectSphereInner(observer, leftSphereCenter, leftSphereRadius, leftVisibleVertices, planeDepth, true, out float l) ||
                !ProjectSphereInner(observer, rightSphereCenter, rightSphereRadius, rightVisibleVertices, planeDepth, false, out float r))
                return false;
            left = observerX + l;
            right = observerX + r;
            return left < right;
        }

        private bool ProjectSphereInner(Vector3 observer, Vector3 center, float radius, Vector3[] visibleVertices,
            float planeDepth, bool leftSphere, out float projected)
        {
            projected = 0f;
            Vector3 v = center - observer;
            double x = Vector3.Dot(v, responseRight), z = Vector3.Dot(v, responseForward), r = radius;
            if (planeDepth <= 0f || r <= 0 || z <= r) return false;
            // Exact tangent slopes of the sphere's horizontal perspective silhouette.
            // Account separately for each world radius and depth; no fixed diameter/gap.
            double tangent = r * Math.Sqrt(x * x + z * z - r * r);
            double slope = (x * z + (leftSphere ? tangent : -tangent)) / (z * z - r * r);
            // Unity's rendered sphere is a tessellated mesh. Its inward outline
            // can sit slightly outside the ideal smooth-sphere tangent; never
            // permit clicks in that small extra visible gap. Projection extrema
            // of a triangle with positive depths occur at its vertices.
            if (visibleVertices == null || visibleVertices.Length == 0) return false;
            double meshSlope = leftSphere ? double.NegativeInfinity : double.PositiveInfinity;
            foreach (Vector3 vertex in visibleVertices)
            {
                Vector3 relative = vertex - observer;
                double depth = Vector3.Dot(relative, responseForward);
                if (depth <= 0) return false;
                double value = Vector3.Dot(relative, responseRight) / depth;
                meshSlope = leftSphere ? Math.Max(meshSlope, value) : Math.Min(meshSlope, value);
            }
            slope = leftSphere ? Math.Min(slope, meshSlope) : Math.Max(slope, meshSlope);
            projected = (float)(planeDepth * slope);
            return !float.IsNaN(projected) && !float.IsInfinity(projected);
        }

        private void CaptureResponseGeometry()
        {
            trialEyePosition = viewer.position;
            leftSphereCenter = leftTarget.position;
            rightSphereCenter = rightTarget.position;
            leftVisibleVertices = CaptureVisibleVertices(leftTarget);
            rightVisibleVertices = CaptureVisibleVertices(rightTarget);
            Vector3 ls = leftTarget.lossyScale, rs = rightTarget.lossyScale;
            leftSphereRadius = Mathf.Max(Mathf.Abs(ls.x), Mathf.Max(Mathf.Abs(ls.y), Mathf.Abs(ls.z))) * 0.5f;
            rightSphereRadius = Mathf.Max(Mathf.Abs(rs.x), Mathf.Max(Mathf.Abs(rs.y), Mathf.Abs(rs.z))) * 0.5f;
            responseGeometryValid = TryProjectedGap(trialEyePosition, out leftInnerEdge, out rightInnerEdge);
            Debug.Log($"Pilot depth geometry v2: trial={trialIndex + 1}; plane={responseOrigin:F4}; " +
                $"leftInner={leftInnerEdge:F6}; rightInner={rightInnerEdge:F6}; " +
                $"radii={leftSphereRadius:F6}/{rightSphereRadius:F6}; valid={responseGeometryValid}; " +
                "fixed trial rectangles; live eye/ray-origin silhouette veto (conservative).");
        }

        private static Vector3[] CaptureVisibleVertices(Transform target)
        {
            var vertices = new List<Vector3>();
            foreach (var filter in target.GetComponentsInChildren<MeshFilter>(true))
            {
                var renderer = filter.GetComponent<MeshRenderer>();
                if (filter.sharedMesh == null || renderer == null || !renderer.enabled) continue;
                foreach (Vector3 vertex in filter.sharedMesh.vertices)
                    vertices.Add(filter.transform.TransformPoint(vertex));
            }
            return vertices.ToArray(); // Cached once per trial, never reallocated per input frame.
        }

        public void SetResponseHover(ResponseSide? side)
        {
            if (promptText == null)
                return;
            string label = side.HasValue ? side.Value.ToString().ToUpperInvariant() : "--";
            string message = "WHICH ONE IS FARTHER?\nPoint LEFT / RIGHT; press trigger\nSelection: " + label;
            if (promptText.text != message)
                promptText.text = message;
        }

        private void CreatePrompt()
        {
            if (viewer == null)
                return;

            Transform existing = viewer.Find("M5_DepthPrompt");
            if (existing != null)
                Destroy(existing.gameObject);

            promptObject = new GameObject("M5_DepthPrompt");
            promptObject.transform.SetParent(viewer, false);
            promptObject.transform.localPosition = new Vector3(0f, 0.12f, 0.70f);
            promptObject.transform.localRotation = Quaternion.identity;

            TextMesh prompt = promptObject.AddComponent<TextMesh>();
            promptText = prompt;
            prompt.anchor = TextAnchor.MiddleCenter;
            prompt.alignment = TextAlignment.Center;
            prompt.characterSize = 0.0028f;
            prompt.fontSize = 42;
            SetResponseHover(null);
        }

        private void HandleTetrisCompleted()
        {
            if (!autoStartAfterTetrisForMilestone5)
                return;

            if (tetrisManager != null)
            {
                tetrisRenderers ??= tetrisManager.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer renderer in tetrisRenderers)
                    renderer.enabled = false;
            }

            VacLevel level = conditionController != null
                ? conditionController.CurrentCondition
                : VacLevel.C1;

            BeginFormal(level);
        }

        public void SetMilestone5AutoStartEnabled(bool enabled)
        {
            autoStartAfterTetrisForMilestone5 = enabled;

            if (tetrisManager == null)
                tetrisManager = FindAnyObjectByType<TetrisManager>();

            if (tetrisManager == null)
                return;

            tetrisManager.onSessionCompleted.RemoveListener(HandleTetrisCompleted);
            if (enabled)
                tetrisManager.onSessionCompleted.AddListener(HandleTetrisCompleted);
        }

        public void BeginPractice(VacLevel practiceCondition)
        {
            BeginBlock(practiceCondition, DepthPhase.Practice, config.practiceDepthTrials);
        }

        public void BeginFormal(VacLevel exposureCondition)
        {
            BeginBlock(exposureCondition, DepthPhase.Post, config.formalDepthTrials);
        }

        // Compatibility overloads for the old two-condition flow until M6 replaces it.
        public void BeginPractice(VacCondition practiceCondition)
        {
            BeginPractice(practiceCondition == VacCondition.Low ? VacLevel.C1 : VacLevel.C3);
        }

        public void BeginFormal(VacCondition exposureCondition)
        {
            BeginFormal(exposureCondition == VacCondition.Low ? VacLevel.C1 : VacLevel.C3);
        }

        private void BeginBlock(VacLevel newCondition, DepthPhase newPhase, int trialCount)
        {
            ResolveReferences();
            EnsureTargets();

            if (config == null || viewer == null || leftTarget == null || rightTarget == null)
            {
                Debug.LogError("DepthJudgmentManager is missing required references.");
                return;
            }

            if (config.depthDifferenceMeters >= config.depthTaskReferenceDistanceMeters * 2f)
            {
                Debug.LogError("Depth difference is too large for the configured reference distance.");
                return;
            }

            if (newPhase != DepthPhase.Practice &&
                createStandaloneTestLog &&
                dataLogger != null &&
                string.IsNullOrWhiteSpace(dataLogger.ParticipantId))
            {
                dataLogger.StartSession($"M5_TEST_{DateTime.Now:yyyyMMdd_HHmmss}");
            }

            CurrentCondition = newCondition;
            phase = newPhase;

            // Fixed outcome task: the same depths are used after every VAC block.
            referenceDepth = config.depthTaskReferenceDistanceMeters;

            fartherSides = BuildBalancedOrder(trialCount);
            trialIndex = 0;
            IsRunning = true;

            Debug.Log(
                $"M5 depth task started: condition={CurrentCondition}; phase={phase}; " +
                $"trials={trialCount}; near={GetNearDepth():F3} m; far={GetFarDepth():F3} m; " +
                "reference=viewer-at-trial-onset; BoardAnchor=unused.");

            ShowTrial();
        }

        // Editor-only compatibility for KeyboardDebugInput. Formal ML2 input uses
        // rectangular response selection; these methods are also used by the input adapter.
        public void SubmitLeft()
        {
            Submit(ResponseSide.Left);
        }

        public void SubmitRight()
        {
            Submit(ResponseSide.Right);
        }

        private void Submit(ResponseSide response)
        {
            if (!IsRunning || !isActiveAndEnabled || lastResponseFrame == Time.frameCount)
                return;

            lastResponseFrame = Time.frameCount;
            float reactionTime = Time.realtimeSinceStartup - stimulusOnsetTime;
            ResponseSide correctSide = fartherSides[trialIndex];
            bool correct = response == correctSide;

            float nearDepth = GetNearDepth();
            float farDepth = GetFarDepth();

            if (phase != DepthPhase.Practice && dataLogger != null)
            {
                dataLogger.LogDepthTrial(
                    CurrentCondition,
                    phase.ToString(),
                    trialIndex + 1,
                    referenceDepth,
                    config.depthDifferenceMeters,
                    correctSide.ToString(),
                    response.ToString(),
                    correct,
                    reactionTime);
            }

            Debug.Log(
                $"M5 depth trial {trialIndex + 1}/{fartherSides.Count}: " +
                $"near={nearDepth:F3} m; far={farDepth:F3} m; " +
                $"farther={correctSide}; response={response}; correct={correct}; " +
                $"rt={reactionTime:F3}s.");

            trialIndex++;

            if (trialIndex >= fartherSides.Count)
            {
                IsRunning = false;
                SetTargetsVisible(false);
                SetPromptVisible(false);
                Debug.Log("M5 depth task complete.");
                onBlockCompleted?.Invoke();
                return;
            }

            ShowTrial();
        }

        private void ShowTrial()
        {
            ResponseSide fartherSide = fartherSides[trialIndex];

            Vector3 forward = viewer.forward.normalized;
            Vector3 right = viewer.right.normalized;
            float halfSeparation = config.targetHorizontalSeparationMeters * 0.5f;

            float nearDepth = GetNearDepth();
            float farDepth = GetFarDepth();

            float leftDepth = fartherSide == ResponseSide.Left ? farDepth : nearDepth;
            float rightDepth = fartherSide == ResponseSide.Right ? farDepth : nearDepth;

            // Capture the viewer pose once at trial onset. The targets are world-fixed
            // for the rest of that trial and do not follow subsequent head movement.
            leftTarget.position =
                viewer.position + forward * leftDepth - right * halfSeparation;

            rightTarget.position =
                viewer.position + forward * rightDepth + right * halfSeparation;

            responseOrigin = viewer.position + forward * referenceDepth;
            responseRight = right;
            responseUp = viewer.up.normalized;
            responseForward = forward;
            SetResponseHover(null);

            ApplyConstantAngularSize(leftTarget, leftReferenceScale, leftDepth);
            ApplyConstantAngularSize(rightTarget, rightReferenceScale, rightDepth);
            CaptureResponseGeometry();

            SetTargetsVisible(true);
            SetPromptVisible(true);
            stimulusOnsetTime = Time.realtimeSinceStartup;
        }

        private float GetNearDepth()
        {
            return referenceDepth - config.depthDifferenceMeters * 0.5f;
        }

        private float GetFarDepth()
        {
            return referenceDepth + config.depthDifferenceMeters * 0.5f;
        }

        private void ApplyConstantAngularSize(
            Transform target,
            Vector3 authoredScale,
            float targetDepthMeters)
        {
            float referenceDistance = config.targetReferenceScaleDistanceMeters;
            float scaleFactor = targetDepthMeters / referenceDistance;
            target.localScale = authoredScale * scaleFactor;
        }

        private void SetTargetsVisible(bool visible)
        {
            if (leftTarget != null)
                leftTarget.gameObject.SetActive(visible);

            if (rightTarget != null)
                rightTarget.gameObject.SetActive(visible);
        }

        private void SetPromptVisible(bool visible)
        {
            if (promptObject != null)
                promptObject.SetActive(visible);
        }

        private static List<ResponseSide> BuildBalancedOrder(int count)
        {
            var list = new List<ResponseSide>(count);
            int leftCount = count / 2;
            int rightCount = count / 2;

            if (count % 2 != 0)
            {
                if (UnityEngine.Random.value < 0.5f)
                    leftCount++;
                else
                    rightCount++;
            }

            for (int i = 0; i < leftCount; i++)
                list.Add(ResponseSide.Left);

            for (int i = 0; i < rightCount; i++)
                list.Add(ResponseSide.Right);

            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }

            return list;
        }
    }
}
