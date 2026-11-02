using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using VACExperiment.Board;

namespace VACExperiment.EditorTools
{
    // Executes the real calibration component without an ML2 marker subsystem.
    // Run from the menu or -executeMethod VACExperiment.EditorTools.GameCenterDistanceCheck.Run.
    public static class GameCenterDistanceCheck
    {
        [MenuItem("VAC/Check Game-Centre Distance")]
        public static void Run()
        {
            GameObject root = new("GameCentreDistanceCheck") { hideFlags = HideFlags.HideAndDontSave };
            root.SetActive(false);
            ExperimentConfig config = ScriptableObject.CreateInstance<ExperimentConfig>();
            try
            {
                Transform anchor = Child(root.transform, "Anchor");
                Transform stimulus = Child(anchor, "GameCentre");
                Transform viewer = Child(root.transform, "Viewer");
                BoardRegistration registration = root.AddComponent<BoardRegistration>();
                ConditionController condition = root.AddComponent<ConditionController>();
                BoardDistanceCalibration calibration = root.AddComponent<BoardDistanceCalibration>();
                Set(condition, "stimulusRoot", stimulus);
                Set(calibration, "boardRegistration", registration);
                Set(calibration, "boardAnchor", anchor);
                Set(calibration, "viewer", viewer);
                Set(calibration, "config", config);
                Set(calibration, "conditionController", condition);
                Set(calibration, "<TargetDistanceMeters>k__BackingField", 1f);
                Set(registration, "<HasRegisteredBoard>k__BackingField", true);
                Set(registration, "lastMarkerSeenTime", Time.unscaledTime);

                // QR is the hypotenuse: game is exactly 1 m, QR is about 1.077 m.
                anchor.position = new Vector3(0f, .4f, 1f);
                stimulus.localPosition = new Vector3(0f, -.4f, 0f);
                Check(calibration, 1f, BoardDistanceState.Ready);

                // QR is exactly 1 m, game is 1.077 m: must reject READY.
                anchor.position = new Vector3(0f, 0f, 1f);
                Check(calibration, Mathf.Sqrt(1.16f), BoardDistanceState.TooFar);

                // Arbitrary board rotation/translation must still use world game centre.
                anchor.SetPositionAndRotation(new Vector3(2f, 1f, -3f), Quaternion.Euler(18f, 37f, 9f));
                viewer.position = stimulus.position - anchor.forward;
                Check(calibration, 1f, BoardDistanceState.Ready);
                viewer.position += anchor.forward * .1f;
                Check(calibration, .9f, BoardDistanceState.TooClose);

                // Missing game centre must not silently substitute the QR anchor.
                Set(condition, "stimulusRoot", null);
                Check(calibration, 0f, BoardDistanceState.AnchorError);
                Set(condition, "stimulusRoot", stimulus);
                Set(registration, "lastMarkerSeenTime", Time.unscaledTime - 100f);
                Check(calibration, 0f, BoardDistanceState.WaitingForMarker);

                // A locked but untracked anchor must not expose a stale valid distance.
                Set(registration, "<IsPoseLocked>k__BackingField", true);
                Check(calibration, 0f, BoardDistanceState.Locking);
                Debug.Log("PASS: game-centre calibration geometry, READY boundaries and invalid tracking guards.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        private static Transform Child(Transform parent, string name)
        {
            GameObject child = new(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static void Set(object target, string field, object value)
        {
            FieldInfo info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            if (info == null)
                throw new InvalidOperationException($"Missing test field {field}");
            info.SetValue(target, value);
        }

        private static void Check(BoardDistanceCalibration calibration, float expected, BoardDistanceState state)
        {
            typeof(BoardDistanceCalibration).GetMethod("RefreshMeasurement", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(calibration, null);
            if (Mathf.Abs(calibration.ActualDistanceMeters - expected) > .0001f ||
                calibration.CurrentState != state || calibration.HasValidViewingDistance != (expected > 0f))
                throw new InvalidOperationException(
                    $"Expected {expected:F4} m / {state}, got {calibration.ActualDistanceMeters:F4} m / {calibration.CurrentState}");
        }
    }
}
