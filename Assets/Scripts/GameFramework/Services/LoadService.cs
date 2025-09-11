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
using GameFramework.UI.Popups;
using GameFramework.UI.Screens;
using UnityEngine;

namespace GameFramework.Services
{
    /// <summary>
    /// Service that orchestrates all game loading operations using EventSystem
    /// 
    /// Intent: Handles loading workflow: validation -> file loading -> session creation -> state transition
    /// 
    /// Design:
    /// - Uses EventSystem for all communication instead of direct Action events
    /// - Integrates with TimeService for proper playtime handling
    /// - Publishes loading progress and completion events through EventSystem
    /// - Subscribes to load game request events from UI and other systems
    /// 
    /// Pros:
    /// - Decoupled event handling through EventSystem
    /// - Multiple systems can listen to loading events without direct coupling
    /// - Consistent event architecture across the framework
    /// - Easy to add new loading event listeners without modifying this service
    /// 
    /// Cons:
    /// - Slightly more overhead than direct Actions for progress events
    /// - Event handling requires knowledge of event class structure
    /// - Debugging loading flow requires EventSystem awareness
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
        /// <summary>
        /// Handles load game requested events from UI or other systems
        /// </summary>
        private async void OnLoadGameRequested(LoadGameRequestedEvent evt)
        {
            Debug.Log($"[LoadService] Load game requested: {evt.SaveFileName}");
            await LoadGameAsync(evt.SaveFileInfo);
        }
        
        /// <summary>
        /// Handles load save file events with automatic popup cleanup
        /// </summary>
        private async void OnLoadSaveFileRequested(LoadSaveFileEvent evt)
        {
            Debug.Log($"[LoadService] Load save file requested: {evt.SaveFileInfo.fileName}");
            
            // Close any open popups first
            await CloseAllPopupsBeforeLoading();
            
            // Load the game session
            await LoadGameAsync(evt.SaveFileInfo);
        }
        #endregion
        
