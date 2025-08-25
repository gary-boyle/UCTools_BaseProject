using System;
using UnityEngine;

namespace UCTools_ConfigVariables
{
    /// <summary>
    /// Abstract base class for all typed configuration variables
    /// </summary>
    [System.Serializable]
    public abstract class ConfigVariableBase
    {
        [Header("Variable Definition")]
        public string name = "";
        public string description = "";
        public ConfigFlags flags = ConfigFlags.None;
        
        [System.NonSerialized] private bool _hasChanged = false;
        
        /// <summary>Variable type for runtime identification</summary>
        public abstract Type ValueType { get; }
        
        /// <summary>Check if the variable has been modified since the last call to ChangeCheck()</summary>
        public bool ChangeCheck()
        {
            if (!_hasChanged)
                return false;
                
            _hasChanged = false;
            return true;
        }
        
        /// <summary>Mark variable as changed</summary>
        protected void MarkChanged()
        {
            _hasChanged = true;
        }
        
        /// <summary>Reset to default value</summary>
        public abstract void ResetToDefault();
        
        /// <summary>Validate the configuration variable setup</summary>
        public abstract bool ValidateConfiguration(out string error);
        
        /// <summary>Get value as object (for generic access)</summary>
        public abstract object GetValueAsObject();
        
        /// <summary>Set value from object (for generic access)</summary>
        public abstract bool SetValueFromObject(object value);
        
        /// <summary>Get value as string (for serialization)</summary>
        public abstract string GetValueAsString();
        
        /// <summary>Set value from string (for deserialization)</summary>
        public abstract bool SetValueFromString(string value);
    }
}