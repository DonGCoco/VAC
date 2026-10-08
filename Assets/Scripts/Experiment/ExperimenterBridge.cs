using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using VACExperiment.Board;
using VACExperiment.Tetris;

namespace VACExperiment
{
    /// <summary>
    /// Small UDP bridge for the external experimenter monitor.
    ///
    /// The laptop discovers the headset by broadcasting a VAC monitor discovery
    /// packet to the headset command port. The headset learns the sender IP from
    /// that packet and then unicasts live status back to the laptop.
    ///
    /// This avoids hard-coding any experimenter computer IP into the Unity build.
    /// </summary>
    public class ExperimenterBridge : MonoBehaviour
    {
        private const string DiscoveryPrefix = "VAC_MONITOR_DISCOVER";

        [Header("References")]
        [SerializeField] private BoardDistanceCalibration calibration;
        [SerializeField] private BoardRegistration boardRegistration;
        [SerializeField] private TetrisManager tetrisManager;

        [Header("Network")]
        [SerializeField, Range(1024, 65535)] private int statusPort = 45555;
        [SerializeField, Range(1024, 65535)] private int commandPort = 45556;
        [SerializeField, Min(0.05f)] private float statusIntervalSeconds = 0.10f;

        private UdpClient statusSender;
        private UdpClient commandReceiver;
        private IPEndPoint statusEndpoint;
        private float nextStatusTime;
        private bool initialized;

        [Serializable]
        private class ExperimenterStatus
        {
            public string condition;
            public string state;
            public float target_m;
            public float actual_m;
            public float locked_m;
            public bool board_locked;
            public bool anchor_tracking;
            public bool tetris_running;
            public bool start_available;
            public float seconds_since_marker_seen;
        }

        private void Start()
        {
            if (calibration == null)
                calibration = FindAnyObjectByType<BoardDistanceCalibration>();

            if (boardRegistration == null)
                boardRegistration = FindAnyObjectByType<BoardRegistration>();

            if (tetrisManager == null)
                tetrisManager = FindAnyObjectByType<TetrisManager>();

            if (calibration == null || boardRegistration == null)
            {
                Debug.LogError(
                    "ExperimenterBridge requires BoardDistanceCalibration and BoardRegistration.");
                enabled = false;
                return;
            }

            try
            {
                statusSender = new UdpClient();

                commandReceiver = new UdpClient(commandPort);
                commandReceiver.Client.Blocking = false;

                initialized = true;
                Debug.Log(
                    $"ExperimenterBridge ready: waiting for monitor discovery on UDP {commandPort}; " +
                    $"status port={statusPort}.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"ExperimenterBridge network initialization failed: {exception.Message}");
                enabled = false;
            }
        }

        private void Update()
        {
            if (!initialized)
                return;

            ReceiveCommands();

            if (statusEndpoint != null && Time.unscaledTime >= nextStatusTime)
            {
                nextStatusTime = Time.unscaledTime + Mathf.Max(0.05f, statusIntervalSeconds);
                SendStatus();
            }
        }

        private void SendStatus()
        {
            if (statusEndpoint == null)
                return;

            var status = new ExperimenterStatus
            {
                condition = calibration.CurrentCondition.ToString(),
                state = calibration.CurrentState.ToString(),
                target_m = calibration.TargetDistanceMeters,
                actual_m = calibration.ActualDistanceMeters,
                locked_m = calibration.LockedDistanceMeters,
                board_locked = calibration.IsBoardLocked,
                anchor_tracking = boardRegistration.IsSpatialAnchorTracking,
                tetris_running = tetrisManager != null && tetrisManager.IsRunning,
                start_available =
                    calibration.CurrentState == BoardDistanceState.Locked &&
                    boardRegistration.IsSpatialAnchorTracking &&
                    (tetrisManager == null || !tetrisManager.IsRunning),
                seconds_since_marker_seen = boardRegistration.SecondsSinceMarkerSeen
            };

            string json = JsonUtility.ToJson(status);
            byte[] payload = Encoding.UTF8.GetBytes(json);

            try
            {
                statusSender.Send(payload, payload.Length, statusEndpoint);
            }
            catch (SocketException exception)
            {
                Debug.LogWarning(
                    $"ExperimenterBridge status send failed: {exception.SocketErrorCode}");
            }
        }

        private void ReceiveCommands()
        {
            if (commandReceiver == null)
                return;

            while (commandReceiver.Available > 0)
            {
                IPEndPoint remote = new(IPAddress.Any, 0);

                try
                {
                    byte[] payload = commandReceiver.Receive(ref remote);
                    string message = Encoding.UTF8.GetString(payload).Trim();

                    if (TryHandleDiscovery(message, remote))
                        continue;

                    HandleCommand(message.ToUpperInvariant(), remote);
                }
                catch (SocketException exception)
                {
                    if (exception.SocketErrorCode != SocketError.WouldBlock)
                    {
                        Debug.LogWarning(
                            $"ExperimenterBridge command receive failed: {exception.SocketErrorCode}");
                    }

                    return;
                }
            }
        }

        private bool TryHandleDiscovery(string message, IPEndPoint remote)
        {
            if (!message.StartsWith(DiscoveryPrefix, StringComparison.Ordinal))
                return false;

            int requestedStatusPort = statusPort;
            string[] parts = message.Split(':');

            if (parts.Length >= 2 &&
                int.TryParse(parts[1], out int parsedPort) &&
                parsedPort >= 1024 &&
                parsedPort <= 65535)
            {
                requestedStatusPort = parsedPort;
            }

            bool changed =
                statusEndpoint == null ||
                !statusEndpoint.Address.Equals(remote.Address) ||
                statusEndpoint.Port != requestedStatusPort;

            statusEndpoint = new IPEndPoint(remote.Address, requestedStatusPort);
            nextStatusTime = 0f;

            if (changed)
            {
                Debug.Log(
                    $"ExperimenterBridge paired with monitor at " +
                    $"{statusEndpoint.Address}:{statusEndpoint.Port}.");
            }

            // Reply immediately so the browser does not wait for the next interval.
            SendStatus();
            return true;
        }

        private void HandleCommand(string command, IPEndPoint remote)
        {
            // Once paired, only accept experiment commands from the paired laptop.
            if (statusEndpoint == null || !statusEndpoint.Address.Equals(remote.Address))
            {
                Debug.LogWarning(
                    $"ExperimenterBridge ignored command '{command}' from unpaired host {remote.Address}.");
                return;
            }

            switch (command)
            {
                case "C1":
                    calibration.SelectC1();
                    break;
                case "C2":
                    calibration.SelectC2();
                    break;
                case "C3":
                    calibration.SelectC3();
                    break;
                case "LOCK":
                    calibration.LockBoardPose();
                    break;
                case "RESCAN":
                    calibration.RescanBoard();
                    break;
                default:
                    Debug.LogWarning($"ExperimenterBridge ignored unknown command '{command}'.");
                    return;
            }

            Debug.Log($"ExperimenterBridge command: {command}");
        }

        private void OnDestroy()
        {
            initialized = false;
            statusSender?.Close();
            commandReceiver?.Close();
            statusSender = null;
            commandReceiver = null;
            statusEndpoint = null;
        }
    }
}
