using System;
using GameFramework.Config.Enums;
using UnityEngine;

namespace GameFramework.Config.Variables
{
    /// <summary>
    /// Boolean configuration variable
    /// </summary>
    [System.Serializable]
    public class BoolConfigVariable : ConfigVariableBase
    {
        [Header("Bool Settings")]
        [SerializeField] private bool defaultValue = false;
        [SerializeField] private bool currentValue = false;
        
        public override Type ValueType => typeof(bool);
        
        /// <summary>Current boolean value</summary>
        public bool Value
        {
            get => currentValue;
            set
            {
                if (currentValue != value)
                {
                    currentValue = value;
                    MarkChanged();
                }
            }
        }
        
        /// <summary>Default boolean value</summary>
        public bool DefaultValue => defaultValue;
        
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
            if (value is bool boolValue)
            {
                Value = boolValue;
                return true;
            }
            return false;
        }
        
        public override string GetValueAsString() => Value.ToString();
        
        public override bool SetValueFromString(string value)
        {
            if (bool.TryParse(value, out bool result))
            {
                Value = result;
                return true;
            }
            return false;
        }
        
        // Constructor
        public BoolConfigVariable(string name, string description, bool defaultValue, ConfigFlags flags = ConfigFlags.None)
        {
            this.name = name;
            this.description = description;
            this.defaultValue = defaultValue;
            this.currentValue = defaultValue;
            this.flags = flags;
        }
        
        public BoolConfigVariable() { }
    }
}