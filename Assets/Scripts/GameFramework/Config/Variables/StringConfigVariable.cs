using System;
using GameFramework.Config.Enums;
using UnityEngine;

namespace GameFramework.Config.Variables
{
    /// <summary>
    /// String configuration variable
    /// </summary>
    [System.Serializable]
    public class StringConfigVariable : ConfigVariableBase
    {
        [Header("String Settings")]
        [SerializeField] private string defaultValue = "";
        [SerializeField] private string currentValue = "";
        
        public override Type ValueType => typeof(string);
        
        /// <summary>Current string value</summary>
        public string Value
        {
            get => currentValue ?? string.Empty;
            set
            {
                string newValue = value ?? string.Empty;
                if (currentValue != newValue)
                {
                    currentValue = newValue;
                    MarkChanged();
                }
            }
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
            
            return true;
        }
        
        public override object GetValueAsObject() => Value;
        
        public override bool SetValueFromObject(object value)
        {
            if (value is string stringValue)
            {
                Value = stringValue;
                return true;
            }
            
            Value = value?.ToString() ?? string.Empty;
            return true;
        }
        
        public override string GetValueAsString() => Value;
        
        public override bool SetValueFromString(string value)
        {
            Value = value;
            return true;
        }
        
        // Constructor
        public StringConfigVariable(string name, string description, string defaultValue, ConfigFlags flags = ConfigFlags.None)
        {
            this.name = name;
            this.description = description;
            this.defaultValue = defaultValue ?? string.Empty;
            this.currentValue = this.defaultValue;
            this.flags = flags;
        }
        
        public StringConfigVariable() { }
    }
}
