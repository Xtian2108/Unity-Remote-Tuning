using System;
using UnityEngine;

namespace RemoteTuning.Core.History
{
    /// <summary>
    /// Represents a single change recorded in the history.
    /// </summary>
    [Serializable]
    public class HistoryEntry
    {
        public string variableId;
        public string variableLabel;
        public string oldValue;
        public string newValue;
        public long timestamp; // DateTime.Ticks
        public string source; // "Local", "Remote", "Preset", etc.

        public HistoryEntry() { }

        public HistoryEntry(string id, string label, object oldVal, object newVal, string src = "Unknown")
        {
            variableId = id;
            variableLabel = label;
            oldValue = oldVal?.ToString() ?? "null";
            newValue = newVal?.ToString() ?? "null";
            timestamp = DateTime.Now.Ticks;
            source = src;
        }

        public DateTime GetDateTime()
        {
            return new DateTime(timestamp);
        }

        public string GetFormattedTime()
        {
            var dt = GetDateTime();
            var diff = DateTime.Now - dt;

            if (diff.TotalSeconds < 60)
                return $"{(int)diff.TotalSeconds}s ago";
            if (diff.TotalMinutes < 60)
                return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24)
                return $"{(int)diff.TotalHours}h ago";
            
            return dt.ToString("HH:mm:ss");
        }

        public override string ToString()
        {
            return $"[{GetFormattedTime()}] {variableLabel}: {oldValue} -> {newValue}";
        }
    }
}
