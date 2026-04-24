using UnityEngine;
using RemoteTuning.Core.Registry;
using RemoteTuning.Core.Models;
using System.Collections.Generic;

namespace RemoteTuning.Core.Persistence
{
    /// <summary>
    /// Persistence layer for Remote Tuning using PlayerPrefs.
    /// Saves and loads variable values automatically.
    /// </summary>
    public static class RemoteTuningPersistence
    {
        private const string PREFS_PREFIX = "RT_";
        private const string SAVED_KEYS_LIST = "RT_SavedKeys";

        /// <summary>
        /// Saves a variable value to PlayerPrefs.
        /// </summary>
        public static void SaveValue(string id, object value, ValueType valueType)
        {
            string key = PREFS_PREFIX + id;

            switch (valueType)
            {
                case ValueType.Float:
                    PlayerPrefs.SetFloat(key, System.Convert.ToSingle(value));
                    break;
                case ValueType.Int:
                    PlayerPrefs.SetInt(key, System.Convert.ToInt32(value));
                    break;
                case ValueType.Bool:
                    PlayerPrefs.SetInt(key, System.Convert.ToBoolean(value) ? 1 : 0);
                    break;
                case ValueType.String:
                case ValueType.Enum:
                    PlayerPrefs.SetString(key, value?.ToString() ?? "");
                    break;
            }

            AddToSavedKeys(id);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Loads a variable value from PlayerPrefs.
        /// Returns true if a saved value was found.
        /// </summary>
        public static bool LoadValue(string id, ValueType valueType, out object value)
        {
            string key = PREFS_PREFIX + id;
            value = null;

            if (!PlayerPrefs.HasKey(key))
            {
                return false;
            }

            switch (valueType)
            {
                case ValueType.Float:
                    value = PlayerPrefs.GetFloat(key);
                    break;
                case ValueType.Int:
                    value = PlayerPrefs.GetInt(key);
                    break;
                case ValueType.Bool:
                    value = PlayerPrefs.GetInt(key) == 1;
                    break;
                case ValueType.String:
                case ValueType.Enum:
                    value = PlayerPrefs.GetString(key);
                    break;
            }

            return true;
        }

        public static void SaveAll()
        {
            var variables = RemoteTuningRegistry.Instance.GetAllVariables();
            int count = 0;

            foreach (var variable in variables)
            {
                var currentValue = variable.GetValue();
                if (currentValue != null)
                {
                    SaveValue(variable.Id, currentValue, variable.Definition.valueType);
                    count++;
                }
            }

            Debug.Log($"[RT Persistence] Saved {count} values");
        }

        public static void LoadAll()
        {
            var variables = RemoteTuningRegistry.Instance.GetAllVariables();
            int count = 0;

            foreach (var variable in variables)
            {
                if (LoadValue(variable.Id, variable.Definition.valueType, out object savedValue))
                {
                    variable.SetValue(savedValue);
                    count++;
                }
            }

            Debug.Log($"[RT Persistence] Loaded {count} values");
        }

        public static void DeleteValue(string id)
        {
            string key = PREFS_PREFIX + id;
            if (PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.DeleteKey(key);
                RemoveFromSavedKeys(id);
                PlayerPrefs.Save();
                Debug.Log($"[RT Persistence] Deleted: {id}");
            }
        }

        public static void DeleteAll()
        {
            var savedKeys = GetSavedKeys();
            foreach (var id in savedKeys)
            {
                PlayerPrefs.DeleteKey(PREFS_PREFIX + id);
            }

            PlayerPrefs.DeleteKey(SAVED_KEYS_LIST);
            PlayerPrefs.Save();
            Debug.Log("[RT Persistence] All values deleted");
        }

        public static bool HasSavedValue(string id)
        {
            return PlayerPrefs.HasKey(PREFS_PREFIX + id);
        }

        private static void AddToSavedKeys(string id)
        {
            var keys = GetSavedKeys();
            if (!keys.Contains(id))
            {
                keys.Add(id);
                SaveKeysList(keys);
            }
        }

        private static void RemoveFromSavedKeys(string id)
        {
            var keys = GetSavedKeys();
            if (keys.Contains(id))
            {
                keys.Remove(id);
                SaveKeysList(keys);
            }
        }

        private static List<string> GetSavedKeys()
        {
            string keysJson = PlayerPrefs.GetString(SAVED_KEYS_LIST, "");
            if (string.IsNullOrEmpty(keysJson))
            {
                return new List<string>();
            }

            try
            {
                var wrapper = JsonUtility.FromJson<StringListWrapper>(keysJson);
                return new List<string>(wrapper.items);
            }
            catch
            {
                return new List<string>();
            }
        }

        private static void SaveKeysList(List<string> keys)
        {
            var wrapper = new StringListWrapper { items = keys.ToArray() };
            string json = JsonUtility.ToJson(wrapper);
            PlayerPrefs.SetString(SAVED_KEYS_LIST, json);
        }

        [System.Serializable]
        private class StringListWrapper
        {
            public string[] items;
        }
    }
}
