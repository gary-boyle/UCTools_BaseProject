using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameFramework.Config.ScriptableObjects;
using GameFramework.Core;
using GameFramework.DataStructures;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Events.Enums;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Interfaces;
using GameFramework.Utilities;
using UnityEngine;

namespace GameFramework.Services
{
    /// <summary>
    /// Save service that handles file saving operations and save file metadata via event system
    /// Now uses unified save event handling for cleaner code and better maintainability
    /// Clean separation - UI triggers saves via events, service handles implementation
    /// All loading operations are handled by LoadService
    /// </summary>
    public class SaveService : ISaveService, IUpdatable
    {
        public bool IsInitialized { get; private set; }

        private GameplaySettings_SO _gameplaySettings;
        
        private readonly IEventSystem _eventSystem;
        private readonly IGameDataService _gameDataService;
        
        // Cache for save file metadata to avoid repeated file I/O
        private readonly Dictionary<string, SaveFileInfo> _saveFileInfoCache = new();
        
        // Auto-save scheduling
        private float _autoSaveTimer = 0f;
        private float _autoSaveInterval = 300f; // Default 5 minutes in seconds
        private bool _autoSaveEnabled = true;
        private bool _autoSaveSchedulingActive = false;

        public SaveService(IEventSystem eventSystem, IGameDataService gameDataService)
        {
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            _gameDataService = gameDataService ?? throw new ArgumentNullException(nameof(gameDataService));
        }

        #region Initialization
        
        public async Task InitializeAsync()
        {
            if (IsInitialized) return;
            
            _gameplaySettings = SettingsRegistry.Get<GameplaySettings_SO>();
            SaveFileUtilities.SaveDirectory = Application.persistentDataPath + "/Saves/";
            SaveFileUtilities.EnsureSaveDirectoryExists();
            
            // Subscribe to unified save request event
            _eventSystem.Subscribe<SaveRequestedEvent>(OnSaveRequested);
            
            // Subscribe to config changes for gameplay settings
            _eventSystem.Subscribe<OptionsChangedEvent>(OnOptionsChanged);
            
            // Apply initial gameplay settings
            ApplyGameplaySettings();
            
            IsInitialized = true;
            await Task.CompletedTask;
        }
        
        public void Shutdown()
        {
            // Unsubscribe from events
            _eventSystem.Unsubscribe<SaveRequestedEvent>(OnSaveRequested);
            _eventSystem.Unsubscribe<OptionsChangedEvent>(OnOptionsChanged);
            
            // Stop auto-save scheduling
            SetAutoSaveSchedulingActive(false);
            
            ClearCaches();
            IsInitialized = false;
        }

        /// <summary>
        /// IUpdatable implementation - handles auto-save timer
        /// </summary>
        public void Update()
        {
            if (!IsInitialized || !_autoSaveSchedulingActive || !_autoSaveEnabled)
                return;

            _autoSaveTimer += Time.deltaTime;

            if (_autoSaveTimer >= _autoSaveInterval)
            {
                _autoSaveTimer = 0f;
                
                // Only auto-save if there's an active game session
                if (CanSaveGame())
                {
                    Debug.Log($"[SaveService] Triggering scheduled auto-save (interval: {_autoSaveInterval / 60f:F1} minutes)");
                    _eventSystem.Publish(SaveRequestedEvent.CreateAutoSave());
                }
            }
        }
        
        /// <summary>
        /// Clears all cached save file metadata
        /// </summary>
        private void ClearCaches()
        {
            _saveFileInfoCache.Clear();
        }
        
        #endregion
        
        #region Gameplay Settings Integration

        /// <summary>
        /// Handle options changed events for gameplay settings
        /// </summary>
        private void OnOptionsChanged(OptionsChangedEvent evt)
        {
            ApplyGameplaySettings();
        }

