using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace VACExperiment
{
    /// <summary>
    /// M7 session logger.
    ///
    /// Every run gets its own timestamped folder so repeated dry runs never append
    /// into a previous participant's CSV files. Formal task rows inherit the current
    /// counterbalancing/block context from the experiment flow.
    /// </summary>
    public class DataLogger : MonoBehaviour
    {
        public string ParticipantId { get; private set; }
        public string SessionId { get; private set; }
        public string SessionFolder => sessionFolder;
        public bool HasSession => !string.IsNullOrWhiteSpace(sessionFolder);

        private string sessionFolder;
        private string sessionPath;
        private string eventsPath;
        private string blocksPath;
        private string depthPath;
        private string tetrisPath;

        private string group = "";
        private string conditionOrder = "";
        private int block;
        private string condition = "";
        private string sequence = "";
        private float focalDistanceMeters;
        private float targetDistanceMeters;
        private float lockedDistanceMeters;

        public void StartSession(string participantId)
        {
            StartSession(participantId, "", "", null);
        }

        public void StartSession(
            string participantId,
            string groupLabel,
            string orderLabel,
            ExperimentConfig config)
        {
            ParticipantId = Sanitize(participantId);
            group = groupLabel ?? "";
            conditionOrder = orderLabel ?? "";
            block = 0;
            condition = "";
            sequence = "";
            focalDistanceMeters = config != null ? config.focalDistanceMeters : 0f;
            targetDistanceMeters = 0f;
            lockedDistanceMeters = 0f;

            string utcStamp = DateTime.UtcNow.ToString(
                "yyyyMMdd_HHmmssfff",
                CultureInfo.InvariantCulture);
            SessionId = $"{ParticipantId}_{utcStamp}";

            string root = Path.Combine(
                Application.persistentDataPath,
                "VACExperimentData");
            sessionFolder = Path.Combine(root, SessionId);
            Directory.CreateDirectory(sessionFolder);

            sessionPath = Path.Combine(sessionFolder, "session.csv");
            eventsPath = Path.Combine(sessionFolder, "events.csv");
            blocksPath = Path.Combine(sessionFolder, "blocks.csv");
            depthPath = Path.Combine(sessionFolder, "depth_trials.csv");
            tetrisPath = Path.Combine(sessionFolder, "tetris.csv");

            WriteSessionMetadata(config);

            EnsureHeader(
                eventsPath,
                "timestamp_utc,participant_id,session_id,group,block,condition,sequence,event,target_distance_m,locked_distance_m,focal_distance_m,vac_target_d,vac_locked_d,detail");
            EnsureHeader(
                blocksPath,
                "timestamp_utc,participant_id,session_id,group,block,condition,sequence,focal_distance_m,target_distance_m,locked_distance_m,target_vac_d,locked_vac_d");
            EnsureHeader(
                depthPath,
                "timestamp_utc,participant_id,session_id,group,block,condition,sequence,focal_distance_m,target_distance_m,locked_distance_m,target_vac_d,locked_vac_d,phase,trial,reference_depth_m,depth_difference_m,near_depth_m,far_depth_m,correct_side,response,correct,reaction_time_s");
            EnsureHeader(
                tetrisPath,
                "timestamp_utc,participant_id,session_id,group,block,condition,sequence,focal_distance_m,target_distance_m,locked_distance_m,target_vac_d,locked_vac_d,duration_s,score,lines_cleared,lines_per_minute,pieces_placed,average_placement_time_s,top_outs,mean_viewing_distance_m,min_viewing_distance_m,max_viewing_distance_m,mean_vac_d");

            LogEvent("SessionStarted", "", "");
            Debug.Log($"VAC M7 session folder: {sessionFolder}");
        }

        public void SetBlockContext(
            int blockIndex,
            VacLevel vacCondition,
            TetrisSequenceId tetrisSequence,
            float focalMeters,
            float targetMeters,
            float lockedMeters = 0f)
        {
            block = blockIndex;
            condition = vacCondition.ToString();
            sequence = tetrisSequence.ToString();
            focalDistanceMeters = focalMeters;
            targetDistanceMeters = targetMeters;
            lockedDistanceMeters = lockedMeters;
        }

        public void LogBlockCalibration()
        {
            Append(blocksPath, string.Join(",",
                Csv(Timestamp()),
                Csv(ParticipantId),
                Csv(SessionId),
                Csv(group),
                I(block),
                Csv(condition),
                Csv(sequence),
                F(focalDistanceMeters),
                F(targetDistanceMeters),
                F(lockedDistanceMeters),
                F(VacMagnitudeDiopters(focalDistanceMeters, targetDistanceMeters)),
                F(VacMagnitudeDiopters(focalDistanceMeters, lockedDistanceMeters))));
        }

        public void LogEvent(string eventName, string eventCondition, string detail)
        {
            string rowCondition = string.IsNullOrWhiteSpace(eventCondition)
                ? condition
                : eventCondition;

            Append(eventsPath, string.Join(",",
                Csv(Timestamp()),
                Csv(ParticipantId),
                Csv(SessionId),
                Csv(group),
                I(block),
                Csv(rowCondition),
                Csv(sequence),
                Csv(eventName),
                OptionalF(targetDistanceMeters),
                OptionalF(lockedDistanceMeters),
                OptionalF(focalDistanceMeters),
                OptionalF(VacMagnitudeDiopters(focalDistanceMeters, targetDistanceMeters)),
                OptionalF(VacMagnitudeDiopters(focalDistanceMeters, lockedDistanceMeters)),
                Csv(detail)));
        }

        public void LogDepthTrial(
            VacCondition legacyCondition,
            string phase,
            int trial,
            float referenceDepthMeters,
            float depthDifferenceMeters,
            string correctSide,
            string response,
            bool correct,
            float reactionTimeSeconds)
        {
            WriteDepthTrial(
                legacyCondition.ToString(),
                phase,
                trial,
                referenceDepthMeters,
                depthDifferenceMeters,
                correctSide,
                response,
                correct,
                reactionTimeSeconds);
        }

        public void LogDepthTrial(
            VacLevel vacCondition,
            string phase,
            int trial,
            float referenceDepthMeters,
            float depthDifferenceMeters,
            string correctSide,
            string response,
            bool correct,
            float reactionTimeSeconds)
        {
            WriteDepthTrial(
                vacCondition.ToString(),
                phase,
                trial,
                referenceDepthMeters,
                depthDifferenceMeters,
                correctSide,
                response,
                correct,
                reactionTimeSeconds);
        }

        public void LogTetrisSummary(
            VacCondition legacyCondition,
            TetrisSequenceId tetrisSequence,
            float durationSeconds,
            int score,
            int linesCleared,
            int piecesPlaced,
            float averagePlacementTimeSeconds,
            int topOuts)
        {
            WriteTetrisSummary(
                legacyCondition.ToString(),
                tetrisSequence,
                durationSeconds,
                score,
                linesCleared,
                piecesPlaced,
                averagePlacementTimeSeconds,
                topOuts,
                null,
                null,
                null);
        }

        public void LogTetrisSummary(
            VacLevel vacCondition,
            TetrisSequenceId tetrisSequence,
            float durationSeconds,
            int score,
            int linesCleared,
            int piecesPlaced,
            float averagePlacementTimeSeconds,
            int topOuts)
        {
            LogTetrisSummary(
                vacCondition,
                tetrisSequence,
                durationSeconds,
                score,
                linesCleared,
                piecesPlaced,
                averagePlacementTimeSeconds,
                topOuts,
                null,
                null,
                null);
        }

        public void LogTetrisSummary(
            VacLevel vacCondition,
            TetrisSequenceId tetrisSequence,
            float durationSeconds,
            int score,
            int linesCleared,
            int piecesPlaced,
            float averagePlacementTimeSeconds,
            int topOuts,
            float? meanViewingDistanceMeters,
            float? minViewingDistanceMeters,
            float? maxViewingDistanceMeters)
        {
            WriteTetrisSummary(
                vacCondition.ToString(),
                tetrisSequence,
                durationSeconds,
                score,
                linesCleared,
                piecesPlaced,
                averagePlacementTimeSeconds,
                topOuts,
                meanViewingDistanceMeters,
                minViewingDistanceMeters,
                maxViewingDistanceMeters);
        }

        private void WriteDepthTrial(
            string rowCondition,
            string phase,
            int trial,
            float referenceDepthMeters,
            float depthDifferenceMeters,
            string correctSide,
            string response,
            bool correct,
            float reactionTimeSeconds)
        {
            float nearDepth = referenceDepthMeters - depthDifferenceMeters * 0.5f;
            float farDepth = referenceDepthMeters + depthDifferenceMeters * 0.5f;

            Append(depthPath, string.Join(",",
                Csv(Timestamp()),
                Csv(ParticipantId),
                Csv(SessionId),
                Csv(group),
                I(block),
                Csv(rowCondition),
                Csv(sequence),
                OptionalF(focalDistanceMeters),
                OptionalF(targetDistanceMeters),
                OptionalF(lockedDistanceMeters),
                OptionalF(VacMagnitudeDiopters(focalDistanceMeters, targetDistanceMeters)),
                OptionalF(VacMagnitudeDiopters(focalDistanceMeters, lockedDistanceMeters)),
                Csv(phase),
                I(trial),
                F(referenceDepthMeters),
                F(depthDifferenceMeters),
                F(nearDepth),
                F(farDepth),
                Csv(correctSide),
                Csv(response),
                correct ? "1" : "0",
                F(reactionTimeSeconds)));
        }

        private void WriteTetrisSummary(
            string rowCondition,
            TetrisSequenceId tetrisSequence,
            float durationSeconds,
            int score,
            int linesCleared,
            int piecesPlaced,
            float averagePlacementTimeSeconds,
            int topOuts,
            float? meanViewingDistanceMeters,
            float? minViewingDistanceMeters,
            float? maxViewingDistanceMeters)
        {
            float minutes = Mathf.Max(durationSeconds / 60f, 0.0001f);
            float linesPerMinute = linesCleared / minutes;

            Append(tetrisPath, string.Join(",",
                Csv(Timestamp()),
                Csv(ParticipantId),
                Csv(SessionId),
                Csv(group),
                I(block),
                Csv(rowCondition),
                Csv(tetrisSequence.ToString()),
                OptionalF(focalDistanceMeters),
                OptionalF(targetDistanceMeters),
                OptionalF(lockedDistanceMeters),
                OptionalF(VacMagnitudeDiopters(focalDistanceMeters, targetDistanceMeters)),
                OptionalF(VacMagnitudeDiopters(focalDistanceMeters, lockedDistanceMeters)),
                F(durationSeconds),
                I(score),
                I(linesCleared),
                F(linesPerMinute),
                I(piecesPlaced),
                F(averagePlacementTimeSeconds),
                I(topOuts),
                NullableF(meanViewingDistanceMeters),
                NullableF(minViewingDistanceMeters),
                NullableF(maxViewingDistanceMeters),
                meanViewingDistanceMeters.HasValue
                    ? OptionalF(VacMagnitudeDiopters(
                        focalDistanceMeters,
                        meanViewingDistanceMeters.Value))
                    : ""));
        }

        private void WriteSessionMetadata(ExperimentConfig config)
        {
            EnsureHeader(
                sessionPath,
                "session_id,participant_id,group,condition_order,started_utc,protocol_version,app_version,unity_version,device_model,operating_system,development_build,focal_distance_m,c1_distance_m,c2_distance_m,c3_distance_m,board_tolerance_m,tetris_duration_s,warmup_condition,warmup_tetris_duration_s,minimum_recovery_s,depth_reference_m,depth_difference_m,formal_depth_trials,practice_depth_trials");

            Append(sessionPath, string.Join(",",
                Csv(SessionId),
                Csv(ParticipantId),
                Csv(group),
                Csv(conditionOrder),
                Csv(Timestamp()),
                Csv(config != null ? config.protocolVersion : ""),
                Csv(Application.version),
                Csv(Application.unityVersion),
                Csv(SystemInfo.deviceModel),
                Csv(SystemInfo.operatingSystem),
                Debug.isDebugBuild ? "1" : "0",
                OptionalF(config != null ? config.focalDistanceMeters : 0f),
                OptionalF(config != null ? config.c1DistanceMeters : 0f),
                OptionalF(config != null ? config.c2DistanceMeters : 0f),
                OptionalF(config != null ? config.c3DistanceMeters : 0f),
                OptionalF(config != null ? config.boardDistanceToleranceMeters : 0f),
                OptionalF(config != null ? config.tetrisDurationSeconds : 0f),
                Csv(config != null ? config.warmupCondition.ToString() : ""),
                OptionalF(config != null ? config.warmupTetrisDurationSeconds : 0f),
                OptionalF(config != null ? config.minimumRecoverySeconds : 0f),
                OptionalF(config != null ? config.depthTaskReferenceDistanceMeters : 0f),
                OptionalF(config != null ? config.depthDifferenceMeters : 0f),
                I(config != null ? config.formalDepthTrials : 0),
                I(config != null ? config.practiceDepthTrials : 0)));
        }

        private static float VacMagnitudeDiopters(
            float focalMeters,
            float vergenceDistanceMeters)
        {
            if (focalMeters <= 0f || vergenceDistanceMeters <= 0f)
                return 0f;

            return Mathf.Abs(
                (1f / focalMeters) -
                (1f / vergenceDistanceMeters));
        }

        private static string Timestamp()
        {
            return DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        }

        private static string F(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string OptionalF(float value)
        {
            return value > 0f ? F(value) : "";
        }

        private static string NullableF(float? value)
        {
            return value.HasValue && value.Value > 0f ? F(value.Value) : "";
        }

        private static string I(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static void EnsureHeader(string path, string header)
        {
            if (!File.Exists(path))
                File.WriteAllText(path, header + Environment.NewLine, Encoding.UTF8);
        }

        private static void Append(string path, string line)
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("DataLogger.StartSession must be called before logging.");
                return;
            }

            File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
        }

        private static string Csv(string value)
        {
            value ??= "";

            string quote = ((char)34).ToString();
            bool needsQuotes =
                value.IndexOf(',') >= 0 ||
                value.IndexOf((char)34) >= 0 ||
                value.IndexOf('\n') >= 0 ||
                value.IndexOf('\r') >= 0;

            if (!needsQuotes)
                return value;

            return quote + value.Replace(quote, quote + quote) + quote;
        }

        private static string Sanitize(string value)
        {
            value ??= "participant";

            foreach (char c in Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');

            string trimmed = value.Trim();
            return string.IsNullOrWhiteSpace(trimmed) ? "participant" : trimmed;
        }
    }
}
