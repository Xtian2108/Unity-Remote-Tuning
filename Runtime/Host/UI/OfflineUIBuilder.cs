using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RemoteTuning.Core.Models;
using RemoteTuning.Core.Registry;
using RemoteTuning.Host.Server;

namespace RemoteTuning.Host.UI
{
    /// <summary>
    /// Builds and manages a dynamic UI for editing registered variables offline,
    /// directly from the host without requiring a remote client connection.
    /// Changes are applied immediately via RemoteTuningRegistry setters.
    /// If a RemoteTuningHost is available, changes are also broadcast to connected clients.
    /// </summary>
    public class OfflineUIBuilder : MonoBehaviour
    {
        #region DATA_AND_FIELDS

        [Header("References")]
        [SerializeField] private Transform contentParent;
        [SerializeField] private ScrollRect scrollRect;
        /// <summary>
        /// Optional reference to the host. If set, value changes are broadcast to connected clients.
        /// </summary>
        [SerializeField] private RemoteTuningHost host;

        [Header("Prefabs")]
        [SerializeField] private GameObject sliderPrefab;
        [SerializeField] private GameObject togglePrefab;
        [SerializeField] private GameObject dropdownPrefab;

        [Header("Settings")]
        [SerializeField] private bool buildOnStart = true;
        /// <summary>
        /// If true, rebuilds the entire UI whenever a variable is added or removed from the registry.
        /// Disable this if variables are registered at startup and never change.
        /// </summary>
        [SerializeField] private bool rebuildOnRegistryChange = false;

        private Dictionary<string, GameObject> _controlWidgets = new Dictionary<string, GameObject>();

        #endregion

        #region LIFECYCLE

        private void Start()
        {
            if (host == null)
            {
                if (!TryGetComponent(out host))
                {
                    host = FindObjectOfType<RemoteTuningHost>();
                }
            }

            if (rebuildOnRegistryChange)
            {
                RemoteTuningRegistry.Instance.OnRegistryChanged += OnRegistryChanged;
            }

            if (buildOnStart)
            {
                BuildUI();
            }
        }

        private void OnDestroy()
        {
            if (rebuildOnRegistryChange)
            {
                RemoteTuningRegistry.Instance.OnRegistryChanged -= OnRegistryChanged;
            }
        }

        #endregion

        #region METHODS

        /// <summary>
        /// Builds the UI from all currently registered variables.
        /// Clears any previously created widgets before rebuilding.
        /// </summary>
        public void BuildUI()
        {
            ClearUI();

            int count = 0;

            foreach (var variable in RemoteTuningRegistry.Instance.GetAllVariables())
            {
                // Sync definition values from the actual getters before building widgets
                variable.UpdateDefinitionValue();
                CreateControlWidget(variable);
                count++;
            }

            Debug.Log($"[OfflineUIBuilder] UI built with {count} controls");
        }

        /// <summary>
        /// Refreshes all existing widget values from the current registry state
        /// without destroying and recreating the widgets.
        /// Call this after applying a preset.
        /// </summary>
        public void RefreshUIValues()
        {
            foreach (var variable in RemoteTuningRegistry.Instance.GetAllVariables())
            {
                if (!_controlWidgets.TryGetValue(variable.Id, out var widget) || widget == null)
                {
                    continue;
                }

                // Sync definition values from the actual getter
                variable.UpdateDefinitionValue();

                switch (variable.Definition.controlType)
                {
                    case ControlType.Slider:
                        UpdateSliderWidget(widget, variable.Definition, variable.Id);
                        break;
                    case ControlType.Toggle:
                        UpdateToggleWidget(widget, variable.Definition, variable.Id);
                        break;
                    case ControlType.Dropdown:
                        UpdateDropdownWidget(widget, variable.Definition, variable.Id);
                        break;
                }
            }
        }

