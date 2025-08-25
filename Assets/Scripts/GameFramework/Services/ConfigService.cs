using System;
using System.Collections.Generic;
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
    /// Configuration service implementation with typed ScriptableObject ConfigCategory integration
    /// </summary>
    public class ConfigService : IConfigService
    {
        public bool IsInitialized { get; private set; }
        
        private readonly IEventSystem _eventSystem;
        private readonly string _configFilePath;
        private readonly List<ConfigCategory> _configCategories = new();
        private readonly Dictionary<string, ConfigVariableBase> _configVariablesByName = new();
        
        public ConfigService(IEventSystem eventSystem)
        {
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            _configFilePath = Application.persistentDataPath + "/config.cfg";
        }
        
        public async Task InitializeAsync()
        {
            if (IsInitialized) return;
            
            Debug.Log("[ConfigService] Initializing configuration system...");
            
            IsInitialized = true;
            await Task.CompletedTask;
        }
        
        public void Shutdown()
        {
            IsInitialized = false;
        }
        
        public void RegisterConfigCategory(ConfigCategory category)
        {
            if (category == null)
            {
                Debug.LogWarning("[ConfigService] Attempted to register null ConfigCategory");
                return;
            }
            
            if (_configCategories.Contains(category))
            {
                Debug.LogWarning($"[ConfigService] ConfigCategory {category.name} already registered");
                return;
            }
            
            _configCategories.Add(category);
            
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
        
        public void RegisterConfigCategories(params ConfigCategory[] categories)
        {
            foreach (var category in categories)
            {
                RegisterConfigCategory(category);
            }
        }
        
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
    }
}