        /// <summary>
        /// Apply current gameplay settings from config
        /// </summary>
        private void ApplyGameplaySettings()
        {
            try
            {
                var autoSaveEnabled = _gameplaySettings.AutoSave.Value;
                var autoSaveIntervalMinutes = _gameplaySettings.AutoSaveInterval.Value;
                
                SetAutoSaveEnabled(autoSaveEnabled);
                SetAutoSaveInterval(autoSaveIntervalMinutes * 60); // Convert to seconds
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveService] Error applying gameplay settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Enable or disable auto-save system
        /// </summary>
        private void SetAutoSaveEnabled(bool enabled)
        {
            if (_autoSaveEnabled == enabled) return;
            
            _autoSaveEnabled = enabled;
            
            if (enabled)
            {
                Debug.Log("[SaveService] Auto-save enabled");
                // Reset timer when re-enabling
                _autoSaveTimer = 0f;
            }
            else
            {
                Debug.Log("[SaveService] Auto-save disabled");
            }
        }

        /// <summary>
        /// Set auto-save interval in seconds
        /// </summary>
        private void SetAutoSaveInterval(int intervalSeconds)
        {
            if (Mathf.Abs(_autoSaveInterval - intervalSeconds) < 0.1f) return;
            
            _autoSaveInterval = intervalSeconds;
            
            // Reset timer to prevent immediate save after interval change
            _autoSaveTimer = 0f;
            
            Debug.Log($"[SaveService] Auto-save interval set to {intervalSeconds / 60f:F1} minutes");
        }

        /// <summary>
        /// Control auto-save scheduling (typically called when game session starts/ends)
        /// </summary>
        public void SetAutoSaveSchedulingActive(bool active)
        {
            if (_autoSaveSchedulingActive == active) return;
            
            _autoSaveSchedulingActive = active;
            _autoSaveTimer = 0f; // Reset timer
            
            Debug.Log($"[SaveService] Auto-save scheduling {(active ? "activated" : "deactivated")}");
        }

        /// <summary>
        /// Get current auto-save settings for debugging/display
        /// </summary>
        public (bool enabled, float intervalMinutes, bool scheduling) GetAutoSaveStatus()
        {
            return (_autoSaveEnabled, _autoSaveInterval / 60f, _autoSaveSchedulingActive);
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
            return await SaveFileUtilities.GetSaveFileNamesAsync();
        }
        
        /// <summary>
        /// Deletes a save file and clears its cache entry
        /// </summary>
        public async Task<bool> DeleteSaveAsync(string saveName)
        {
            var success = await SaveFileUtilities.DeleteSaveFileAsync(saveName);
            if (success)
            {
                InvalidateCache(saveName);
                Debug.Log($"[SaveService] Deleted save '{saveName}' and cleared caches");
            }
            return success;
        }
        
        /// <summary>
        /// Deletes a save file using SaveFileInfo object
        /// </summary>
        public async Task<bool> DeleteSaveFileByInfoAsync(SaveFileInfo saveFileInfo)
        {
            if (saveFileInfo == null) return false;
            return await DeleteSaveAsync(saveFileInfo.FileName);
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
                    .OrderByDescending(info => info.LastSaveTime)
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
            return SaveFileUtilities.HasAnySaveFiles();
        }
        
        /// <summary>
        /// Gets the full file path for a save name
        /// </summary>
        public string GetSaveFilePath(string saveName)
        {
            return SaveFileUtilities.GetSaveFilePath(saveName);
        }
        
        /// <summary>
        /// Invalidates cached metadata for a specific save file
        /// </summary>
        public void InvalidateCache(string saveName)
        {
            _saveFileInfoCache.Remove(saveName);
        }
        
        #endregion
        
        #region Private Save Operations
        
        /// <summary>
        /// Performs a regular save with automatic timestamp-based naming
        /// </summary>
        private async Task<(bool success, string saveName)> PerformRegularSaveAsync()
        {
            var gameDataService = GameManager.GetService<IGameDataService>();
            if (gameDataService?.CurrentSession == null)
            {
                Debug.LogError("[SaveService] No active game session to save");
                return (false, null);
            }

            string saveName = SaveFileUtilities.GenerateTimestampSaveName(gameDataService.CurrentSession, false);
            bool success = await SaveGameSessionInternalAsync(gameDataService.CurrentSession, saveName, false);
            
            return (success, saveName);
        }
        
        /// <summary>
        /// Performs an autosave, only overwriting existing autosaves for the current player
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
            await DeleteCurrentPlayerAutoSaveAsync(gameDataService.CurrentSession.PlayerName);
            
            // Use consistent autosave naming (no timestamp) for each player
            string saveName = SaveFileUtilities.GenerateAutoSaveName(gameDataService.CurrentSession);
            bool success = await SaveGameSessionInternalAsync(gameDataService.CurrentSession, saveName, true);
            
            return (success, saveName);
        }
        
        /// <summary>
        /// Overwrites an existing save file with the current game session
        /// </summary>
        private async Task<bool> PerformOverwriteSaveAsync(SaveFileInfo targetSaveFile)
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
                string saveName = targetSaveFile.FileName;
                bool wasAutoSave = saveName.Contains("[AUTOSAVE]");
                bool success = await SaveGameSessionInternalAsync(gameDataService.CurrentSession, saveName, wasAutoSave);
        
