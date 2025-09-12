using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GameFramework.Config.Data;
using UnityEngine;
using GameFramework.Config.Enums;
using GameFramework.Config.ScriptableObjects;
using GameFramework.Config.Variables;

namespace GameFramework.Core
{
    /// <summary>
    /// Simple static registry for settings ScriptableObjects
    /// Gets references from GameManager and handles unified JSON persistence
    /// </summary>
    public static class SettingsRegistry
    {
        private static Dictionary<Type, ConfigCategoryBase>  _settings;
        private static bool _isInitialized = false;
        private static string _configFilePath;

        /// <summary>
        /// Initialize with settings from GameManager
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized) return;

            _configFilePath = Path.Combine(Application.persistentDataPath, "config.json");

            _settings = GameManager.Instance.GetConfigurationSettings();
            
            _isInitialized = true;
        }

        /// <summary>
        /// Get settings by ConfigCatergory
        /// </summary>
        public static T Get<T>() where T : ConfigCategoryBase
        {
            if (_settings.TryGetValue(typeof(T), out var category) && category is T typedCategory)
            {
                return typedCategory;
            }
        
            Debug.LogError($"[SettingsRegistry] Settings of type {typeof(T).Name} not found");
            return null;
        }

        /// <summary>
        /// Save all settings to unified JSON config file asynchronously
        /// </summary>
        public static async Task SaveAllSettingsAsync()
        {
            if (!_isInitialized) return;

            try
            {
                var configData = new ConfigData();
                var valuesToSave = new Dictionary<string, ConfigValue>();

                // Collect all saveable config variables from all categories
                foreach (var category in _settings.Values)
                {
                    var variables = category.GetAllVariables();
                    foreach (var variable in variables)
                    {
                        if (variable.flags == ConfigFlags.Save)
                        {
                            valuesToSave[variable.name] = new ConfigValue
                            {
                                Value = variable.GetValueAsString(),
                                Type = variable.ValueType.Name
                            };
                        }
                    }
                }

                // Assign to ConfigData (this will convert Dictionary to List)
                configData.Values = valuesToSave;

                // Save as JSON
                var json = JsonUtility.ToJson(configData, true);
                Directory.CreateDirectory(Path.GetDirectoryName(_configFilePath));
        
                using (var writer = new StreamWriter(_configFilePath, false))
                {
                    await writer.WriteAsync(json);
                }

                Debug.Log($"[SettingsRegistry] Saved {valuesToSave.Count} config values to {_configFilePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SettingsRegistry] Failed to save settings: {e.Message}");
            }
        }
        
        /// <summary>
        /// Load all settings from unified JSON config file asynchronously
        /// </summary>
        public static async Task LoadAllSettingsAsync()
        {
            if (!_isInitialized) return;

            try
            {
                if (!File.Exists(_configFilePath))
                {
                    Debug.Log("[SettingsRegistry] No config file found, using defaults");
                    return;
                }

                string json;
                using (var reader = new StreamReader(_configFilePath))
                {
                    json = await reader.ReadToEndAsync();
                }

                var configData = JsonUtility.FromJson<ConfigData>(json);

                if (configData?.Values == null)
                {
                    Debug.LogWarning("[SettingsRegistry] Invalid config file format");
                    return;
                }

                // Create lookup dictionary for all variables across all categories
                var allVariables = new Dictionary<string, ConfigVariableBase>();
                foreach (var category in _settings.Values)
                {
                    var variables = category.GetAllVariables();
                    foreach (var variable in variables)
                    {
                        allVariables[variable.name] = variable;
                    }
                }

                // Apply loaded values
                int appliedCount = 0;
                foreach (var kvp in configData.Values)
                {
                    if (allVariables.TryGetValue(kvp.Key, out var variable))
                    {
                        if (variable.SetValueFromString(kvp.Value.Value))
                        {
                            appliedCount++;
                        }
                    }
                }

                Debug.Log($"[SettingsRegistry] Loaded and applied {appliedCount}/{configData.Values.Count} config values");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SettingsRegistry] Failed to load settings: {e.Message}");
            }
        }

        /// <summary>
        /// Reset all settings to defaults and save
        /// </summary>
        public static async Task ResetAllToDefaults()
        {
            if (!_isInitialized) return;

            foreach (var category in _settings.Values)
            {
                var variables = category.GetAllVariables();
                foreach (var variable in variables)
                {
                    variable.ResetToDefault();
                }
            }

            await SaveAllSettingsAsync();
            Debug.Log("[SettingsRegistry] Reset all settings to defaults");
        }

        /// <summary>
        /// Get all registered settings (for debugging)
        /// </summary>
        public static IReadOnlyDictionary<Type, ConfigCategoryBase>  GetAllSettings()
        {
            return _settings;
        }
    }





}