        /// <summary>
        /// Destroys all generated UI widgets and clears the widget dictionary.
        /// </summary>
        public void ClearUI()
        {
            foreach (var widget in _controlWidgets.Values)
            {
                if (widget != null)
                {
                    Destroy(widget);
                }
            }

            _controlWidgets.Clear();

            if (contentParent != null)
            {
                foreach (Transform child in contentParent)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        private void CreateControlWidget(RegisteredVariable variable)
        {
            GameObject widget = null;

            switch (variable.Definition.controlType)
            {
                case ControlType.Slider:
                    widget = CreateSliderWidget(variable);
                    break;
                case ControlType.Toggle:
                    widget = CreateToggleWidget(variable);
                    break;
                case ControlType.Dropdown:
                    widget = CreateDropdownWidget(variable);
                    break;
                default:
                    Debug.LogWarning($"[OfflineUIBuilder] Unsupported control type '{variable.Definition.controlType}' for '{variable.Id}'");
                    break;
            }

            if (widget != null)
            {
                _controlWidgets[variable.Id] = widget;
            }
        }

        private GameObject CreateSliderWidget(RegisteredVariable variable)
        {
            if (sliderPrefab == null)
            {
                Debug.LogError("[OfflineUIBuilder] Slider prefab not assigned!");
                return null;
            }

            var widget = Instantiate(sliderPrefab, contentParent);
            var definition = variable.Definition;

            FindLabels(widget, out TextMeshProUGUI tmpLabel, out Text regularLabel);

            float currentValue = definition.valueType == ValueType.Int
                ? definition.intValue
                : definition.floatValue;

            SetLabelText(tmpLabel, regularLabel, $"{definition.label}: {definition.GetCurrentValue()}");

            var slider = widget.GetComponentInChildren<Slider>();
            if (slider != null)
            {
                slider.minValue    = definition.minValue;
                slider.maxValue    = definition.maxValue;
                slider.wholeNumbers = definition.wholeNumbers;
                slider.SetValueWithoutNotify(Mathf.Clamp(currentValue, slider.minValue, slider.maxValue));

                slider.onValueChanged.AddListener((value) =>
                {
                    OnSliderChanged(variable, value, tmpLabel, regularLabel);
                });
            }

            return widget;
        }

        private GameObject CreateToggleWidget(RegisteredVariable variable)
        {
            if (togglePrefab == null)
            {
                Debug.LogError("[OfflineUIBuilder] Toggle prefab not assigned!");
                return null;
            }

            var widget = Instantiate(togglePrefab, contentParent);
            var definition = variable.Definition;

            FindLabels(widget, out TextMeshProUGUI tmpLabel, out Text regularLabel);
            SetLabelText(tmpLabel, regularLabel, definition.label);

            var toggle = widget.GetComponentInChildren<Toggle>();
            if (toggle != null)
            {
                toggle.SetIsOnWithoutNotify(definition.boolValue);
                toggle.onValueChanged.AddListener((value) =>
                {
                    OnToggleChanged(variable, value);
                });
            }

            return widget;
        }

        private GameObject CreateDropdownWidget(RegisteredVariable variable)
        {
            if (dropdownPrefab == null)
            {
                Debug.LogError("[OfflineUIBuilder] Dropdown prefab not assigned!");
                return null;
            }

            var widget = Instantiate(dropdownPrefab, contentParent);
            var definition = variable.Definition;

            FindDropdownLabel(widget, out TextMeshProUGUI tmpLabel, out Text regularLabel);
            SetLabelText(tmpLabel, regularLabel, definition.label);

            var tmpDropdown = widget.GetComponentInChildren<TMP_Dropdown>();
            var regularDropdown = widget.GetComponentInChildren<Dropdown>();

            if (tmpDropdown != null && definition.options != null && definition.options.Length > 0)
            {
                tmpDropdown.ClearOptions();
                tmpDropdown.AddOptions(new List<string>(definition.options));

                int currentIndex = System.Array.IndexOf(definition.options, definition.stringValue);
                tmpDropdown.SetValueWithoutNotify(currentIndex >= 0 ? currentIndex : 0);

                tmpDropdown.onValueChanged.AddListener((index) =>
                {
                    if (index >= 0 && index < definition.options.Length)
                    {
                        OnDropdownChanged(variable, definition.options[index]);
                    }
                });
            }
            else if (regularDropdown != null && definition.options != null && definition.options.Length > 0)
            {
                regularDropdown.ClearOptions();
                regularDropdown.AddOptions(new List<string>(definition.options));

                int currentIndex = System.Array.IndexOf(definition.options, definition.stringValue);
                regularDropdown.SetValueWithoutNotify(currentIndex >= 0 ? currentIndex : 0);

                regularDropdown.onValueChanged.AddListener((index) =>
                {
                    if (index >= 0 && index < definition.options.Length)
                    {
                        OnDropdownChanged(variable, definition.options[index]);
                    }
                });
            }

            return widget;
        }

        // ---- Value change handlers ----

        private void OnSliderChanged(RegisteredVariable variable, float value, TextMeshProUGUI tmpLabel, Text regularLabel)
        {
            if (variable.Definition.valueType == ValueType.Int)
            {
                int intValue = (int)value;
                variable.Definition.intValue = intValue;
                RemoteTuningRegistry.Instance.SetValue(variable.Id, intValue);
                SetLabelText(tmpLabel, regularLabel, $"{variable.Definition.label}: {intValue}");
            }
            else
            {
                variable.Definition.floatValue = value;
                RemoteTuningRegistry.Instance.SetValue(variable.Id, value);
                SetLabelText(tmpLabel, regularLabel, $"{variable.Definition.label}: {value:F2}");
            }

            BroadcastChange(variable);
        }

        private void OnToggleChanged(RegisteredVariable variable, bool value)
        {
            variable.Definition.boolValue = value;
            RemoteTuningRegistry.Instance.SetValue(variable.Id, value);
            BroadcastChange(variable);
        }

        private void OnDropdownChanged(RegisteredVariable variable, string value)
        {
            variable.Definition.stringValue = value;
            RemoteTuningRegistry.Instance.SetValue(variable.Id, value);
            BroadcastChange(variable);
        }

        // ---- Widget update methods (used by RefreshUIValues) ----

        private void UpdateSliderWidget(GameObject widget, ControlDefinition definition, string variableId)
        {
            var slider = widget.GetComponentInChildren<Slider>();
            if (slider == null)
            {
                return;
            }

            FindLabels(widget, out TextMeshProUGUI tmpLabel, out Text regularLabel);

            slider.onValueChanged.RemoveAllListeners();

            float newValue = definition.valueType == ValueType.Int ? definition.intValue : definition.floatValue;
            slider.SetValueWithoutNotify(Mathf.Clamp(newValue, slider.minValue, slider.maxValue));

            SetLabelText(tmpLabel, regularLabel, $"{definition.label}: {definition.GetCurrentValue()}");

            var variable = RemoteTuningRegistry.Instance.GetVariable(variableId);
            if (variable != null)
            {
                slider.onValueChanged.AddListener((value) =>
                {
                    OnSliderChanged(variable, value, tmpLabel, regularLabel);
                });
            }
        }

        private void UpdateToggleWidget(GameObject widget, ControlDefinition definition, string variableId)
        {
            var toggle = widget.GetComponentInChildren<Toggle>();
            if (toggle == null)
            {
                return;
            }

            toggle.onValueChanged.RemoveAllListeners();
            toggle.SetIsOnWithoutNotify(definition.boolValue);

            var variable = RemoteTuningRegistry.Instance.GetVariable(variableId);
            if (variable != null)
            {
                toggle.onValueChanged.AddListener((value) =>
                {
                    OnToggleChanged(variable, value);
                });
            }
        }

        private void UpdateDropdownWidget(GameObject widget, ControlDefinition definition, string variableId)
        {
            var dropdown = widget.GetComponentInChildren<TMP_Dropdown>();
            if (dropdown == null)
            {
                return;
            }

            dropdown.onValueChanged.RemoveAllListeners();

            int selectedIndex = 0;
            if (definition.options != null && definition.stringValue != null)
            {
                for (int i = 0; i < definition.options.Length; i++)
                {
                    if (definition.options[i] == definition.stringValue)
                    {
                        selectedIndex = i;
                        break;
                    }
                }
            }

            dropdown.SetValueWithoutNotify(selectedIndex);
            dropdown.RefreshShownValue();

            var variable = RemoteTuningRegistry.Instance.GetVariable(variableId);
            if (variable != null)
            {
                dropdown.onValueChanged.AddListener((index) =>
                {
                    if (index >= 0 && index < definition.options.Length)
                    {
                        OnDropdownChanged(variable, definition.options[index]);
                    }
                });
            }
        }

        // ---- Broadcast helper ----

        /// <summary>
        /// Sends the updated variable value to all connected remote clients.
        /// Only runs if a RemoteTuningHost reference is set and it is running.
        /// </summary>
        private void BroadcastChange(RegisteredVariable variable)
        {
            if (host == null || !host.IsRunning)
            {
                return;
            }

            host.BroadcastVariableChange(variable.Id, variable.GetValue(), variable.Definition.valueType);
        }

        // ---- Label helpers ----

        private static void FindLabels(GameObject widget, out TextMeshProUGUI tmpLabel, out Text regularLabel)
        {
            tmpLabel = null;
            regularLabel = null;

            foreach (var label in widget.GetComponentsInChildren<TextMeshProUGUI>())
            {
                string nameLower = label.gameObject.name.ToLower();
                if (nameLower.Contains("label") || nameLower.Contains("text"))
                {
                    tmpLabel = label;
                    return;
                }
            }

            foreach (var label in widget.GetComponentsInChildren<Text>())
            {
                string nameLower = label.gameObject.name.ToLower();
                if (nameLower.Contains("label") || nameLower.Contains("text"))
                {
                    regularLabel = label;
                    return;
                }
            }
        }

        private static void FindDropdownLabel(GameObject widget, out TextMeshProUGUI tmpLabel, out Text regularLabel)
        {
            tmpLabel = null;
            regularLabel = null;

            foreach (var label in widget.GetComponentsInChildren<TextMeshProUGUI>())
            {
                string nameLower = label.gameObject.name.ToLower();
                bool isInsideDropdown = label.GetComponentInParent<Dropdown>() != null
                    || label.GetComponentInParent<TMP_Dropdown>() != null;

                if (!isInsideDropdown && (nameLower.Contains("label") || nameLower.Contains("text")))
                {
                    tmpLabel = label;
                    return;
                }
            }

            foreach (var label in widget.GetComponentsInChildren<Text>())
            {
                string nameLower = label.gameObject.name.ToLower();
                bool isInsideDropdown = label.GetComponentInParent<Dropdown>() != null;

                if (!isInsideDropdown && (nameLower.Contains("label") || nameLower.Contains("text")))
                {
                    regularLabel = label;
                    return;
                }
            }
        }

        private static void SetLabelText(TextMeshProUGUI tmpLabel, Text regularLabel, string text)
        {
            if (tmpLabel != null)
            {
                tmpLabel.text = text;
            }
            else if (regularLabel != null)
            {
                regularLabel.text = text;
            }
        }

        private void OnRegistryChanged()
        {
            BuildUI();
        }

        #endregion
    }
}

