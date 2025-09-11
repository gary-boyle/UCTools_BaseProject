using System.Threading.Tasks;
using GameFramework.DataStructures;

namespace GameFramework.Services.Interfaces
{
    /// <summary>
    /// Interface for save service that handles both file operations and business logic
    /// Provides clean separation between low-level file I/O and high-level save operations
    /// Supports both regular saves and autosaves with automatic file management
    /// </summary>
    public interface ISaveService : IGameService
    {
        bool IsInitialized { get; }
        
        Task InitializeAsync();
        
        void Shutdown();
        
        #region Business Logic Methods

        bool CanSaveGame();
        
        Task<(bool success, string saveName)> PerformRegularSaveAsync();
        
        Task<(bool success, string saveName)> PerformAutoSaveAsync();
        
        #endregion
        
        #region Core Save/Load Operations
        
        Task<string[]> GetSaveFilesAsync();
        
        Task<bool> DeleteSaveAsync(string saveName);
        
        #endregion
        
        #region Utility Methods
        
        bool HasAnySaves();
        
        string GetSaveFilePath(string saveName);
        
        #endregion
        
        #region UI Support Operations
        
        Task<SaveFileInfo[]> GetSaveFileInfosAsync();
        
        Task<bool> DeleteSaveFileByInfoAsync(SaveFileInfo saveFileInfo);
        
        #endregion
    }
}
