using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RemoteTuning.Core.Models;
using RemoteTuning.Client.Connection;

namespace RemoteTuning.Client.UI
{
    /// <summary>
    /// Generates dynamic UI based on the schema received from the server
    /// Creates controls (sliders, toggles, dropdowns) at runtime
    /// </summary>
    public class DynamicUIBuilder : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RemoteTuningClient client;
        [SerializeField] private Transform contentParent;
        [SerializeField] private ScrollRect scrollRect;

        [Header("Prefabs")]
        [SerializeField] private GameObject sliderPrefab;
        [SerializeField] private GameObject togglePrefab;
        [SerializeField] private GameObject dropdownPrefab;

        [Header("Settings")]
        [SerializeField] private float controlSpacing = 10f;

        private Dictionary<string, GameObject> _controlWidgets = new Dictionary<string, GameObject>();
        private RemoteTuningSchema _currentSchema;

        private void Start()
        {
            if (client == null)
            {
                client = FindObjectOfType<RemoteTuningClient>();
            }

            if (client != null)
            {
                client.OnSchemaReceived += BuildUI;
            }
        }

        /// <summary>
        /// Builds the UI based on the schema
        /// </summary>
        public void BuildUI(RemoteTuningSchema schema)
        {
            if (schema == null || schema.controls == null)
            {
                Debug.LogWarning("[DynamicUIBuilder] Schema is null or empty");
                return;
            }
            _currentSchema = schema;
            ClearUI();
            foreach (var control in schema.controls)
            {
                CreateControlWidget(control);
            }
            Debug.Log($"[DynamicUIBuilder] UI built: {_controlWidgets.Count} controls");
        }

        private void CreateControlWidget(ControlDefinition control)
        {
            GameObject widget = null;

            switch (control.controlType)
            {
                case ControlType.Slider:
                    widget = CreateSliderWidget(control);
                    break;
                case ControlType.Toggle:
                    widget = CreateToggleWidget(control);
                    break;
                case ControlType.Dropdown:
                    widget = CreateDropdownWidget(control);
                    break;
                case ControlType.InputField:
                    widget = CreateInputFieldWidget(control);
                    break;
                default:
                    Debug.LogWarning($"[DynamicUIBuilder] Unsupported control type: {control.controlType}");
                    break;
            }

            if (widget != null)
            {
                _controlWidgets[control.id] = widget;
            }
        }

        private GameObject CreateSliderWidget(ControlDefinition control)
        {
            if (sliderPrefab == null)
            {
                Debug.LogError("[DynamicUIBuilder] Slider prefab not assigned!");
                return null;
            }

            var widget = Instantiate(sliderPrefab, contentParent);

            // Configurar label - buscar TextMeshProUGUI primero, luego Text
            TextMeshProUGUI tmpLabel = null;
            Text regularLabel = null;

            // Buscar todos los TextMeshProUGUI
            var tmpLabels = widget.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var label in tmpLabels)
            {
                if (label.gameObject.name.ToLower().Contains("label") || 
                    label.gameObject.name.ToLower().Contains("text"))
                {
                    tmpLabel = label;
                    break;
                }
            }

            // Si no hay TMP, buscar Text regular
            if (tmpLabel == null)
            {
                var regularLabels = widget.GetComponentsInChildren<Text>();
                foreach (var label in regularLabels)
                {
                    if (label.gameObject.name.ToLower().Contains("label") || 
                        label.gameObject.name.ToLower().Contains("text"))
                    {
                        regularLabel = label;
                        break;
                    }
                }
            }

            // Establecer el texto inicial
            string labelText = $"{control.label}: {control.GetCurrentValue()}";
            if (tmpLabel != null)
            {
                tmpLabel.text = labelText;
            }
            else if (regularLabel != null)
            {
                regularLabel.text = labelText;
            }
            else
            {
                Debug.LogWarning($"[DynamicUIBuilder] No label found for slider: {control.id}");
            }

            // Configurar slider
            var slider = widget.GetComponentInChildren<Slider>();
            if (slider != null)
            {
                slider.minValue = control.minValue;
                slider.maxValue = control.maxValue;

                if (control.valueType == ValueType.Float)
                {
                    slider.value = control.floatValue;
                }
                else if (control.valueType == ValueType.Int)
                {
                    slider.value = control.intValue;
                    slider.wholeNumbers = true;
                }

                // Agregar listener
                slider.onValueChanged.AddListener((value) =>
                {
                    OnSliderChanged(control, value, tmpLabel, regularLabel);
                });
            }

