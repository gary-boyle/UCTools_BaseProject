using System.Threading.Tasks;
using GameFramework.Services.Interfaces;
using GameFramework.DataStructures;
using GameFramework.SaveSystem.Data;

namespace GameFramework.LoadSystem.Interfaces
{
    /// <summary>
    /// Interface for loading save data and transforming it into live game state
    /// Handles game logic for loading, not file operations
    /// </summary>
    public interface ILoadService : IGameService
    {
        /// <summary>
        /// Indicates if a load operation is currently in progress
        /// </summary>
        bool IsLoading { get; }

        /// <summary>
        /// Loads a save file and applies it to the current game state
        /// </summary>
        /// <param name="saveFileInfo">Save file to load</param>
        /// <returns>True if loading succeeded</returns>
        Task<bool> LoadGameStateAsync(SaveFileInfo saveFileInfo);

        /// <summary>
        /// Loads save data from SaveFileDataV2 and applies it to game state
        /// Includes scene loading and progress reporting  
        /// </summary>
        /// <param name="saveFileData">Save file data to apply</param>
        /// <param name="isNewGame">Whether this is loading a new game (affects progress messages)</param>
        /// <returns>True if loading succeeded</returns>
        Task<bool> LoadGameStateAsync(SaveFileData saveFileData, bool isNewGame = false);

        /// <summary>
        /// Converts SaveFileDataV2 to live game objects without applying to game state
        /// Useful for previewing or validation
        /// </summary>
        /// <param name="saveFileData">Save data to convert</param>
        /// <returns>Converted game objects or null if conversion failed</returns>
        Task<LoadedGameState> ConvertSaveDataAsync(SaveFileData saveFileData);
    }
}
