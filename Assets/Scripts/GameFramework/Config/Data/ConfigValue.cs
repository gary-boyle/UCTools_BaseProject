using System;

namespace GameFramework.Config.Data
{
    /// <summary>
    /// Serializable container for config values with type information
    /// </summary>
    [Serializable]
    public class ConfigValue
    {
        public string Value;
        public string Type;
    }
}