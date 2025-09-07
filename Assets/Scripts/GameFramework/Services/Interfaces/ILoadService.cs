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
        Task<bool> LoadGameAsync(string saveFileName);
        Task<bool> LoadGameAsync(SaveFileInfo saveFileInfo);
        Task<bool> LoadMostRecentGameAsync();
        
        // Loading support
        Task<SaveFileInfo[]> GetLoadableSaveFilesAsync();
        Task<bool> CanLoadGame(string saveFileName);
        Task<bool> CanLoadGame(SaveFileInfo saveFileInfo);
        
        // Events for loading progress
        event Action<string, float> LoadingProgressChanged;
        event Action<string> LoadingMessageChanged;
        event Action<Exception> LoadingFailed;
        event Action<GameSession> LoadingCompleted;
    }
}