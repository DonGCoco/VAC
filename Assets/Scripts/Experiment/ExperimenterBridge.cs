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
        [SerializeField] private ParticipantSession participantSession;
        [SerializeField] private FormalExperimentFlowController formalFlow;
        [SerializeField] private DataLogger dataLogger;

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
            public string participant_id;
            public string group;
            public string condition_order;
            public int block;
            public string sequence;
            public string phase;
            public float recovery_remaining_s;
            public bool recovery_ready;
            public bool development_shortcuts;
            public bool logging_ready;
            public string session_id;
        }

        private void Start()
        {
            if (calibration == null)
                calibration = FindAnyObjectByType<BoardDistanceCalibration>();

            if (boardRegistration == null)
                boardRegistration = FindAnyObjectByType<BoardRegistration>();

            if (tetrisManager == null)
                tetrisManager = FindAnyObjectByType<TetrisManager>();

            if (participantSession == null)
                participantSession = FindAnyObjectByType<ParticipantSession>();

            if (dataLogger == null)
                dataLogger = FindAnyObjectByType<DataLogger>();

            if (participantSession == null)
            {
                GameObject sessionObject = new("M6_ParticipantSession");
                participantSession = sessionObject.AddComponent<ParticipantSession>();
            }

            if (formalFlow == null)
                formalFlow = FindAnyObjectByType<FormalExperimentFlowController>();

            if (formalFlow == null)
                formalFlow = participantSession.gameObject.AddComponent<FormalExperimentFlowController>();

            formalFlow.SetParticipantSession(participantSession);

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
                    (tetrisManager == null || !tetrisManager.IsRunning) &&
                    (formalFlow == null || !formalFlow.HasActiveParticipant || formalFlow.ParticipantStartAllowed),
                seconds_since_marker_seen = boardRegistration.SecondsSinceMarkerSeen,
                participant_id = participantSession != null ? participantSession.ParticipantId : "",
                group = participantSession != null ? participantSession.GroupLabel : "",
                condition_order = participantSession != null ? participantSession.ConditionOrderLabel : "",
                block = participantSession != null ? participantSession.CurrentBlockIndex : 0,
                sequence =
                    formalFlow != null &&
                    formalFlow.HasActiveParticipant &&
                    (formalFlow.Phase == FormalExperimentPhase.WarmupCalibration ||
                     formalFlow.Phase == FormalExperimentPhase.WarmupTetris ||
                     formalFlow.Phase == FormalExperimentPhase.WarmupDepthPractice)
                        ? "T"
                        : participantSession != null &&
                          participantSession.HasAssignment &&
                          participantSession.CurrentBlockIndex >= 1
                            ? participantSession.GetSequenceForBlock(participantSession.CurrentBlockIndex).ToString()
                            : "",
                phase = formalFlow != null ? formalFlow.PhaseLabel : "",
                recovery_remaining_s = formalFlow != null ? formalFlow.RecoveryRemainingSeconds : 0f,
                recovery_ready = formalFlow != null && formalFlow.RecoveryMinimumSatisfied,
                development_shortcuts =
                    formalFlow != null && formalFlow.DevelopmentShortcutsAvailable,
                logging_ready = dataLogger != null && dataLogger.HasSession,
                session_id = dataLogger != null ? dataLogger.SessionId : ""
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

                    if (TryHandleParticipantAssignment(message, remote))
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

        private bool TryHandleParticipantAssignment(string message, IPEndPoint remote)
        {
            if (!message.StartsWith("PARTICIPANT:", StringComparison.OrdinalIgnoreCase))
                return false;

            if (statusEndpoint == null || !statusEndpoint.Address.Equals(remote.Address))
            {
                Debug.LogWarning(
                    $"ExperimenterBridge ignored participant assignment from unpaired host {remote.Address}.");
                return true;
            }

            string[] parts = message.Split(':');
            if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[1]))
            {
                Debug.LogWarning("M6 participant assignment rejected: missing participant ID.");
                return true;
            }

            string participantId = parts[1].Trim();
            int groupOverride = 0;

            if (parts.Length >= 3)
            {
                string groupToken = parts[2].Trim().ToUpperInvariant();
                groupOverride = groupToken switch
                {
                    "" => 0,
                    "AUTO" => 0,
                    "G1" => 1,
                    "1" => 1,
                    "G2" => 2,
                    "2" => 2,
                    "G3" => 3,
                    "3" => 3,
                    _ => -1
                };
            }

            if (groupOverride < 0)
            {
                Debug.LogWarning(
                    $"M6 participant assignment rejected: invalid group override in '{message}'.");
                return true;
            }

            try
            {
                if (formalFlow != null)
                    formalFlow.AssignParticipant(participantId, groupOverride);
                else
                    participantSession.Assign(participantId, groupOverride);

                SendStatus();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"M6 participant assignment rejected: {exception.Message}");
            }

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
                case "C2":
                case "C3":
                    if (formalFlow != null && formalFlow.HasActiveParticipant)
                    {
                        Debug.LogWarning(
                            $"ExperimenterBridge ignored manual {command} during active M6 session.");
                        return;
                    }

                    if (command == "C1")
                        calibration.SelectC1();
                    else if (command == "C2")
                        calibration.SelectC2();
                    else
                        calibration.SelectC3();
                    break;

                case "LOCK":
                    calibration.LockBoardPose();
                    break;
                case "RESCAN":
                    calibration.RescanBoard();
                    break;
                case "CONTINUE":
                    formalFlow?.ContinueAfterQuestionnaire();
                    break;
                case "RECOVERY_DONE":
                    formalFlow?.ContinueAfterRecovery();
                    break;
                case "DEV_SKIP_RECOVERY":
                    formalFlow?.SkipRecoveryForDevelopment();
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
