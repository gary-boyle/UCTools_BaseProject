using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using GameFramework.Services.Interfaces;
using GameFramework.LoadSystem.Interfaces;
using GameFramework.LoadSystem.Services;
using GameFramework.DataStructures;
using GameFramework.EventSystem.Events;
using GameFramework.SaveSystem.Data;
using GameFramework.SaveSystem.Utilities;
using GameFramework.SaveSystem;
using GameFramework.EventSystem.Interfaces;
using GameFramework.FileSystem.Interfaces;

namespace GameFramework.LoadSystem.Services
{
    /// <summary>
    /// Enhanced LoadService that uses the new clean save system and runtime object instantiation.
    /// Supports both legacy SaveFileData and new SaveFileDataV2 formats.
    /// Uses PrefabRegistry and RuntimeObjectInstantiator instead of Resources folder.
    /// </summary>
    public class LoadServiceV2 : ILoadService
    {
        #region Private Fields
        private IFileService _fileService;
        private IGameDataService _gameDataService;
        private ISceneService _sceneService;
        private IEventSystem _eventSystem;
        private RuntimeObjectInstantiator _runtimeInstantiator;
        private SaveFileInfo _currentLoadingSaveFile;
        #endregion

        #region IGameService Implementation
        public bool IsInitialized { get; private set; }
        public bool IsLoading { get; private set; }

        public LoadServiceV2(
            IFileService fileService, 
            IGameDataService gameDataService, 
            ISceneService sceneService,
            IEventSystem eventSystem,
            RuntimeObjectInstantiator runtimeInstantiator)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _gameDataService = gameDataService ?? throw new ArgumentNullException(nameof(gameDataService));
            _sceneService = sceneService ?? throw new ArgumentNullException(nameof(sceneService));
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            _runtimeInstantiator = runtimeInstantiator ?? throw new ArgumentNullException(nameof(runtimeInstantiator));
        }
        
        public async Task InitializeAsync()
        {
            if (IsInitialized) return;

            Debug.Log("[LoadServiceV2] Initializing enhanced load service...");

            // Subscribe to begin load events from UI
            _eventSystem.Subscribe<BeginLoadGameEvent>(OnBeginLoadGameRequested);
            _eventSystem.Subscribe<BeginNewGameLoadEvent>(OnBeginNewGameLoadRequested);

            IsInitialized = true;
            Debug.Log("[LoadServiceV2] Enhanced load service initialized and subscribed to events");
        }

        public void Shutdown()
        {
            if (!IsInitialized) return;

            Debug.Log("[LoadServiceV2] Shutting down enhanced load service...");

            _eventSystem?.Unsubscribe<BeginLoadGameEvent>(OnBeginLoadGameRequested);
            _eventSystem?.Unsubscribe<BeginNewGameLoadEvent>(OnBeginNewGameLoadRequested);
            
            _fileService = null;
            _gameDataService = null;
            _sceneService = null;
            _eventSystem = null;
            _runtimeInstantiator = null;
            IsInitialized = false;
            
            Debug.Log("[LoadServiceV2] Enhanced load service shutdown complete");
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
                Debug.LogError("[LoadServiceV2] Received begin load event with null save file info");
                return;
            }

            Debug.Log($"[LoadServiceV2] Beginning load process for: {evt.SaveFileInfo.FileName}");

            // Store the save file info for progress reporting
            _currentLoadingSaveFile = evt.SaveFileInfo;

            // Start the loading process
            bool success = await LoadGameStateAsync(evt.SaveFileInfo);
            
