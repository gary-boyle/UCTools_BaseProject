using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using UnityEngine;
using IConfigService = GameFramework.Services.Interfaces.IConfigService;

namespace GameFramework.Services
{
    /// <summary>
    /// Configuration service implementation with ConfigVar integration and constructor injection
    /// </summary>
    public class ConfigService : IConfigService
    {
        public bool IsInitialized { get; private set; }
        
        private readonly IEventSystem _eventSystem;
        private readonly string _configFilePath;
        private readonly Dictionary<string, UCTools_ConfigVariables.ConfigVar> _registeredConfigVars = new();
        
        /// <summary>
        /// Constructor injection - receives required dependencies
        /// </summary>
        public ConfigService(IEventSystem eventSystem)
        {
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            _configFilePath = Application.persistentDataPath + "/config.cfg";
        }
        
        public async Task InitializeAsync()
        {
            if (IsInitialized) return;
            
            Debug.Log("[ConfigService] Initializing configuration system...");
            
            // Initialize ConfigVar system
            UCTools_ConfigVariables.ConfigVar.Init();
            
            // Register all ConfigVars from the global registry
            foreach (var kvp in UCTools_ConfigVariables.ConfigVar.ConfigVars)
            {
                RegisterConfigVar(kvp.Value);
            }
            
            IsInitialized = true;
            await Task.CompletedTask;
        }
        
        public void Shutdown()
        {
            IsInitialized = false;
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
                
                foreach (var cvar in _registeredConfigVars.Values)
                {
                    if ((cvar.flags & UCTools_ConfigVariables.ConfigFlags.Save) == UCTools_ConfigVariables.ConfigFlags.Save)
                    {
                        lines.Add($"{cvar.name} \"{cvar.Value}\"");
                    }
                }
                
                await System.IO.File.WriteAllLinesAsync(_configFilePath, lines);
                
                // Clear dirty flags
                UCTools_ConfigVariables.ConfigVar.DirtyFlags = UCTools_ConfigVariables.ConfigFlags.None;
                
                // Publish options changed event using injected event system
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
            if (_registeredConfigVars.TryGetValue(configName.ToLower(), out var cvar))
            {
                if (typeof(T) == typeof(string))
                    return (T)(object)cvar.Value;
                else if (typeof(T) == typeof(int))
                    return (T)(object)cvar.IntValue;
                else if (typeof(T) == typeof(float))
                    return (T)(object)cvar.FloatValue;
                else if (typeof(T) == typeof(bool))
                    return (T)(object)(cvar.IntValue != 0);
            }
            
            return default(T);
        }
        
        public void SetConfigValue<T>(string configName, T value)
        {
            if (_registeredConfigVars.TryGetValue(configName.ToLower(), out var cvar))
            {
                cvar.Value = value.ToString();
            }
            else
            {
                Debug.LogWarning($"[ConfigService] Config variable '{configName}' not found");
            }
        }
        
        public void ResetToDefaults()
        {
            UCTools_ConfigVariables.ConfigVar.ResetAllToDefault();
            
            // Publish options changed event using injected event system
            _eventSystem.Publish<OptionsChangedEvent>();
        }
        
        public void RegisterConfigVar(UCTools_ConfigVariables.ConfigVar configVar)
        {
            _registeredConfigVars[configVar.name] = configVar;
        }
        
        private void ParseConfigLines(string[] lines)
        {
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("//"))
                    continue;
                    
                // Parse format: variablename "value"
                var spaceIndex = trimmed.IndexOf(' ');
                if (spaceIndex > 0)
                {
                    var varName = trimmed.Substring(0, spaceIndex);
                    var valueStart = trimmed.IndexOf('"', spaceIndex);
                    var valueEnd = trimmed.LastIndexOf('"');
                    
                    if (valueStart >= 0 && valueEnd > valueStart)
                    {
                        var value = trimmed.Substring(valueStart + 1, valueEnd - valueStart - 1);
                        
                        if (_registeredConfigVars.TryGetValue(varName.ToLower(), out var cvar))
                        {
                            cvar.Value = value;
                        }
                    }
                }
            }
        }
    }
}