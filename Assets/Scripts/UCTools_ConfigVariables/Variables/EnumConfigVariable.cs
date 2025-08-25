using System;
using UnityEngine;

namespace UCTools_ConfigVariables
{
    /// <summary>
    /// Configuration variable for enum values with dropdown support
    /// </summary>
    [System.Serializable]
    public class EnumConfigVariable<T> : ConfigVariableBase where T : System.Enum
    {
        [Header("Enum Settings")]
        [SerializeField] private T defaultValue;
        [SerializeField] private T currentValue;
        
        public override Type ValueType => typeof(T);
        
        /// <summary>Current enum value</summary>
        public T Value
        {
            get => currentValue;
            set
            {
                if (!currentValue.Equals(value))
                {
                    currentValue = value;
                    MarkChanged();
                }
            }
        }
        
        /// <summary>Current value as integer (for UI binding)</summary>
        public int IntValue
        {
            get => Convert.ToInt32(currentValue);
            set => Value = (T)Enum.ToObject(typeof(T), value);
        }
        
        public T DefaultValue => defaultValue;
        
        public override void ResetToDefault()
        {
            Value = defaultValue;
        }
        
        public override bool ValidateConfiguration(out string error)
        {
            error = "";
            
            if (string.IsNullOrEmpty(name))
            {
                error = "Name cannot be empty";
                return false;
            }
            
            if (!typeof(T).IsEnum)
            {
                error = $"Type {typeof(T).Name} is not an enum";
                return false;
            }
            
            return true;
        }
        
        public override object GetValueAsObject() => Value;
        
        public override bool SetValueFromObject(object value)
        {
            if (value is T enumValue)
            {
                Value = enumValue;
                return true;
            }
            else if (value is int intValue)
            {
                try
                {
                    Value = (T)Enum.ToObject(typeof(T), intValue);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            else if (value is string stringValue)
            {
                try
                {
                    Value = (T)Enum.Parse(typeof(T), stringValue, true);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }
        
        public override string GetValueAsString() => Value.ToString();
        
        public override bool SetValueFromString(string value)
        {
            try
            {
                Value = (T)Enum.Parse(typeof(T), value, true);
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        // Constructor
        public EnumConfigVariable(string name, string description, T defaultValue, ConfigFlags flags = ConfigFlags.None)
        {
            this.name = name;
            this.description = description;
            this.defaultValue = defaultValue;
            this.currentValue = defaultValue;
            this.flags = flags;
            
            if (!ValidateConfiguration(out string error))
            {
                Debug.LogError($"[EnumConfigVariable] Invalid configuration: {error}");
            }
        }
        
        public EnumConfigVariable() 
        {
            defaultValue = default(T);
            currentValue = default(T);
        }
    }

}
