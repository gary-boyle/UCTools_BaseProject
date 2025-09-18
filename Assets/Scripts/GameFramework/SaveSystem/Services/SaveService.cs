using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GameFramework.Components;
using GameFramework.DataStructures;
using UnityEngine;
using GameFramework.Services.Interfaces;
using GameFramework.SaveSystem.Data;
using GameFramework.SaveSystem.Utilities;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Events.Enums;
using GameFramework.EventSystem.Interfaces;
using GameFramework.SaveSystem.Interfaces;
using GameFramework.FileSystem.Interfaces;

namespace GameFramework.SaveSystem.Services
{
    /// <summary>
    /// Main save service responsible for orchestrating save operations
    /// Handles event-driven save requests and coordinates with registry and file service
    /// Delegates file I/O operations to FileService for separation of concerns
    /// </summary>
    public class SaveService : ISaveService
    {
        #region Private Fields
        private const string AUTO_SAVE_PREFIX = "AutoSave_";
        private const string REGULAR_SAVE_PREFIX = "Save_";
        private const string SAVE_FILE_EXTENSION = ".json";
        #endregion

        #region IGameService Implementation
        public bool IsInitialized { get; private set; }

        // Dependencies
        private readonly IEventSystem _eventSystem;
        private readonly ISaveDataRegistry _saveDataRegistry;
        private readonly IFileService _fileService;
        
        public SaveService(
            IEventSystem eventSystem,
            ISaveDataRegistry saveDataRegistry,
            IFileService fileService)
        {
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            _saveDataRegistry = saveDataRegistry ?? throw new ArgumentNullException(nameof(saveDataRegistry));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        }
        
        public async Task InitializeAsync()
        {
            if (IsInitialized) return;

            Debug.Log("[SaveService] Initializing save service...");

            // Note: FileService handles directory creation, we just subscribe to events
            SubscribeToEvents();
            
            IsInitialized = true;
            Debug.Log("[SaveService] Save service initialized successfully");
        }

        public void Shutdown()
        {
            if (!IsInitialized) return;

            UnsubscribeFromEvents();
            
            Debug.Log("[SaveService] Shutting down save service...");
            IsInitialized = false;
            Debug.Log("[SaveService] Save service shutdown complete");
        }
        
        private void SubscribeToEvents()
        {
            _eventSystem.Subscribe<SaveRequestedEvent>(OnSaveRequested);
        }
        
        private void UnsubscribeFromEvents()
        {
            _eventSystem.Unsubscribe<SaveRequestedEvent>(OnSaveRequested);
        }
        
        #endregion

        #region Event Handlers
        /// <summary>
        /// Handles save requested events from the event system
        /// Processes different save types (Regular, Auto, Overwrite)
        /// </summary>
        public async void OnSaveRequested(SaveRequestedEvent saveEvent)
        {
            if (!IsInitialized)
            {
                Debug.LogError("[SaveService] Cannot process save request - service not initialized");
                PublishSaveFailedEvent("Save service not initialized", saveEvent.SaveType);
                return;
            }

            if (_saveDataRegistry == null)
            {
                Debug.LogError("[SaveService] Cannot process save request - registry dependency not set");
                PublishSaveFailedEvent("Save data registry not available", saveEvent.SaveType);
                return;
            }

            if (_fileService == null)
            {
                Debug.LogError("[SaveService] Cannot process save request - file service dependency not set");
                PublishSaveFailedEvent("File service not available", saveEvent.SaveType);
                return;
            }

            Debug.Log($"[SaveService] Processing {saveEvent.SaveType} save request");

            try
            {
                string fileName = await ProcessSaveRequest(saveEvent);
                if (!string.IsNullOrEmpty(fileName))
                {
                    PublishSaveCompletedEvent(fileName, saveEvent.SaveType);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveService] Save operation failed: {ex.Message}");
                PublishSaveFailedEvent($"Save operation failed: {ex.Message}", saveEvent.SaveType, ex);
            }
        }
        #endregion

        #region Save Operations
        /// <summary>
        /// Processes save request based on save type
        /// Returns the saved file name on success, null on failure
        /// </summary>
        private async Task<string> ProcessSaveRequest(SaveRequestedEvent saveEvent)
        {
            string fileName;
    
            switch (saveEvent.SaveType)
            {
                case SaveType.Regular:
                    fileName = GenerateRegularSaveFileName();
                    break;
            
                case SaveType.Auto:
                    fileName = await GetAutoSaveFileName();
                    break;
            
                case SaveType.Overwrite:
                    if (saveEvent.TargetSaveFile == null)
                    {
                        throw new InvalidOperationException("Overwrite save requires target save file");
                    }
                    fileName = saveEvent.TargetSaveFile.FileName;
                    break;
            
                default:
                    throw new ArgumentException($"Unknown save type: {saveEvent.SaveType}");
            }

            return await PerformSave(fileName, saveEvent.SaveType == SaveType.Auto);
        }

