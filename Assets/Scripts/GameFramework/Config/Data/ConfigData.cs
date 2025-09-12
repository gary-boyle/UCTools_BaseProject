using System;
using System.Collections.Generic;
using GameFramework.Core;

namespace GameFramework.Config.Data
{
    /// <summary>
    /// Data structure for JSON serialization of all config values
    /// </summary>
    [Serializable]
    public class ConfigData
    {
        public Dictionary<string, ConfigValue> Values = new Dictionary<string, ConfigValue>();
    }
}