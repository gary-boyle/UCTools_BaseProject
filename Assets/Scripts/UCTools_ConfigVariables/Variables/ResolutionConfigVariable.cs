using System;
using UnityEngine;

namespace UCTools_ConfigVariables
{
     
    /// <summary>
    /// Specific resolution config variable for cleaner usage
    /// </summary>
    [System.Serializable]
    public class ResolutionConfigVariable : EnumConfigVariable<ResolutionOption>
    {
        /// <summary>Get current resolution width</summary>
        public int Width => Value.GetResolution().width;
        
        /// <summary>Get current resolution height</summary>
        public int Height => Value.GetResolution().height;
        
        /// <summary>Get current display name</summary>
        public string DisplayName => Value.GetDisplayName();
        
        public ResolutionConfigVariable(string name, string description, ResolutionOption defaultValue, ConfigFlags flags = ConfigFlags.Save)
            : base(name, description, defaultValue, flags)
        {
        }
        
        public ResolutionConfigVariable() : base() 
        {
        }
    }
}
