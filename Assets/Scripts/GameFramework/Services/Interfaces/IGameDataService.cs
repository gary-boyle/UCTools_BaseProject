using System;
using System.Threading.Tasks;
using GameFramework.DataStructures;
using GameFramework.StateMachine.Data;

namespace GameFramework.Services.Interfaces
{
    /// <summary>
    /// Clean IGameDataService interface for unified GameSession management
    /// </summary>
    public interface IGameDataService
    {
        bool IsInitialized { get; }
        GameSession CurrentSession { get; }
        LoadingConfiguration CurrentLoadingConfig { get; set; }
        
        // Service Lifecycle
        Task InitializeAsync();
        void Shutdown();
        
        // Session Management
        void CreateNewGameSession(LoadingConfiguration config);
        void LoadGameSession(GameSession session);
        void ClearSession();
        bool HasActiveSession();
        bool IsValidGameSession(GameSession session);
        
        // Data Access
        PlayerState GetPlayerState();
        GameProgress GetGameProgress();
        T GetCustomData<T>(string key, T defaultValue = default);
        void SetCustomData<T>(string key, T value);
        T GetLoadingData<T>(string key, T defaultValue = default);
    }
}