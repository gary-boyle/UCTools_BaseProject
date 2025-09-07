using System;
using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.DataStructures;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine;
using GameFramework.StateMachine.Data;
using GameFramework.StateMachine.Enum;
using UnityEngine;

namespace GameFramework.Services
{
    /// <summary>
    /// Service that orchestrates all game loading operations
    /// Handles loading workflow: validation -> file loading -> session creation -> state transition
    /// </summary>
    public class LoadService : ILoadService
    {
        public bool IsInitialized { get; private set; }
        public bool IsLoading { get; private set; }
        
        // Events for loading progress
        public event Action<string, float> LoadingProgressChanged;
        public event Action<string> LoadingMessageChanged;
        public event Action<Exception> LoadingFailed;
        public event Action<GameSession> LoadingCompleted;
        
        // Dependencies
        private readonly IEventSystem _eventSystem;
        private readonly ISaveService _saveService;
        private readonly IGameDataService _gameDataService;
        private readonly IGameStateMachine _stateMachine;
        
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
            
            Debug.Log("[LoadService] Initializing load service...");
            
            // Subscribe to load game events
            _eventSystem.Subscribe<LoadGameRequestedEvent>(OnLoadGameRequested);
            _eventSystem.Subscribe<LoadSaveFileEvent>(OnLoadSaveFileRequested);
            
            IsInitialized = true;
            await Task.CompletedTask;
        }
        
        public void Shutdown()
        {
            if (!IsInitialized) return;
            
            // Unsubscribe from events
            _eventSystem.Unsubscribe<LoadGameRequestedEvent>(OnLoadGameRequested);
            _eventSystem.Unsubscribe<LoadSaveFileEvent>(OnLoadSaveFileRequested);
            
            IsInitialized = false;
        }
        #endregion
        
        #region Event Handlers
        private async void OnLoadGameRequested(LoadGameRequestedEvent evt)
        {
            Debug.Log($"[LoadService] Load game requested: {evt.SaveFileName}");
            await LoadGameAsync(evt.SaveFileInfo);
        }
        
        private async void OnLoadSaveFileRequested(LoadSaveFileEvent evt)
        {
            Debug.Log($"[LoadService] Load save file requested: {evt.SaveFileInfo.fileName}");
            await LoadGameAsync(evt.SaveFileInfo);
        }
        #endregion
        
        #region Loading Operations
        /// <summary>
        /// Loads a game by save file name
        /// </summary>
        public async Task<bool> LoadGameAsync(string saveFileName)
        {
            if (string.IsNullOrEmpty(saveFileName))
            {
                Debug.LogError("[LoadService] Save file name is null or empty");
                return false;
            }
            
            try
            {
                // Get save file info first
                var saveFileInfo = await _saveService.GetSaveFileInfoAsync(saveFileName);
                if (saveFileInfo == null)
                {
                    Debug.LogError($"[LoadService] Could not get save file info for: {saveFileName}");
                    return false;
                }
                
                return await LoadGameAsync(saveFileInfo);
            }
            catch (Exception e)
            {
                Debug.LogError($"[LoadService] Error loading game by name '{saveFileName}': {e}");
                LoadingFailed?.Invoke(e);
                return false;
            }
        }
        
        /// <summary>
        /// Loads a game using SaveFileInfo (main loading method)
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
                Debug.LogError($"[LoadService] Error loading game '{saveFileInfo.fileName}': {e}");
                LoadingFailed?.Invoke(e);
                return false;
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        /// <summary>
        /// Loads the most recently saved game
        /// </summary>
        public async Task<bool> LoadMostRecentGameAsync()
        {
            try
            {
                var mostRecentSave = _saveService.GetMostRecentSaveName();
                if (string.IsNullOrEmpty(mostRecentSave))
                {
                    Debug.LogWarning("[LoadService] No recent save found");
                    return false;
                }
                
                return await LoadGameAsync(mostRecentSave);
            }
            catch (Exception e)
            {
                Debug.LogError($"[LoadService] Error loading most recent game: {e}");
                LoadingFailed?.Invoke(e);
                return false;
            }
        }
        #endregion
        
        #region Loading Workflow
        /// <summary>
        /// Executes the complete loading workflow
        /// </summary>
        private async Task<bool> ExecuteLoadingWorkflow(SaveFileInfo saveFileInfo)
        {
            Debug.Log($"[LoadService] Starting loading workflow for: {saveFileInfo.fileName}");
            
            // Step 1: Validate save file
            NotifyProgress("Validating save file...", 0.1f);
            if (!await ValidateSaveFile(saveFileInfo))
            {
                return false;
            }
            
            // Step 2: Load game session from file
            NotifyProgress("Loading save data...", 0.3f);
            var gameSession = await LoadGameSession(saveFileInfo);
            if (gameSession == null)
            {
                return false;
            }
            
            // Step 3: Create loading configuration
            NotifyProgress("Preparing game world...", 0.5f);
            var loadingConfig = CreateLoadingConfiguration(gameSession);
            
            // Step 4: Set up game data service
            NotifyProgress("Initializing game systems...", 0.7f);
            SetupGameDataService(gameSession, loadingConfig);
            
            // Step 5: Initiate state transition
            NotifyProgress("Starting game...", 0.9f);
            await InitiateGameLoading();
            
            // Step 6: Complete
            NotifyProgress("Loading complete!", 1.0f);
            LoadingCompleted?.Invoke(gameSession);
            
            Debug.Log($"[LoadService] Successfully loaded game: {saveFileInfo.fileName}");
            return true;
        }
        
