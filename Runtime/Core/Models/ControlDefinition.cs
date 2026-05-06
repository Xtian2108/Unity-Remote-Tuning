using System;
using UnityEngine;

namespace RemoteTuning.Core.Models
{
    /// <summary>
    /// Individual control definition compatible with JsonUtility.
    /// Uses separate fields per type (instead of List) for JsonUtility compatibility.
    /// </summary>
    [Serializable]
    public class ControlDefinition
    {
        // Identity
        public string id;                    // e.g. "ball.kickForce"
        public string label;                 // e.g. "Kick Force"
        
        // Type
        public ControlType controlType;
        public ValueType valueType;
        
        // Values (separate fields per type for JsonUtility)
        public float floatValue;
        public int intValue;
        public bool boolValue;
        public string stringValue;
        
        // Constraints
        public float minValue;
        public float maxValue;
        public float step;

        // Slider-specific options
        /// <summary>
        /// When true, the slider will only allow whole number values.
        /// Automatically set to true for Int value types; can also be enabled for Float sliders.
        /// </summary>
        public bool wholeNumbers;
        
        // Options for Dropdown/Enum (array instead of List)
        public string[] options;
        
        // Optional metadata
        public string tooltip;
        public bool readOnly;

        public ControlDefinition()
        {
            // Default values
            minValue = 0f;
            maxValue = 100f;
            step = 1f;
            readOnly = false;
        }
        
        public object GetCurrentValue()
        {
            switch (valueType)
            {
                case ValueType.Float:
                    return floatValue;
                case ValueType.Int:
                    return intValue;
                case ValueType.Bool:
                    return boolValue;
                case ValueType.String:
                case ValueType.Enum:
                    return stringValue;
                default:
                    return null;
            }
        }
        
        public void SetCurrentValue(object value)
        {
            switch (valueType)
            {
                case ValueType.Float:
                    floatValue = Convert.ToSingle(value);
                    break;
                case ValueType.Int:
                    intValue = Convert.ToInt32(value);
                    break;
                case ValueType.Bool:
                    boolValue = Convert.ToBoolean(value);
                    break;
                case ValueType.String:
                case ValueType.Enum:
                    stringValue = value?.ToString();
                    break;
            }
        }
        
        /// <summary>
        /// Sets the value from a string (useful for loading from presets).
        /// </summary>
        public void SetValue(object value)
        {
            if (value == null)
                return;
                
            string stringVal = value.ToString();
            
            switch (valueType)
            {
                case ValueType.Float:
                    if (float.TryParse(stringVal, out float f))
                        floatValue = f;
                    break;
                case ValueType.Int:
                    if (int.TryParse(stringVal, out int i))
                        intValue = i;
                    break;
                case ValueType.Bool:
                    if (bool.TryParse(stringVal, out bool b))
                        boolValue = b;
                    break;
                case ValueType.String:
                case ValueType.Enum:
                    stringValue = stringVal;
                    break;
            }
        }
    }
}
