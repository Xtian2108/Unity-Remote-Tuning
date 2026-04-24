using System;
using RemoteTuning.Core.Models;
namespace RemoteTuning.Core.Registry
{
    /// <summary>
    /// Represents a registered variable with getter, setter, and metadata.
    /// </summary>
    public class RegisteredVariable
    {
        public string Id { get; private set; }
        public ControlDefinition Definition { get; private set; }
        public object DefaultValue { get; private set; }
        private Func<object> _getter;
        private Action<object> _setter;
        public event Action<object> OnValueChanged;
        
        public RegisteredVariable(string id, ControlDefinition definition, Func<object> getter, Action<object> setter, object customDefaultValue = null)
        {
            Id = id;
            Definition = definition;
            _getter = getter;
            _setter = setter;
            
            // Use custom default value if provided, otherwise use current value
            DefaultValue = customDefaultValue ?? getter?.Invoke();
        }
        
        public object GetValue()
        {
            return _getter?.Invoke();
        }
        public void SetValue(object value)
        {
            _setter?.Invoke(value);
            OnValueChanged?.Invoke(value);
        }
        /// <summary>
        /// Syncs the definition's stored value from the getter.
        /// </summary>
        public void UpdateDefinitionValue()
        {
            object currentValue = GetValue();
            if (currentValue != null)
            {
                Definition.SetCurrentValue(currentValue);
            }
        }

        public void ResetToDefault()
        {
            if (DefaultValue != null)
            {
                SetValue(DefaultValue);
            }
        }

        /// <summary>
        /// Returns true if the current value differs from the default.
        /// </summary>
        public bool IsModified()
        {
            var currentValue = GetValue();
            if (currentValue == null && DefaultValue == null)
                return false;
            if (currentValue == null || DefaultValue == null)
                return true;
            
            return !currentValue.ToString().Equals(DefaultValue.ToString());
        }
    }
}