        /// <summary>
        /// Performs the actual save operation
        /// Collects data from registry and delegates file writing to FileService
        /// </summary>
        private async Task<string> PerformSave(string fileName, bool isAutoSave)
        {
            Debug.Log($"[SaveService] Starting save operation: {fileName}");

            // Create save file data container
            var saveFileData = new SaveFileData
            {
                SaveTime = DateTime.Now,
                WasAutoSave = isAutoSave
            };

            // Collect data from all registered saveables
            var registeredObjects = _saveDataRegistry.GetAllSaveableObjects();
            
            int successCount = 0;
            int failureCount = 0;

            foreach (var kvp in registeredObjects)
            {
                try
                {
                    var saveable = kvp.Value;
                    Debug.Log($"[SaveService] Processing saveable: {saveable.SaveKey} (Type: {saveable.TypeName})");
                    
                    // Check if this is a destroyed MonoBehaviour
                    if (saveable is MonoBehaviour mb && mb == null)
                    {
                        Debug.LogWarning($"[SaveService] Saveable {saveable.SaveKey} is a destroyed MonoBehaviour, removing from registry");
                        _saveDataRegistry.DeregisterSaveable(saveable.SaveKey);
                        failureCount++;
                        continue;
                    }

                    // Get save data from the saveable object
                    var saveData = saveable.GetSaveData();
                    
                    if (saveData == null)
                    {
                        Debug.LogWarning($"[SaveService] Saveable {saveable.SaveKey} returned null save data");
                        failureCount++;
                        continue;
                    }

                    // Use reflection to assign to the appropriate field in SaveFileData
                    bool assigned = saveFileData.SetSaveData(saveable.SaveKey, saveData);
                    
                    if (assigned)
                    {
                        successCount++;
                        Debug.Log($"[SaveService] Successfully collected save data for: {saveable.SaveKey}");
                    }
                    else
                    {
                        failureCount++;
                        Debug.LogError($"[SaveService] Failed to assign save data for: {saveable.SaveKey}. " +
                                      $"Ensure SaveFileData has a public field named '{saveable.SaveKey}'");
                    }
                }
                catch (Exception ex)
                {
                    failureCount++;
                    Debug.LogError($"[SaveService] Exception while collecting save data from {kvp.Key}: {ex.Message}");
                }
            }

            Debug.Log($"[SaveService] Save data collection complete. Success: {successCount}, Failures: {failureCount}");

            // Validate the collected data (now allows null PlayerData)
            bool isValid = saveFileData.ValidateData();
            if (!isValid)
            {
                throw new InvalidOperationException("Save data validation failed - essential data is missing");
            }
            
            // Additional info about what was saved
            if (saveFileData.PlayerData == null)
            {
                Debug.LogError("[SaveService] Save completed without PlayerData - player may not be instantiated yet");
            }

            // Delegate file writing to FileService
            Debug.Log($"[SaveService] Delegating file write to FileService: {fileName}");
            bool writeSuccess = await _fileService.WriteSaveFileAsync(fileName, saveFileData);
            
            if (!writeSuccess)
            {
                throw new InvalidOperationException($"FileService failed to write save file: {fileName}");
            }
            
            Debug.Log($"[SaveService] Save completed successfully: {fileName}");
            return fileName;
        }
        #endregion

        #region File Name Generation
        /// <summary>
        /// Generates filename for regular saves with timestamp
        /// </summary>
        private string GenerateRegularSaveFileName()
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            return $"{REGULAR_SAVE_PREFIX}{timestamp}{SAVE_FILE_EXTENSION}";
        }

        /// <summary>
        /// Generates filename for auto saves with timestamp (fallback for when no UniqueID available)
        /// </summary>
        private string GenerateAutoSaveFileName()
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            return $"{AUTO_SAVE_PREFIX}{timestamp}{SAVE_FILE_EXTENSION}";
        }
        
        /// <summary>
        /// Gets the auto-save filename for the current player using their unique ID
        /// Returns existing auto-save filename if found, otherwise generates new one
        /// </summary>
        private async Task<string> GetAutoSaveFileName()
        {
            // Get current player from registered saveables
            var registeredObjects = _saveDataRegistry.GetAllSaveableObjects();
            var playerData = registeredObjects.Values.FirstOrDefault(s => s.SaveKey == "PlayerData") as PlayerData;
    
            if (playerData?.UniqueID == null)
            {
                Debug.LogWarning("[SaveService] No Player UniqueID found, using timestamp-based auto-save");
                return GenerateAutoSaveFileName();
            }

            string playerUniqueId = playerData.UniqueID;
            string autoSaveFileName = $"AutoSave_{playerUniqueId}.json";
    
            // Check if this auto-save already exists using FileService
            if (_fileService.SaveFileExists(autoSaveFileName))
            {
                return autoSaveFileName;
            }
            else
            {
                return autoSaveFileName;
            }
        }
        #endregion

        #region Event Publishing
        /// <summary>
        /// Publishes save completed event
        /// </summary>
        private void PublishSaveCompletedEvent(string fileName, SaveType saveType)
        {
            var completedEvent = new SaveCompletedEvent(fileName, saveType);
            _eventSystem?.Publish(completedEvent); 
        }

        /// <summary>
        /// Publishes save failed event
        /// </summary>
        private void PublishSaveFailedEvent(string errorMessage, SaveType saveType, Exception exception = null)
        {
            var failedEvent = new SaveFailedEvent(errorMessage, saveType, exception);
            _eventSystem?.Publish(failedEvent); 
        }
        #endregion
    }
}
