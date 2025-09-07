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
        
        /// <summary>
        /// Checks if the game can currently be saved based on session state
        /// </summary>
        bool CanSaveGame();
        
        /// <summary>
        /// Performs a regular save with automatic timestamp-based naming
        /// Returns success status and the generated save name
        /// </summary>
        Task<(bool success, string saveName)> PerformRegularSaveAsync();
        
        /// <summary>
        /// Performs an autosave, always overwriting the existing autosave file
        /// Returns success status and the generated save name
        /// </summary>
        Task<(bool success, string saveName)> PerformAutoSaveAsync();
        
        #endregion
        
        #region Core Save/Load Operations
        
        /// <summary>
        /// Saves a game session with timestamp-based naming
        /// </summary>
        Task<bool> SaveGameSessionAsync(GameSession session, string saveName = null, bool isAutoSave = false);
        
        /// <summary>
        /// Loads a game session from a save file
        /// </summary>
        Task<GameSession> LoadGameSessionAsync(string saveName);
        
        /// <summary>
        /// Gets all save file names
        /// </summary>
        Task<string[]> GetSaveFilesAsync();
        
        /// <summary>
        /// Deletes a save file by name
        /// </summary>
        Task<bool> DeleteSaveAsync(string saveName);
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Checks if any save files exist
        /// </summary>
        bool HasAnySaves();
        
        /// <summary>
        /// Gets the name of the most recently saved file
        /// </summary>
        string GetMostRecentSaveName();
        
        #endregion
        
        #region UI Support Operations
        
        /// <summary>
        /// Gets formatted save file information for UI display, sorted by most recent first
        /// </summary>
        Task<SaveFileInfo[]> GetSaveFileInfosAsync();
        
        /// <summary>
        /// Gets formatted save file information for a specific save file
        /// </summary>
        Task<SaveFileInfo> GetSaveFileInfoAsync(string saveName);
        
        /// <summary>
        /// Deletes a save file using SaveFileInfo
        /// </summary>
        Task<bool> DeleteSaveFileByInfoAsync(SaveFileInfo saveFileInfo);
        
        /// <summary>
        /// Loads a game session using SaveFileInfo
        /// </summary>
        Task<GameSession> LoadGameSessionByInfoAsync(SaveFileInfo saveFileInfo);
        
        #endregion
    }
}
