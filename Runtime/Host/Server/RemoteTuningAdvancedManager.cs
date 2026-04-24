using System.Linq;
using UnityEngine;
using RemoteTuning.Core.Registry;
using RemoteTuning.Core.Persistence;
using RemoteTuning.Core.Presets;
using RemoteTuning.Core.History;

namespace RemoteTuning.Host.Server
{
    /// <summary>
    /// Advanced Remote Tuning manager.
    /// Integrates persistence, presets, history, search and stats.
    /// </summary>
    public class RemoteTuningAdvancedManager : MonoBehaviour
    {
        [Header("Persistence Settings")]
        [SerializeField] private bool autoLoad = true;
        [SerializeField] private bool autoSaveOnChange = true;
        [SerializeField] private bool saveOnApplicationQuit = true;

        [Header("History Settings")]
        [SerializeField] private bool enableHistory = true;
        [SerializeField] private int maxHistorySize = 100;
        [SerializeField] private bool logChangesToConsole = true;

        [Header("Preset Settings")]
        [SerializeField] private bool autoSaveDefaultPreset = true;
        [SerializeField] private string defaultPresetName = "Default";

        [Header("Status - Read Only")]
        [SerializeField] private int valoresGuardados = 0;
        [SerializeField] private int valoresCargados = 0;
        [SerializeField] private int variablesRegistradas = 0;
        [SerializeField] private int variablesModificadas = 0;
        [SerializeField] private int entradasHistorial = 0;
        [SerializeField] private int presetsGuardados = 0;

        private void Start()
        {
            InitializeManagers();
            
            if (autoLoad)
            {
                LoadAll();
            }

            if (autoSaveOnChange)
            {
                SubscribeToChanges();
            }

            if (enableHistory)
            {
                StartHistoryTracking();
            }

            if (autoSaveDefaultPreset)
            {
                SaveDefaultPreset();
            }

            UpdateStats();
        }

        private void InitializeManagers()
        {
            if (enableHistory)
            {
                HistoryManager.Instance.SetMaxHistorySize(maxHistorySize);
                
                if (logChangesToConsole)
                {
                    HistoryManager.Instance.OnHistoryEntryAdded += (entry) =>
                    {
                        Debug.Log($"[Change] {entry}");
                    };
                }
            }

            PresetManager.OnPresetsChanged += UpdateStats;

            Debug.Log("[AdvancedManager] Initialized");
        }

        private void SubscribeToChanges()
        {
            var variables = RemoteTuningRegistry.Instance.GetAllVariables();
            
            foreach (var variable in variables)
            {
                variable.OnValueChanged += (value) =>
                {
                    RemoteTuningPersistence.SaveValue(variable.Id, value, variable.Definition.valueType);
                    valoresGuardados++;
                    UpdateStats();
                };
            }

            Debug.Log($"[AdvancedManager] Auto-save enabled for {variables.Count()} variables");
        }

        private void StartHistoryTracking()
        {
            HistoryManager.Instance.StartTracking();
        }

        #region Public Methods - Persistence

        /// <summary>
        /// Loads all saved values.
        /// </summary>
        public void LoadAll()
        {
            var variables = RemoteTuningRegistry.Instance.GetAllVariables();
            valoresCargados = 0;

            foreach (var variable in variables)
            {
                if (RemoteTuningPersistence.LoadValue(variable.Id, variable.Definition.valueType, out object savedValue))
                {
                    variable.SetValue(savedValue);
                    valoresCargados++;
                }
            }

            Debug.Log($"[AdvancedManager] Loaded {valoresCargados} values");
            UpdateStats();
        }

        /// <summary>
        /// Saves all current values.
        /// </summary>
        public void SaveAll()
        {
            RemoteTuningPersistence.SaveAll();
            Debug.Log("[AdvancedManager] All values saved");
            UpdateStats();
        }

        /// <summary>
        /// Deletes all saved values.
        /// </summary>
        public void DeleteAll()
        {
            RemoteTuningPersistence.DeleteAll();
            valoresGuardados = 0;
            valoresCargados = 0;
            Debug.Log("[AdvancedManager] All values deleted");
            UpdateStats();
        }

        #endregion

        #region Public Methods - Presets

        /// <summary>
        /// Saves the current state as a named preset.
        /// </summary>
        public bool SavePreset(string presetName, string description = "")
        {
            bool success = PresetManager.SavePreset(presetName, description);
            UpdateStats();
            return success;
        }

        /// <summary>
        /// Loads a preset by name.
        /// </summary>
        public bool LoadPreset(string presetName)
        {
            bool success = PresetManager.LoadPreset(presetName);
            UpdateStats();
            return success;
        }

        /// <summary>
        /// Deletes a preset by name.
        /// </summary>
        public bool DeletePreset(string presetName)
        {
            bool success = PresetManager.DeletePreset(presetName);
            UpdateStats();
            return success;
        }

