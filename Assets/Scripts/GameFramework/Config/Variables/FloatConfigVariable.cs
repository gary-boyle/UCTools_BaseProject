using System;
using GameFramework.Config.Enums;
using UnityEngine;

namespace GameFramework.Config.Variables
{
    /// <summary>
    /// Float configuration variable with optional min/max constraints
    /// </summary>
    [System.Serializable]
    public class FloatConfigVariable : ConfigVariableBase
    {
        [Header("Float Settings")]
        [SerializeField] private float defaultValue = 0f;
        [SerializeField] private float currentValue = 0f;
        
        [Header("Constraints")]
        [SerializeField] private bool hasMinValue = false;
        [SerializeField] private float minValue = 0f;
        [SerializeField] private bool hasMaxValue = false;
        [SerializeField] private float maxValue = 1f;
        
        public override Type ValueType => typeof(float);
        
        /// <summary>Current float value with constraint validation</summary>
        public float Value
        {
            get => currentValue;
            set
            {
                float clampedValue = value;
                
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
                
                if (!Mathf.Approximately(currentValue, clampedValue))
                {
                    currentValue = clampedValue;
                    MarkChanged();
                }
            }
        }
        
        public float DefaultValue => defaultValue;
        public float MinValue => minValue;
        public float MaxValue => maxValue;
        public bool HasMinValue => hasMinValue;
        public bool HasMaxValue => hasMaxValue;
        
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
            if (value is float floatValue)
            {
                Value = floatValue;
                return true;
            }
            else if (value is double doubleValue)
            {
                Value = (float)doubleValue;
                return true;
            }
            return false;
        }
        
        public override string GetValueAsString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        
        public override bool SetValueFromString(string value)
        {
            if (float.TryParse(value, System.Globalization.NumberStyles.Float, 
                System.Globalization.CultureInfo.InvariantCulture, out float result))
            {
                Value = result;
                return true;
            }
            return false;
        }
        
        // Constructor with constraints
        public FloatConfigVariable(string name, string description, float defaultValue, 
            ConfigFlags flags = ConfigFlags.None, float? minValue = null, float? maxValue = null)
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
                Debug.LogError($"[FloatConfigVariable] Invalid configuration: {error}");
            }
        }
        
        public FloatConfigVariable() { }
    }
}