            return widget;
        }

        private GameObject CreateToggleWidget(ControlDefinition control)
        {
            if (togglePrefab == null)
            {
                Debug.LogError("[DynamicUIBuilder] Toggle prefab not assigned!");
                return null;
            }

            var widget = Instantiate(togglePrefab, contentParent);

            // Configurar label - buscar TextMeshProUGUI primero, luego Text
            TextMeshProUGUI tmpLabel = null;
            Text regularLabel = null;

            // Buscar todos los TextMeshProUGUI
            var tmpLabels = widget.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var label in tmpLabels)
            {
                if (label.gameObject.name.ToLower().Contains("label") || 
                    label.gameObject.name.ToLower().Contains("text"))
                {
                    tmpLabel = label;
                    break;
                }
            }

            // Si no hay TMP, buscar Text regular
            if (tmpLabel == null)
            {
                var regularLabels = widget.GetComponentsInChildren<Text>();
                foreach (var label in regularLabels)
                {
                    if (label.gameObject.name.ToLower().Contains("label") || 
                        label.gameObject.name.ToLower().Contains("text"))
                    {
                        regularLabel = label;
                        break;
                    }
                }
            }

            // Establecer el texto
            if (tmpLabel != null)
            {
                tmpLabel.text = control.label;
            }
            else if (regularLabel != null)
            {
                regularLabel.text = control.label;
            }
            else
            {
                Debug.LogWarning($"[DynamicUIBuilder] No label found for toggle: {control.id}");
            }

            // Configurar toggle
            var toggle = widget.GetComponentInChildren<Toggle>();
            if (toggle != null)
            {
                toggle.isOn = control.boolValue;

                toggle.onValueChanged.AddListener((value) =>
                {
                    OnToggleChanged(control, value);
                });
            }

            return widget;
        }

        private GameObject CreateDropdownWidget(ControlDefinition control)
        {
            if (dropdownPrefab == null)
            {
                Debug.LogError("[DynamicUIBuilder] Dropdown prefab not assigned!");
                return null;
            }

            var widget = Instantiate(dropdownPrefab, contentParent);

            // Configurar label - buscar TextMeshProUGUI primero, luego Text
            // IMPORTANTE: No confundir con el label del dropdown mismo
            TextMeshProUGUI tmpLabel = null;
            Text regularLabel = null;

            // Buscar todos los TextMeshProUGUI que NO sean parte del dropdown
            var tmpLabels = widget.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var label in tmpLabels)
            {
                // Ignorar el label del dropdown (hijo del dropdown)
                var dropdown = label.GetComponentInParent<Dropdown>();
                var tmpDropdown = label.GetComponentInParent<TMP_Dropdown>();
                
                if (dropdown == null && tmpDropdown == null && 
                    (label.gameObject.name.ToLower().Contains("label") || 
                     label.gameObject.name.ToLower().Contains("text")))
                {
                    tmpLabel = label;
                    break;
                }
            }

            // Si no hay TMP, buscar Text regular
            if (tmpLabel == null)
            {
                var regularLabels = widget.GetComponentsInChildren<Text>();
                foreach (var label in regularLabels)
                {
                    var dropdown = label.GetComponentInParent<Dropdown>();
                    
                    if (dropdown == null && 
                        (label.gameObject.name.ToLower().Contains("label") || 
                         label.gameObject.name.ToLower().Contains("text")))
                    {
                        regularLabel = label;
                        break;
                    }
                }
            }

            // Establecer el texto
            if (tmpLabel != null)
            {
                tmpLabel.text = control.label;
            }
            else if (regularLabel != null)
            {
                regularLabel.text = control.label;
            }
            else
            {
                Debug.LogWarning($"[DynamicUIBuilder] No label found for dropdown: {control.id}");
            }

            // Configurar dropdown - buscar TMP_Dropdown primero, luego Dropdown regular
            var tmpDropdownComp = widget.GetComponentInChildren<TMP_Dropdown>();
            var dropdownComp = widget.GetComponentInChildren<Dropdown>();

            if (tmpDropdownComp != null && control.options != null && control.options.Length > 0)
            {
                tmpDropdownComp.ClearOptions();
                tmpDropdownComp.AddOptions(new List<string>(control.options));

                int currentIndex = System.Array.IndexOf(control.options, control.stringValue);
                if (currentIndex >= 0)
                {
                    tmpDropdownComp.value = currentIndex;
                }

                tmpDropdownComp.onValueChanged.AddListener((index) =>
                {
                    string selectedValue = control.options[index];
                    OnDropdownChanged(control, selectedValue);
                });
            }
            else if (dropdownComp != null && control.options != null && control.options.Length > 0)
            {
                dropdownComp.ClearOptions();
                dropdownComp.AddOptions(new List<string>(control.options));

                int currentIndex = System.Array.IndexOf(control.options, control.stringValue);
                if (currentIndex >= 0)
                {
                    dropdownComp.value = currentIndex;
                }

                dropdownComp.onValueChanged.AddListener((index) =>
                {
                    string selectedValue = control.options[index];
                    OnDropdownChanged(control, selectedValue);
                });
            }
            else
            {
                Debug.LogWarning($"[DynamicUIBuilder] No dropdown or options found for: {control.id}");
            }

            return widget;
        }

        private GameObject CreateInputFieldWidget(ControlDefinition control)
        {
            // Similar a slider pero con InputField
            // Por ahora simplificado
            Debug.LogWarning($"[DynamicUIBuilder] InputField not implemented yet for: {control.id}");
            return null;
        }

        private void OnSliderChanged(ControlDefinition control, float value, TextMeshProUGUI tmpLabel, Text regularLabel)
        {
            // CRÍTICO: Actualizar el valor en el control para que SaveProfile lo lea correctamente
            if (control.valueType == ValueType.Int)
            {
                control.intValue = (int)value;
            }
            else
            {
                control.floatValue = value;
            }

            // Actualizar label
            string labelText = "";
            if (control.valueType == ValueType.Int)
            {
                labelText = $"{control.label}: {(int)value}";
            }
            else
            {
                labelText = $"{control.label}: {value:F2}";
            }

            if (tmpLabel != null)
            {
                tmpLabel.text = labelText;
            }
            else if (regularLabel != null)
            {
                regularLabel.text = labelText;
            }

            // Enviar al servidor
            if (client != null && client.IsConnected)
            {
                if (control.valueType == ValueType.Int)
                {
                    client.SendValueChange(control.id, (int)value, ValueType.Int);
                }
                else
                {
                    client.SendValueChange(control.id, value, ValueType.Float);
                }
            }
        }

        private void OnToggleChanged(ControlDefinition control, bool value)
        {
            // CRÍTICO: Actualizar el valor en el control para que SaveProfile lo lea correctamente
            control.boolValue = value;

            if (client != null && client.IsConnected)
            {
                client.SendValueChange(control.id, value, ValueType.Bool);
            }
        }

        private void OnDropdownChanged(ControlDefinition control, string value)
        {
            // CRÍTICO: Actualizar el valor en el control PRIMERO
            control.stringValue = value;
            
            if (client != null && client.IsConnected)
            {
                client.SendValueChange(control.id, value, ValueType.Enum);
            }
        }

        /// <summary>
        /// Actualiza los valores de la UI con los valores actuales del schema
        /// IMPORTANTE: Llama esto después de modificar los valores del schema
        /// para reflejar los cambios en los controles visuales
        /// </summary>
        public void RefreshUIValues()
        {
            if (_currentSchema == null || _currentSchema.controls == null)
            {
                Debug.LogError("[DynamicUIBuilder] No schema to refresh UI from");
                return;
            }

            foreach (var control in _currentSchema.controls)
            {
                if (!_controlWidgets.ContainsKey(control.id)) continue;
                var widget = _controlWidgets[control.id];
                if (widget == null) continue;

                switch (control.controlType)
                {
                    case ControlType.Slider:
                        UpdateSliderWidget(widget, control);
                        break;
                    case ControlType.Toggle:
                        UpdateToggleWidget(widget, control);
                        break;
                    case ControlType.Dropdown:
                        UpdateDropdownWidget(widget, control);
                        break;
                    case ControlType.InputField:
                        UpdateInputFieldWidget(widget, control);
                        break;
                }
            }
        }

        /// <summary>
        /// DEBUG: Fuerza la actualización de un control específico
        /// </summary>
        public void ForceUpdateControl(string controlId)
        {
            if (_currentSchema == null || !_controlWidgets.ContainsKey(controlId))
            {
                Debug.LogError($"[DynamicUIBuilder] Cannot update control: {controlId}");
                return;
            }

            var control = System.Array.Find(_currentSchema.controls, c => c.id == controlId);
            if (control == null)
            {
                Debug.LogError($"[DynamicUIBuilder] Control {controlId} not found in schema");
                return;
            }

            var widget = _controlWidgets[controlId];
            switch (control.controlType)
            {
                case ControlType.Slider:
                    UpdateSliderWidget(widget, control);
                    break;
                case ControlType.Toggle:
                    UpdateToggleWidget(widget, control);
                    break;
                case ControlType.Dropdown:
                    UpdateDropdownWidget(widget, control);
                    break;
            }
        }

        private bool UpdateSliderWidget(GameObject widget, ControlDefinition control)
        {
            var slider = widget.GetComponentInChildren<Slider>();
            if (slider == null)
            {
                Debug.LogWarning($"[DynamicUIBuilder] Slider component not found for: {control.id}");
                return false;
            }

            // Buscar el label antes de remover listeners
            TextMeshProUGUI tmpLabel = null;
            Text regularLabel = null;

            var tmpLabels = widget.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var label in tmpLabels)
            {
                if (label.gameObject.name.ToLower().Contains("label") || 
                    label.gameObject.name.ToLower().Contains("text"))
                {
                    tmpLabel = label;
                    break;
                }
            }

            if (tmpLabel == null)
            {
                var regularLabels = widget.GetComponentsInChildren<Text>();
                foreach (var label in regularLabels)
                {
                    if (label.gameObject.name.ToLower().Contains("label") || 
                        label.gameObject.name.ToLower().Contains("text"))
                    {
                        regularLabel = label;
                        break;
                    }
                }
            }

            // Remover listener temporalmente para evitar loops infinitos
            slider.onValueChanged.RemoveAllListeners();

            // Calcular nuevo valor
            float newValue = 0f;
            if (control.valueType == ValueType.Float)
            {
                newValue = control.floatValue;
            }
            else if (control.valueType == ValueType.Int)
            {
                newValue = control.intValue;
            }
            
            // Verificar rango del slider
            if (newValue < slider.minValue || newValue > slider.maxValue)
            {
                Debug.LogWarning($"[DynamicUIBuilder] Value {newValue} out of range [{slider.minValue}, {slider.maxValue}] for slider '{control.id}'");
                newValue = Mathf.Clamp(newValue, slider.minValue, slider.maxValue);
            }
            
            float oldValue = slider.value;
            slider.value = newValue;
            slider.SetValueWithoutNotify(newValue);
            
            string labelText = $"{control.label}: {control.GetCurrentValue()}";
            if (tmpLabel != null)
            {
                tmpLabel.text = labelText;
            }
            else if (regularLabel != null)
            {
                regularLabel.text = labelText;
            }

            Debug.Log($"[DynamicUIBuilder] Slider '{control.id}' updated: {oldValue} -> {slider.value}");

            // Re-agregar listener
            slider.onValueChanged.AddListener((value) =>
            {
                OnSliderChanged(control, value, tmpLabel, regularLabel);
            });

            return true;
        }

        private bool UpdateToggleWidget(GameObject widget, ControlDefinition control)
        {
            var toggle = widget.GetComponentInChildren<Toggle>();
            if (toggle == null)
                return false;

            // Remover listener temporalmente
            toggle.onValueChanged.RemoveAllListeners();

            // Actualizar valor
            toggle.isOn = control.boolValue;

            // Re-agregar listener
            toggle.onValueChanged.AddListener((value) =>
            {
                OnToggleChanged(control, value);
            });

            return true;
        }

        private bool UpdateDropdownWidget(GameObject widget, ControlDefinition control)
        {
            var dropdown = widget.GetComponentInChildren<TMP_Dropdown>();
            if (dropdown == null)
                return false;

            dropdown.onValueChanged.RemoveAllListeners();

            int selectedIndex = 0;
            if (control.options != null && control.stringValue != null)
            {
                for (int i = 0; i < control.options.Length; i++)
                {
                    if (control.options[i] == control.stringValue)
                    {
                        selectedIndex = i;
                        break;
                    }
                }
            }

            dropdown.SetValueWithoutNotify(selectedIndex);
            dropdown.RefreshShownValue();

            dropdown.onValueChanged.AddListener((index) =>
            {
                if (index >= 0 && index < control.options.Length)
                {
                    OnDropdownChanged(control, control.options[index]);
                }
            });

            return true;
        }

        private bool UpdateInputFieldWidget(GameObject widget, ControlDefinition control)
        {
            var inputField = widget.GetComponentInChildren<TMP_InputField>();
            if (inputField == null)
                return false;

            // Remover listener temporalmente
            inputField.onEndEdit.RemoveAllListeners();

            // Actualizar valor
            inputField.text = control.stringValue ?? "";

            // Re-agregar listener
            inputField.onEndEdit.AddListener((value) =>
            {
                if (client != null && client.IsConnected)
                {
                    client.SendValueChange(control.id, value, ValueType.String);
                }
            });

            return true;
        }

        /// <summary>
        /// Callback cuando se actualizan valores desde el servidor
        /// </summary>
        private void OnValuesUpdated()
        {
            RefreshUIValues();
        }

        /// <summary>
        /// Limpia toda la UI generada
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

            // También limpiar cualquier hijo del contentParent
            foreach (Transform child in contentParent)
            {
                Destroy(child.gameObject);
            }
        }

        private void OnDestroy()
        {
            if (client != null)
            {
                client.OnSchemaReceived -= BuildUI;
                client.OnValuesUpdated -= OnValuesUpdated;
            }
        }
    }
}

