using System;
using System.Collections.Generic;
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
    /// Save service that handles only file saving operations and save file metadata
    /// Clean separation - only deals with writing files and providing save file information
    /// All loading operations are handled by LoadService
    /// </summary>
    public class SaveService : ISaveService
    {
        public bool IsInitialized { get; private set; }
        
        private readonly IEventSystem _eventSystem;
        private readonly string _saveDirectory;
        private const string SAVE_EXTENSION = ".gamesave";
        private const string AUTOSAVE_IDENTIFIER = "[AUTOSAVE]";
        
        private readonly Dictionary<string, SaveFileInfo> _saveFileInfoCache = new();
        
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
            ClearCaches();
            IsInitialized = false;
        }
        
        private void ClearCaches()
        {
            _saveFileInfoCache.Clear();
        }
        #endregion
        
        #region Save Operations
        
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
        /// Internal method that performs the actual save operation
        /// </summary>
        private async Task<bool> SaveGameSessionInternalAsync(GameSession session, string saveName, bool isAutoSave)
        {
            try
            {
                // Update session timestamp before saving
                session.UpdateLastSaveTime();
                
                // Store whether this was an autosave in the session
                session.SetCustomData("isAutoSave", isAutoSave.ToString());
                
                // Store current playtime info from TimeService for save file metadata
                var playTimeInfo = session.GetPlayTimeInfo();
                session.SetCustomData("playTimeAtSave", playTimeInfo.GameTime);
                session.SetCustomData("sessionTimeAtSave", playTimeInfo.SessionTime);
                session.SetCustomData("timeTrackingActive", playTimeInfo.IsTracking);
                
                var json = JsonUtility.ToJson(session, true);
                var filePath = GetSaveFilePath(saveName);
                
                await System.IO.File.WriteAllTextAsync(filePath, json);
                
                // Clear cache when new save is created
                InvalidateCache(saveName);
                
                _eventSystem.Publish(new SaveGameEvent());
                
                Debug.Log($"[SaveService] Game session saved as '{saveName}' (AutoSave: {isAutoSave}) - " +
                         $"Playtime: {playTimeInfo.FormattedGameTime}, Session: {playTimeInfo.FormattedSessionTime}");
                
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveService] Error saving game session '{saveName}': {e}");
                return false;
            }
        }
        
        private async Task DeleteExistingAutoSaveAsync()
        {
            try
            {
                var existingAutoSaves = await GetAutoSaveFilesAsync();
                foreach (string autoSaveFile in existingAutoSaves)
                {
                    await DeleteSaveAsync(autoSaveFile);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveService] Error deleting existing autosaves: {ex.Message}");
            }
        }
        
        private async Task<string[]> GetAutoSaveFilesAsync()
        {
            var allSaveFiles = await GetSaveFilesAsync();
            return allSaveFiles.Where(fileName => fileName.Contains(AUTOSAVE_IDENTIFIER)).ToArray();
        }
        
        #endregion
        
        #region File Operations
        
        public async Task<string[]> GetSaveFilesAsync()
        {
            return await Task.Run(() =>
            {
                if (!System.IO.Directory.Exists(_saveDirectory))
                    return Array.Empty<string>();
                    
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

            if (!System.IO.File.Exists(filePath)) return false;
            
            await Task.Run(() => System.IO.File.Delete(filePath));
                
            // Clear caches for deleted file
            InvalidateCache(saveName);
                
            Debug.Log($"[SaveService] Deleted save '{saveName}' and cleared caches");
            return true;

        }
        
        public bool HasAnySaves()
        {
            if (!System.IO.Directory.Exists(_saveDirectory))
                return false;
                
            var files = System.IO.Directory.GetFiles(_saveDirectory, "*" + SAVE_EXTENSION);
            return files.Length > 0;
        }
        
        /// <summary>
        /// Gets the full file path for a save file name
        /// </summary>
        public string GetSaveFilePath(string saveName)
        {
            return _saveDirectory + saveName + SAVE_EXTENSION;
        }
        
        #endregion
        
        #region Save File Information (Metadata)
        
        /// <summary>
        /// Gets formatted save file information with intelligent caching
        /// </summary>
        public async Task<SaveFileInfo[]> GetSaveFileInfosAsync()
        {
            try
            {
                var saveFileNames = await GetSaveFilesAsync();
                var saveFileInfos = new List<SaveFileInfo>();
                
                foreach (var fileName in saveFileNames)
                {
                    var info = await GetCachedSaveFileInfoAsync(fileName);
                    if (info != null)
                    {
                        saveFileInfos.Add(info);
                    }
                }
                
                // Sort by most recent first
                var sortedInfos = saveFileInfos
                    .OrderByDescending(info => info.lastSaveTime)
                    .ToArray();
                
                Debug.Log($"[SaveService] Loaded {sortedInfos.Length} save file infos (cached: {_saveFileInfoCache.Count})");
                return sortedInfos;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveService] Error loading save file infos: {e}");
                return Array.Empty<SaveFileInfo>();
            }
        }
        
        /// <summary>
        /// Gets save file info with caching - only loads from disk if file changed
        /// </summary>
        private async Task<SaveFileInfo> GetCachedSaveFileInfoAsync(string fileName)
        {
            var filePath = GetSaveFilePath(fileName);
            if (!System.IO.File.Exists(filePath))
                return null;
            
            var fileTime = System.IO.File.GetLastWriteTime(filePath);
            
            // Check if we have valid cached info
            if (_saveFileInfoCache.TryGetValue(fileName, out var cached) && 
                cached.lastSaveTime == fileTime)
            {
                return cached;
            }
            
            // Load fresh info - delegate to LoadService for actual loading
            var loadService = GameManager.GetService<ILoadService>();
            var info = await CreateSaveFileInfoAsync(fileName, loadService);
            if (info != null)
            {
                _saveFileInfoCache[fileName] = info;
            }
            
            return info;
        }
        
        /// <summary>
        /// Creates SaveFileInfo from a save file name using LoadService for loading
        /// </summary>
        private static async Task<SaveFileInfo> CreateSaveFileInfoAsync(string fileName, ILoadService loadService)
        {
            try
            {
                var gameSession = await loadService.LoadGameSessionAsync(fileName);
                if (gameSession == null) return null;
                
                var saveInfo = new SaveFileInfo(fileName, gameSession);
                Debug.Log($"[SaveService] Created SaveFileInfo for '{fileName}' - " +
                          $"Game time: {saveInfo.formattedPlayTime}, Session time: {saveInfo.formattedSessionTime}");
                return saveInfo;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveService] Failed to create SaveFileInfo for '{fileName}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Deletes a save file using SaveFileInfo
        /// </summary>
        public async Task<bool> DeleteSaveFileByInfoAsync(SaveFileInfo saveFileInfo)
        {
            if (saveFileInfo == null) return false;
            return await DeleteSaveAsync(saveFileInfo.fileName);
        }
        
        #endregion
        
        #region Helper Methods
        
        private static string GenerateTimestampSaveName(GameSession session, bool isAutoSave)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string playerName = session.playerName ?? "Player";
            
            return isAutoSave ? $"{playerName}_{AUTOSAVE_IDENTIFIER}_{timestamp}" : $"{playerName}_Save_{timestamp}";
        }
        
        public void InvalidateCache(string saveName)
        {
            _saveFileInfoCache.Remove(saveName);
        }
        
        #endregion
    }
}
