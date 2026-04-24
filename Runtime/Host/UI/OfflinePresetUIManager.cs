using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RemoteTuning.Core.Presets;
using RemoteTuning.Core.Registry;

namespace RemoteTuning.Host.UI
{
    /// <summary>
    /// Manages three preset profile slots for offline host tuning.
    /// Reads and writes values directly through RemoteTuningRegistry,
    /// without requiring a remote client connection.
    /// </summary>
    public class OfflinePresetUIManager : MonoBehaviour
    {
        #region DATA_AND_FIELDS

        [Header("References")]
        [SerializeField] private OfflineUIBuilder uiBuilder;

        [Header("Profile Buttons")]
        [SerializeField] private Button profile1Button;
        [SerializeField] private Button profile2Button;
        [SerializeField] private Button profile3Button;

        [Header("Profile Labels")]
        [SerializeField] private TextMeshProUGUI profile1Text;
        [SerializeField] private TextMeshProUGUI profile2Text;
        [SerializeField] private TextMeshProUGUI profile3Text;

        [Header("Save Button")]
        [SerializeField] private Button saveCurrentButton;

        [Header("Visual State")]
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private Color activeColor = Color.green;
        [SerializeField] private Color savedColor = Color.yellow;

        private const string PROFILE_1_KEY = "OfflineProfile_1";
        private const string PROFILE_2_KEY = "OfflineProfile_2";
        private const string PROFILE_3_KEY = "OfflineProfile_3";
        private const string ACTIVE_PROFILE_KEY = "OfflineActiveProfile";

        // 0 = none active, 1/2/3 = active profile index
        private int _activeProfile;
        private Dictionary<string, PresetData> _cachedProfiles = new Dictionary<string, PresetData>();

        #endregion

        #region LIFECYCLE

        private void Start()
        {
            if (uiBuilder == null)
            {
                if (!TryGetComponent(out uiBuilder))
                {
                    uiBuilder = FindObjectOfType<OfflineUIBuilder>();
                }
            }

            SetupButtons();
            LoadProfileStates();
            UpdateUI();
        }

        #endregion

        #region METHODS

        /// <summary>
        /// Loads the given profile slot (1-3) into the registry.
        /// Auto-saves the current profile before switching.
        /// </summary>
        public void LoadProfile(int profileNumber)
        {
            if (profileNumber < 1 || profileNumber > 3)
            {
                Debug.LogError($"[OfflinePresetUIManager] Invalid profile number: {profileNumber}");
                return;
            }

            StartCoroutine(LoadProfileCoroutine(profileNumber));
        }

        /// <summary>
        /// Saves the current registry values into the active profile slot.
        /// Falls back to Profile 1 if no profile is currently active.
        /// </summary>
        public void SaveCurrentProfile()
        {
            int profileToSave = _activeProfile > 0 ? _activeProfile : 1;

            if (_activeProfile == 0)
            {
                Debug.LogWarning("[OfflinePresetUIManager] No active profile, saving to Profile 1 by default");
            }

            SaveProfileInternal(profileToSave);
        }

        /// <summary>
        /// Saves the current registry values into the specified profile slot.
        /// </summary>
        public void SaveToProfile(int profileNumber)
        {
            if (profileNumber < 1 || profileNumber > 3)
            {
                Debug.LogError($"[OfflinePresetUIManager] Invalid profile number: {profileNumber}");
                return;
            }

            SaveProfileInternal(profileNumber);
        }

        /// <summary>
        /// Deletes the specified profile slot from persistence.
        /// </summary>
        public void ResetProfile(int profileNumber)
        {
            if (profileNumber < 1 || profileNumber > 3)
            {
                return;
            }

            string profileKey = GetProfileKey(profileNumber);
            PlayerPrefs.DeleteKey(profileKey);
            PlayerPrefs.Save();

            if (_cachedProfiles.ContainsKey(profileKey))
            {
                _cachedProfiles.Remove(profileKey);
            }

            if (_activeProfile == profileNumber)
            {
                _activeProfile = 0;
                PlayerPrefs.SetInt(ACTIVE_PROFILE_KEY, 0);
                PlayerPrefs.Save();
            }

            UpdateUI();
        }

        /// <summary>
        /// Deletes all three profile slots from persistence.
        /// </summary>
        public void ResetAllProfiles()
        {
            for (int i = 1; i <= 3; i++)
            {
                ResetProfile(i);
            }
        }

        private void SetupButtons()
        {
            if (profile1Button != null)
            {
                profile1Button.onClick.AddListener(() => LoadProfile(1));
            }

            if (profile2Button != null)
            {
                profile2Button.onClick.AddListener(() => LoadProfile(2));
            }

            if (profile3Button != null)
            {
                profile3Button.onClick.AddListener(() => LoadProfile(3));
            }

            if (saveCurrentButton != null)
            {
                saveCurrentButton.onClick.AddListener(SaveCurrentProfile);
            }
        }

