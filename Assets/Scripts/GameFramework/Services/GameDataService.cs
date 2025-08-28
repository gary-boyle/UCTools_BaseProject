using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Data;
using UnityEngine;

namespace GameFramework.Services
{
    public class GameDataService : IGameDataService
    {
        private readonly Dictionary<string, object> _gameData = new();
        private readonly Dictionary<string, object> _transientData = new();
        
        public event Action<string, object> ValueChanged;

        // Strongly typed properties for common data
        public string PlayerName 
        { 
            get => GetValue<string>("playerName", "Player"); 
            set => SetValue("playerName", value); 
        }
        
        public int PlayerLevel 
        { 
            get => GetValue<int>("playerLevel", 1); 
            set => SetValue("playerLevel", value); 
        }
        
        public float PlayerHealth 
        { 
            get => GetValue<float>("playerHealth", 100f); 
            set => SetValue("playerHealth", value); 
        }
        
        public string CurrentScene 
        { 
            get => GetValue<string>("currentScene", ""); 
            set => SetValue("currentScene", value); 
        }
        
        public bool IsNewGame 
        { 
            get => GetValue<bool>("isNewGame", true); 
            set => SetValue("isNewGame", value); 
        }
        
        public DateTime SessionStartTime 
        { 
            get => GetValue<DateTime>("sessionStartTime", DateTime.Now); 
            set => SetValue("sessionStartTime", value); 
        }
        
        public LoadingConfiguration CurrentLoadingConfig { get; set; }

        public async Task InitializeAsync()
        {
            Debug.Log("[GameDataService] Initializing game data service...");
            SessionStartTime = DateTime.Now;
            await Task.CompletedTask;
        }

        public void Shutdown()
        {
            _gameData.Clear();
            _transientData.Clear();
            CurrentLoadingConfig = null;
        }

        public T GetValue<T>(string key, T defaultValue = default)
        {
            // Check transient data first, then persistent data
            if (_transientData.TryGetValue(key, out var transientValue))
            {
                try { return (T)transientValue; }
                catch { /* Fall through to persistent data or default */ }
            }
            
            if (_gameData.TryGetValue(key, out var value))
            {
                try { return (T)value; }
                catch { return defaultValue; }
            }
            
            return defaultValue;
        }

        public void SetValue<T>(string key, T value)
        {
            var oldValue = GetValue<object>(key);
            _gameData[key] = value;
            
            if (!Equals(oldValue, value))
            {
                ValueChanged?.Invoke(key, value);
            }
        }
        
        public void SetTransientValue<T>(string key, T value)
        {
            _transientData[key] = value;
        }

        public bool HasValue(string key)
        {
            return _gameData.ContainsKey(key) || _transientData.ContainsKey(key);
        }

        public void RemoveValue(string key)
        {
            _gameData.Remove(key);
            _transientData.Remove(key);
        }

        public void SetValues(Dictionary<string, object> values)
        {
            foreach (var kvp in values)
            {
                SetValue(kvp.Key, kvp.Value);
            }
        }

        public Dictionary<string, object> GetAllValues()
        {
            var result = new Dictionary<string, object>(_gameData);
            
            // Overlay transient data
            foreach (var kvp in _transientData)
            {
                result[kvp.Key] = kvp.Value;
            }
            
            return result;
        }

        public void ClearTransientData()
        {
            _transientData.Clear();
        }
        
        // Helper methods for loading configurations
        public void SetLoadingConfiguration(LoadingConfiguration config)
        {
            CurrentLoadingConfig = config;
        }
        
        public T GetLoadingData<T>(string key, T defaultValue = default)
        {
            if (CurrentLoadingConfig?.GameData?.ContainsKey(key) == true)
            {
                try
                {
                    return (T)CurrentLoadingConfig.GameData[key];
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }
    }
}
