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
    /// The headset continuously publishes live calibration state; the laptop may
    /// select C1/C2/C3 and issue LOCK/RESCAN. START deliberately stays participant-side.
    /// </summary>
    public class ExperimenterBridge : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BoardDistanceCalibration calibration;
        [SerializeField] private BoardRegistration boardRegistration;
        [SerializeField] private TetrisManager tetrisManager;

        [Header("Network")]
        [Tooltip("Laptop IPv4 address. Broadcast is convenient for development; set the experiment laptop IP if the lab network blocks broadcast.")]
        [SerializeField] private string experimenterHost = "255.255.255.255";

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

            if (!IPAddress.TryParse(experimenterHost, out IPAddress hostAddress))
            {
                Debug.LogError(
                    $"ExperimenterBridge host '{experimenterHost}' is not a valid IPv4 address.");
                enabled = false;
                return;
            }

            try
            {
                statusEndpoint = new IPEndPoint(hostAddress, statusPort);
                statusSender = new UdpClient();
                statusSender.EnableBroadcast = true;

                commandReceiver = new UdpClient(commandPort);
                commandReceiver.Client.Blocking = false;

                initialized = true;
                Debug.Log(
                    $"ExperimenterBridge ready: status->{experimenterHost}:{statusPort}, " +
                    $"commands<-:{commandPort}.");
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

            if (Time.unscaledTime >= nextStatusTime)
            {
                nextStatusTime = Time.unscaledTime + Mathf.Max(0.05f, statusIntervalSeconds);
                SendStatus();
            }
        }

        private void SendStatus()
        {
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
                    string command = Encoding.UTF8.GetString(payload).Trim().ToUpperInvariant();
                    HandleCommand(command);
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

        private void HandleCommand(string command)
        {
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
        }
    }
}
