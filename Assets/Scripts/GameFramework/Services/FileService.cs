using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using GameFramework.FileSystem.Interfaces;
using GameFramework.DataStructures;
using GameFramework.SaveSystem.Data;
using GameFramework.SaveSystem.Utilities;

namespace GameFramework.FileSystem.Services
{
    /// <summary>
    /// Service responsible for all save file system operations
    /// Handles reading, writing, deleting, and scanning save files
    /// Pure file I/O service with no game logic
    /// </summary>
    public class FileService : IFileService
    {
        #region Private Fields
        private string _saveDirectory;
        private const string SAVE_FILE_EXTENSION = ".json";
        private const string BACKUP_EXTENSION = ".backup";
        #endregion

        #region IGameService Implementation
        public bool IsInitialized { get; private set; }

        public async Task InitializeAsync()
        {
            if (IsInitialized) return;
            
            // Initialize save directory
            _saveDirectory = Path.Combine(Application.persistentDataPath, "Saves");
            
            try
            {
                if (!Directory.Exists(_saveDirectory))
                {
                    Directory.CreateDirectory(_saveDirectory);
                }

                IsInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FileService] Failed to initialize save directory: {ex.Message}");
                throw;
            }
        }

        public void Shutdown()
        {
            if (!IsInitialized) return;

            IsInitialized = false;
        }
        #endregion

        #region IFileService Implementation
        /// <summary>
        /// Gets all available save files as SaveFileInfo objects
        /// </summary>
        public async Task<SaveFileInfo[]> GetSaveFilesAsync()
        {
            if (!IsInitialized)
            {
                Debug.LogError("[FileService] Cannot get save files - service not initialized");
                return new SaveFileInfo[0];
            }

            try
            {
                if (!Directory.Exists(_saveDirectory))
                {
                    Debug.LogWarning($"[FileService] Save directory does not exist: {_saveDirectory}");
                    return new SaveFileInfo[0];
                }

                // Get all JSON files in save directory
                var saveFiles = Directory.GetFiles(_saveDirectory, $"*{SAVE_FILE_EXTENSION}")
                    .Where(filePath => !filePath.EndsWith(BACKUP_EXTENSION)) // Exclude backup files
                    .Select(filePath => SaveFileInfo.CreateFromFile(filePath))
                    .Where(info => info != null && info.IsValid())
                    .OrderByDescending(info => info.LastSaveTime)
                    .ToArray();

                return saveFiles;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FileService] Error scanning save files: {ex.Message}");
                return new SaveFileInfo[0];
            }
        }

        /// <summary>
        /// Reads and deserializes a save file
        /// </summary>
        public async Task<SaveFileData> ReadSaveFileAsync(string fileName)
        {
            if (!IsInitialized)
            {
                Debug.LogError("[FileService] Cannot read save file - service not initialized");
                return null;
            }

            if (string.IsNullOrEmpty(fileName))
            {
                Debug.LogError("[FileService] Cannot read save file - filename is null or empty");
                return null;
            }

            try
            {
                string filePath = GetSaveFilePath(fileName);
                
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"[FileService] Save file does not exist: {fileName}");
                    return null;
                }
                
                // Read file content
                string json = await File.ReadAllTextAsync(filePath);
                
                if (string.IsNullOrEmpty(json))
                {
                    Debug.LogError($"[FileService] Save file is empty: {fileName}");
                    return null;
                }

                // Deserialize JSON to SaveFileData
                var saveFileData = JsonSerializationHelper.DeserializeFromJson<SaveFileData>(json);
                
                if (saveFileData == null)
                {
                    Debug.LogError($"[FileService] Failed to deserialize save file: {fileName}");
                    return null;
                }

                return saveFileData;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FileService] Error reading save file {fileName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Writes SaveFileData to a save file with backup functionality
        /// </summary>
        public async Task<bool> WriteSaveFileAsync(string fileName, SaveFileData saveFileData)
        {
            if (!IsInitialized)
            {
                Debug.LogError("[FileService] Cannot write save file - service not initialized");
                return false;
            }

            if (string.IsNullOrEmpty(fileName) || saveFileData == null)
            {
                Debug.LogError("[FileService] Cannot write save file - invalid parameters");
                return false;
            }

            try
            {
                string filePath = GetSaveFilePath(fileName);
                
                // Serialize to JSON
                string json = JsonSerializationHelper.SerializeToJson(saveFileData, true);
                if (string.IsNullOrEmpty(json))
                {
                    Debug.LogError($"[FileService] Failed to serialize save data for {fileName}");
                    return false;
                }

                // Write to file
                await File.WriteAllTextAsync(filePath, json);

                // Verify the file was written correctly
                if (File.Exists(filePath)) return true;

                Debug.LogError($"[FileService] Save file was not created: {fileName}");
                return false;
                
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FileService] Error writing save file {fileName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Deletes a save file from disk
        /// </summary>
        public async Task<bool> DeleteSaveFileAsync(string fileName)
        {
            if (!IsInitialized)
            {
                Debug.LogError("[FileService] Cannot delete save file - service not initialized");
                return false;
            }

            if (string.IsNullOrEmpty(fileName))
            {
                Debug.LogError("[FileService] Cannot delete save file - filename is null or empty");
                return false;
            }

            try
            {
                string filePath = GetSaveFilePath(fileName);
                
                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"[FileService] Save file does not exist for deletion: {fileName}");
                    return true; // Consider it successfully "deleted" if it doesn't exist
                }
                
                File.Delete(filePath);
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FileService] Error deleting save file {fileName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if a save file exists
        /// </summary>
        public bool SaveFileExists(string fileName)
        {
            if (!IsInitialized || string.IsNullOrEmpty(fileName))
                return false;

            string filePath = GetSaveFilePath(fileName);
            return File.Exists(filePath);
        }

        /// <summary>
        /// Gets the full path to a save file
        /// </summary>
        public string GetSaveFilePath(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return string.Empty;

            // Ensure the filename has the correct extension
            if (!fileName.EndsWith(SAVE_FILE_EXTENSION))
            {
                fileName += SAVE_FILE_EXTENSION;
            }

            return Path.Combine(_saveDirectory, fileName);
        }

        #endregion
    }
}
