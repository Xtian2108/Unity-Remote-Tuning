using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RemoteTuning.Core.Models;
namespace RemoteTuning.Core.Registry
{
    /// <summary>
    /// Singleton that manages all variables exposed for Remote Tuning.
    /// </summary>
    public class RemoteTuningRegistry
    {
        private static RemoteTuningRegistry _instance;
        public static RemoteTuningRegistry Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new RemoteTuningRegistry();
                return _instance;
            }
        }
        private Dictionary<string, RegisteredVariable> _variables = new Dictionary<string, RegisteredVariable>();
        public event Action OnRegistryChanged;
        private RemoteTuningRegistry() { }
        /// <summary>
        /// Registers a float variable as a slider control.
        /// </summary>
        /// <param name="id">Unique identifier for the variable.</param>
        /// <param name="label">Display label shown in the UI.</param>
        /// <param name="getter">Function that returns the current float value.</param>
        /// <param name="setter">Action that applies a new float value.</param>
        /// <param name="min">Minimum slider value. Defaults to 0.</param>
        /// <param name="max">Maximum slider value. Defaults to 100.</param>
        /// <param name="step">Step increment. Defaults to 0.1.</param>
        /// <param name="defaultValue">Optional override for the reset default value.</param>
        /// <param name="wholeNumbers">
        /// When true, the slider will snap to whole number values even though the underlying type is float.
        /// Defaults to false.
        /// </param>
        public void RegisterFloat(string id, string label, Func<float> getter, Action<float> setter, 
            float min = 0f, float max = 100f, float step = 0.1f, float? defaultValue = null, bool wholeNumbers = false)
        {
            var definition = new ControlDefinition
            {
                id           = id,
                label        = label,
                controlType  = ControlType.Slider,
                valueType    = RemoteTuning.Core.Models.ValueType.Float,
                minValue     = min,
                maxValue     = max,
                step         = step,
                wholeNumbers = wholeNumbers
            };
            var variable = new RegisteredVariable(
                id,
                definition,
                () => getter(),
                (val) => setter(Convert.ToSingle(val)),
                defaultValue
            );
            _variables[id] = variable;
            OnRegistryChanged?.Invoke();
        }
        /// <summary>
        /// Registers an int variable as a slider control.
        /// </summary>
        /// <param name="id">Unique identifier for the variable.</param>
        /// <param name="label">Display label shown in the UI.</param>
        /// <param name="getter">Function that returns the current int value.</param>
        /// <param name="setter">Action that applies a new int value.</param>
        /// <param name="min">Minimum slider value. Defaults to 0.</param>
        /// <param name="max">Maximum slider value. Defaults to 100.</param>
        /// <param name="step">Step increment. Defaults to 1.</param>
        /// <param name="defaultValue">Optional override for the reset default value.</param>
        /// <param name="wholeNumbers">
        /// Whether the slider snaps to whole numbers. Defaults to true for int variables.
        /// Can be set to false only in exceptional cases where fractional display is desired
        /// while the underlying value is still stored as int.
        /// </param>
        public void RegisterInt(string id, string label, Func<int> getter, Action<int> setter, 
            int min = 0, int max = 100, int step = 1, int? defaultValue = null, bool wholeNumbers = true)
        {
            var definition = new ControlDefinition
            {
                id           = id,
                label        = label,
                controlType  = ControlType.Slider,
                valueType    = RemoteTuning.Core.Models.ValueType.Int,
                minValue     = min,
                maxValue     = max,
                step         = step,
                wholeNumbers = wholeNumbers
            };
            var variable = new RegisteredVariable(
                id,
                definition,
                () => getter(),
                (val) => setter(Convert.ToInt32(val)),
                defaultValue
            );
            _variables[id] = variable;
            OnRegistryChanged?.Invoke();
        }
        /// <summary>
        /// Registers a bool variable as a toggle control.
        /// </summary>
        public void RegisterBool(string id, string label, Func<bool> getter, Action<bool> setter, 
            bool? defaultValue = null)
        {
            var definition = new ControlDefinition
            {
                id = id,
                label = label,
                controlType = ControlType.Toggle,
                valueType = RemoteTuning.Core.Models.ValueType.Bool
            };
            var variable = new RegisteredVariable(
                id,
                definition,
                () => getter(),
                (val) => setter(Convert.ToBoolean(val)),
                defaultValue
            );
            _variables[id] = variable;
            OnRegistryChanged?.Invoke();
        }
        /// <summary>
        /// Registers an enum variable as a dropdown control.
        /// </summary>
        public void RegisterEnum(string id, string label, Func<string> getter, Action<string> setter, 
            string[] options)
        {
            var definition = new ControlDefinition
            {
                id          = id,
                label       = label,
                controlType = ControlType.Dropdown,
                valueType   = RemoteTuning.Core.Models.ValueType.Enum,
                options     = options
            };
            var variable = new RegisteredVariable(
                id,
                definition,
                () => getter(),
                (val) => setter(val?.ToString())
            );
            _variables[id] = variable;
            OnRegistryChanged?.Invoke();
        }
        /// <summary>
        /// Registers a string variable as an input field control.
        /// </summary>
        public void RegisterString(string id, string label, Func<string> getter, Action<string> setter)
        {
            var definition = new ControlDefinition
            {
                id = id,
                label = label,
                controlType = ControlType.InputField,
                valueType = RemoteTuning.Core.Models.ValueType.String
            };
            var variable = new RegisteredVariable(
                id,
                definition,
                () => getter(),
                (val) => setter(val?.ToString())
            );
            _variables[id] = variable;
            OnRegistryChanged?.Invoke();
        }
        public void Unregister(string id)
        {
            if (_variables.ContainsKey(id))
            {
                _variables.Remove(id);
                OnRegistryChanged?.Invoke();
            }
        }
        public RegisteredVariable GetVariable(string id)
        {
            return _variables.TryGetValue(id, out var variable) ? variable : null;
        }
        public IEnumerable<RegisteredVariable> GetAllVariables()
        {
            return _variables.Values;
        }
        public bool SetValue(string id, object value)
        {
            var variable = GetVariable(id);
            if (variable != null)
            {
                variable.SetValue(value);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Generates the full JSON schema from all registered variables.
        /// </summary>
        public RemoteTuningSchema GenerateSchema(string gameId, string gameName)
        {
            var schema = new RemoteTuningSchema
            {
                gameId = gameId,
                gameName = gameName
            };
            // Sync current values
            foreach (var variable in _variables.Values)
            {
                variable.UpdateDefinitionValue();
            }
            // Convert to array for JsonUtility
            schema.controls = _variables.Values
                .Select(v => v.Definition)
                .ToArray();
            return schema;
        }
        public void Clear()
        {
            _variables.Clear();
            OnRegistryChanged?.Invoke();
        }

        /// <summary>
        /// Resets a specific variable to its default value.
        /// </summary>
        public bool ResetToDefault(string id)
        {
            var variable = GetVariable(id);
            if (variable != null)
            {
                variable.ResetToDefault();
                Debug.Log($"[Registry] Variable '{id}' reset to default");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Resets all variables to their default values.
        /// </summary>
        public void ResetAllToDefaults()
        {
            int count = 0;
            foreach (var variable in _variables.Values)
            {
                variable.ResetToDefault();
                count++;
            }
            Debug.Log($"[Registry] {count} variables reset to defaults");
        }

        /// <summary>
        /// Searches variables by name or label.
        /// </summary>
        public IEnumerable<RegisteredVariable> SearchVariables(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return GetAllVariables();

            query = query.ToLower();
            return _variables.Values.Where(v => 
                v.Id.ToLower().Contains(query) || 
                v.Definition.label.ToLower().Contains(query));
        }


        /// <summary>
        /// Returns variables whose current value differs from their default.
        /// </summary>
        public IEnumerable<RegisteredVariable> GetModifiedVariables()
        {
            return _variables.Values.Where(v => v.IsModified());
        }

        public int GetVariableCount()
        {
            return _variables.Count;
        }
    }
}
