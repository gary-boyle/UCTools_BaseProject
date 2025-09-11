using System.Threading.Tasks;
using GameFramework.DataStructures;

namespace GameFramework.Services.Interfaces
{
    /// <summary>
    /// Interface for save service that handles file operations and save file metadata
    /// Save operations are triggered via event system for clean separation of concerns
    /// Provides file management and metadata retrieval for UI systems
    /// 
    /// INTENT: Minimal interface focused on file operations and metadata
    /// DESIGN: Save operations moved to event system, interface only exposes what external systems need
    /// PROS: Clean separation, reduced coupling, focused responsibilities
    /// CONS: Less direct control for external systems (but that's by design)
    /// </summary>
    public interface ISaveService : IGameService
    {
        bool IsInitialized { get; }
        
        Task InitializeAsync();
        void Shutdown();
        
        #region Save Validation
        
        /// <summary>
        /// Checks if the game can currently be saved based on session state
        /// Used by UI to determine save button availability
        /// </summary>
        bool CanSaveGame();
        
        #endregion
        
        #region File Management
        
        /// <summary>
        /// Deletes a save file using SaveFileInfo
        /// </summary>
        Task<bool> DeleteSaveFileByInfoAsync(SaveFileInfo saveFileInfo);
        
        #endregion
        
        #region Metadata & Utilities
        
        /// <summary>
        /// Gets formatted save file information with caching
        /// </summary>
        Task<SaveFileInfo[]> GetSaveFileInfosAsync();
        
        /// <summary>
        /// Checks if any save files exist
        /// </summary>
        bool HasAnySaves();
        
        /// <summary>
        /// Gets the full file path for a save file name
        /// </summary>
        string GetSaveFilePath(string saveName);
        
        #endregion
    }
}