        private IEnumerator LoadProfileCoroutine(int profileNumber)
        {
            // Auto-save the current active profile before switching to the new one
            if (_activeProfile > 0 && _activeProfile != profileNumber)
            {
                Debug.Log($"[OfflinePresetUIManager] Auto-saving Profile {_activeProfile} before switching to Profile {profileNumber}");
                SaveProfileInternal(_activeProfile);
            }

            string profileKey = GetProfileKey(profileNumber);

            if (!PlayerPrefs.HasKey(profileKey))
            {
                Debug.LogWarning($"[OfflinePresetUIManager] Profile {profileNumber} not found. Setting as active without applying values.");
                _activeProfile = profileNumber;
                PlayerPrefs.SetInt(ACTIVE_PROFILE_KEY, _activeProfile);
                PlayerPrefs.Save();
                UpdateUI();
                yield break;
            }

            string json = PlayerPrefs.GetString(profileKey);
            var wrapper = JsonUtility.FromJson<PresetManager.PresetWrapper>(json);
            var preset = wrapper.ToPresetData();

            // Apply values to registry - this calls the actual property setters
            ApplyPresetToRegistry(preset);

            _activeProfile = profileNumber;
            PlayerPrefs.SetInt(ACTIVE_PROFILE_KEY, _activeProfile);
            PlayerPrefs.Save();

            // Wait one frame so the registry setters have propagated
            yield return null;

            if (uiBuilder != null)
            {
                uiBuilder.RefreshUIValues();
            }
            else
            {
                Debug.LogWarning("[OfflinePresetUIManager] OfflineUIBuilder is null. UI values were not refreshed.");
            }

            UpdateUI();
            Debug.Log($"[OfflinePresetUIManager] Profile {profileNumber} loaded ({preset.values.Count} values applied)");
        }

        private void SaveProfileInternal(int profileNumber)
        {
            var preset = new PresetData(
                $"OfflineProfile {profileNumber}",
                $"Offline host profile #{profileNumber}"
            );

            foreach (var variable in RemoteTuningRegistry.Instance.GetAllVariables())
            {
                object currentValue = variable.GetValue();
                if (currentValue != null)
                {
                    preset.values[variable.Id] = currentValue;
                }
            }

            string profileKey = GetProfileKey(profileNumber);
            var wrapper = new PresetManager.PresetWrapper(preset);
            string json = JsonUtility.ToJson(wrapper);

            PlayerPrefs.SetString(profileKey, json);
            PlayerPrefs.Save();

            _cachedProfiles[profileKey] = preset;

            _activeProfile = profileNumber;
            PlayerPrefs.SetInt(ACTIVE_PROFILE_KEY, _activeProfile);
            PlayerPrefs.Save();

            UpdateUI();
            Debug.Log($"[OfflinePresetUIManager] Profile {profileNumber} saved ({preset.values.Count} values)");
        }

        private void ApplyPresetToRegistry(PresetData preset)
        {
            foreach (var kvp in preset.values)
            {
                // GetVariable returns null if not found - safe to call SetValue only when found
                var variable = RemoteTuningRegistry.Instance.GetVariable(kvp.Key);
                if (variable != null)
                {
                    variable.SetValue(kvp.Value);
                }
            }
        }

        private void LoadProfileStates()
        {
            _activeProfile = PlayerPrefs.GetInt(ACTIVE_PROFILE_KEY, 0);

            for (int i = 1; i <= 3; i++)
            {
                string profileKey = GetProfileKey(i);
                if (PlayerPrefs.HasKey(profileKey))
                {
                    string json = PlayerPrefs.GetString(profileKey);
                    var wrapper = JsonUtility.FromJson<PresetManager.PresetWrapper>(json);
                    _cachedProfiles[profileKey] = wrapper.ToPresetData();
                }
            }
        }

        private void UpdateUI()
        {
            UpdateProfileButton(1, profile1Button, profile1Text);
            UpdateProfileButton(2, profile2Button, profile2Text);
            UpdateProfileButton(3, profile3Button, profile3Text);
        }

        private void UpdateProfileButton(int profileNumber, Button button, TextMeshProUGUI label)
        {
            if (button == null)
            {
                return;
            }

            string profileKey = GetProfileKey(profileNumber);
            bool exists = PlayerPrefs.HasKey(profileKey);
            bool isActive = _activeProfile == profileNumber;

            var colors = button.colors;
            colors.normalColor = isActive ? activeColor : (exists ? savedColor : defaultColor);
            button.colors = colors;

            if (label != null)
            {
                string status = isActive ? "[ACTIVE]" : (exists ? "[Saved]" : "[Empty]");
                label.text = $"Profile {profileNumber} {status}";
            }
        }

        private string GetProfileKey(int profileNumber)
        {
            switch (profileNumber)
            {
                case 1: return PROFILE_1_KEY;
                case 2: return PROFILE_2_KEY;
                case 3: return PROFILE_3_KEY;
                default: return null;
            }
        }

        #endregion
    }
}