        private async Task<bool> ValidateSaveFile(SaveFileInfo saveFileInfo)
        {
            try
            {
                // Check if save file exists and is valid
                var canLoad = await CanLoadGame(saveFileInfo);
                if (!canLoad)
                {
                    Debug.LogError($"[LoadService] Cannot load save file: {saveFileInfo.fileName}");
                    return false;
                }
                
                Debug.Log($"[LoadService] Save file validation passed: {saveFileInfo.fileName}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[LoadService] Save file validation failed: {e}");
                return false;
            }
        }
        
        private async Task<GameSession> LoadGameSession(SaveFileInfo saveFileInfo)
        {
            try
            {
                var gameSession = await _saveService.LoadGameSessionByInfoAsync(saveFileInfo);
                if (gameSession == null)
                {
                    Debug.LogError($"[LoadService] Failed to load game session from: {saveFileInfo.fileName}");
                    return null;
                }
                
                Debug.Log($"[LoadService] Game session loaded: Player={gameSession.playerName}, Scene={gameSession.currentScene}");
                return gameSession;
            }
            catch (Exception e)
            {
                Debug.LogError($"[LoadService] Error loading game session: {e}");
                return null;
            }
        }
        
        private LoadingConfiguration CreateLoadingConfiguration(GameSession gameSession)
        {
            Debug.Log($"[LoadService] Creating loading configuration for scene: {gameSession.currentScene}");
            
            var loadingConfig = LoadingConfiguration.LoadSave(
                gameSession.currentScene, 
                gameSession.customData
            );
            
            // Enrich loading configuration with session data
            loadingConfig.PlayerName = gameSession.playerName;
            loadingConfig.GameData["difficulty"] = gameSession.difficulty;
            loadingConfig.GameData["playerLevel"] = gameSession.player.level;
            loadingConfig.GameData["playerHealth"] = gameSession.player.health;
            loadingConfig.GameData["playerMaxHealth"] = gameSession.player.maxHealth;
            loadingConfig.GameData["playerExperience"] = gameSession.player.experience;
            loadingConfig.GameData["playerPosition"] = gameSession.player.position;
            loadingConfig.GameData["totalPlayTime"] = gameSession.totalPlayTimeSeconds;
            loadingConfig.GameData["score"] = gameSession.progress.score;
            loadingConfig.GameData["sessionStartTime"] = gameSession.sessionStartTime.ToString();
            
            return loadingConfig;
        }
        
        private void SetupGameDataService(GameSession gameSession, LoadingConfiguration loadingConfig)
        {
            Debug.Log("[LoadService] Setting up game data service with loaded session");
            
            // Set the loaded session as current
            _gameDataService.LoadGameSession(gameSession);
            
            // Set the loading configuration
            _gameDataService.CurrentLoadingConfig = loadingConfig;
            
            Debug.Log($"[LoadService] Game data service configured for: {gameSession.playerName}");
        }
        
        private async Task InitiateGameLoading()
        {
            Debug.Log("[LoadService] Initiating state transition to loading");
            
            try
            {
                await _stateMachine.ChangeStateAsync(GameStateType.Loading);
                Debug.Log("[LoadService] State transition to loading initiated");
            }
            catch (Exception e)
            {
                Debug.LogError($"[LoadService] Failed to transition to loading state: {e}");
                throw;
            }
        }
        #endregion
        
        #region Loading Support
        /// <summary>
        /// Gets all save files that can be loaded
        /// </summary>
        public async Task<SaveFileInfo[]> GetLoadableSaveFilesAsync()
        {
            try
            {
                return await _saveService.GetSaveFileInfosAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[LoadService] Error getting loadable save files: {e}");
                return new SaveFileInfo[0];
            }
        }
        
        /// <summary>
        /// Checks if a save file can be loaded
        /// </summary>
        public async Task<bool> CanLoadGame(string saveFileName)
        {
            if (string.IsNullOrEmpty(saveFileName)) return false;
            
            try
            {
                var saveFileInfo = await _saveService.GetSaveFileInfoAsync(saveFileName);
                return await CanLoadGame(saveFileInfo);
            }
            catch (Exception e)
            {
                Debug.LogError($"[LoadService] Error checking if can load '{saveFileName}': {e}");
                return false;
            }
        }
        
        /// <summary>
        /// Checks if a save file can be loaded using SaveFileInfo
        /// </summary>
        public async Task<bool> CanLoadGame(SaveFileInfo saveFileInfo)
        {
            if (saveFileInfo == null) return false;
            
            try
            {
                // Try to load the actual game session to validate it
                var gameSession = await _saveService.LoadGameSessionByInfoAsync(saveFileInfo);
                return gameSession != null && IsValidGameSession(gameSession);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LoadService] Save file '{saveFileInfo.fileName}' cannot be loaded: {e.Message}");
                return false;
            }
        }
        
        private bool IsValidGameSession(GameSession gameSession)
        {
            // Basic validation of game session
            return gameSession != null &&
                   !string.IsNullOrEmpty(gameSession.playerName) &&
                   !string.IsNullOrEmpty(gameSession.currentScene) &&
                   gameSession.player != null &&
                   gameSession.progress != null;
        }
        #endregion
        
        #region Progress Notification
        private void NotifyProgress(string message, float progress)
        {
            Debug.Log($"[LoadService] {message} ({progress:P0})");
            
            LoadingProgressChanged?.Invoke(message, progress);
            LoadingMessageChanged?.Invoke(message);
            
            // Also publish global loading progress event
            _eventSystem.Publish(new LoadingProgressEvent
            {
                Progress = progress,
                Message = message
            });
        }
        #endregion
    }
}