            if (!success)
            {
                Debug.LogError($"[LoadServiceV2] Failed to load game state from: {evt.SaveFileInfo.FileName}");
            }
        }

        /// <summary>
        /// Handles begin new game load events - creates fresh SaveFileDataV2 and uses existing loading pipeline
        /// </summary>
        private async void OnBeginNewGameLoadRequested(BeginNewGameLoadEvent evt)
        {
            if (evt == null)
            {
                Debug.LogError("[LoadServiceV2] Received begin new game load event with null event data");
                return;
            }

            Debug.Log($"[LoadServiceV2] Beginning new game load process - Player: {evt.PlayerName}, Difficulty: {evt.Difficulty}, Scene: {evt.StartingScene}");

            try
            {
                // Create fresh SaveFileDataV2 for new game - this unifies the loading process
                var newGameSaveData = CreateNewGameSaveData(evt.PlayerName, evt.Difficulty, evt.StartingScene);
                if (newGameSaveData == null)
                {
                    Debug.LogError("[LoadServiceV2] Failed to create new game save data");
                    _eventSystem?.Publish(new LoadingFailedEvent(new Exception("Failed to create new game data")));
                    return;
                }

                // Use the existing SaveFileDataV2 loading pipeline - this ensures identical behavior
                bool success = await LoadGameStateAsync(newGameSaveData, isNewGame: true);
                
                if (!success)
                {
                    Debug.LogError("[LoadServiceV2] Failed to load new game state through unified pipeline");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LoadServiceV2] Error in new game loading process: {ex.Message}");
                _eventSystem?.Publish(new LoadingFailedEvent(ex));
            }
        }
        #endregion

        #region ILoadService Implementation
        /// <summary>
        /// Loads a save file and applies it to current game state with progress reporting.
        /// Works exclusively with the new SaveFileDataV2 format.
        /// </summary>
        public async Task<bool> LoadGameStateAsync(SaveFileInfo saveFileInfo)
        {
            if (!IsInitialized)
            {
                Debug.LogError("[LoadServiceV2] Cannot load game state - service not initialized");
                return false;
            }

            if (IsLoading)
            {
                Debug.LogWarning("[LoadServiceV2] Load operation already in progress");
                return false;
            }

            try
            {
                IsLoading = true;
                
                // Step 1: Initialize loading
                await PublishProgress("Initializing load...", 0.0f);
                await Task.Delay(100); // Small delay for UI feedback

                // Step 2: Read save file from disk
                await PublishProgress("Reading save file...", 0.1f);
                var saveFileDataV2 = await ReadSaveFileV2Async(saveFileInfo.FileName);
                if (saveFileDataV2 == null)
                {
                    throw new Exception("Failed to read save file from disk");
                }

                // Step 3: Continue with V2 loading pipeline
                return await LoadGameStateInternalAsync(saveFileDataV2, false);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LoadServiceV2] Error loading game state: {ex.Message}");
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
        /// Loads save data from SaveFileDataV2 and applies it to game state
        /// Includes runtime object instantiation, scene loading and progress reporting
        /// </summary>
        public async Task<bool> LoadGameStateAsync(SaveFileDataV2 saveFileData, bool isNewGame = false)
        {
            if (!IsInitialized || _gameDataService == null || saveFileData == null)
                return false;

            if (IsLoading)
            {
                Debug.LogWarning("[LoadServiceV2] Load operation already in progress");
                return false;
            }
            
            try
            {
                IsLoading = true;
                return await LoadGameStateInternalAsync(saveFileData, isNewGame);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LoadServiceV2] Error loading game state: {ex.Message}");
                _eventSystem?.Publish(new LoadingFailedEvent(ex));
                return false;
            }
            finally
            {
                IsLoading = false;
            }
        }


        /// <summary>
        /// Converts SaveFileDataV2 to live game objects without applying to services
        /// </summary>
        public async Task<LoadedGameState> ConvertSaveDataAsync(SaveFileDataV2 saveFileData)
        {
            if (saveFileData == null) return null;

            try
            {
                var loadedGameState = new LoadedGameState
                {
                    GameSessionData = ConvertToGameSessionData(saveFileData.GameSessionData),
                    PlayerSaveData = saveFileData.PlayerData
                };

                return loadedGameState.IsValid() ? loadedGameState : null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LoadServiceV2] Error converting save data: {ex.Message}");
                return null;
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Internal method that handles the actual loading process for SaveFileDataV2
        /// </summary>
        private async Task<bool> LoadGameStateInternalAsync(SaveFileDataV2 saveFileData, bool isNewGame)
        {
            string loadType = isNewGame ? "new game" : "save";

            // Step 1: Initialize loading
            await PublishProgress(isNewGame ? "Initializing new game..." : "Initializing load...", 0.0f);
            await Task.Delay(100);

            // Step 2: Validate data
            await PublishProgress(isNewGame ? "Setting up game data..." : "Validating save data...", 0.1f);
            await Task.Delay(100);

            if (!saveFileData.ValidateData())
            {
                throw new Exception($"Game data is invalid - cannot load {loadType}");
            }

            // Step 3: Convert save data to runtime objects
            await PublishProgress(isNewGame ? "Creating game objects..." : "Converting save data...", 0.2f);
            var loadedGameState = await ConvertSaveDataAsync(saveFileData);
            if (loadedGameState == null || !loadedGameState.IsValid())
            {
                throw new Exception($"Failed to convert {loadType} data to game objects");
            }

            // Step 4: Load scene if specified
            await PublishProgress("Loading scene...", 0.4f);
            var sceneToLoad = loadedGameState.GameSessionData?.CurrentScene;
            if (!string.IsNullOrEmpty(sceneToLoad))
            {
                Debug.Log($"[LoadServiceV2] Loading scene for {loadType}: {sceneToLoad}");
                
                bool sceneLoaded = await _sceneService.LoadSceneWithProgressAsync(sceneToLoad, (sceneProgress) =>
                {
                    // Map scene progress (0-1) to our overall progress range (0.4-0.6)
                    float mappedProgress = 0.4f + (sceneProgress * 0.2f);
                    _eventSystem?.Publish(new LoadingProgressEvent("Loading scene...", mappedProgress));
                });
                
                if (!sceneLoaded)
                {
                    throw new Exception($"Failed to load scene: {sceneToLoad}");
                }
            }

            // Step 5: Instantiate runtime objects
            await PublishProgress("Creating runtime objects...", 0.6f);
            await InstantiateRuntimeObjectsAsync(saveFileData);

            // Step 6: Apply to game data service
            await PublishProgress("Applying game state...", 0.85f);
            _gameDataService.LoadGameData(loadedGameState.GameSessionData, loadedGameState.PlayerSaveData);
            await Task.Delay(200);

            // Step 7: Complete
            await PublishProgress(isNewGame ? "New game ready!" : "Loading complete!", 1.0f);
            await Task.Delay(100);

            // Publish completion event
            _eventSystem?.Publish(new LoadingCompletedEvent());
            
            return true;
        }


        /// <summary>
        /// Finds an existing SaveableBase object in the scene with matching uniqueID and type.
        /// This prevents duplicate instantiation and allows updating existing scene objects.
        /// </summary>
        /// <param name="uniqueID">The unique ID to search for</param>
        /// <param name="typeName">The expected type name to match</param>
        /// <returns>The existing GameObject if found, null otherwise</returns>
        private GameObject FindExistingSceneObject(string uniqueID, string typeName)
        {
            if (string.IsNullOrEmpty(uniqueID) || string.IsNullOrEmpty(typeName))
                return null;

            // Find all SaveableBase components in the scene
            var allSaveables = UnityEngine.Object.FindObjectsOfType<SaveableBase>();

            foreach (var saveable in allSaveables)
            {
                // Check if both uniqueID and typeName match
                if (saveable.UniqueID == uniqueID && saveable.TypeName == typeName)
                {
                    Debug.Log($"[LoadServiceV2] Found existing scene object: {uniqueID} ({typeName}) on GameObject '{saveable.gameObject.name}'");
                    return saveable.gameObject;
                }
            }

            Debug.Log($"[LoadServiceV2] No existing scene object found for: {uniqueID} ({typeName})");
            return null;
        }

        /// <summary>
        /// Instantiates or updates all runtime objects from save data.
        /// First checks for existing objects in scene with matching uniqueID and type,
        /// updates those if found, otherwise instantiates new objects.
        /// </summary>
        private async Task InstantiateRuntimeObjectsAsync(SaveFileDataV2 saveFileData)
        {
            var instantiationTasks = new List<Task<GameObject>>();
            int existingUpdatedCount = 0;
            int newObjectCount = 0;

            // Get all runtime objects from save data generically
            var allRuntimeObjects = saveFileData.GetAllRuntimeObjects();

            // Process each runtime object
            foreach (var runtimeData in allRuntimeObjects)
            {
                // First, check if an object with this uniqueID and type already exists in the scene
                var existingObject = FindExistingSceneObject(runtimeData.uniqueID, runtimeData.typeName);
                
                if (existingObject != null)
                {
                    // Update existing object instead of instantiating new one
                    Debug.Log($"[LoadServiceV2] Found existing scene object {runtimeData.uniqueID} ({runtimeData.typeName}) - updating instead of instantiating");
                    
                    bool configured = await _runtimeInstantiator.ConfigureObjectAsync(existingObject, runtimeData);
                    if (configured)
                    {
                        existingUpdatedCount++;
                    }
                    else
                    {
                        Debug.LogError($"[LoadServiceV2] Failed to configure existing object {runtimeData.uniqueID}");
                    }
                }
                else
                {
                    // No existing object found, instantiate new one
                    instantiationTasks.Add(_runtimeInstantiator.InstantiateObjectAsync(runtimeData));
                    newObjectCount++;
                }
            }

            // Wait for all instantiations to complete
            var results = await Task.WhenAll(instantiationTasks);

            int instantiationSuccessCount = 0;
            int instantiationFailureCount = 0;

            foreach (var result in results)
            {
                if (result != null)
                    instantiationSuccessCount++;
                else
                    instantiationFailureCount++;
            }

            Debug.Log($"[LoadServiceV2] Runtime object processing complete: {existingUpdatedCount} existing updated, " +
                     $"{instantiationSuccessCount} new instantiated, {instantiationFailureCount} failed");
        }

        /// <summary>
        /// Reads SaveFileDataV2 directly from disk
        /// </summary>
        private async Task<SaveFileDataV2> ReadSaveFileV2Async(string fileName)
        {
            try
            {
                // Read the file as JSON and deserialize directly to SaveFileDataV2
                string filePath = System.IO.Path.Combine(Application.persistentDataPath, "Saves", fileName);
                if (!System.IO.File.Exists(filePath))
                {
                    Debug.LogError($"[LoadServiceV2] Save file not found: {fileName}");
                    return null;
                }
                
                string jsonContent = await System.IO.File.ReadAllTextAsync(filePath);
                var saveFileData = JsonUtility.FromJson<SaveFileDataV2>(jsonContent);
                
                Debug.Log($"[LoadServiceV2] Successfully read SaveFileDataV2 from: {fileName}");
                return saveFileData;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LoadServiceV2] Error reading SaveFileDataV2 from {fileName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates new game save data
        /// </summary>
        private SaveFileDataV2 CreateNewGameSaveData(string playerName, string difficulty, string startingScene)
        {
            var newGameData = new SaveFileDataV2();
            
            // Create new GameSessionData
            newGameData.GameSessionData = new GameSessionSaveData
            {
                uniqueID = UniqueIDGenerator.GenerateUniqueID("gamesession"),
                difficulty = difficulty,
                currentScene = startingScene,
                gameTime = 0
            };

            // Create new PlayerData
            newGameData.PlayerData = new PlayerSaveData
            {
                uniqueID = UniqueIDGenerator.GenerateUniqueID("player"),
                playerName = playerName,
                // Set other default values as needed
            };

            Debug.Log($"[LoadServiceV2] Created new game save data for player: {playerName}");
            return newGameData;
        }

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
            if (saveData == null)
            {
                Debug.LogError("[LoadServiceV2] Cannot convert null GameSessionSaveData");
                return null;
            }

            // Use constructor that preserves the unique ID from save data
            return new GameSessionData(
                saveData.uniqueID,   
                saveData.difficulty,
                saveData.currentScene,
                saveData.gameTime
            );
        }
        #endregion
    }
}
