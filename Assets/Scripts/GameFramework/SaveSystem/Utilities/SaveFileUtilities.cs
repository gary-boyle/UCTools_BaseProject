using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using GameFramework.DataStructures;

namespace GameFramework.Utilities
{
    /// <summary>
    /// Static utility class for save file operations, path generation, and naming conventions
    /// Handles all file system operations and save file naming logic
    /// </summary>
    public static class SaveFileUtilities
    {
        private const string SAVE_EXTENSION = ".gamesave";
        private const string AUTOSAVE_IDENTIFIER = "[AUTOSAVE]";

        /// <summary>
        /// Gets the standard save directory path
        /// </summary>
        public static string SaveDirectory;

        /// <summary>
        /// Ensures the save directory exists, creating it if necessary
        /// </summary>
        public static void EnsureSaveDirectoryExists()
        {
            var saveDir = SaveDirectory;
            if (!Directory.Exists(saveDir))
            {
                Directory.CreateDirectory(saveDir);
                Debug.Log($"[SaveFileUtilities] Created save directory: {saveDir}");
            }
        }

        /// <summary>
        /// Gets the full file path for a save name
        /// </summary>
        /// <param name="saveName">Name of the save file</param>
        /// <returns>Full file path including extension</returns>
        public static string GetSaveFilePath(string saveName)
        {
            if (string.IsNullOrEmpty(saveName))
                throw new ArgumentException("Save name cannot be null or empty", nameof(saveName));

            return SaveDirectory + saveName + SAVE_EXTENSION;
        }

        /// <summary>
        /// Gets all save file names in the save directory
        /// </summary>
        /// <returns>Array of save file names without extensions</returns>
        public static string[] GetSaveFileNames()
        {
            if (!Directory.Exists(SaveDirectory))
                return Array.Empty<string>();

            var files = Directory.GetFiles(SaveDirectory, "*" + SAVE_EXTENSION);
            var saveNames = new string[files.Length];

            for (int i = 0; i < files.Length; i++)
            {
                saveNames[i] = Path.GetFileNameWithoutExtension(files[i]);
            }

            return saveNames;
        }

        /// <summary>
        /// Asynchronously gets all save file names
        /// </summary>
        /// <returns>Task containing array of save file names</returns>
        public static async Task<string[]> GetSaveFileNamesAsync()
        {
            return await Task.Run(GetSaveFileNames);
        }

        /// <summary>
        /// Checks if a save file exists
        /// </summary>
        /// <param name="saveName">Name of the save file</param>
        /// <returns>True if file exists</returns>
        public static bool SaveFileExists(string saveName)
        {
            if (string.IsNullOrEmpty(saveName)) return false;
            return File.Exists(GetSaveFilePath(saveName));
        }

