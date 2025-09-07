using System;
using System.Linq;
using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.DataStructures;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Interfaces;
using UnityEngine;

namespace GameFramework.Services
{
    /// <summary>
    /// Enhanced save service that handles both file operations and UI support
    /// Provides clean separation between low-level file I/O and high-level UI operations
    /// Uses timestamp-based naming for all save files with special autosave handling
    /// Autosaves always overwrite existing autosave files to prevent save directory bloat
    /// Works with GameDataService as the single source of session data
    /// </summary>
    public class SaveService : ISaveService
    {
        public bool IsInitialized { get; private set; }
        
        private readonly IEventSystem _eventSystem;
        private readonly string _saveDirectory;
        private const string SAVE_EXTENSION = ".gamesave";
        private const string AUTOSAVE_IDENTIFIER = "[AUTOSAVE]";
        
        public SaveService(IEventSystem eventSystem)
        {
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            _saveDirectory = Application.persistentDataPath + "/Saves/";
        }
        
        #region Initialization
        public async Task InitializeAsync()
        {
            if (IsInitialized) return;
            
            Debug.Log("[SaveService] Initializing save system...");
            
            // Ensure save directory exists
            if (!System.IO.Directory.Exists(_saveDirectory))
            {
                System.IO.Directory.CreateDirectory(_saveDirectory);
            }
            
            IsInitialized = true;
            await Task.CompletedTask;
        }
        
        public void Shutdown()
        {
            IsInitialized = false;
        }
        #endregion
        
        #region Business Logic Methods
        
        /// <summary>
        /// Checks if the game can currently be saved based on session state
        /// </summary>
        public bool CanSaveGame()
        {
            var gameDataService = GameManager.GetService<IGameDataService>();
            return gameDataService?.CurrentSession != null;
        }
        
        /// <summary>
        /// Performs a regular save with automatic timestamp-based naming
        /// </summary>
        public async Task<(bool success, string saveName)> PerformRegularSaveAsync()
        {
            var gameDataService = GameManager.GetService<IGameDataService>();
            if (gameDataService?.CurrentSession == null)
            {
                Debug.LogError("[SaveService] No active game session to save");
                return (false, null);
            }

            string saveName = GenerateTimestampSaveName(gameDataService.CurrentSession, false);
            bool success = await SaveGameSessionInternalAsync(gameDataService.CurrentSession, saveName, false);
            
            return (success, saveName);
        }
        
        /// <summary>
        /// Performs an autosave, always overwriting the existing autosave file
        /// </summary>
        public async Task<(bool success, string saveName)> PerformAutoSaveAsync()
        {
            var gameDataService = GameManager.GetService<IGameDataService>();
            if (gameDataService?.CurrentSession == null)
            {
                Debug.LogError("[SaveService] No active game session to autosave");
                return (false, null);
            }

            // Delete existing autosave first
            await DeleteExistingAutoSaveAsync();
            
            string saveName = GenerateTimestampSaveName(gameDataService.CurrentSession, true);
            bool success = await SaveGameSessionInternalAsync(gameDataService.CurrentSession, saveName, true);
            
            return (success, saveName);
        }
        
