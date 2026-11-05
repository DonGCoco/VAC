using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VACExperiment;

namespace VACExperiment.EditorTools
{
    // Exercises production C# in Unity; no controller or participant data required.
    public static class VACDepthRectangleChecks
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static int checks;
        private static void Check(bool value, string message)
        {
            if (!value) throw new Exception("Depth rectangle check failed: " + message);
            checks++;
        }
        private static void Set(object owner, string field, object value) =>
            owner.GetType().GetField(field, Private).SetValue(owner, value);
        private static T Get<T>(object owner, string field) =>
            (T)owner.GetType().GetField(field, Private).GetValue(owner);
        private static void Call(object owner, string method, params object[] args) =>
            owner.GetType().GetMethod(method, Private).Invoke(owner, args);

        [MenuItem("VAC/Checks/Depth rectangle response")]
        public static void Run()
        {
            var randomState = UnityEngine.Random.state;
            GameObject fixture = null;
            ExperimentConfig config = null;
            try
            {
                checks = 0;
                EditorSceneManager.OpenScene("Assets/Scenes/M1_SmokeTest.unity");
                var boundInput = UnityEngine.Object.FindAnyObjectByType<MagicLeapDepthInput>();
                var manager = UnityEngine.Object.FindAnyObjectByType<DepthJudgmentManager>();
                Check(boundInput != null && boundInput.enabled && boundInput.gameObject.activeInHierarchy,
                    "scene input is explicitly present and enabled");
                Check(Get<DepthJudgmentManager>(boundInput, "depthTask") == manager, "scene manager binding");
                Transform origin = Get<Transform>(boundInput, "controllerRayOrigin");
                Check(origin != null && origin.name == "Controller", "ML2 Controller transform binding");
                XRRayInteractor rayInteractor = origin.GetComponent<XRRayInteractor>();
                Check(rayInteractor != null && rayInteractor.enabled, "bound controller has active XRI ray");
                Check(rayInteractor.rayOriginTransform == null || rayInteractor.rayOriginTransform == origin,
                    "binding matches XRI ray origin");
                var actionManager = UnityEngine.Object.FindAnyObjectByType<InputActionManager>();
                var actions = actionManager.actionAssets[0].FindActionMap("Controller", true);
                Check(actions.FindAction("Trigger", true).bindings.Any(b => b.path.Contains("MagicLeapController")),
                    "trigger uses ML2 action bindings");
                Check(actions.FindAction("Status", true) != null && actions.FindAction("IsTracked", true) != null,
                    "tracking actions available");

                fixture = new GameObject("DepthRectangleCheckFixture");
                var viewer = new GameObject("CheckViewer").transform;
                viewer.SetParent(fixture.transform, false);
                config = ScriptableObject.CreateInstance<ExperimentConfig>();
                manager = fixture.AddComponent<DepthJudgmentManager>();
                Set(manager, "config", config);
                Set(manager, "viewer", viewer);
                Set(manager, "createStandaloneTestLog", false);
                Call(manager, "Awake");
                var input = fixture.AddComponent<MagicLeapDepthInput>();
                Set(input, "depthTask", manager);
                var left = Get<Transform>(manager, "leftTarget");
                var right = Get<Transform>(manager, "rightTarget");
                Check(left.GetComponentsInChildren<Collider>(true).All(c => !c.enabled) &&
                    right.GetComponentsInChildren<Collider>(true).All(c => !c.enabled), "spheres cannot receive clicks");
                manager.BeginPractice(VacLevel.C2);
                Check(manager.TrialCount == 3, "practice count unchanged");
                Vector3 leftPosition = left.position, rightPosition = right.position;
                Vector3 leftScale = left.localScale, rightScale = right.localScale;
                Check(Mathf.Approximately(Mathf.Abs(left.position.x - right.position.x), .12f), "sphere spacing unchanged");
                Check(Mathf.Approximately(Mathf.Min(left.position.z, right.position.z), .97f) &&
                    Mathf.Approximately(Mathf.Max(left.position.z, right.position.z), 1.03f), "sphere depths unchanged");
                Check(Mathf.Approximately(left.localScale.x, .04f * left.position.z) &&
                    Mathf.Approximately(right.localScale.x, .04f * right.position.z), "sphere sizes unchanged");
                Ray At(float x, float y = 0) => new Ray(new Vector3(x, y, 0), Vector3.forward);
                bool Side(float x, float y = 0) => manager.TryGetResponseSide(At(x, y), out _);
                Check(manager.TryGetResponseSide(At(-.1f), out var side) && side == ResponseSide.Left, "left region");
                Check(manager.TryGetResponseSide(At(.1f), out side) && side == ResponseSide.Right, "right region");
                foreach (float x in new[] { -.03f, 0f, .03f, -.381f, .381f })
                    Check(!Side(x), "gap/outer horizontal exclusion " + x);
                float outerEdge = Get<float>(manager, "responseCenterGapMeters") * .5f +
                    Get<float>(manager, "responseRegionWidthMeters");
                Check(Side(-outerEdge, .2f) && Side(outerEdge, -.2f), "inclusive configured outer edges");
                Check(!Side(.1f, .201f) && !Side(-.1f, -.201f), "vertical exclusion");
                Check(!Side(float.NaN), "invalid intersection rejected");
                Check(!manager.TryGetResponseSide(new Ray(Vector3.zero, Vector3.right), out _), "parallel ray rejected");
                Check(!manager.TryGetResponseSide(new Ray(new Vector3(0, 0, 2), Vector3.back), out _), "back side rejected");
                Check(!manager.TryGetResponseSide(new Ray(new Vector3(.1f, 0, 2), Vector3.forward), out _), "intersection behind ray rejected");
                viewer.SetPositionAndRotation(new Vector3(3, 2, 1), Quaternion.Euler(10, 50, 0));
                Check(Side(.1f) && left.position == leftPosition && right.position == rightPosition,
                    "response plane and spheres world-fixed during trial");
                manager.SetResponseHover(ResponseSide.Left);
                Check(Get<TextMesh>(manager, "promptText").text.EndsWith("LEFT"), "left hover text");
                manager.SetResponseHover(ResponseSide.Right);
                Check(Get<TextMesh>(manager, "promptText").text.EndsWith("RIGHT"), "right hover text");
                Check(left.localScale == leftScale && right.localScale == rightScale, "hover does not resize spheres");
                viewer.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                void Poll(bool pressed, Ray ray, bool tracked = true)
                {
                    // Edit-mode batch execution has no player frames; each Poll
                    // represents a later frame. Explicitly test same-frame guard below.
                    Set(manager, "lastResponseFrame", -1);
                    Call(input, "ProcessInput", pressed, tracked, ray);
                }
                Poll(true, At(-.1f));
                Check(manager.CurrentTrialNumber == 1, "held at block start rejected");
                Poll(false, At(-.1f)); Poll(true, At(-.1f));
                Check(manager.CurrentTrialNumber == 2, "fresh press advances once");
                Poll(true, At(.1f)); Poll(true, At(-.1f));
                Check(manager.CurrentTrialNumber == 2, "held sweep cannot answer next trial");
                Poll(false, At(0)); Poll(true, At(0)); Poll(true, At(.1f));
                Check(manager.CurrentTrialNumber == 2, "press in gap then move in rejected");
                Poll(false, At(.1f)); Poll(true, At(.1f));
                Check(manager.CurrentTrialNumber == 3, "release and repress accepted");
                Poll(true, At(-.1f), false);
                Check(Get<TextMesh>(manager, "promptText").text.EndsWith("--"), "tracking loss clears hover");
                Poll(true, At(-.1f));
                Check(manager.CurrentTrialNumber == 3, "held on tracking recovery rejected");
                Poll(false, At(-.1f)); Poll(true, At(-.1f));
                Check(!manager.IsRunning, "three practice responses complete block");
                manager.BeginFormal(VacLevel.C2);
                Check(manager.TrialCount == 8, "formal count unchanged");
                var order = Get<System.Collections.Generic.List<ResponseSide>>(manager, "fartherSides");
                Check(order.Count(s => s == ResponseSide.Left) == 4, "formal sides remain balanced");
                Set(manager, "lastResponseFrame", -1);
                manager.SubmitLeft();
                manager.SubmitRight();
                Check(manager.CurrentTrialNumber == 2, "same-frame duplicate rejected");
                for (int i = 1; i < 8; i++)
                {
                    Poll(false, At(.1f)); Poll(true, At(.1f));
                }
                Check(!manager.IsRunning, "eight formal responses complete block");
                Debug.Log($"VAC_DEPTH_RECTANGLE_CHECKS_PASS count={checks}; real Unity C#, synthetic input (not ML2 acceptance)");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
            finally
            {
                if (fixture != null) UnityEngine.Object.DestroyImmediate(fixture);
                if (config != null) UnityEngine.Object.DestroyImmediate(config);
                UnityEngine.Random.state = randomState;
            }
        }
    }
}
