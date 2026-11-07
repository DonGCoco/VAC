using System;
using MagicLeap.Examples;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VACExperiment.Tetris
{
    /// <summary>
    /// Uses the official Magic Leap OpenXR controller action map already included
    /// with the ML Rig sample. No duplicate custom input actions are created.
    /// </summary>
    public class MagicLeapTetrisInput : MonoBehaviour
    {
        [SerializeField] private TetrisManager tetrisManager;
        [SerializeField, Range(0.1f, 0.9f)] private float horizontalThreshold = 0.35f;

        private MagicLeapController controller;
        private bool subscribed;
        private bool gameplayArmed;

        private void OnEnable()
        {
            gameplayArmed = false;
            if (tetrisManager == null)
                tetrisManager = GetComponent<TetrisManager>();

            try
            {
                controller = MagicLeapController.Instance;
                controller.TrackpadClicked += HandleTrackpadClick;
                controller.TriggerPressed += HandleTrigger;
                controller.BumperPressed += HandleBumper;
                controller.MenuPressed += HandleMenu;
                subscribed = true;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"M4 could not bind the official Magic Leap controller actions: {exception.Message}");
            }
        }

        private void OnDisable()
        {
            gameplayArmed = false;
            if (!subscribed || controller == null)
                return;

            controller.TrackpadClicked -= HandleTrackpadClick;
            controller.TriggerPressed -= HandleTrigger;
            controller.BumperPressed -= HandleBumper;
            controller.MenuPressed -= HandleMenu;
            subscribed = false;
        }

        private void HandleTrackpadClick(InputAction.CallbackContext context)
        {
            if (!gameplayArmed || tetrisManager == null || !tetrisManager.IsRunning)
                return;

            float x = controller.TouchPosition.x;

            if (x <= -horizontalThreshold)
                tetrisManager.MoveLeft();
            else if (x >= horizontalThreshold)
                tetrisManager.MoveRight();
        }

        private void HandleTrigger(InputAction.CallbackContext context)
        {
            if (gameplayArmed && tetrisManager != null && tetrisManager.IsRunning)
                tetrisManager.RotateClockwise();
        }

        private void HandleBumper(InputAction.CallbackContext context)
        {
            if (gameplayArmed && tetrisManager != null && tetrisManager.IsRunning)
                tetrisManager.HardDrop();
        }

        private void HandleMenu(InputAction.CallbackContext context)
        {
            if (tetrisManager != null && tetrisManager.IsBusy)
                tetrisManager.EndSessionEarly();
        }

        private void Update()
        {
            UpdateInputGate(tetrisManager != null && tetrisManager.IsRunning &&
                controller != null && controller.IsTracked,
                controller != null && controller.TriggerIsPressed,
                controller != null && controller.BumperIsPressed);
        }

        private void UpdateInputGate(bool runningAndTracked, bool triggerPressed, bool bumperPressed)
        {
            if (!runningAndTracked) gameplayArmed = false;
            else if (!triggerPressed && !bumperPressed) gameplayArmed = true;
        }
    }
}
