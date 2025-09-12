using System;

namespace GameFramework.Config.Data
{
    /// <summary>
    /// Individual config value for JSON serialization
    /// </summary>
    [Serializable]
    public class ConfigValue
    {
        public string Value;
        public string Type; // For debugging/validation
    }
}