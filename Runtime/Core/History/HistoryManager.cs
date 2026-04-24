using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RemoteTuning.Core.Registry;

namespace RemoteTuning.Core.History
{
    /// <summary>
    /// Manages the change history of variable values.
    /// </summary>
    public class HistoryManager
    {
        private static HistoryManager _instance;
        public static HistoryManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new HistoryManager();
                return _instance;
            }
        }

        private List<HistoryEntry> _history = new List<HistoryEntry>();
        private Dictionary<string, object> _previousValues = new Dictionary<string, object>();
        private int _maxHistorySize = 100;

        public event Action<HistoryEntry> OnHistoryEntryAdded;
        public event Action OnHistoryCleared;

        private HistoryManager() { }

        public void SetMaxHistorySize(int max)
        {
            _maxHistorySize = Mathf.Max(10, max);
            TrimHistory();
        }

        /// <summary>
        /// Subscribes to value changes for all registered variables.
        /// </summary>
        public void StartTracking()
        {
            var variables = RemoteTuningRegistry.Instance.GetAllVariables();
            
            foreach (var variable in variables)
            {
                _previousValues[variable.Id] = variable.GetValue();

                variable.OnValueChanged += (newValue) =>
                {
                    RecordChange(variable.Id, variable.Definition.label, newValue, "Remote");
                };
            }

            Debug.Log($"[HistoryManager] Tracking {variables.Count()} variables");
        }

        /// <summary>
        /// Records a value change in history. Only records if the value actually changed.
        /// </summary>
        public void RecordChange(string variableId, string label, object newValue, string source = "Unknown")
        {
            object oldValue = _previousValues.ContainsKey(variableId) 
                ? _previousValues[variableId] 
                : null;

            // Only record if value actually changed
            if (oldValue?.ToString() == newValue?.ToString())
                return;

            var entry = new HistoryEntry(variableId, label, oldValue, newValue, source);
            _history.Add(entry);
            _previousValues[variableId] = newValue;

            TrimHistory();
            OnHistoryEntryAdded?.Invoke(entry);
        }

        public List<HistoryEntry> GetHistory()
        {
            return new List<HistoryEntry>(_history);
        }

        public List<HistoryEntry> GetRecentHistory(int count = 10)
        {
            return _history.TakeLast(count).ToList();
        }

        public List<HistoryEntry> GetHistoryForVariable(string variableId)
        {
            return _history.Where(e => e.variableId == variableId).ToList();
        }

        public List<HistoryEntry> GetHistorySince(DateTime since)
        {
            long ticks = since.Ticks;
            return _history.Where(e => e.timestamp >= ticks).ToList();
        }

        public List<HistoryEntry> GetRecentChanges(int seconds = 60)
        {
            var since = DateTime.Now.AddSeconds(-seconds);
            return GetHistorySince(since);
        }

        public void ClearHistory()
        {
            _history.Clear();
            OnHistoryCleared?.Invoke();
            Debug.Log("[HistoryManager] History cleared");
        }

        public string ExportHistory()
        {
            var wrapper = new HistoryWrapper { entries = _history };
            return JsonUtility.ToJson(wrapper, true);
        }

        public HistoryStats GetStats()
        {
            return new HistoryStats
            {
                totalChanges = _history.Count,
                uniqueVariables = _history.Select(e => e.variableId).Distinct().Count(),
                oldestChange = _history.FirstOrDefault()?.GetDateTime(),
                newestChange = _history.LastOrDefault()?.GetDateTime(),
                changesLast24h = GetHistorySince(DateTime.Now.AddDays(-1)).Count,
                changesLastHour = GetHistorySince(DateTime.Now.AddHours(-1)).Count
            };
        }

        private void TrimHistory()
        {
            if (_history.Count > _maxHistorySize)
            {
                int toRemove = _history.Count - _maxHistorySize;
                _history.RemoveRange(0, toRemove);
            }
        }

        #region Serialization Helpers

        [Serializable]
        private class HistoryWrapper
        {
            public List<HistoryEntry> entries = new List<HistoryEntry>();
        }

        #endregion
    }

    /// <summary>
    /// History statistics.
    /// </summary>
    public class HistoryStats
    {
        public int totalChanges;
        public int uniqueVariables;
        public DateTime? oldestChange;
        public DateTime? newestChange;
        public int changesLast24h;
        public int changesLastHour;

        public override string ToString()
        {
            return $"Total: {totalChanges} | Variables: {uniqueVariables} | Last hour: {changesLastHour}";
        }
    }
}

