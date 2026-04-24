using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RemoteTuning.Core.Presets;
using RemoteTuning.Core.Models;
using RemoteTuning.Client.Connection;

namespace RemoteTuning.Client.UI
{
    public class PresetUIManager : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private RemoteTuningClient client;
        [SerializeField] private DynamicUIBuilder uiBuilder;
        
        [Header("Botones de Perfiles")]
        [SerializeField] private Button profile1Button;
        [SerializeField] private Button profile2Button;
        [SerializeField] private Button profile3Button;
        
        [Header("Textos de Perfiles")]
        [SerializeField] private TextMeshProUGUI profile1Text;
        [SerializeField] private TextMeshProUGUI profile2Text;
        [SerializeField] private TextMeshProUGUI profile3Text;
        
        [Header("Botón Guardar")]
        [SerializeField] private Button saveCurrentButton;
        
        [Header("Estado Visual")]
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private Color activeColor = Color.green;
        [SerializeField] private Color modifiedColor = Color.yellow;
        
        // Profile keys
        private const string PROFILE_1_KEY = "ClientProfile_1";
        private const string PROFILE_2_KEY = "ClientProfile_2";
        private const string PROFILE_3_KEY = "ClientProfile_3";
        private const string ACTIVE_PROFILE_KEY = "ActiveProfile";
        
        private int _activeProfile; // 0 = none, 1/2/3 = active profile
        private Dictionary<string, PresetData> _cachedProfiles = new Dictionary<string, PresetData>();

