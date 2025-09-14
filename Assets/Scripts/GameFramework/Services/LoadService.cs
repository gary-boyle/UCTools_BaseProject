using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.DataStructures;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine;
using GameFramework.StateMachine.Data;
using GameFramework.StateMachine.Enum;
using GameFramework.StateMachine.Interfaces;
using GameFramework.Utilities;
using UnityEngine;

namespace GameFramework.Services
{
    /// <summary>
    /// Service that handles all game loading operations using EventSystem
    /// Simplified to focus on loading workflow and state management
    /// File operations and validation delegated to utility classes
    /// </summary>
    public class LoadService : ILoadService
    {
        public bool IsInitialized { get; private set; }
        public bool IsLoading { get; private set; }
        
        // Dependencies
        private readonly IEventSystem _eventSystem;
        private readonly IGameDataService _gameDataService;
        private readonly IGameStateMachine _stateMachine;
        
        public LoadService(
            IEventSystem eventSystem,
            IGameDataService gameDataService,
            IGameStateMachine stateMachine)
        {
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
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
        /// Loads game session from file using utility classes
        /// </summary>
        public async Task<GameSession> LoadGameSessionAsync(string saveName)
        {
            if (!SaveFileUtilities.SaveFileExists(saveName))
            {
                Debug.LogWarning($"[LoadService] Save file '{saveName}' not found");
                return null;
            }
    
            try
            {
                // Use utility for file reading
                var json = await SaveFileUtilities.ReadSaveFileAsync(saveName);
                if (string.IsNullOrEmpty(json))
                {
                    Debug.LogError($"[LoadService] Failed to read save file '{saveName}'");
                    return null;
                }
                
                // Use utility for deserialization
                var session = GameSessionSerializer.DeserializeFromJson(json);
                
                // Validate loaded session using simplified validation
                if (!IsValidGameSession(session))
                {
                    Debug.LogError($"[LoadService] Loaded session from '{saveName}' failed validation");
                    return null;
                }
                
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
            return saveFileInfo?.FileName != null ? await LoadGameSessionAsync(saveFileInfo.FileName) : null;
        }
        
        /// <summary>
        /// Main entry point for loading a game session
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
            // Load and validate save data
            NotifyProgress("Loading and validating save data...", 0.2f);
            var gameSession = await LoadAndValidateGameSession(saveFileInfo);
            if (gameSession == null) return false;
            
            // Prepare loading configuration
            NotifyProgress("Preparing game world...", 0.5f);
            var loadingConfig = CreateLoadSaveConfiguration(gameSession);
            
            // Configure game systems
            NotifyProgress("Initializing game systems...", 0.7f);
            _gameDataService.CurrentLoadingConfig = loadingConfig;
            
            // Trigger state machine transition
            NotifyProgress("Starting game...", 0.9f);
            await _stateMachine.ChangeStateAsync(GameStateType.Loading);
            
            // Notify completion
            NotifyProgress("Loading complete!", 1.0f);
            _eventSystem.Publish(new LoadingCompletedEvent(gameSession));
            
            return true;
        }
        
        /// <summary>
        /// Loads and validates game session using utility classes
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
        /// Creates loading configuration with game session data
        /// </summary>
        private static LoadingConfiguration CreateLoadSaveConfiguration(GameSession gameSession)
        {
            var gameData = new Dictionary<string, object>
            {
                ["gameSession"] = gameSession,
                ["difficulty"] = gameSession.Difficulty,
                ["savedPlayTime"] = gameSession.SavedGameTime,        
                ["timeServiceManaged"] = true,
                ["sessionStartTime"] = gameSession.SessionStartTime.ToString(),    
                ["lastSaveTime"] = gameSession.LastSaveTime.ToString(),           
                // Time data for restoration - only game time now
                ["savedGameTime"] = gameSession.SavedGameTime,        
                ["hasTimeData"] = gameSession.HasSavedTimeData        
            };

            return new LoadingConfiguration
            {
                Type = LoadingType.LoadSave,
                SceneName = gameSession.CurrentScene,
                PlayerName = gameSession.PlayerName,
                ShowLoadingScreen = true,
                MinimumLoadingTime = 2f,
                GameData = gameData
            };
        }
        
        #endregion
        
        #region Load Validation Methods
        
        /// <summary>
        /// Basic game session validation using utility
        /// </summary>
        public async Task<bool> CanLoadGame(SaveFileInfo saveFileInfo)
        {
            if (saveFileInfo?.FileName == null) return false;
            
            try
            {
                // Check file exists
                if (!SaveFileUtilities.SaveFileExists(saveFileInfo.FileName))
                    return false;
                
                // Quick JSON structure validation
                var json = await SaveFileUtilities.ReadSaveFileAsync(saveFileInfo.FileName);
                return JsonValidationUtilities.ValidateGameSessionJsonStructure(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LoadService] Validation failed for '{saveFileInfo.FileName}': {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Basic GameSession validation
        /// </summary>
        private static bool IsValidGameSession(GameSession session)
        {
            return session != null &&
                   !string.IsNullOrEmpty(session.PlayerName) &&
                   !string.IsNullOrEmpty(session.CurrentScene);
        }
        
        #endregion
        
        #region UI Support Methods
        
        /// <summary>
        /// Uses SaveService for file enumeration, focuses on load validation
        /// </summary>
        public async Task<SaveFileInfo[]> GetLoadableSaveFilesAsync()
        {
            try
            {
                Debug.Log("[LoadService] Getting loadable save files for UI display...");
                
                // Get save service for file enumeration
                var saveService = GameManager.GetService<ISaveService>();
                var allSaveFileInfos = await saveService.GetSaveFileInfosAsync();
                
                if (allSaveFileInfos.Length == 0)
                {
                    Debug.Log("[LoadService] No save files found");
                    return Array.Empty<SaveFileInfo>();
                }
                
                // Simple parallel validation
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
        /// Basic parallel validation
        /// </summary>
        private async Task<SaveFileInfo[]> FilterToLoadableFilesAsync(IEnumerable<SaveFileInfo> allSaveFiles)
        {
            var validationTasks = allSaveFiles.Select(async saveFileInfo => new 
            {
                SaveFile = saveFileInfo,
                IsLoadable = await CanLoadGame(saveFileInfo)
            });
            
            var validationResults = await Task.WhenAll(validationTasks);
            
            return validationResults
                .Where(result => result.IsLoadable)
                .Select(result => result.SaveFile)
                .ToArray();
        }
        
        #endregion
        
        #region Progress Notification
        
        private void NotifyProgress(string message, float progress)
        {
            _eventSystem.Publish(new LoadingProgressEvent(message, progress));
        }

        #endregion
    }
}
