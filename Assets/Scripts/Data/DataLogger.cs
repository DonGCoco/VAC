using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace VACExperiment
{
    public class DataLogger : MonoBehaviour
    {
        public string ParticipantId { get; private set; }

        private string sessionFolder;
        private string eventsPath;
        private string depthPath;
        private string tetrisPath;

        public void StartSession(string participantId)
        {
            ParticipantId = Sanitize(participantId);
            sessionFolder = Path.Combine(Application.persistentDataPath, "VACExperimentData");
            Directory.CreateDirectory(sessionFolder);

            eventsPath = Path.Combine(sessionFolder, $"{ParticipantId}_events.csv");
            depthPath = Path.Combine(sessionFolder, $"{ParticipantId}_depth.csv");
            tetrisPath = Path.Combine(sessionFolder, $"{ParticipantId}_tetris.csv");

            EnsureHeader(eventsPath,
                "timestamp,participant_id,event,condition,detail");
            EnsureHeader(depthPath,
                "timestamp,participant_id,condition,phase,trial,reference_depth_m,depth_difference_m,closer_side,response,correct,reaction_time_s");
            EnsureHeader(tetrisPath,
                "timestamp,participant_id,condition,sequence,duration_s,score,lines_cleared,lines_per_minute,pieces_placed,average_placement_time_s,top_outs");

            LogEvent("SessionStarted", "", "");
            Debug.Log($"VAC experiment data folder: {sessionFolder}");
        }

        public void LogEvent(string eventName, string condition, string detail)
        {
            Append(eventsPath, string.Join(",",
                Csv(Timestamp()),
                Csv(ParticipantId),
                Csv(eventName),
                Csv(condition),
                Csv(detail)));
        }

        public void LogDepthTrial(
            VacCondition condition,
            string phase,
            int trial,
            float referenceDepthMeters,
            float depthDifferenceMeters,
            string closerSide,
            string response,
            bool correct,
            float reactionTimeSeconds)
        {
            Append(depthPath, string.Join(",",
                Csv(Timestamp()),
                Csv(ParticipantId),
                Csv(condition.ToString()),
                Csv(phase),
                trial.ToString(CultureInfo.InvariantCulture),
                F(referenceDepthMeters),
                F(depthDifferenceMeters),
                Csv(closerSide),
                Csv(response),
                correct ? "1" : "0",
                F(reactionTimeSeconds)));
        }

        public void LogTetrisSummary(
            VacCondition condition,
            TetrisSequenceId sequence,
            float durationSeconds,
            int score,
            int linesCleared,
            int piecesPlaced,
            float averagePlacementTimeSeconds,
            int topOuts)
        {
            float minutes = Mathf.Max(durationSeconds / 60f, 0.0001f);
            float linesPerMinute = linesCleared / minutes;

            Append(tetrisPath, string.Join(",",
                Csv(Timestamp()),
                Csv(ParticipantId),
                Csv(condition.ToString()),
                Csv(sequence.ToString()),
                F(durationSeconds),
                score.ToString(CultureInfo.InvariantCulture),
                linesCleared.ToString(CultureInfo.InvariantCulture),
                F(linesPerMinute),
                piecesPlaced.ToString(CultureInfo.InvariantCulture),
                F(averagePlacementTimeSeconds),
                topOuts.ToString(CultureInfo.InvariantCulture)));
        }

        private static string Timestamp()
        {
            return DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        }

        private static string F(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
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
            if (value.Contains(",") || value.Contains(""") || value.Contains("\n"))
                return """ + value.Replace(""", """") + """;
            return value;
        }

        private static string Sanitize(string value)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');
            return value.Trim();
        }
    }
}
