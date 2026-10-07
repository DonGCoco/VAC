using System;
using MagicLeap.Examples;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VACExperiment
{
    /// <summary>
    /// Depth-task responses using the official Magic Leap OpenXR controller actions.
    /// Click the left or right side of the trackpad to choose which target appears closer.
    /// </summary>
    public class MagicLeapDepthInput : MonoBehaviour
    {
        [SerializeField] private DepthJudgmentManager depthTask;
        [SerializeField, Range(0.1f, 0.9f)] private float horizontalThreshold = 0.25f;

        private MagicLeapController controller;
        private bool subscribed;

        private void OnEnable()
        {
            if (depthTask == null)
                depthTask = GetComponent<DepthJudgmentManager>();

            try
            {
                controller = MagicLeapController.Instance;
                controller.TrackpadClicked += HandleTrackpadClick;
                subscribed = true;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"M5 could not bind the official Magic Leap controller actions: {exception.Message}");
            }
        }

        private void OnDisable()
        {
            if (!subscribed || controller == null)
                return;

            controller.TrackpadClicked -= HandleTrackpadClick;
            subscribed = false;
        }

        private void HandleTrackpadClick(InputAction.CallbackContext context)
        {
            if (depthTask == null || !depthTask.IsRunning)
                return;

            float x = controller.TouchPosition.x;

            if (x <= -horizontalThreshold)
                depthTask.SubmitLeft();
            else if (x >= horizontalThreshold)
                depthTask.SubmitRight();
        }
    }
}
