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
        GameSessionData CurrentSessionData { get; }
        LoadingConfiguration CurrentLoadingConfig { get; set; }
        
        // Service Lifecycle
        Task InitializeAsync();
        void Shutdown();
        
        // Session Management
        void CreateNewGameSession(LoadingConfiguration config);
        void LoadGameSession(GameSessionData sessionData);
        void ClearSession();
        bool HasActiveSession();
        bool IsValidGameSession(GameSessionData sessionData);
        void UpdateSessionSaveTime(GameSessionData sessionData = null);
    }
}