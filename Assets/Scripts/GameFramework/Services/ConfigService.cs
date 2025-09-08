using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using UnityEngine;
using IConfigService = GameFramework.Services.Interfaces.IConfigService;
using GrameFramework.Config;
using UCTools_ConfigVariables;

namespace GameFramework.Services
{
    /// <summary>
    /// Configuration service implementation with typed ScriptableObject ConfigCategory integration and query support
    /// 
    /// Intent: Centralized configuration management with ScriptableObject registry and query capabilities
    /// Design: Service locator pattern with type-based lookup for ScriptableObjects
    /// Pros: 
    /// - Centralized ScriptableObject management
    /// - Type-safe querying
    /// - Clean separation of concerns
    /// - Easy testing and mocking
    /// Cons:
    /// - Additional complexity for ScriptableObject management
    /// - Memory overhead for type dictionaries
    /// </summary>
    public class ConfigService : IConfigService
    {
        public bool IsInitialized { get; private set; }
        
        private readonly IEventSystem _eventSystem;
        private readonly string _configFilePath;
        private readonly List<ConfigCategory> _configCategories = new();
        private readonly Dictionary<string, ConfigVariableBase> _configVariablesByName = new();
        private readonly Dictionary<Type, ConfigCategory> _configCategoriesByType = new();
        
        public ConfigService(IEventSystem eventSystem)
        {
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            _configFilePath = Application.persistentDataPath + "/config.cfg";
        }
        
        public async Task InitializeAsync()
        {
            if (IsInitialized) return;
            
            Debug.Log("[ConfigService] Initializing configuration system...");
            
            // Auto-load ScriptableObjects from Resources if needed
            await LoadScriptableObjectsFromResources();
            
            IsInitialized = true;
            await Task.CompletedTask;
        }
        
        public void Shutdown()
        {
            _configCategories.Clear();
            _configVariablesByName.Clear();
            _configCategoriesByType.Clear();
            IsInitialized = false;
        }
        
        /// <summary>
        /// Register a configuration category ScriptableObject
        /// </summary>
        public void RegisterConfigCategory(ConfigCategory category)
        {
            if (category == null)
            {
                Debug.LogWarning("[ConfigService] Attempted to register null ConfigCategory");
                return;
            }
            
            var categoryType = category.GetType();
            
            if (_configCategoriesByType.ContainsKey(categoryType))
            {
                Debug.LogWarning($"[ConfigService] ConfigCategory of type {categoryType.Name} already registered");
                return;
            }
            
            if (_configCategories.Contains(category))
            {
                Debug.LogWarning($"[ConfigService] ConfigCategory {category.name} already registered");
                return;
            }
            
            // Add to collections
            _configCategories.Add(category);
            _configCategoriesByType[categoryType] = category;
            
            // Register all variables from this category
            RegisterVariablesFromCategory(category);
            
            Debug.Log($"[ConfigService] Registered ConfigCategory: {categoryType.Name} ({category.name})");
        }
        
        /// <summary>
        /// Register multiple configuration categories
        /// </summary>
        public void RegisterConfigCategories(params ConfigCategory[] categories)
        {
            foreach (var category in categories)
            {
                RegisterConfigCategory(category);
            }
        }
        
        #region ScriptableObject Query Methods
        
        /// <summary>
        /// Get a configuration category by type
        /// </summary>
        public T GetConfigCategory<T>() where T : ConfigCategory
        {
            return GetConfigCategory(typeof(T)) as T;
        }
        
        /// <summary>
        /// Get a configuration category by type
        /// </summary>
        public ConfigCategory GetConfigCategory(Type categoryType)
        {
            if (categoryType == null)
            {
                Debug.LogError("[ConfigService] GetConfigCategory called with null type");
                return null;
            }
            
            if (!typeof(ConfigCategory).IsAssignableFrom(categoryType))
            {
                Debug.LogError($"[ConfigService] Type {categoryType.Name} is not a ConfigCategory");
                return null;
            }
            
            if (_configCategoriesByType.TryGetValue(categoryType, out var category))
            {
                return category;
            }
            
            Debug.LogWarning($"[ConfigService] ConfigCategory of type {categoryType.Name} not found");
            return null;
        }
        
        /// <summary>
        /// Get all registered configuration categories
        /// </summary>
        public IReadOnlyList<ConfigCategory> GetAllConfigCategories()
        {
            return _configCategories.AsReadOnly();
        }
        
        /// <summary>
        /// Get all configuration categories of a specific type (useful for inheritance scenarios)
        /// </summary>
        public IReadOnlyList<T> GetConfigCategoriesOfType<T>() where T : ConfigCategory
        {
            return _configCategories.OfType<T>().ToList().AsReadOnly();
        }
        
        /// <summary>
        /// Check if a configuration category of the specified type is registered
        /// </summary>
        public bool HasConfigCategory<T>() where T : ConfigCategory
        {
            return HasConfigCategory(typeof(T));
        }
        
        /// <summary>
        /// Check if a configuration category of the specified type is registered
        /// </summary>
        public bool HasConfigCategory(Type categoryType)
        {
            return categoryType != null && _configCategoriesByType.ContainsKey(categoryType);
        }
        
        #endregion
        
        #region Configuration File Operations
        
        public async Task LoadConfigAsync()
        {
            if (System.IO.File.Exists(_configFilePath))
            {
                try
                {
                    var lines = await System.IO.File.ReadAllLinesAsync(_configFilePath);
                    ParseConfigLines(lines);
                    Debug.Log("[ConfigService] Configuration loaded successfully");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ConfigService] Error loading config: {e}");
                }
            }
            else
            {
                Debug.Log("[ConfigService] No config file found, using defaults");
            }
        }
        