        /// <summary>
        /// Deletes any existing autosave files to ensure only one autosave exists
        /// </summary>
        private async Task DeleteExistingAutoSaveAsync()
        {
            try
            {
                var existingAutoSaves = await GetAutoSaveFilesAsync();
                foreach (string autoSaveFile in existingAutoSaves)
                {
                    await DeleteSaveAsync(autoSaveFile);
                    Debug.Log($"[SaveService] Deleted existing autosave: {autoSaveFile}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveService] Error deleting existing autosaves: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Gets all autosave file names
        /// </summary>
        private async Task<string[]> GetAutoSaveFilesAsync()
        {
            var allSaveFiles = await GetSaveFilesAsync();
            return allSaveFiles.Where(fileName => fileName.Contains(AUTOSAVE_IDENTIFIER)).ToArray();
        }
        
        #endregion
        
        #region Core Save/Load Operations
        
        /// <summary>
        /// Legacy method for backward compatibility - now delegates to business logic methods
        /// </summary>
        public async Task<bool> SaveGameSessionAsync(GameSession session, string saveName = null, bool isAutoSave = false)
        {
            if (session == null) return false;
            
            // If no save name provided, generate one
            if (string.IsNullOrEmpty(saveName))
            {
                saveName = GenerateTimestampSaveName(session, isAutoSave);
            }
            
            return await SaveGameSessionInternalAsync(session, saveName, isAutoSave);
        }
        
        /// <summary>
        /// Internal method that performs the actual save operation
        /// </summary>
        private async Task<bool> SaveGameSessionInternalAsync(GameSession session, string saveName, bool isAutoSave)
        {
            try
            {
                // Update session timestamp before saving
                session.lastSaveTime = DateTime.Now;
                session.UpdatePlayTime();
                
                // Store whether this was an autosave in the session
                session.customData["isAutoSave"] = isAutoSave.ToString();
                
                var json = JsonUtility.ToJson(session, true);
                var filePath = GetSaveFilePath(saveName);
                
                await System.IO.File.WriteAllTextAsync(filePath, json);
                
                _eventSystem.Publish(new SaveGameEvent());
                Debug.Log($"[SaveService] Game session saved as '{saveName}' (AutoSave: {isAutoSave})");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveService] Error saving game session '{saveName}': {e}");
                return false;
            }
        }
        
        public async Task<GameSession> LoadGameSessionAsync(string saveName)
        {
            var filePath = GetSaveFilePath(saveName);
            
            if (!System.IO.File.Exists(filePath))
            {
                Debug.LogWarning($"[SaveService] Save file '{saveName}' not found");
                return null;
            }
            
            try
            {
                var json = await System.IO.File.ReadAllTextAsync(filePath);
                var session = JsonUtility.FromJson<GameSession>(json);
                
                _eventSystem.Publish(new LoadGameEvent());
                Debug.Log($"[SaveService] Game session loaded from '{saveName}'");
                return session;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveService] Error loading save '{saveName}': {e}");
                return null;
            }
        }
        
        public async Task<string[]> GetSaveFilesAsync()
        {
            return await Task.Run(() =>
            {
                if (!System.IO.Directory.Exists(_saveDirectory))
                    return new string[0];
                    
                var files = System.IO.Directory.GetFiles(_saveDirectory, "*" + SAVE_EXTENSION);
                var saveNames = new string[files.Length];
                
                for (int i = 0; i < files.Length; i++)
                {
                    saveNames[i] = System.IO.Path.GetFileNameWithoutExtension(files[i]);
                }
                
                return saveNames;
            });
        }
        
        public async Task<bool> DeleteSaveAsync(string saveName)
        {
            var filePath = GetSaveFilePath(saveName);
            
            if (System.IO.File.Exists(filePath))
            {
                await Task.Run(() => System.IO.File.Delete(filePath));
                Debug.Log($"[SaveService] Deleted save '{saveName}'");
                return true;
            }
            
            return false;
        }
        
        public bool HasAnySaves()
        {
            if (!System.IO.Directory.Exists(_saveDirectory))
                return false;
                
            var files = System.IO.Directory.GetFiles(_saveDirectory, "*" + SAVE_EXTENSION);
            return files.Length > 0;
        }
        
        public string GetMostRecentSaveName()
        {
            if (!System.IO.Directory.Exists(_saveDirectory))
                return null;
                
            var files = System.IO.Directory.GetFiles(_saveDirectory, "*" + SAVE_EXTENSION);
            if (files.Length == 0)
                return null;
                
            var mostRecentFile = "";
            var mostRecentTime = DateTime.MinValue;
            
            foreach (var file in files)
            {
                var writeTime = System.IO.File.GetLastWriteTime(file);
                if (writeTime > mostRecentTime)
                {
                    mostRecentTime = writeTime;
                    mostRecentFile = file;
                }
            }
            
            return System.IO.Path.GetFileNameWithoutExtension(mostRecentFile);
        }
        #endregion
        
        #region UI Support Operations
        /// <summary>
        /// Gets formatted save file information for UI display, sorted by most recent first
        /// </summary>
        public async Task<SaveFileInfo[]> GetSaveFileInfosAsync()
        {
            try
            {
                var saveFileNames = await GetSaveFilesAsync();
                
                // Load save file info in parallel for better performance
                var loadTasks = saveFileNames.Select(CreateSaveFileInfoAsync);
                var results = await Task.WhenAll(loadTasks);
                
                // Filter out nulls and sort by most recent first
                return results
                    .Where(info => info != null)
                    .OrderByDescending(info => info.lastSaveTime)
                    .ToArray();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveService] Error loading save file infos: {e}");
                return new SaveFileInfo[0];
            }
        }
        
        /// <summary>
        /// Gets formatted save file information for a specific save file
        /// </summary>
        public async Task<SaveFileInfo> GetSaveFileInfoAsync(string saveName)
        {
            return await CreateSaveFileInfoAsync(saveName);
        }
        
        /// <summary>
        /// Deletes a save file using SaveFileInfo
        /// </summary>
        public async Task<bool> DeleteSaveFileByInfoAsync(SaveFileInfo saveFileInfo)
        {
            if (saveFileInfo == null) return false;
            return await DeleteSaveAsync(saveFileInfo.fileName);
        }
        
        /// <summary>
        /// Loads a game session using SaveFileInfo
        /// </summary>
        public async Task<GameSession> LoadGameSessionByInfoAsync(SaveFileInfo saveFileInfo)
        {
            if (saveFileInfo == null) return null;
            return await LoadGameSessionAsync(saveFileInfo.fileName);
        }
        #endregion
        
        #region Private Helper Methods
        private string GetSaveFilePath(string saveName)
        {
            return _saveDirectory + saveName + SAVE_EXTENSION;
        }
        
        /// <summary>
        /// Generates a timestamp-based save name
        /// </summary>
        private string GenerateTimestampSaveName(GameSession session, bool isAutoSave)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string playerName = session.playerName ?? "Player";
            
            if (isAutoSave)
            {
                return $"{playerName}_{AUTOSAVE_IDENTIFIER}_{timestamp}";
            }
            else
            {
                return $"{playerName}_Save_{timestamp}";
            }
        }
        
        /// <summary>
        /// Creates SaveFileInfo from a save file name, handling errors gracefully
        /// </summary>
        private async Task<SaveFileInfo> CreateSaveFileInfoAsync(string fileName)
        {
            try
            {
                var gameSession = await LoadGameSessionAsync(fileName);
                return gameSession != null ? new SaveFileInfo(fileName, gameSession) : null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveService] Failed to create SaveFileInfo for '{fileName}': {ex.Message}");
                return null;
            }
        }
        #endregion
    }
}
