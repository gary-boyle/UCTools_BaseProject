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
    /// Save service that handles file saving operations and save file metadata via event system
    /// Clean separation - UI triggers saves via events, service handles implementation
    /// All loading operations are handled by LoadService
    /// 
    /// Design: Event-driven architecture with caching for performance
    /// Pros: Decoupled, performant metadata caching, player-specific autosaves
    /// Cons: Async complexity, cache invalidation management
    /// </summary>
    public class SaveService : ISaveService
    {
        public bool IsInitialized { get; private set; }
        
        private readonly IEventSystem _eventSystem;
        private readonly string _saveDirectory;
        private const string SAVE_EXTENSION = ".gamesave";
        private const string AUTOSAVE_IDENTIFIER = "[AUTOSAVE]";
        
        // Cache for save file metadata to avoid repeated file I/O
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
            
            // Subscribe to save request events
            _eventSystem.Subscribe<RegularSaveRequestedEvent>(OnRegularSaveRequested);
            _eventSystem.Subscribe<AutoSaveRequestedEvent>(OnAutoSaveRequested);
            _eventSystem.Subscribe<OverwriteSaveRequestedEvent>(OnOverwriteSaveRequested);
            
            IsInitialized = true;
            await Task.CompletedTask;
        }
        
        public void Shutdown()
        {
            // Unsubscribe from events
            _eventSystem.Unsubscribe<RegularSaveRequestedEvent>(OnRegularSaveRequested);
            _eventSystem.Unsubscribe<AutoSaveRequestedEvent>(OnAutoSaveRequested);
            _eventSystem.Unsubscribe<OverwriteSaveRequestedEvent>(OnOverwriteSaveRequested);
            
            ClearCaches();
            IsInitialized = false;
        }
        
        /// <summary>
        /// Clears all cached save file metadata
        /// </summary>
        private void ClearCaches()
        {
            _saveFileInfoCache.Clear();
        }
        
        #endregion
        
        #region Public Interface - Save Validation
        
        /// <summary>
        /// Checks if the game can currently be saved based on session state
        /// </summary>
        public bool CanSaveGame()
        {
            var gameDataService = GameManager.GetService<IGameDataService>();
            return gameDataService?.CurrentSession != null;
        }
        
        #endregion
        
        #region Public Interface - File Management
        
        /// <summary>
        /// Gets all save file names asynchronously
        /// </summary>
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
        
        /// <summary>
        /// Deletes a save file and clears its cache entry
        /// </summary>
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
        
        /// <summary>
        /// Deletes a save file using SaveFileInfo object
        /// </summary>
        public async Task<bool> DeleteSaveFileByInfoAsync(SaveFileInfo saveFileInfo)
        {
            if (saveFileInfo == null) return false;
            return await DeleteSaveAsync(saveFileInfo.fileName);
        }
        
        #endregion
        
        #region Public Interface - Metadata & Utilities
        
        /// <summary>
        /// Gets metadata for all save files, sorted by most recent first
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
                
                return sortedInfos;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveService] Error loading save file infos: {e}");
                return Array.Empty<SaveFileInfo>();
            }
        }
        
        /// <summary>
        /// Checks if any save files exist
        /// </summary>
        public bool HasAnySaves()
        {
            if (!System.IO.Directory.Exists(_saveDirectory))
                return false;
                
            var files = System.IO.Directory.GetFiles(_saveDirectory, "*" + SAVE_EXTENSION);
            return files.Length > 0;
        }
        
        /// <summary>
        /// Gets the full file path for a save name
        /// </summary>
        public string GetSaveFilePath(string saveName)
        {
            return _saveDirectory + saveName + SAVE_EXTENSION;
        }
        
        /// <summary>
        /// Invalidates cached metadata for a specific save file
        /// </summary>
        public void InvalidateCache(string saveName)
        {
            _saveFileInfoCache.Remove(saveName);
        }
        
        #endregion
        
        #region Private Save Operations (Event-Driven)
        
        /// <summary>
        /// Performs a regular save with automatic timestamp-based naming
        /// Called internally via event system
        /// </summary>
        private async Task<(bool success, string saveName)> PerformRegularSaveAsync()
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
        /// Performs an autosave, only overwriting existing autosaves for the current player
        /// Uses consistent naming instead of timestamps for autosaves
        /// </summary>
        private async Task<(bool success, string saveName)> PerformAutoSaveAsync()
        {
            var gameDataService = GameManager.GetService<IGameDataService>();
            if (gameDataService?.CurrentSession == null)
            {
                Debug.LogError("[SaveService] No active game session to autosave");
                return (false, null);
            }

            // Delete existing autosave for current player only
            await DeleteCurrentPlayerAutoSaveAsync(gameDataService.CurrentSession.playerName);
            
            // Use consistent autosave naming (no timestamp) for each player
            string saveName = GenerateAutoSaveName(gameDataService.CurrentSession);
            bool success = await SaveGameSessionInternalAsync(gameDataService.CurrentSession, saveName, true);
            
            return (success, saveName);
        }
        
        /// <summary>
        /// Overwrites an existing save file with the current game session
        /// Called internally via event system
        /// </summary>
        private async Task<bool> OverwriteSaveFileAsync(SaveFileInfo targetSaveFile)
        {
            if (!CanOverwriteSaveFile(targetSaveFile)) return false;

            var gameDataService = GameManager.GetService<IGameDataService>();
            
            if (gameDataService?.CurrentSession == null)
            {
                Debug.LogError("[SaveService] No active game session to save for overwrite");
                return false;
            }

            if (targetSaveFile == null)
            {
                Debug.LogError("[SaveService] No target save file specified for overwrite");
                return false;
            }

            try
            {
                string saveName = targetSaveFile.fileName;
                bool wasAutoSave = saveName.Contains(AUTOSAVE_IDENTIFIER);
                bool success = await SaveGameSessionInternalAsync(gameDataService.CurrentSession, saveName, wasAutoSave);
        
                if (!success)
                {
                    Debug.LogError($"[SaveService] Failed to overwrite save file: {saveName}");
                }
        
                return success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveService] Error overwriting save file '{targetSaveFile.fileName}': {ex}");
                return false;
            }
        }
        
        #endregion
        
        #region Private Helper Methods
        
        /// <summary>
        /// Checks if a specific save file can be overwritten
        /// </summary>
        private bool CanOverwriteSaveFile(SaveFileInfo targetSaveFile)
        {
            if (!CanSaveGame()) return false;
            if (targetSaveFile == null) return false;
    
            var filePath = GetSaveFilePath(targetSaveFile.fileName);
            return System.IO.File.Exists(filePath);
        }
        
        /// <summary>
        /// Internal method that performs the actual save operation with game session data
        /// Updates playtime tracking and metadata before saving
        /// </summary>
        private async Task<bool> SaveGameSessionInternalAsync(GameSession session, string saveName, bool isAutoSave)
        {
            try
            {
                session.UpdateLastSaveTime();
                session.WasAutoSave = isAutoSave;
                
                // Store playtime information in custom data
                var playTimeInfo = session.GetPlayTimeInfo();
                session.SetCustomData("playTimeAtSave", playTimeInfo.GameTime);
                session.SetCustomData("sessionTimeAtSave", playTimeInfo.SessionTime);
                session.SetCustomData("timeTrackingActive", playTimeInfo.IsTracking);
                
                var json = JsonUtility.ToJson(session, true);
                var filePath = GetSaveFilePath(saveName);
                
                await System.IO.File.WriteAllTextAsync(filePath, json);
                
                // Invalidate cache since file content changed
                InvalidateCache(saveName);
                _eventSystem.Publish(new SaveGameEvent());
                
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveService] Error saving game session '{saveName}': {e}");
                return false;
            }
        }
        
        /// <summary>
        /// Deletes only the autosave file for the current player, preserving other players' autosaves
        /// This ensures each player maintains their own autosave without interfering with others
        /// </summary>
        private async Task DeleteCurrentPlayerAutoSaveAsync(string currentPlayerName)
        {
            if (string.IsNullOrEmpty(currentPlayerName))
            {
                Debug.LogWarning("[SaveService] Cannot delete player autosave - no current player name");
                return;
            }
                
            try
            {
                var playerAutoSaveFiles = await GetPlayerAutoSaveFilesAsync(currentPlayerName);
                foreach (string autoSaveFile in playerAutoSaveFiles)
                {
                    await DeleteSaveAsync(autoSaveFile);
                    Debug.Log($"[SaveService] Deleted existing autosave for player '{currentPlayerName}': {autoSaveFile}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveService] Error deleting autosaves for player '{currentPlayerName}': {ex.Message}");
            }
        }
        
        /// <summary>
        /// Gets autosave files specifically for a given player name
        /// </summary>
        private async Task<string[]> GetPlayerAutoSaveFilesAsync(string playerName)
        {
            var allSaveFiles = await GetSaveFilesAsync();
            return allSaveFiles.Where(fileName => 
                fileName.Contains(AUTOSAVE_IDENTIFIER) && 
                fileName.StartsWith(playerName + "_", StringComparison.OrdinalIgnoreCase)
            ).ToArray();
        }
        
        
        /// <summary>
        /// Gets cached save file info, loading and caching it if not already cached
        /// Uses file modification time to determine if cache is still valid
        /// </summary>
        private async Task<SaveFileInfo> GetCachedSaveFileInfoAsync(string fileName)
        {
            var filePath = GetSaveFilePath(fileName);
            if (!System.IO.File.Exists(filePath))
                return null;
            
            var fileTime = System.IO.File.GetLastWriteTime(filePath);
            
            // Check if we have valid cached data
            if (_saveFileInfoCache.TryGetValue(fileName, out var cached) && 
                cached.lastSaveTime == fileTime)
            {
                return cached;
            }
            
            // Load and cache new data
            var loadService = GameManager.GetService<ILoadService>();
            var info = await CreateSaveFileInfoAsync(fileName, loadService);
            if (info != null)
            {
                _saveFileInfoCache[fileName] = info;
            }
            
            return info;
        }
        
        /// <summary>
        /// Creates SaveFileInfo by loading the game session data
        /// </summary>
        private static async Task<SaveFileInfo> CreateSaveFileInfoAsync(string fileName, ILoadService loadService)
        {
            try
            {
                var gameSession = await loadService.LoadGameSessionAsync(fileName);
                if (gameSession == null) return null;
                
                var saveInfo = new SaveFileInfo(fileName, gameSession);
                return saveInfo;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveService] Failed to create SaveFileInfo for '{fileName}': {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Generates timestamped save names for regular saves
        /// </summary>
        private static string GenerateTimestampSaveName(GameSession session, bool isAutoSave)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string playerName = session.playerName ?? "Player";
            
            return isAutoSave ? $"{playerName}_{AUTOSAVE_IDENTIFIER}_{timestamp}" : $"{playerName}_Save_{timestamp}";
        }
        
        /// <summary>
        /// Generates consistent autosave names (without timestamps) for each player
        /// This ensures each player has only one autosave file that gets overwritten
        /// </summary>
        private static string GenerateAutoSaveName(GameSession session)
        {
            string playerName = session.playerName ?? "Player";
            return $"{playerName}_{AUTOSAVE_IDENTIFIER}";
        }
        
        #endregion
        
        #region Event Handlers
        
        /// <summary>
        /// Handles regular save requests from the event system
        /// </summary>
        private async void OnRegularSaveRequested(RegularSaveRequestedEvent saveEvent)
        {
            try
            {
                var (success, saveName) = await PerformRegularSaveAsync();
                
                if (success)
                {
                    _eventSystem.Publish(new SaveCompletedEvent(saveName, false, false));
                }
                else
                {
                    _eventSystem.Publish(new SaveFailedEvent("Regular save operation failed", false, false));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveService] Error handling regular save request: {ex}");
                _eventSystem.Publish(new SaveFailedEvent(ex.Message, false, false, ex));
            }
        }

        /// <summary>
        /// Handles auto-save requests from the event system
        /// </summary>
        private async void OnAutoSaveRequested(AutoSaveRequestedEvent saveEvent)
        {
            try
            {
                var (success, saveName) = await PerformAutoSaveAsync();
                
                if (success)
                {
                    _eventSystem.Publish(new SaveCompletedEvent(saveName, true, false));
                }
                else
                {
                    _eventSystem.Publish(new SaveFailedEvent("Auto-save operation failed", true, false));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveService] Error handling auto-save request: {ex}");
                _eventSystem.Publish(new SaveFailedEvent(ex.Message, true, false, ex));
            }
        }

        /// <summary>
        /// Handles overwrite save requests from the event system
        /// </summary>
        private async void OnOverwriteSaveRequested(OverwriteSaveRequestedEvent saveEvent)
        {
            try
            {
                bool success = await OverwriteSaveFileAsync(saveEvent.TargetSaveFile);
                
                if (success)
                {
                    _eventSystem.Publish(new SaveCompletedEvent(saveEvent.TargetSaveFile.fileName, false, true));
                }
                else
                {
                    _eventSystem.Publish(new SaveFailedEvent("Overwrite save operation failed", false, true));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveService] Error handling overwrite save request: {ex}");
                _eventSystem.Publish(new SaveFailedEvent(ex.Message, false, true, ex));
            }
        }
        
        #endregion
    }
}