        #region Popup Management
        /// <summary>
        /// Closes all popups before starting load process
        /// </summary>
        private async Task CloseAllPopupsBeforeLoading()
        {
            try
            {
                var uiService = GameManager.GetService<IUIService>();
                if (uiService != null)
                {
                    // Close all popups that might be open
                    await uiService.HidePopupAsync<LoadGamePopup>();
                    await uiService.HidePopupAsync<PausePopup>();
                    await uiService.HidePopupAsync<OptionsPopup>();
                    await uiService.HidePopupAsync<SaveGamePopup>();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LoadService] Error closing popups (continuing anyway): {ex.Message}");
                // Don't fail loading because of popup issues
            }
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
                
                // Publish loading failed event through EventSystem
                _eventSystem.Publish(new LoadingFailedEvent(e));
                return false;
            }
        }
        
        /// <summary>
        /// Loads a game using SaveFileInfo (main loading method)
        /// TimeService will handle playtime restoration automatically
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
                Debug.Log($"[LoadService] Starting load for: {saveFileInfo.fileName} - " +
                         $"Playtime: {saveFileInfo.formattedPlayTime}");
                return await ExecuteLoadingWorkflow(saveFileInfo);
            }
            catch (Exception e)
            {
                Debug.LogError($"[LoadService] Error loading game '{saveFileInfo.fileName}': {e}");
                
                // Publish loading failed event through EventSystem
                _eventSystem.Publish(new LoadingFailedEvent(e));
                return false;
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        #endregion
        
        #region Loading Workflow
        /// <summary>
        /// Executes the complete loading workflow
        /// TimeService integration ensures proper playtime handling
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
            
            // Step 3: Create loading configuration for LoadSave
            NotifyProgress("Preparing game world...", 0.5f);
            var loadingConfig = CreateLoadSaveConfiguration(gameSession);
            
            // Step 4: Set up game data service
            NotifyProgress("Initializing game systems...", 0.7f);
            SetupGameDataService(gameSession, loadingConfig);
            
            // Step 5: Initiate state transition
            NotifyProgress("Starting game...", 0.9f);
            await InitiateGameLoading();
            
            // Step 6: Complete
            NotifyProgress("Loading complete!", 1.0f);
            
            // Publish loading completed event through EventSystem
            _eventSystem.Publish(new LoadingCompletedEvent(gameSession));
            
            Debug.Log($"[LoadService] Successfully loaded game: {saveFileInfo.fileName} - " +
                     $"TimeService will manage playtime from here");
            return true;
        }
        
        /// <summary>
        /// Validates save file integrity and compatibility
        /// </summary>
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
        
        /// <summary>
        /// Loads game session from save file using SaveService
        /// </summary>
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
                
                return gameSession;
            }
            catch (Exception e)
            {
                Debug.LogError($"[LoadService] Error loading game session: {e}");
                return null;
            }
        }
        
        /// <summary>
        /// Creates loading configuration specifically for LoadSave type
        /// Includes TimeService integration metadata
        /// </summary>
        private LoadingConfiguration CreateLoadSaveConfiguration(GameSession gameSession)
        {
            // Create proper LoadSave loading configuration
            var loadingConfig = new LoadingConfiguration
            {
                Type = LoadingType.LoadSave,
                SceneName = gameSession.currentScene,
                PlayerName = gameSession.playerName,
                ShowLoadingScreen = true,
                MinimumLoadingTime = 2f,
                GameData = new System.Collections.Generic.Dictionary<string, object>()
            };
            
            // Store the complete game session for LoadingState
            loadingConfig.GameData["gameSession"] = gameSession;
            
            // Store individual values for easy access
            loadingConfig.GameData["difficulty"] = gameSession.difficulty;
            loadingConfig.GameData["playerLevel"] = gameSession.player.level;
            loadingConfig.GameData["playerHealth"] = gameSession.player.health;
            loadingConfig.GameData["playerMaxHealth"] = gameSession.player.maxHealth;
            loadingConfig.GameData["playerExperience"] = gameSession.player.experience;
            loadingConfig.GameData["playerPosition"] = gameSession.player.position;
            loadingConfig.GameData["playerRotation"] = gameSession.player.rotation;
            loadingConfig.GameData["score"] = gameSession.progress.score;
            loadingConfig.GameData["sessionStartTime"] = gameSession.sessionStartTime.ToString();
            loadingConfig.GameData["lastSaveTime"] = gameSession.lastSaveTime.ToString();
            
            // TimeService will handle playtime - just store for reference
            loadingConfig.GameData["savedPlayTime"] = gameSession.TotalPlayTimeSeconds;
            loadingConfig.GameData["timeServiceManaged"] = true;
            
            // Copy all custom data from session
            foreach (var kvp in gameSession.customData)
            {
                // Avoid overwriting system keys
                if (!loadingConfig.GameData.ContainsKey(kvp.Key))
                {
                    loadingConfig.GameData[kvp.Key] = kvp.Value;
                }
            }
            
            return loadingConfig;
        }
        
        /// <summary>
        /// Sets up GameDataService with loaded session configuration
        /// </summary>
        private void SetupGameDataService(GameSession gameSession, LoadingConfiguration loadingConfig)
        {
            // Set up the loading configuration for LoadingState to use
            // TimeService will handle playtime restoration automatically
            _gameDataService.CurrentLoadingConfig = loadingConfig;
        }
        
        /// <summary>
        /// Initiates state transition to loading state
        /// </summary>
        private async Task InitiateGameLoading()
        {
            try
            {
                await _stateMachine.ChangeStateAsync(GameStateType.Loading);
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
        /// Checks if a save file can be loaded using SaveFileInfo
        /// </summary>
        public async Task<bool> CanLoadGame(SaveFileInfo saveFileInfo)
        {
            if (saveFileInfo == null) return false;
            
            try
            {
                // Try to load the actual game session to validate it
                var gameSession = await _saveService.LoadGameSessionByInfoAsync(saveFileInfo);
                var isValid = IsValidGameSession(gameSession);
                
                if (!isValid)
                {
                    Debug.LogWarning($"[LoadService] Save file '{saveFileInfo.fileName}' failed validation");
                }
                
                return isValid;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LoadService] Save file '{saveFileInfo.fileName}' cannot be loaded: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Validates game session integrity
        /// </summary>
        private bool IsValidGameSession(GameSession gameSession)
        {
            // Basic validation of game session
            if (gameSession == null)
            {
                Debug.LogWarning("[LoadService] GameSession is null");
                return false;
            }
            
            if (string.IsNullOrEmpty(gameSession.playerName))
            {
                Debug.LogWarning("[LoadService] GameSession has invalid player name");
                return false;
            }
            
            if (string.IsNullOrEmpty(gameSession.currentScene))
            {
                Debug.LogWarning("[LoadService] GameSession has invalid scene name");
                return false;
            }
            
            if (gameSession.player == null)
            {
                Debug.LogWarning("[LoadService] GameSession has null player state");
                return false;
            }
            
            if (gameSession.progress == null)
            {
                Debug.LogWarning("[LoadService] GameSession has null progress data");
                return false;
            }
            
            Debug.Log($"[LoadService] GameSession validation passed for {gameSession.playerName}");
            return true;
        }
        #endregion
        
        #region Progress Notification
        /// <summary>
        /// Notifies loading progress through EventSystem
        /// Publishes both specific progress events and general loading progress event
        /// </summary>
        private void NotifyProgress(string message, float progress)
        {
            Debug.Log($"[LoadService] {message} ({progress:P0})");
            
            // Publish loading progress events through EventSystem
            _eventSystem.Publish(new LoadingProgressChangedEvent(message, progress));
            _eventSystem.Publish(new LoadingMessageChangedEvent(message));
            
            // Also publish global loading progress event for broader system use
            _eventSystem.Publish(new LoadingProgressEvent
            {
                Progress = progress,
                Message = message
            });
        }
        #endregion
    }
}