                if (!success)
                {
                    Debug.LogError($"[SaveService] Failed to overwrite save file: {saveName}");
                }
        
                return success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveService] Error overwriting save file '{targetSaveFile.FileName}': {ex}");
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
    
            var filePath = GetSaveFilePath(targetSaveFile.FileName);
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
                // Update session metadata using TimeService directly
                session.LastSaveTime = DateTime.Now;
                _gameDataService.UpdateSessionSaveTime(session);
                session.WasAutoSave = isAutoSave;
        
                // Use utility for serialization
                var json = GameSessionSerializer.SerializeToJson(session, true);
        
                // Use utility for file writing
                var success = await SaveFileUtilities.WriteSaveFileAsync(saveName, json);
        
                if (success)
                {
                    // Invalidate cache since file content changed
                    InvalidateCache(saveName);
                    _eventSystem.Publish(new SaveGameEvent());
                }
        
                return success;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveService] Error saving game session '{saveName}': {e}");
                return false;
            }
        }

        /// <summary>
        /// Deletes only the autosave file for the current player, preserving other players' autosaves
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
                var playerAutoSaveFiles = await SaveFileUtilities.GetPlayerAutoSaveFilesAsync(currentPlayerName);
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
        /// Gets cached save file info, loading and caching it if not already cached
        /// Uses file modification time to determine if cache is still valid
        /// </summary>
        private async Task<SaveFileInfo> GetCachedSaveFileInfoAsync(string fileName)
        {
            if (!SaveFileUtilities.SaveFileExists(fileName))
                return null;
            
            var fileTime = SaveFileUtilities.GetSaveFileLastWriteTime(fileName);
            
            // Check if we have valid cached data
            if (_saveFileInfoCache.TryGetValue(fileName, out var cached) && 
                cached.LastSaveTime == fileTime)
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
        
        #endregion
        
        #region Unified Event Handler
        
        /// <summary>
        /// Unified event handler for all save requests
        /// Routes to appropriate save operation based on SaveType
        /// </summary>
        private async void OnSaveRequested(SaveRequestedEvent saveEvent)
        {
            Debug.Log($"[SaveService] Processing {saveEvent.SaveType} save request at {saveEvent.RequestTime}");
            
            try
            {
                switch (saveEvent.SaveType)
                {
                    case SaveType.Regular:
                        await HandleRegularSave();
                        break;
                        
                    case SaveType.Auto:
                        await HandleAutoSave();
                        break;
                        
                    case SaveType.Overwrite:
                        await HandleOverwriteSave(saveEvent.TargetSaveFile);
                        break;
                        
                    default:
                        Debug.LogError($"[SaveService] Unknown save type: {saveEvent.SaveType}");
                        _eventSystem.Publish(new SaveFailedEvent($"Unknown save type: {saveEvent.SaveType}", saveEvent.SaveType));
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveService] Error handling {saveEvent.SaveType} save request: {ex}");
                _eventSystem.Publish(new SaveFailedEvent(ex.Message, saveEvent.SaveType, ex));
            }
        }
        
        /// <summary>
        /// Handles regular save requests
        /// </summary>
        private async Task HandleRegularSave()
        {
            var (success, saveName) = await PerformRegularSaveAsync();
            
            if (success)
            {
                _eventSystem.Publish(new SaveCompletedEvent(saveName, SaveType.Regular));
            }
            else
            {
                _eventSystem.Publish(new SaveFailedEvent("Regular save operation failed", SaveType.Regular));
            }
        }
        
        /// <summary>
        /// Handles auto-save requests
        /// </summary>
        private async Task HandleAutoSave()
        {
            var (success, saveName) = await PerformAutoSaveAsync();
            
            if (success)
            {
                _eventSystem.Publish(new SaveCompletedEvent(saveName, SaveType.Auto));
            }
            else
            {
                _eventSystem.Publish(new SaveFailedEvent("Auto-save operation failed", SaveType.Auto));
            }
        }
        
        /// <summary>
        /// Handles overwrite save requests
        /// </summary>
        private async Task HandleOverwriteSave(SaveFileInfo targetSaveFile)
        {
            bool success = await PerformOverwriteSaveAsync(targetSaveFile);
            
            if (success)
            {
                _eventSystem.Publish(new SaveCompletedEvent(targetSaveFile.FileName, SaveType.Overwrite));
            }
            else
            {
                _eventSystem.Publish(new SaveFailedEvent("Overwrite save operation failed", SaveType.Overwrite));
            }
        }
        
        #endregion
    }
}
