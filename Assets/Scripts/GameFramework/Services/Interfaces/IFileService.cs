using System.Threading.Tasks;
using GameFramework.Services.Interfaces;
using GameFramework.DataStructures;
using GameFramework.SaveSystem.Data;

namespace GameFramework.FileSystem.Interfaces
{
    /// <summary>
    /// Interface for file system operations related to save files
    /// Handles file I/O, directory management, and file metadata
    /// </summary>
    public interface IFileService : IGameService
    {
        /// <summary>
        /// Gets all available save files as SaveFileInfo objects for UI display
        /// </summary>
        /// <returns>Array of SaveFileInfo objects</returns>
        Task<SaveFileInfo[]> GetSaveFilesAsync();

        // /// <summary>
        // /// Reads and deserializes a save file to SaveFileData
        // /// </summary>
        // /// <param name="fileName">Name of the save file to read</param>
        // /// <returns>SaveFileData or null if reading failed</returns>
        // Task<SaveFileData> ReadSaveFileAsync(string fileName);

        // /// <summary>
        // /// Writes SaveFileData to a save file
        // /// </summary>
        // /// <param name="fileName">Name of the save file to write</param>
        // /// <param name="saveFileData">Data to write</param>
        // /// <returns>True if writing succeeded</returns>
        // Task<bool> WriteSaveFileAsync(string fileName, SaveFileData saveFileData);

        /// <summary>
        /// Deletes a save file from disk
        /// </summary>
        /// <param name="fileName">Name of the save file to delete</param>
        /// <returns>True if deletion succeeded</returns>
        Task<bool> DeleteSaveFileAsync(string fileName);

        /// <summary>
        /// Checks if a save file exists
        /// </summary>
        /// <param name="fileName">Name of the save file to check</param>
        /// <returns>True if file exists</returns>
        bool SaveFileExists(string fileName);
    }
}
