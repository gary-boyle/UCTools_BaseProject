using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameFramework.DataStructures;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine;
using GameFramework.StateMachine.Data;
using GameFramework.StateMachine.Enum;
using GameFramework.StateMachine.Interfaces;
using UnityEngine;

namespace GameFramework.Services
{
    /// <summary>
    /// Service that handles all game loading operations using EventSystem
    /// Clean separation - only deals with loading files and validation for loading
    /// All saving operations are handled by SaveService
    /// </summary>
    public class LoadService : ILoadService
    {
        public bool IsInitialized { get; private set; }
        public bool IsLoading { get; private set; }
        
        // Dependencies
        private readonly IEventSystem _eventSystem;
        private readonly ISaveService _saveService;
        private readonly IGameDataService _gameDataService;
        private readonly IGameStateMachine _stateMachine;
        
        // Validation cache for load operations
        private readonly Dictionary<string, (DateTime fileTime, bool isValid)> _loadValidationCache = new();
        
        public LoadService(
            IEventSystem eventSystem,
            ISaveService saveService,
            IGameDataService gameDataService,
            IGameStateMachine stateMachine)
        {
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
            _gameDataService = gameDataService ?? throw new ArgumentNullException(nameof(gameDataService));
            _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        }
        
        #region Lifecycle
        
        public async Task InitializeAsync()
        {
            if (IsInitialized) return;
            
            SubscribeToEvents();
            
            IsInitialized = true;
            await Task.CompletedTask;
        }
        
        public void Shutdown()
        {
            if (!IsInitialized) return;
            
            UnsubscribeFromEvents();
            _loadValidationCache.Clear();
            
            IsInitialized = false;
        }
        
        private void SubscribeToEvents()
        {
            _eventSystem.Subscribe<LoadGameRequestedEvent>(OnLoadGameRequested);
            _eventSystem.Subscribe<LoadSaveFileEvent>(OnLoadSaveFileRequested);
        }
        
        private void UnsubscribeFromEvents()
        {
            _eventSystem.Unsubscribe<LoadGameRequestedEvent>(OnLoadGameRequested);
            _eventSystem.Unsubscribe<LoadSaveFileEvent>(OnLoadSaveFileRequested);
        }
        #endregion
        
        #region Event Handlers
        
        private async void OnLoadGameRequested(LoadGameRequestedEvent evt)
        {
            await HandleLoadRequest(evt.SaveFileInfo, $"LoadGameRequested: {evt.SaveFileName}");
        }
        
        private async void OnLoadSaveFileRequested(LoadSaveFileEvent evt)
        {
            // Notify other systems that loading is starting
            _eventSystem.Publish(new LoadingStartedEvent(evt.SaveFileInfo));
            
            await HandleLoadRequest(evt.SaveFileInfo, $"LoadSaveFile: {evt.SaveFileInfo.FileName}");
        }
        
        private async Task HandleLoadRequest(SaveFileInfo saveFileInfo, string context)
        {
            Debug.Log($"[LoadService] {context}");
            await LoadGameAsync(saveFileInfo);
        }
        #endregion
        
        #region Core Load Operations
        
        /// <summary>
        /// Loads game session from file
        /// </summary>
        public async Task<GameSession> LoadGameSessionAsync(string saveName)
        {
            var filePath = _saveService.GetSaveFilePath(saveName);
    
            if (!System.IO.File.Exists(filePath))
            {
                Debug.LogWarning($"[LoadService] Save file '{saveName}' not found");
                return null;
            }
    
            try
            {
                var json = await System.IO.File.ReadAllTextAsync(filePath);
                var session = JsonUtility.FromJson<GameSession>(json);
                
                // Validate loaded session
                if (!IsValidLoadedSession(session))
                {
                    Debug.LogError($"[LoadService] Loaded session from '{saveName}' failed validation");
                    return null;
                }
        
                // Restore time data to services for proper integration
                session.RestoreTimeDataToService();
        
                _eventSystem.Publish(new LoadGameEvent());
                
                return session;
            }
            catch (Exception e)
            {
                Debug.LogError($"[LoadService] Error loading save '{saveName}': {e}");
                return null;
            }
        }
        
        /// <summary>
        /// Loads a game session using SaveFileInfo
        /// </summary>
        private async Task<GameSession> LoadGameSessionByInfoAsync(SaveFileInfo saveFileInfo)
        {
            if (saveFileInfo == null) return null;
            return await LoadGameSessionAsync(saveFileInfo.FileName);
        }
        
