using System;
using System.Threading.Tasks;
using UnityEngine;
using GameFramework.Services.Interfaces;
using GameFramework.LoadSystem.Interfaces;
using GameFramework.DataStructures;
using GameFramework.EventSystem.Events;
using GameFramework.SaveSystem.Data;
using GameFramework.EventSystem.Interfaces;
using GameFramework.FileSystem.Interfaces;

namespace GameFramework.LoadSystem.Services
{
    /// <summary>
    /// Service responsible for transforming save data into live game state
    /// Handles game logic for loading, delegates file operations to FileService
    /// Publishes progress events for UI updates
    /// </summary>
    public class LoadService : ILoadService
    {
        #region Private Fields
        private IFileService _fileService;
        private IGameDataService _gameDataService;
        private IEventSystem _eventSystem;
        private SaveFileInfo _currentLoadingSaveFile;
        #endregion

        #region IGameService Implementation
        public bool IsInitialized { get; private set; }
        public bool IsLoading { get; private set; }

        public LoadService(
            IFileService fileService, 
            IGameDataService gameDataService, 
            IEventSystem eventSystem)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _gameDataService = gameDataService ?? throw new ArgumentNullException(nameof(gameDataService));
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
        }
        
        public async Task InitializeAsync()
        {
            if (IsInitialized) return;

            Debug.Log("[LoadService] Initializing load service...");

            // Subscribe to begin load events from UI
            _eventSystem.Subscribe<BeginLoadGameEvent>(OnBeginLoadGameRequested);

            IsInitialized = true;
            Debug.Log("[LoadService] Load service initialized and subscribed to events");
        }

        public void Shutdown()
        {
            if (!IsInitialized) return;

            Debug.Log("[LoadService] Shutting down load service...");

            _eventSystem?.Unsubscribe<BeginLoadGameEvent>(OnBeginLoadGameRequested);
            
            _fileService = null;
            _gameDataService = null;
            _eventSystem = null;
            IsInitialized = false;
            
            Debug.Log("[LoadService] Load service shutdown complete");
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Handles begin load game events from UI - starts the loading process
        /// </summary>
        private async void OnBeginLoadGameRequested(BeginLoadGameEvent evt)
        {
            if (evt?.SaveFileInfo == null)
            {
                Debug.LogError("[LoadService] Received begin load event with null save file info");
                return;
            }

            Debug.Log($"[LoadService] Beginning load process for: {evt.SaveFileInfo.FileName}");

            // Store the save file info for progress reporting
            _currentLoadingSaveFile = evt.SaveFileInfo;

            // Start the loading process
            bool success = await LoadGameStateAsync(evt.SaveFileInfo);
            
            if (!success)
            {
                Debug.LogError($"[LoadService] Failed to load game state from: {evt.SaveFileInfo.FileName}");
            }
        }
        #endregion

        #region ILoadService Implementation
        /// <summary>
        /// Loads a save file and applies it to current game state with progress reporting
        /// </summary>
        public async Task<bool> LoadGameStateAsync(SaveFileInfo saveFileInfo)
        {
            if (!IsInitialized)
            {
                Debug.LogError("[LoadService] Cannot load game state - service not initialized");
                return false;
            }

            if (IsLoading)
            {
                Debug.LogWarning("[LoadService] Load operation already in progress");
                return false;
            }

            try
            {
                IsLoading = true;
                
                // Step 1: Initialize loading
                await PublishProgress("Initializing load...", 0.0f);
                await Task.Delay(100); // Small delay for UI feedback

                // Step 2: Read save file from disk
                await PublishProgress("Reading save file...", 0.2f);
                var saveFileData = await _fileService.ReadSaveFileAsync(saveFileInfo.FileName);
                if (saveFileData == null)
                {
                    throw new Exception("Failed to read save file from disk");
                }

                // Step 3: Validate save data
                await PublishProgress("Validating save data...", 0.4f);
                await Task.Delay(100);
                
                if (!saveFileData.ValidateData())
                {
                    throw new Exception("Save file data is corrupted or invalid");
                }

                // Step 4: Convert save data to runtime objects
                await PublishProgress("Converting save data...", 0.6f);
                var loadedGameState = await ConvertSaveDataAsync(saveFileData);
                if (loadedGameState == null || !loadedGameState.IsValid())
                {
                    throw new Exception("Failed to convert save data to game objects");
                }

                // Step 5: Apply to game data service
                await PublishProgress("Applying game state...", 0.8f);
                _gameDataService.LoadGameData(loadedGameState.GameSessionData, loadedGameState.PlayerData);
                await Task.Delay(200); // Small delay for processing

                // Step 6: Complete
                await PublishProgress("Loading complete!", 1.0f);
                await Task.Delay(100);

                // Publish completion event
                _eventSystem?.Publish(new LoadingCompletedEvent());
                
                Debug.Log($"[LoadService] Successfully loaded game state from: {saveFileInfo.FileName}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LoadService] Error loading game state: {ex.Message}");
                _eventSystem?.Publish(new LoadingFailedEvent(ex));
                return false;
            }
            finally
            {
                IsLoading = false;
                _currentLoadingSaveFile = null;
            }
        }

        /// <summary>
        /// Loads save data from SaveFileData and applies it to game state
        /// </summary>
        public async Task<bool> LoadGameStateAsync(SaveFileData saveFileData)
        {
            // This method is used internally and doesn't need progress reporting
            if (!IsInitialized || _gameDataService == null || saveFileData == null)
                return false;

            try
            {
                var loadedGameState = await ConvertSaveDataAsync(saveFileData);
                if (loadedGameState == null || !loadedGameState.IsValid())
                    return false;

                _gameDataService.LoadGameData(loadedGameState.GameSessionData, loadedGameState.PlayerData);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LoadService] Error applying game state: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Converts SaveFileData to live game objects without applying
        /// </summary>
        public async Task<LoadedGameState> ConvertSaveDataAsync(SaveFileData saveFileData)
        {
            if (saveFileData == null) return null;

            try
            {
                var loadedGameState = new LoadedGameState
                {
                    GameSessionData = ConvertToGameSessionData(saveFileData.GameSessionData),
                    PlayerData = ConvertToPlayerData(saveFileData.PlayerData)
                };

                return loadedGameState.IsValid() ? loadedGameState : null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LoadService] Error converting save data: {ex.Message}");
                return null;
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Publishes loading progress events
        /// </summary>
        private async Task PublishProgress(string message, float progress)
        {
            _eventSystem?.Publish(new LoadingProgressEvent(message, progress));
            
            // Small delay to allow UI to update
            await Task.Delay(50);
        }

        private GameSessionData ConvertToGameSessionData(GameSessionSaveData saveData)
        {
            if (saveData == null) return null;
            return new GameSessionData(saveData.difficulty, saveData.currentScene, saveData.gameTime);
        }

        private PlayerData ConvertToPlayerData(PlayerSaveData saveData)
        {
            if (saveData == null) return null;
            return new PlayerData(saveData.playerName, saveData.Position, saveData.Rotation);
        }
        #endregion
    }
}
