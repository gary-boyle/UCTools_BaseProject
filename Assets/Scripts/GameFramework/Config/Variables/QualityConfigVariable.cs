using System;
using GameFramework.Config.Enums;
using UnityEngine;

namespace GameFramework.Config.Variables
{
    /// <summary>
    /// Specific quality config variable for cleaner usage
    /// </summary>
    [System.Serializable]
    public class QualityConfigVariable : EnumConfigVariable<QualityOption>
    {
        /// <summary>Get current Unity quality level</summary>
        public int QualityLevel => Value.GetQualityLevel();
        
        /// <summary>Get current display name</summary>
        public string DisplayName => Value.GetDisplayName();
        
        public QualityConfigVariable(string name, string description, QualityOption defaultValue, ConfigFlags flags = ConfigFlags.Save)
            : base(name, description, defaultValue, flags)
        {
        }
        
        public QualityConfigVariable() : base() 
        {
        }
    }
}