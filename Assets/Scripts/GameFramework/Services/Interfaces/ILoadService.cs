using System;
using System.Threading.Tasks;
using GameFramework.DataStructures;

namespace GameFramework.Services.Interfaces
{
    /// <summary>
    /// Service responsible for orchestrating all game loading operations
    /// Handles the business logic of loading games, creating sessions, and managing state transitions
    /// </summary>
    public interface ILoadService
    {
        bool IsInitialized { get; }
        bool IsLoading { get; }
        
        // Lifecycle
        Task InitializeAsync();
        void Shutdown();
        
        // Loading operations
        Task<GameSessionData> LoadGameSessionAsync(string saveName);

        // Loading support
        Task<SaveFileInfo[]> GetLoadableSaveFilesAsync();
        
    }
}