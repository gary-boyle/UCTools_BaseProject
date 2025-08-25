using System;
using UnityEngine;

namespace UCTools_ConfigVariables
{
    /// <summary>
    /// Integer configuration variable with optional min/max constraints
    /// </summary>
    [System.Serializable]
    public class IntConfigVariable : ConfigVariableBase
    {
        [Header("Int Settings")]
        [SerializeField] private int defaultValue = 0;
        [SerializeField] private int currentValue = 0;
        
        [Header("Constraints")]
        [SerializeField] private bool hasMinValue = false;
        [SerializeField] private int minValue = 0;
        [SerializeField] private bool hasMaxValue = false;
        [SerializeField] private int maxValue = 100;
        
        public override Type ValueType => typeof(int);
        
        /// <summary>Current integer value with constraint validation</summary>
        public int Value
        {
            get => currentValue;
            set
            {
                int clampedValue = value;
                
                if (hasMinValue && clampedValue < minValue)
                {
                    Debug.LogWarning($"[{name}] Value {clampedValue} below minimum {minValue}, clamping");
                    clampedValue = minValue;
                }
                if (hasMaxValue && clampedValue > maxValue)
                {
                    Debug.LogWarning($"[{name}] Value {clampedValue} above maximum {maxValue}, clamping");
                    clampedValue = maxValue;
                }
                
                if (currentValue != clampedValue)
                {
                    currentValue = clampedValue;
                    MarkChanged();
                }
            }
        }
        
        public int DefaultValue
        {
            get => defaultValue;
            set => defaultValue = value;
        }

        public int MinValue
        {
            get => minValue;
            set => minValue = value;
        }

        public int MaxValue
        {
            get => maxValue;
            set => maxValue = value;
        }

        public bool HasMinValue
        {
            get => hasMinValue;
            set => hasMinValue = value;
        }

        public bool HasMaxValue
        {
            get => hasMaxValue;
            set => hasMaxValue = value;
        }

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
            
            if (hasMinValue && hasMaxValue && minValue > maxValue)
            {
                error = "Min value cannot be greater than max value";
                return false;
            }
            
            // Validate default value is within constraints
            if (hasMinValue && defaultValue < minValue)
            {
                error = $"Default value {defaultValue} is below minimum {minValue}";
                return false;
            }
            
            if (hasMaxValue && defaultValue > maxValue)
            {
                error = $"Default value {defaultValue} is above maximum {maxValue}";
                return false;
            }
            
            return true;
        }
        
        public override object GetValueAsObject() => Value;
        
        public override bool SetValueFromObject(object value)
        {
            if (value is int intValue)
            {
                Value = intValue;
                return true;
            }
            return false;
        }
        
        public override string GetValueAsString() => Value.ToString();
        
        public override bool SetValueFromString(string value)
        {
            if (int.TryParse(value, out int result))
            {
                Value = result;
                return true;
            }
            return false;
        }
        
        // Constructor with constraints
        public IntConfigVariable(string name, string description, int defaultValue, 
            ConfigFlags flags = ConfigFlags.None, int? minValue = null, int? maxValue = null)
        {
            this.name = name;
            this.description = description;
            this.defaultValue = defaultValue;
            this.flags = flags;
            
            if (minValue.HasValue)
            {
                this.hasMinValue = true;
                this.minValue = minValue.Value;
            }
            
            if (maxValue.HasValue)
            {
                this.hasMaxValue = true;
                this.maxValue = maxValue.Value;
            }
            
            // Validate and set initial value
            this.currentValue = defaultValue;
            if (ValidateConfiguration(out string error))
            {
                Value = defaultValue; // This will apply constraints
            }
            else
            {
                Debug.LogError($"[IntConfigVariable] Invalid configuration: {error}");
            }
        }
        
        public IntConfigVariable() { }
    }
}