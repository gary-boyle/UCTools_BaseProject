using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using GameFramework.Services.Interfaces;
using GameFramework.SaveSystem.Interfaces;
using GameFramework.SaveSystem.Data;
using GameFramework.SaveSystem.Utilities;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Events.Enums;
using GameFramework.EventSystem.Interfaces;

namespace GameFramework.SaveSystem.Services
{
    /// <summary>
    /// Enhanced SaveService that uses the new clean save system with direct field storage.
    /// Creates SaveFileDataV2 with typed runtime object collections instead of nested JSON strings.
    /// Works with SaveableBase objects and the new RuntimeObjectSaveData system.
    /// </summary>
    public class SaveService : ISaveService
    {
        #region Private Fields
        private IGameDataService _gameDataService;
        private IEventSystem _eventSystem;
        private ISaveDataRegistry _saveDataRegistry;
        
        private bool _isSaving = false;
        #endregion

        #region IGameService Implementation
        public bool IsInitialized { get; private set; }

        public SaveService(
            IGameDataService gameDataService, 
            IEventSystem eventSystem,
            ISaveDataRegistry saveDataRegistry)
        {
            _gameDataService = gameDataService ?? throw new ArgumentNullException(nameof(gameDataService));
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            _saveDataRegistry = saveDataRegistry ?? throw new ArgumentNullException(nameof(saveDataRegistry));
        }

        public async Task InitializeAsync()
        {
            if (IsInitialized) return;

            Debug.Log("[SaveServiceV2] Initializing enhanced save service...");

            // Subscribe to save events
            _eventSystem.Subscribe<SaveRequestedEvent>(OnSaveRequested);

            IsInitialized = true;
            Debug.Log("[SaveServiceV2] Enhanced save service initialized and subscribed to events");

            await Task.CompletedTask;
        }

        public void Shutdown()
        {
            if (!IsInitialized) return;

            Debug.Log("[SaveServiceV2] Shutting down enhanced save service...");

            _eventSystem?.Unsubscribe<SaveRequestedEvent>(OnSaveRequested);

            _gameDataService = null;
            _eventSystem = null;
            _saveDataRegistry = null;
            IsInitialized = false;

            Debug.Log("[SaveServiceV2] Enhanced save service shutdown complete");
        }
        #endregion

        #region ISaveService Implementation
        /// <summary>
        /// Handles save requested events from the event system
        /// Implements the ISaveService interface requirement
        /// </summary>
        public async void OnSaveRequested(SaveRequestedEvent saveEvent)
        {
            if (!IsInitialized)
            {
                Debug.LogError("[SaveServiceV2] Cannot process save request - service not initialized");
                _eventSystem?.Publish(new SaveFailedEvent("Save service not initialized", saveEvent.SaveType));
                return;
            }

            Debug.Log($"[SaveServiceV2] Processing {saveEvent.SaveType} save request");

            try
            {
                string fileName = null;
                bool isAutoSave = saveEvent.SaveType == SaveType.Auto;

                switch (saveEvent.SaveType)
                {
                    case SaveType.Regular:
                        fileName = $"save_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                        break;
                    case SaveType.Auto:
                        fileName = $"autosave_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                        break;
                    case SaveType.Overwrite:
                        fileName = saveEvent.TargetSaveFile?.FileName;
                        break;
                }

                if (string.IsNullOrEmpty(fileName))
                {
                    throw new Exception("Could not determine save file name");
                }

                bool success = await SaveGameStateAsync(fileName, isAutoSave);
                
                if (success)
                {
                    _eventSystem?.Publish(new SaveCompletedEvent(fileName, saveEvent.SaveType));
                }
                else
                {
                    _eventSystem?.Publish(new SaveFailedEvent("Save operation failed", saveEvent.SaveType));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveServiceV2] Save operation failed: {ex.Message}");
                _eventSystem?.Publish(new SaveFailedEvent($"Save operation failed: {ex.Message}", saveEvent.SaveType, ex));
            }
        }
        #endregion