        /// <summary>
        /// Main entry point for loading a game session from save file information
        /// </summary>
        public async Task<bool> LoadGameAsync(SaveFileInfo saveFileInfo)
        {
            if (saveFileInfo == null)
            {
                Debug.LogError("[LoadService] SaveFileInfo is null");
                return false;
            }
            
            if (IsLoading)
            {
                Debug.LogWarning("[LoadService] Already loading a game, ignoring request");
                return false;
            }
            
            try
            {
                IsLoading = true;
                return await ExecuteLoadingWorkflow(saveFileInfo);
            }
            catch (Exception e)
            {
                Debug.LogError($"[LoadService] Error loading game '{saveFileInfo.FileName}': {e}");
                _eventSystem.Publish(new LoadingFailedEvent(e));
                return false;
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        /// <summary>
        /// Executes the complete loading workflow
        /// </summary>
        private async Task<bool> ExecuteLoadingWorkflow(SaveFileInfo saveFileInfo)
        {
            // Load save data and validate its integrity
            NotifyProgress("Loading and validating save data...", 0.2f);
            var gameSession = await LoadAndValidateGameSession(saveFileInfo);
            if (gameSession == null) return false;
            
            // Prepare loading configuration with session data
            NotifyProgress("Preparing game world...", 0.5f);
            var loadingConfig = CreateLoadSaveConfiguration(gameSession);
            
            // Configure game systems with loaded data
            NotifyProgress("Initializing game systems...", 0.7f);
            _gameDataService.CurrentLoadingConfig = loadingConfig;
            
            // Trigger state machine transition to loading state
            NotifyProgress("Starting game...", 0.9f);
            await _stateMachine.ChangeStateAsync(GameStateType.Loading);
            
            // Notify completion
            NotifyProgress("Loading complete!", 1.0f);
            _eventSystem.Publish(new LoadingCompletedEvent(gameSession));
            
            return true;
        }
        
        /// <summary>
        /// Loads game session data from disk and validates its integrity
        /// </summary>
        private async Task<GameSession> LoadAndValidateGameSession(SaveFileInfo saveFileInfo)
        {
            try
            {
                var gameSession = await LoadGameSessionByInfoAsync(saveFileInfo);
        
                if (!_gameDataService.IsValidGameSession(gameSession))
                {
                    Debug.LogError($"[LoadService] Invalid game session: {saveFileInfo.FileName}");
                    return null;
                }
        
                return gameSession;
            }
            catch (Exception e)
            {
                Debug.LogError($"[LoadService] Error loading/validating game session: {e}");
                return null;
            }
        }
        
        /// <summary>
        /// Creates a loading configuration for the LoadSave loading type
        /// </summary>
        private static LoadingConfiguration CreateLoadSaveConfiguration(GameSession gameSession)
        {
            var gameData = new Dictionary<string, object>
            {
                // Core session data
                ["gameSession"] = gameSession,
                ["difficulty"] = gameSession.difficulty,
                ["savedPlayTime"] = gameSession.TotalPlayTimeSeconds,
                ["timeServiceManaged"] = true,
                
                // Player data
                ["playerLevel"] = gameSession.player.Level,
                ["playerHealth"] = gameSession.player.Health,
                ["playerMaxHealth"] = gameSession.player.MaxHealth,
                ["playerExperience"] = gameSession.player.Experience,
                ["playerPosition"] = gameSession.player.Position,
                ["playerRotation"] = gameSession.player.Rotation,
                
                // Progress data
                ["score"] = gameSession.progress.Score,
                ["sessionStartTime"] = gameSession.sessionStartTime.ToString(),
                ["lastSaveTime"] = gameSession.lastSaveTime.ToString()
            };
            
            // Merge custom data without overwriting system keys
            foreach (var (key, value) in gameSession.customData)
            {
                gameData.TryAdd(key, value);
            }
            
            return new LoadingConfiguration
            {
                Type = LoadingType.LoadSave,
                SceneName = gameSession.currentScene,
                PlayerName = gameSession.playerName,
                ShowLoadingScreen = true,
                MinimumLoadingTime = 2f,
                GameData = gameData
            };
        }
        
        #endregion
        
        #region Load Validation Methods
        
        /// <summary>
        /// Performs lightweight validation of a save file for loading purposes
        /// </summary>
        private async Task<bool> ValidateSaveFileAsync(SaveFileInfo saveFileInfo)
        {
            if (saveFileInfo == null || string.IsNullOrEmpty(saveFileInfo.FileName))
                return false;
                
            return await ValidateSaveFileAsync(saveFileInfo.FileName);
        }
        
        /// <summary>
        /// Performs lightweight validation of a save file by name for loading
        /// </summary>
        private async Task<bool> ValidateSaveFileAsync(string saveName)
        {
            if (string.IsNullOrEmpty(saveName))
                return false;
            
            var filePath = _saveService.GetSaveFilePath(saveName);
            
            // Check if file exists
            if (!System.IO.File.Exists(filePath))
                return false;
            
            // Check cache first
            var fileTime = System.IO.File.GetLastWriteTime(filePath);
            if (_loadValidationCache.TryGetValue(saveName, out var cached) && 
                cached.fileTime == fileTime)
            {
                return cached.isValid;
            }
            
            try
            {
                // Lightweight validation - read file and check basic structure
                var json = await System.IO.File.ReadAllTextAsync(filePath);
                var isValid = await ValidateJsonStructureAsync(json);
                
                // Cache result
                _loadValidationCache[saveName] = (fileTime, isValid);
                
                return isValid;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LoadService] Validation failed for '{saveName}': {e.Message}");
                _loadValidationCache[saveName] = (fileTime, false);
                return false;
            }
        }
        
        /// <summary>
        /// Validates JSON structure for loading without full deserialization
        /// </summary>
        private static async Task<bool> ValidateJsonStructureAsync(string json)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Check for required fields (lightweight check)
                    return !string.IsNullOrEmpty(json) &&
                           json.Contains("\"playerName\"") &&
                           json.Contains("\"currentScene\"") &&
                           json.Contains("\"player\"") &&
                           json.Contains("\"progress\"") &&
                           json.Length > 100; // Basic size check
                }
                catch
                {
                    return false;
                }
            });
        }
        
        /// <summary>
        /// Validates a loaded GameSession object for completeness
        /// </summary>
        private static bool IsValidLoadedSession(GameSession session)
        {
            return session != null &&
                   !string.IsNullOrEmpty(session.playerName) &&
                   !string.IsNullOrEmpty(session.currentScene) &&
                   session.player != null &&
                   session.progress != null;
        }
        
        /// <summary>
        /// Determines whether a specific save file can be successfully loaded
        /// </summary>
        public async Task<bool> CanLoadGame(SaveFileInfo saveFileInfo)
        {
            if (saveFileInfo == null) return false;
            
            try
            {
                return await ValidateSaveFileAsync(saveFileInfo);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LoadService] Save file '{saveFileInfo.FileName}' validation failed: {e.Message}");
                return false;
            }
        }
        
        #endregion
        
        #region UI Support Methods

        /// <summary>
        /// Gets all save files that can be successfully loaded, with full display information
        /// </summary>
        public async Task<SaveFileInfo[]> GetLoadableSaveFilesAsync()
        {
            try
            {
                Debug.Log("[LoadService] Getting loadable save files for UI display...");
                
                // Get all save file information from SaveService (uses caching)
                var allSaveFileInfos = await _saveService.GetSaveFileInfosAsync();
                
                if (allSaveFileInfos.Length == 0)
                {
                    Debug.Log("[LoadService] No save files found");
                    return Array.Empty<SaveFileInfo>();
                }
                
                // Filter to only loadable files using parallel validation
                var loadableFiles = await FilterToLoadableFilesAsync(allSaveFileInfos);
                
                Debug.Log($"[LoadService] Found {loadableFiles.Length} loadable save files out of {allSaveFileInfos.Length} total");
                return loadableFiles;
            }
            catch (Exception e)
            {
                Debug.LogError($"[LoadService] Error getting loadable save files: {e}");
                return Array.Empty<SaveFileInfo>();
            }
        }

        /// <summary>
        /// Filters save file infos to only those that can be successfully loaded
        /// </summary>
        private async Task<SaveFileInfo[]> FilterToLoadableFilesAsync(IEnumerable<SaveFileInfo> allSaveFiles)
        {
            // Create validation tasks for parallel execution
            var validationTasks = allSaveFiles.Select(async saveFileInfo => new 
            {
                SaveFile = saveFileInfo,
                IsLoadable = await CanLoadGame(saveFileInfo)
            });
            
            // Execute validations in parallel
            var validationResults = await Task.WhenAll(validationTasks);
            
            // Filter to only loadable files and maintain original sorting
            var loadableFiles = validationResults
                .Where(result => result.IsLoadable)
                .Select(result => result.SaveFile)
                .ToArray();
            
            return loadableFiles;
        }
        
        #endregion
        
        #region Progress Notification

        /// <summary>
        /// Publishes loading progress events to notify UI and other systems of loading status
        /// </summary>
        private void NotifyProgress(string message, float progress)
        {
            _eventSystem.Publish(new LoadingProgressEvent(message, progress));
        }

        #endregion
    }
}
