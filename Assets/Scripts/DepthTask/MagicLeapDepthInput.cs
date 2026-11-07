using System;
using MagicLeap.Examples;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VACExperiment
{
    /// <summary>
    /// Select left/right via the controller ray on a fixed response plane.
    /// A fresh trigger press submits once; the central gap never submits.
    /// </summary>
    public class MagicLeapDepthInput : MonoBehaviour
    {
        [SerializeField] private DepthJudgmentManager depthTask;
        [Tooltip("Assign the ML2 controller ray origin when several rays exist in the scene.")]
        [SerializeField] private Transform controllerRayOrigin;

        private MagicLeapController controller;
        private bool previousPressed = true;
        private bool wasRunning;
        private XRRayInteractor controllerRay;
        private float nextDiagnosticTime;

        private void OnEnable()
        {
            previousPressed = true;
            wasRunning = false;
            if (depthTask == null)
                depthTask = GetComponent<DepthJudgmentManager>();
            if (controllerRayOrigin == null)
            {
                var rays = FindObjectsByType<XRRayInteractor>(FindObjectsSortMode.None);
                if (rays.Length == 1)
                    controllerRayOrigin = rays[0].rayOriginTransform != null
                        ? rays[0].rayOriginTransform : rays[0].transform;
                else
                    Debug.LogError("Depth input: assign the ML2 Controller Ray Origin; expected exactly one active XRRayInteractor.");
            }
            try
            {
                if (controllerRayOrigin != null)
                    controllerRay = controllerRayOrigin.GetComponentInParent<XRRayInteractor>();
                controller = MagicLeapController.Instance;
                if (controllerRayOrigin != null)
                    Debug.Log($"M5 depth response ray bound: {controllerRayOrigin.name}.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Depth input could not bind ML2 actions: {exception.Message}");
            }
        }

        private void LateUpdate()
        {
            const InputTrackingState required = InputTrackingState.Position | InputTrackingState.Rotation;
            bool tracked = controller != null && controller.IsTracked &&
                (controller.TrackingState & required) == required &&
                controllerRayOrigin != null && controllerRayOrigin.gameObject.activeInHierarchy &&
                controllerRay != null && controllerRay.isActiveAndEnabled &&
                controllerRay.lineType == XRRayInteractor.LineType.StraightLine;
            // Use the current origin chosen by XRI (it may initialise a child at Awake).
            Transform rayOrigin = controllerRay != null && controllerRay.rayOriginTransform != null
                ? controllerRay.rayOriginTransform : controllerRayOrigin;
            Ray ray = tracked
                ? new Ray(rayOrigin.position, rayOrigin.forward)
                : default;
            ProcessInput(controller != null && controller.TriggerIsPressed, tracked, ray);
        }

        // Kept separate from device polling so the actual latch/geometry path can
        // also be exercised by the Unity batch check without a physical controller.
        private void ProcessInput(bool pressed, bool tracked, Ray ray)
        {
            bool running = depthTask != null && depthTask.IsRunning && depthTask.isActiveAndEnabled;
            // A block beginning with a held trigger must first observe release.
            if (!running || !tracked)
            {
                previousPressed = true;
                wasRunning = false;
                if (depthTask != null)
                    depthTask.SetResponseHover(null);
                return;
            }
            if (!wasRunning)
            {
                previousPressed = true;
                wasRunning = true;
            }
            bool valid = depthTask.TryGetResponseSide(ray, out ResponseSide side);
            depthTask.SetResponseHover(valid ? (ResponseSide?)side : null);
            if (Debug.isDebugBuild && Time.unscaledTime >= nextDiagnosticTime)
            {
                nextDiagnosticTime = Time.unscaledTime + 1f;
                Debug.Log($"Pilot depth ray: origin={ray.origin:F4}; direction={ray.direction:F4}; " +
                    $"selection={(valid ? side.ToString() : "--")}; inner={depthTask.ResponseInnerEdges:F6}.");
            }
            bool freshPress = pressed && !previousPressed;
            previousPressed = pressed;
            if (!freshPress || !valid)
                return;
            if (side == ResponseSide.Left)
                depthTask.SubmitLeft();
            else
                depthTask.SubmitRight();
        }

        private void OnDisable()
        {
            previousPressed = true;
            wasRunning = false;
            if (depthTask != null)
                depthTask.SetResponseHover(null);
        }
    }
}