        /// <summary>
        /// Saves the default preset with current values.
        /// </summary>
        public void SaveDefaultPreset()
        {
            SavePreset(defaultPresetName, "Default game values");
        }

        /// <summary>
        /// Logs all available presets to the console.
        /// </summary>
        public void ListPresets()
        {
            var presets = PresetManager.GetPresetNames();
            Debug.Log($"[Presets] Available ({presets.Count}):");
            foreach (var presetName in presets)
            {
                var info = PresetManager.GetPresetInfo(presetName);
                Debug.Log($"  {presetName} - {info.values.Count} values - {info.GetFormattedDate()}");
            }
        }

        #endregion

        #region Public Methods - Reset

        /// <summary>
        /// Resets all variables to their default values.
        /// </summary>
        public void ResetAllToDefaults()
        {
            RemoteTuningRegistry.Instance.ResetAllToDefaults();
            UpdateStats();
        }

        /// <summary>
        /// Resets a specific variable to its default value.
        /// </summary>
        public bool ResetVariable(string variableId)
        {
            bool success = RemoteTuningRegistry.Instance.ResetToDefault(variableId);
            UpdateStats();
            return success;
        }

        #endregion

        #region Public Methods - History

        /// <summary>
        /// Logs recent history entries to the console.
        /// </summary>
        public void ShowRecentHistory(int count = 10)
        {
            var recent = HistoryManager.Instance.GetRecentHistory(count);
            Debug.Log($"[History] Last {count} changes:");
            foreach (var entry in recent)
            {
                Debug.Log($"  {entry}");
            }
        }

        /// <summary>
        /// Logs history statistics to the console.
        /// </summary>
        public void ShowHistoryStats()
        {
            var stats = HistoryManager.Instance.GetStats();
            Debug.Log($"[History] Stats: {stats}");
        }

        /// <summary>
        /// Clears all history entries.
        /// </summary>
        public void ClearHistory()
        {
            HistoryManager.Instance.ClearHistory();
            UpdateStats();
        }

        #endregion

        #region Public Methods - Search

        /// <summary>
        /// Searches variables by query and logs results to the console.
        /// </summary>
        public void SearchAndPrint(string query)
        {
            var results = RemoteTuningRegistry.Instance.SearchVariables(query);
            int count = results.Count();
            Debug.Log($"[Search] '{query}' - {count} results:");
            foreach (var variable in results)
            {
                Debug.Log($"  {variable.Definition.label} ({variable.Id}) = {variable.GetValue()}");
            }
        }

        /// <summary>
        /// Logs all modified variables to the console.
        /// </summary>
        public void ShowModifiedVariables()
        {
            var modified = RemoteTuningRegistry.Instance.GetModifiedVariables();
            int count = modified.Count();
            Debug.Log($"[Modified] {count} modified variables:");
            foreach (var variable in modified)
            {
                Debug.Log($"  {variable.Definition.label}: {variable.DefaultValue} -> {variable.GetValue()}");
            }
        }

        #endregion

        #region Stats and Lifecycle

        private void UpdateStats()
        {
            variablesRegistradas = RemoteTuningRegistry.Instance.GetVariableCount();
            variablesModificadas = RemoteTuningRegistry.Instance.GetModifiedVariables().Count();
            entradasHistorial = HistoryManager.Instance.GetHistory().Count;
            presetsGuardados = PresetManager.GetPresetNames().Count;
        }

        private void Update()
        {
            // Update stats every second (for Inspector)
            if (Time.frameCount % 60 == 0)
            {
                UpdateStats();
            }
        }

        private void OnApplicationQuit()
        {
            if (saveOnApplicationQuit)
            {
                SaveAll();
            }
        }

        private void OnDestroy()
        {
            if (saveOnApplicationQuit)
            {
                SaveAll();
            }
        }

        #endregion

        #region Context Menu

        [ContextMenu("Save All")]
        private void ContextMenuSaveAll() => SaveAll();

        [ContextMenu("Load All")]
        private void ContextMenuLoadAll() => LoadAll();

        [ContextMenu("Delete All")]
        private void ContextMenuDeleteAll() => DeleteAll();

        [ContextMenu("Reset to Defaults")]
        private void ContextMenuResetAll() => ResetAllToDefaults();

        [ContextMenu("Save Preset 'Test'")]
        private void ContextMenuSaveTestPreset() => SavePreset("Test", "Test preset");

        [ContextMenu("List Presets")]
        private void ContextMenuListPresets() => ListPresets();

        [ContextMenu("Show History")]
        private void ContextMenuShowHistory() => ShowRecentHistory(10);

        [ContextMenu("Show Modified Variables")]
        private void ContextMenuShowModified() => ShowModifiedVariables();

        #endregion
    }
}
