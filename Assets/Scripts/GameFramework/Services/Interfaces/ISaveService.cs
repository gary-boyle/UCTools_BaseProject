using System;
using System.Threading.Tasks;
using GameFramework.DataStructures;

namespace GameFramework.Services.Interfaces
{
    /// <summary>
    /// Enhanced save service interface that handles both low-level file operations
    /// and higher-level UI support operations
    /// </summary>
    public interface ISaveService : IGameService
    {
        bool IsInitialized { get; }
        
        // Core save/load operations
        Task InitializeAsync();
        void Shutdown();
        Task<bool> SaveGameSessionAsync(GameSession session, string saveName = null);
        Task<GameSession> LoadGameSessionAsync(string saveName);
        Task<string[]> GetSaveFilesAsync();
        Task<bool> DeleteSaveAsync(string saveName);
        bool HasAnySaves();
        string GetMostRecentSaveName();
        
        // UI support operations - these handle the conversion and formatting
        Task<SaveFileInfo[]> GetSaveFileInfosAsync();
        Task<SaveFileInfo> GetSaveFileInfoAsync(string saveName);
        Task<bool> DeleteSaveFileByInfoAsync(SaveFileInfo saveFileInfo);
        Task<GameSession> LoadGameSessionByInfoAsync(SaveFileInfo saveFileInfo);
    }
}