        #region ISaveService Implementation
        /// <summary>
        /// Saves the current game state to a file using the new clean save system
        /// </summary>
        public async Task<bool> SaveGameStateAsync(string fileName, bool isAutoSave = false)
        {
            if (!IsInitialized)
            {
                Debug.LogError("[SaveServiceV2] Cannot save game state - service not initialized");
                return false;
            }

            if (_isSaving)
            {
                Debug.LogWarning("[SaveServiceV2] Save operation already in progress");
                return false;
            }

            try
            {
                _isSaving = true;
                
                // Step 1: Initialize saving
                await PublishProgress("Initializing save...", 0.0f);
                await Task.Delay(100);

                // Step 2: Create save file data container
                await PublishProgress("Gathering game data...", 0.1f);
                var saveFileData = await CreateSaveFileDataAsync(isAutoSave);
                if (saveFileData == null)
                {
                    throw new Exception("Failed to create save file data");
                }

                // Step 3: Collect runtime object data
                await PublishProgress("Collecting runtime objects...", 0.3f);
                await CollectRuntimeObjectDataAsync(saveFileData);

                // Step 4: Validate save data
                await PublishProgress("Validating save data...", 0.7f);
                if (!saveFileData.ValidateData())
                {
                    throw new Exception("Save data validation failed");
                }

                // Step 5: Write to disk
                await PublishProgress("Writing to disk...", 0.8f);
                bool writeSuccess = await WriteSaveFileV2Async(fileName, saveFileData);
                if (!writeSuccess)
                {
                    throw new Exception("Failed to write save file to disk");
                }

                // Step 6: Complete
                await PublishProgress("Save complete!", 1.0f);
                await Task.Delay(100);

                // Publish completion event
                _eventSystem?.Publish(new SavingCompletedEvent(fileName, isAutoSave));
                
                Debug.Log($"[SaveServiceV2] Successfully saved game state to: {fileName}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveServiceV2] Error saving game state: {ex.Message}");
                _eventSystem?.Publish(new SavingFailedEvent(ex, fileName, isAutoSave));
                return false;
            }
            finally
            {
                _isSaving = false;
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Creates the base SaveFileDataV2 container with core game data
        /// </summary>
        private async Task<SaveFileData> CreateSaveFileDataAsync(bool isAutoSave)
        {
            try
            {
                var saveFileData = new SaveFileData
                {
                    SaveTime = DateTime.Now,
                    WasAutoSave = isAutoSave
                };

                var sessionData = _gameDataService?.GetGameSessionData();
                
                // Get core game data from GameDataService (unchanged as requested)
                if (_gameDataService?.GetGameSessionData() != null)
                {
                    saveFileData.GameSessionData = new GameSessionSaveData
                    {
                        uniqueID = sessionData.UniqueID,
                        difficulty = sessionData.Difficulty,
                        currentScene = sessionData.CurrentScene,
                        gameTime = sessionData.GameTime
                    };
                }

                var playerData = _gameDataService?.GetPlayerData();

                if (playerData != null)
                {
                    // Assuming PlayerData has a method to get save data (unchanged as requested)
                    saveFileData.PlayerData = playerData.GetSaveData() as PlayerSaveData;
                }

                Debug.Log("[SaveServiceV2] Created base save file data");
                return saveFileData;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveServiceV2] Error creating save file data: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Collects runtime object data from all registered SaveableBase objects.
        /// Note: Core system objects (GameSessionData, PlayerData) are handled separately 
        /// in CreateSaveFileDataAsync and are skipped here.
        /// </summary>
        private async Task CollectRuntimeObjectDataAsync(SaveFileData saveFileData)
        {
            var registeredObjects = _saveDataRegistry.GetAllSaveableObjects();
            
            int successCount = 0;
            int failureCount = 0;

            foreach (var kvp in registeredObjects)
            {
                try
                {
                    var saveable = kvp.Value;
                    
                    // Skip core system objects - they're handled separately in CreateSaveFileDataAsync
                    if (saveable.SaveKey == "GameSessionData" || saveable.SaveKey == "PlayerData")
                    {
                        Debug.Log($"[SaveServiceV2] Skipping core system object: {saveable.SaveKey} (handled separately)");
                        continue;
                    }
                    
                    // Check if this is a destroyed MonoBehaviour
                    if (saveable is MonoBehaviour mb && mb == null)
                    {
                        Debug.LogWarning($"[SaveServiceV2] Saveable {saveable.SaveKey} is a destroyed MonoBehaviour, removing from registry");
                        _saveDataRegistry.DeregisterSaveable(saveable.SaveKey);
                        failureCount++;
                        continue;
                    }

                    // Get runtime save data from SaveableBase objects
                    if (saveable is SaveableBase saveableV2)
                    {
                        var runtimeSaveData = saveableV2.CreateRuntimeSaveData();
                        if (runtimeSaveData != null)
                        {
                            bool added = saveFileData.SetRuntimeObjectData(runtimeSaveData);
                            if (added)
                            {
                                successCount++;
                                Debug.Log($"[SaveServiceV2] Collected runtime save data for: {saveable.SaveKey}");
                            }
                            else
                            {
                                Debug.LogWarning($"[SaveServiceV2] Failed to add runtime save data for: {saveable.SaveKey}");
                                failureCount++;
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[SaveServiceV2] SaveableBase {saveable.SaveKey} returned null save data");
                            failureCount++;
                        }
                    }
                    else
                    {
                        Debug.LogError($"[SaveServiceV2] Object {saveable.SaveKey} is not SaveableBase - only V2 save system objects are supported");
                        failureCount++;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SaveServiceV2] Error collecting save data from {kvp.Key}: {ex.Message}");
                    failureCount++;
                }
            }

            Debug.Log($"[SaveServiceV2] Runtime object data collection complete: {successCount} succeeded, {failureCount} failed");
            await Task.Delay(100); // Small delay for progress reporting
        }

        /// <summary>
        /// Writes SaveFileDataV2 directly to disk as JSON
        /// </summary>
        private async Task<bool> WriteSaveFileV2Async(string fileName, SaveFileData saveFileData)
        {
            try
            {
                string saveDirectory = System.IO.Path.Combine(Application.persistentDataPath, "Saves");
                if (!System.IO.Directory.Exists(saveDirectory))
                {
                    System.IO.Directory.CreateDirectory(saveDirectory);
                }
                
                string filePath = System.IO.Path.Combine(saveDirectory, fileName);
                string jsonContent = JsonUtility.ToJson(saveFileData, true);
                
                await System.IO.File.WriteAllTextAsync(filePath, jsonContent);
                
                Debug.Log($"[SaveServiceV2] Successfully wrote SaveFileDataV2 to: {fileName}");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SaveServiceV2] Error writing SaveFileDataV2 to {fileName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Publishes saving progress events
        /// </summary>
        private async Task PublishProgress(string message, float progress)
        {
            _eventSystem?.Publish(new SavingProgressEvent(message, progress));
            
            // Small delay to allow UI to update
            await Task.Delay(50);
        }
        #endregion
    }

    // Event classes for the new save system
    public class SavingProgressEvent
    {
        public string Message { get; }
        public float Progress { get; }

        public SavingProgressEvent(string message, float progress)
        {
            Message = message;
            Progress = progress;
        }
    }

    public class SavingCompletedEvent
    {
        public string FileName { get; }
        public bool WasAutoSave { get; }

        public SavingCompletedEvent(string fileName, bool wasAutoSave)
        {
            FileName = fileName;
            WasAutoSave = wasAutoSave;
        }
    }

    public class SavingFailedEvent
    {
        public Exception Exception { get; }
        public string FileName { get; }
        public bool WasAutoSave { get; }

        public SavingFailedEvent(Exception exception, string fileName, bool wasAutoSave)
        {
            Exception = exception;
            FileName = fileName;
            WasAutoSave = wasAutoSave;
        }
    }
}