        private void Start()
        {
            SetupButtons();
            LoadProfileStates();
            UpdateUI();
            
            if (client == null)
            {
                client = FindObjectOfType<RemoteTuningClient>();
            }
            
            if (client != null)
            {
                client.OnSchemaReceived += OnSchemaReceived;
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

        public void LoadProfile(int profileNumber)
        {
            if (profileNumber < 1 || profileNumber > 3)
            {
                Debug.LogError($"[PresetUIManager] Invalid profile number: {profileNumber}");
                return;
            }
            
            StartCoroutine(LoadProfileCoroutine(profileNumber));
        }

        private System.Collections.IEnumerator LoadProfileCoroutine(int profileNumber)
        {
            if (_activeProfile > 0 && _activeProfile != profileNumber && client != null && client.Schema != null)
            {
                Debug.Log($"[PresetUIManager] Auto-saving Profile {_activeProfile} before switching to Profile {profileNumber}");
                SaveProfileInternal(_activeProfile);
            }

            string profileKey = GetProfileKey(profileNumber);

            if (!PlayerPrefs.HasKey(profileKey))
            {
                Debug.LogWarning($"[PresetUIManager] Profile {profileNumber} not found. Creating empty.");
                _activeProfile = profileNumber;
                PlayerPrefs.SetInt(ACTIVE_PROFILE_KEY, _activeProfile);
                PlayerPrefs.Save();
                UpdateUI();
                yield break;
            }

            string json = PlayerPrefs.GetString(profileKey);
            var wrapper = JsonUtility.FromJson<PresetManager.PresetWrapper>(json);
            var preset = wrapper.ToPresetData();

            _activeProfile = profileNumber;
            PlayerPrefs.SetInt(ACTIVE_PROFILE_KEY, _activeProfile);
            PlayerPrefs.Save();

            ApplyPresetLocally(preset);

            yield return null;

            if (uiBuilder != null)
            {
                uiBuilder.RefreshUIValues();
            }
            else
            {
                Debug.LogError("[PresetUIManager] DynamicUIBuilder not found");
            }

            yield return null;

            if (client != null && client.IsConnected && client.Schema != null)
            {
                SendPresetToServer(preset);
            }

            UpdateUI();

            Debug.Log($"[PresetUIManager] Profile {profileNumber} loaded");
        }

        /// <summary>
        /// Saves the current values from the client schema as the active profile.
        /// If no profile is active, defaults to Profile 1.
        /// </summary>
        public void SaveCurrentProfile()
        {
            int profileToSave = _activeProfile;

            if (profileToSave == 0)
            {
                profileToSave = 1;
                Debug.LogWarning("[PresetUIManager] No active profile, saving to Profile 1 by default");
            }

            SaveProfileInternal(profileToSave);
        }

        /// <summary>
        /// Saves the current client schema values into the specified profile slot.
        /// </summary>
        public void SaveToProfile(int profileNumber)
        {
            if (profileNumber < 1 || profileNumber > 3)
            {
                Debug.LogError($"[PresetUIManager] Invalid profile number: {profileNumber}");
                return;
            }
            
            SaveProfileInternal(profileNumber);
        }

        /// <summary>
        /// Saves the current client schema values into the specified profile slot.
        /// </summary>
        private void SaveProfileInternal(int profileNumber)
        {
            if (profileNumber < 1 || profileNumber > 3)
            {
                Debug.LogError($"[PresetUIManager] Invalid profile number: {profileNumber}");
                return;
            }

            if (client == null || client.Schema == null)
            {
                Debug.LogError("[PresetUIManager] Cannot save: client has no schema loaded.");
                return;
            }

            var preset = new PresetData($"Profile {profileNumber}", $"Client profile #{profileNumber}");

            foreach (var control in client.Schema.controls)
            {
                preset.values[control.id] = control.GetCurrentValue();
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

            Debug.Log($"[PresetUIManager] Profile {profileNumber} saved ({preset.values.Count} values)");
        }

        private void ApplyPresetLocally(PresetData preset)
        {
            if (client == null || client.Schema == null)
            {
                Debug.LogError("[PresetUIManager] Cannot apply preset: client or schema is null");
                return;
            }

            foreach (var kvp in preset.values)
            {
                for (int i = 0; i < client.Schema.controls.Length; i++)
                {
                    if (client.Schema.controls[i].id == kvp.Key)
                    {
                        client.Schema.controls[i].SetValue(kvp.Value);
                        break;
                    }
                }
            }
        }

        private void SendPresetToServer(PresetData preset)
        {
            if (client == null || !client.IsConnected || client.Schema == null)
            {
                Debug.LogError("[PresetUIManager] Cannot send to server: client not connected");
                return;
            }

            foreach (var kvp in preset.values)
            {
                ControlDefinition control = null;
                foreach (var ctrl in client.Schema.controls)
                {
                    if (ctrl.id == kvp.Key)
                    {
                        control = ctrl;
                        break;
                    }
                }

                if (control != null)
                {
                    client.SendValueChange(kvp.Key, kvp.Value, control.valueType);
                }
            }
        }

        private void UpdateUI()
        {
            UpdateProfileButton(1, profile1Button, profile1Text);
            UpdateProfileButton(2, profile2Button, profile2Text);
            UpdateProfileButton(3, profile3Button, profile3Text);
        }

        private void UpdateProfileButton(int profileNumber, Button button, TextMeshProUGUI text)
        {
            if (button == null) return;
            
            string profileKey = GetProfileKey(profileNumber);
            bool exists = PlayerPrefs.HasKey(profileKey);
            bool isActive = _activeProfile == profileNumber;
            
            // Update color
            var colors = button.colors;
            if (isActive)
            {
                colors.normalColor = activeColor;
            }
            else if (exists)
            {
                colors.normalColor = modifiedColor;
            }
            else
            {
                colors.normalColor = defaultColor;
            }
            button.colors = colors;
            
            // Update text
            if (text != null)
            {
                string statusText;
                if (isActive)
                    statusText = " [ACTIVE]";
                else if (exists)
                    statusText = " [Saved]";
                else
                    statusText = " [Empty]";

                text.text = $"Profile {profileNumber}{statusText}";
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

        /// <summary>
        /// When the schema is received, just update the UI.
        /// The active profile is not loaded automatically to avoid confusion.
        /// </summary>
        private void OnSchemaReceived(RemoteTuningSchema schema)
        {
            UpdateUI();
        }

        /// <summary>
        /// Deletes a profile slot from PlayerPrefs.
        /// </summary>
        public void ResetProfile(int profileNumber)
        {
            if (profileNumber < 1 || profileNumber > 3)
                return;

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
        /// Deletes all three profile slots from PlayerPrefs.
        /// </summary>
        public void ResetAllProfiles()
        {
            for (int i = 1; i <= 3; i++)
            {
                ResetProfile(i);
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

        private void OnDestroy()
        {
            if (client != null)
            {
                client.OnSchemaReceived -= OnSchemaReceived;
            }
        }
    }
}
