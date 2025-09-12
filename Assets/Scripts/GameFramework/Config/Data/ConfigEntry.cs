using System;
using GameFramework.Config.Data;

namespace GameFramework.Config.Data
{
    /// <summary>
    /// Serializable key-value pair for config entries
    /// </summary>
    [Serializable]
    public class ConfigEntry
    {
        public string key;
        public ConfigValue value;
    }
}