using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameFramework.StateMachine.Data;

namespace GameFramework.Services.Interfaces
{
    public interface IGameDataService
    {
        // Player Data
        string PlayerName { get; set; }
        int PlayerLevel { get; set; }
        float PlayerHealth { get; set; }
        
        // Game Session Data
        string CurrentScene { get; set; }
        bool IsNewGame { get; set; }
        DateTime SessionStartTime { get; set; }
        
        // Loading Configuration
        LoadingConfiguration CurrentLoadingConfig { get; set; }
        
        // Generic data storage for flexibility
        T GetValue<T>(string key, T defaultValue = default);
        void SetValue<T>(string key, T value);
        bool HasValue(string key);
        void RemoveValue(string key);
        
        // Bulk operations
        void SetValues(Dictionary<string, object> values);
        Dictionary<string, object> GetAllValues();
        void ClearTransientData();
        
        // Events
        event Action<string, object> ValueChanged;
        
        Task InitializeAsync();
        void Shutdown();
    }
}