        /// <summary>
        /// Deletes a save file
        /// </summary>
        /// <param name="saveName">Name of the save file to delete</param>
        /// <returns>True if file was deleted successfully</returns>
        public static bool DeleteSaveFile(string saveName)
        {
            if (string.IsNullOrEmpty(saveName)) return false;

            var filePath = GetSaveFilePath(saveName);
            if (!File.Exists(filePath)) return false;

            try
            {
                File.Delete(filePath);
                Debug.Log($"[SaveFileUtilities] Deleted save file: {saveName}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveFileUtilities] Failed to delete save file '{saveName}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Asynchronously deletes a save file
        /// </summary>
        /// <param name="saveName">Name of the save file to delete</param>
        /// <returns>Task containing true if deletion succeeded</returns>
        public static async Task<bool> DeleteSaveFileAsync(string saveName)
        {
            return await Task.Run(() => DeleteSaveFile(saveName));
        }

        /// <summary>
        /// Gets the last write time of a save file
        /// </summary>
        /// <param name="saveName">Name of the save file</param>
        /// <returns>Last write time, or DateTime.MinValue if file doesn't exist</returns>
        public static DateTime GetSaveFileLastWriteTime(string saveName)
        {
            if (string.IsNullOrEmpty(saveName)) return DateTime.MinValue;

            var filePath = GetSaveFilePath(saveName);
            return File.Exists(filePath) ? File.GetLastWriteTime(filePath) : DateTime.MinValue;
        }

        /// <summary>
        /// Generates timestamped save names for regular saves
        /// </summary>
        /// <param name="session">Game session for player name</param>
        /// <param name="isAutoSave">Whether this is an auto-save</param>
        /// <returns>Generated save name with timestamp</returns>
        public static string GenerateTimestampSaveName(GameSession session, bool isAutoSave)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string playerName = session.playerName ?? "Player";

            return isAutoSave 
                ? $"{playerName}_{AUTOSAVE_IDENTIFIER}_{timestamp}" 
                : $"{playerName}_Save_{timestamp}";
        }

        /// <summary>
        /// Generates consistent autosave names (without timestamps) for each player
        /// This ensures each player has only one autosave file that gets overwritten
        /// </summary>
        /// <param name="session">Game session for player name</param>
        /// <returns>Generated autosave name</returns>
        public static string GenerateAutoSaveName(GameSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            string playerName = session.playerName ?? "Player";
            return $"{playerName}_{AUTOSAVE_IDENTIFIER}";
        }

        /// <summary>
        /// Gets autosave files specifically for a given player name
        /// </summary>
        /// <param name="playerName">Name of the player</param>
        /// <returns>Array of autosave file names for the player</returns>
        public static string[] GetPlayerAutoSaveFiles(string playerName)
        {
            if (string.IsNullOrEmpty(playerName)) return Array.Empty<string>();

            var allSaveFiles = GetSaveFileNames();
            return allSaveFiles.Where(fileName =>
                fileName.Contains(AUTOSAVE_IDENTIFIER) &&
                fileName.StartsWith(playerName + "_", StringComparison.OrdinalIgnoreCase)
            ).ToArray();
        }

        /// <summary>
        /// Asynchronously gets autosave files for a player
        /// </summary>
        /// <param name="playerName">Name of the player</param>
        /// <returns>Task containing array of autosave file names</returns>
        public static async Task<string[]> GetPlayerAutoSaveFilesAsync(string playerName)
        {
            return await Task.Run(() => GetPlayerAutoSaveFiles(playerName));
        }

        /// <summary>
        /// Checks if any save files exist in the save directory
        /// </summary>
        /// <returns>True if at least one save file exists</returns>
        public static bool HasAnySaveFiles()
        {
            return GetSaveFileNames().Length > 0;
        }

        /// <summary>
        /// Reads save file content as string
        /// </summary>
        /// <param name="saveName">Name of the save file</param>
        /// <returns>File content as string, or null if file doesn't exist</returns>
        public static async Task<string> ReadSaveFileAsync(string saveName)
        {
            if (string.IsNullOrEmpty(saveName)) return null;

            var filePath = GetSaveFilePath(saveName);
            if (!File.Exists(filePath)) return null;

            try
            {
                return await File.ReadAllTextAsync(filePath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveFileUtilities] Failed to read save file '{saveName}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Writes content to a save file
        /// </summary>
        /// <param name="saveName">Name of the save file</param>
        /// <param name="content">Content to write</param>
        /// <returns>True if write succeeded</returns>
        public static async Task<bool> WriteSaveFileAsync(string saveName, string content)
        {
            if (string.IsNullOrEmpty(saveName) || content == null) return false;

            EnsureSaveDirectoryExists();
            var filePath = GetSaveFilePath(saveName);

            try
            {
                await File.WriteAllTextAsync(filePath, content);
                Debug.Log($"[SaveFileUtilities] Successfully wrote save file: {saveName}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveFileUtilities] Failed to write save file '{saveName}': {ex.Message}");
                return false;
            }
        }

    }
}
