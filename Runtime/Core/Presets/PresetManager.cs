using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RemoteTuning.Core.Registry;

namespace RemoteTuning.Core.Presets
{
    /// <summary>
    /// Manages saved configurations (presets) for Remote Tuning.
    /// </summary>
    public static class PresetManager
    {
        private const string PRESET_PREFIX = "RT_Preset_";
        private const string PRESET_LIST_KEY = "RT_PresetList";
        
        public static event Action OnPresetsChanged;

        /// <summary>
        /// Saves the current registry state as a named preset.
        /// </summary>
        public static bool SavePreset(string presetName, string description = "")
        {
            if (string.IsNullOrWhiteSpace(presetName))
            {
                Debug.LogError("[PresetManager] Preset name cannot be empty");
                return false;
            }

            var preset = new PresetData(presetName, description);
            var variables = RemoteTuningRegistry.Instance.GetAllVariables();

            foreach (var variable in variables)
            {
                var value = variable.GetValue();
                if (value != null)
                {
                    preset.values[variable.Id] = value;
                }
            }

            // Save preset
            string presetKey = PRESET_PREFIX + presetName;
            string json = JsonUtility.ToJson(new PresetWrapper(preset));
            PlayerPrefs.SetString(presetKey, json);
            
            // Update preset list
            AddToPresetList(presetName);
            PlayerPrefs.Save();

            Debug.Log($"[PresetManager] Preset saved: '{presetName}' ({preset.values.Count} values)");
            OnPresetsChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Loads and applies a preset by name.
        /// </summary>
        public static bool LoadPreset(string presetName)
        {
            string presetKey = PRESET_PREFIX + presetName;
            
            if (!PlayerPrefs.HasKey(presetKey))
            {
                Debug.LogWarning($"[PresetManager] Preset '{presetName}' not found");
                return false;
            }

            string json = PlayerPrefs.GetString(presetKey);
            var wrapper = JsonUtility.FromJson<PresetWrapper>(json);
            var preset = wrapper.ToPresetData();

            int loadedCount = 0;
            var variables = RemoteTuningRegistry.Instance.GetAllVariables().ToDictionary(v => v.Id);

            foreach (var kvp in preset.values)
            {
                if (variables.TryGetValue(kvp.Key, out var variable))
                {
                    variable.SetValue(kvp.Value);
                    loadedCount++;
                }
            }

            Debug.Log($"[PresetManager] Preset loaded: '{presetName}' - {loadedCount}/{preset.values.Count} values applied");
            return true;
        }

        /// <summary>
        /// Deletes a preset by name.
        /// </summary>
        public static bool DeletePreset(string presetName)
        {
            string presetKey = PRESET_PREFIX + presetName;
            
            if (!PlayerPrefs.HasKey(presetKey))
            {
                Debug.LogWarning($"[PresetManager] Preset '{presetName}' does not exist");
                return false;
            }

            PlayerPrefs.DeleteKey(presetKey);
            RemoveFromPresetList(presetName);
            PlayerPrefs.Save();

            Debug.Log($"[PresetManager] Preset deleted: '{presetName}'");
            OnPresetsChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Returns all saved preset names.
        /// </summary>
        public static List<string> GetPresetNames()
        {
            if (!PlayerPrefs.HasKey(PRESET_LIST_KEY))
            {
                return new List<string>();
            }

            string listJson = PlayerPrefs.GetString(PRESET_LIST_KEY);
            
            if (string.IsNullOrEmpty(listJson))
            {
                return new List<string>();
            }

            try
            {
                var result = JsonUtility.FromJson<StringList>(listJson);
                return result?.items ?? new List<string>();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PresetManager] Error loading preset list: {e.Message}. Creating new list.");
                return new List<string>();
            }
        }

        /// <summary>
        /// Returns preset data without loading it into the registry.
        /// </summary>
        public static PresetData GetPresetInfo(string presetName)
        {
            string presetKey = PRESET_PREFIX + presetName;
            
            if (!PlayerPrefs.HasKey(presetKey))
                return null;

            string json = PlayerPrefs.GetString(presetKey);
            var wrapper = JsonUtility.FromJson<PresetWrapper>(json);
            return wrapper.ToPresetData();
        }

        /// <summary>
        /// Exports a preset as a JSON string.
        /// </summary>
        public static string ExportPreset(string presetName)
        {
            var preset = GetPresetInfo(presetName);
            if (preset == null)
                return null;

            var wrapper = new PresetWrapper(preset);
            return JsonUtility.ToJson(wrapper, true);
        }

        /// <summary>
        /// Imports a preset from a JSON string.
        /// </summary>
        public static bool ImportPreset(string json, string newName = null)
        {
            try
            {
                var wrapper = JsonUtility.FromJson<PresetWrapper>(json);
                var preset = wrapper.ToPresetData();
                
                if (!string.IsNullOrEmpty(newName))
                {
                    preset.presetName = newName;
                }

                string presetKey = PRESET_PREFIX + preset.presetName;
                PlayerPrefs.SetString(presetKey, json);
                AddToPresetList(preset.presetName);
                PlayerPrefs.Save();

                Debug.Log($"[PresetManager] Preset imported: '{preset.presetName}'");
                OnPresetsChanged?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[PresetManager] Error importing preset: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Deletes all saved presets.
        /// </summary>
        public static void DeleteAllPresets()
        {
            var presets = GetPresetNames();
            foreach (var preset in presets)
            {
                string presetKey = PRESET_PREFIX + preset;
                PlayerPrefs.DeleteKey(presetKey);
            }
            
            PlayerPrefs.DeleteKey(PRESET_LIST_KEY);
            PlayerPrefs.Save();

            Debug.Log($"[PresetManager] All presets deleted ({presets.Count})");
            OnPresetsChanged?.Invoke();
        }

        #region Helper Methods

        private static void AddToPresetList(string presetName)
        {
            var list = GetPresetNames();
            if (!list.Contains(presetName))
            {
                list.Add(presetName);
                SavePresetList(list);
            }
        }

        private static void RemoveFromPresetList(string presetName)
        {
            var list = GetPresetNames();
            if (list.Remove(presetName))
            {
                SavePresetList(list);
            }
        }

        private static void SavePresetList(List<string> list)
        {
            var wrapper = new StringList { items = list };
            string json = JsonUtility.ToJson(wrapper);
            PlayerPrefs.SetString(PRESET_LIST_KEY, json);
        }

        #endregion

        #region Serialization Helpers

        [Serializable]
        private class StringList
        {
            public List<string> items = new List<string>();
        }

        [Serializable]
        public class PresetValue
        {
            public string key;
            public string value;
        }

        [Serializable]
        public class PresetWrapper
        {
            public string presetName;
            public string description;
            public long timestamp;
            public List<PresetValue> values = new List<PresetValue>();

            public PresetWrapper() { }

            public PresetWrapper(PresetData preset)
            {
                presetName = preset.presetName;
                description = preset.description;
                timestamp = preset.timestamp;

                foreach (var kvp in preset.values)
                {
                    values.Add(new PresetValue
                    {
                        key = kvp.Key,
                        value = kvp.Value?.ToString()
                    });
                }
            }

            public PresetData ToPresetData()
            {
                var preset = new PresetData(presetName, description);
                preset.timestamp = timestamp;

                foreach (var pv in values)
                {
                    preset.values[pv.key] = pv.value;
                }

                return preset;
            }
        }

        #endregion
    }
}