        public async Task SaveConfigAsync()
        {
            try
            {
                var lines = new List<string>();
                
                foreach (var variable in _configVariablesByName.Values)
                {
                    if ((variable.flags & ConfigFlags.Save) == ConfigFlags.Save)
                    {
                        lines.Add($"{variable.name} \"{variable.GetValueAsString()}\"");
                    }
                }
                
                await System.IO.File.WriteAllLinesAsync(_configFilePath, lines);
                
                _eventSystem.Publish<OptionsChangedEvent>();
                
                Debug.Log("[ConfigService] Configuration saved successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ConfigService] Error saving config: {e}");
            }
        }
        
        #endregion
        
        #region Configuration Value Operations
        
        public T GetConfigValue<T>(string configName)
        {
            if (_configVariablesByName.TryGetValue(configName.ToLower(), out var variable))
            {
                if (variable.GetValueAsObject() is T typedValue)
                {
                    return typedValue;
                }
                
                // Try conversion for compatible types
                try
                {
                    return (T)Convert.ChangeType(variable.GetValueAsObject(), typeof(T));
                }
                catch
                {
                    Debug.LogWarning($"[ConfigService] Cannot convert {variable.ValueType.Name} to {typeof(T).Name} for '{configName}'");
                }
            }
            
            return default(T);
        }
        
        public void SetConfigValue<T>(string configName, T value)
        {
            if (_configVariablesByName.TryGetValue(configName.ToLower(), out var variable))
            {
                if (!variable.SetValueFromObject(value))
                {
                    Debug.LogWarning($"[ConfigService] Failed to set value for '{configName}': type mismatch");
                }
            }
            else
            {
                Debug.LogWarning($"[ConfigService] Config variable '{configName}' not found");
            }
        }
        
        public void ResetToDefaults()
        {
            foreach (var variable in _configVariablesByName.Values)
            {
                variable.ResetToDefault();
            }
            
            _eventSystem.Publish<OptionsChangedEvent>();
        }
        
        #endregion
        
        #region Private Helper Methods
        
        /// <summary>
        /// Register all variables from a configuration category
        /// </summary>
        private void RegisterVariablesFromCategory(ConfigCategory category)
        {
            var variables = category.GetAllVariables();
            foreach (var variable in variables)
            {
                if (string.IsNullOrEmpty(variable.name))
                {
                    Debug.LogWarning($"[ConfigService] Found ConfigVariable with empty name in {category.name}");
                    continue;
                }
                
                var key = variable.name.ToLower();
                if (_configVariablesByName.ContainsKey(key))
                {
                    Debug.LogWarning($"[ConfigService] Duplicate ConfigVariable name: {variable.name}");
                    continue;
                }
                
                if (variable.ValidateConfiguration(out string error))
                {
                    _configVariablesByName[key] = variable;
                    Debug.Log($"[ConfigService] Registered ConfigVariable: {variable.name} ({variable.ValueType.Name})");
                }
                else
                {
                    Debug.LogError($"[ConfigService] Invalid ConfigVariable '{variable.name}': {error}");
                }
            }
        }
        
        /// <summary>
        /// Auto-load ScriptableObjects from Resources folder during initialization
        /// </summary>
        private async Task LoadScriptableObjectsFromResources()
        {
            try
            {
                // Load all ScriptableObjects that inherit from ConfigCategory
                var configObjects = Resources.LoadAll<ConfigCategory>("");
                
                foreach (var configObject in configObjects)
                {
                    RegisterConfigCategory(configObject);
                }
                
                if (configObjects.Length > 0)
                {
                    Debug.Log($"[ConfigService] Auto-loaded {configObjects.Length} ConfigCategory ScriptableObjects from Resources");
                }
                else
                {
                    Debug.LogWarning("[ConfigService] No ConfigCategory ScriptableObjects found in Resources");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ConfigService] Error loading ScriptableObjects from Resources: {e}");
            }
            
            await Task.CompletedTask;
        }
        
        /// <summary>
        /// Parse configuration lines from file
        /// </summary>
        private void ParseConfigLines(string[] lines)
        {
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("//"))
                    continue;
                    
                var spaceIndex = trimmed.IndexOf(' ');
                if (spaceIndex > 0)
                {
                    var varName = trimmed.Substring(0, spaceIndex);
                    var valueStart = trimmed.IndexOf('"', spaceIndex);
                    var valueEnd = trimmed.LastIndexOf('"');
                    
                    if (valueStart >= 0 && valueEnd > valueStart)
                    {
                        var value = trimmed.Substring(valueStart + 1, valueEnd - valueStart - 1);
                        
                        if (_configVariablesByName.TryGetValue(varName.ToLower(), out var variable))
                        {
                            variable.SetValueFromString(value);
                        }
                    }
                }
            }
        }
        
        #endregion
        
        #region Debug and Utility Methods
        
        /// <summary>
        /// Get debug information about registered categories and variables
        /// </summary>
        public string GetDebugInfo()
        {
            var info = $"ConfigService Debug Info:\n";
            info += $"Categories: {_configCategories.Count}\n";
            info += $"Variables: {_configVariablesByName.Count}\n\n";
            
            foreach (var category in _configCategories)
            {
                info += $"Category: {category.GetType().Name} ({category.name})\n";
                var variables = category.GetAllVariables();
                foreach (var variable in variables)
                {
                    info += $"  - {variable.name}: {variable.GetValueAsString()}\n";
                }
                info += "\n";
            }
            
            return info;
        }
        
        #endregion
    }
}
