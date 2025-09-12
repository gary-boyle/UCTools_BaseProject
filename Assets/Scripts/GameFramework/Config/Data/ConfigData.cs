using System;
using System.Collections.Generic;
using GameFramework.Config.Variables;
using GameFramework.Core;
using UnityEngine;

namespace GameFramework.Config.Data
{
    /// <summary>
    /// Data structure for JSON serialization of all config values
    /// Uses List instead of Dictionary for Unity JsonUtility compatibility
    /// </summary>
    [Serializable]
    public class ConfigData
    {
        [SerializeField] 
        public List<ConfigEntry> entries = new List<ConfigEntry>();
        
        /// <summary>
        /// Helper property to work with data as Dictionary (runtime only)
        /// </summary>
        public Dictionary<string, ConfigValue> Values
        {
            get
            {
                var dict = new Dictionary<string, ConfigValue>();
                foreach (var entry in entries)
                {
                    dict[entry.key] = entry.value;
                }
                return dict;
            }
            set
            {
                entries.Clear();
                foreach (var kvp in value)
                {
                    entries.Add(new ConfigEntry { key = kvp.Key, value = kvp.Value });
                }
            }
        }
    }
    

}