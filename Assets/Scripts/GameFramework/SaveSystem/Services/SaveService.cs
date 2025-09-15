using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using GameFramework.Services.Interfaces;
using GameFramework.SaveSystem.Data;
using GameFramework.SaveSystem.Utilities;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Events.Enums;
using GameFramework.EventSystem.Interfaces;
using GameFramework.SaveSystem.Interfaces;

namespace GameFramework.SaveSystem.Services
{
    /// <summary>
    /// Main save service responsible for orchestrating save operations
    /// Handles event-driven save requests and coordinates with registry and serialization
    /// Manages file I/O operations with proper error handling
    /// </summary>
    public class SaveService : ISaveService
    {
        #region Private Fields
        private string _saveDirectory;
        private const string SAVE_FILE_EXTENSION = ".json";
        private const string AUTO_SAVE_PREFIX = "AutoSave_";
        private const string REGULAR_SAVE_PREFIX = "Save_";
        #endregion

        #region IGameService Implementation
        public bool IsInitialized { get; private set; }

        // Dependencies
        private readonly IEventSystem _eventSystem;
        private readonly ISaveDataRegistry _saveDataRegistry;
        
        public SaveService(
            IEventSystem eventSystem,
            ISaveDataRegistry saveDataRegistry)
        {
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            _saveDataRegistry = saveDataRegistry ?? throw new ArgumentNullException(nameof(saveDataRegistry));
        }
        
        public async Task InitializeAsync()
        {
            if (IsInitialized) return;

            Debug.Log("[SaveService] Initializing save service...");

            // Initialize save directory
            _saveDirectory = Path.Combine(Application.persistentDataPath, "Saves");
            if (!Directory.Exists(_saveDirectory))
            {
                Directory.CreateDirectory(_saveDirectory);
                Debug.Log($"[SaveService] Created save directory: {_saveDirectory}");
            }

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
                    fileName = GenerateAutoSaveFileName();
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
        /// Collects data from registry, serializes, and writes to file
        /// </summary>
        private async Task<string> PerformSave(string fileName, bool isAutoSave)
        {
            // Create save file data container
            var saveFileData = new SaveFileData
            {
                SaveTime = DateTime.Now,
                WasAutoSave = isAutoSave
            };

            // Collect data from registered saveables
            var registeredObjects = _saveDataRegistry.GetAllSaveableObjects();

            foreach (var kvp in registeredObjects)
            {
                try
                {
                    var saveable = kvp.Value;
                    var saveData = saveable.GetSaveData();
        
                    // Store as JSON string directly
                    var savedObjectData = new SavedObjectData(saveable.TypeName, saveData);
                    //saveFileData.SavedObjects[kvp.Key] = savedObjectData;
        
                    saveFileData.AddSavedObject(kvp.Key, savedObjectData);

                    Debug.Log($"[SaveService] Collected save data for: {saveable.SaveKey}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SaveService] Failed to collect save data from {kvp.Key}: {ex.Message}");
                }
            }

            // Serialize to JSON
            string json = JsonSerializationHelper.SerializeToJson(saveFileData, true);
            if (string.IsNullOrEmpty(json))
            {
                throw new InvalidOperationException("Failed to serialize save data to JSON");
            }

            // Write to file
            string filePath = Path.Combine(_saveDirectory, fileName);
            await WriteJsonToFileAsync(filePath, json);
            
            Debug.Log($"[SaveService] Save completed successfully: {fileName}");
            return fileName;
        }

        /// <summary>
        /// Writes JSON string to file asynchronously
        /// </summary>
        private async Task WriteJsonToFileAsync(string filePath, string json)
        {
            try
            {
                using (var writer = new StreamWriter(filePath, false))
                {
                    await writer.WriteAsync(json);
                }
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to write save file to {filePath}: {ex.Message}", ex);
            }
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
        /// Generates filename for auto saves with timestamp
        /// </summary>
        private string GenerateAutoSaveFileName()
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            return $"{AUTO_SAVE_PREFIX}{timestamp}{SAVE_FILE_EXTENSION}";
        }
        #endregion

        #region Event Publishing
        /// <summary>
        /// Publishes save completed event
        /// </summary>
        private void PublishSaveCompletedEvent(string fileName, SaveType saveType)
        {
            var completedEvent = new SaveCompletedEvent(fileName, saveType);
            // TODO: Integrate with your event system to publish the event
            Debug.Log($"[SaveService] Save completed event: {fileName} ({saveType})");
        }

        /// <summary>
        /// Publishes save failed event
        /// </summary>
        private void PublishSaveFailedEvent(string errorMessage, SaveType saveType, Exception exception = null)
        {
            var failedEvent = new SaveFailedEvent(errorMessage, saveType, exception);
            // TODO: Integrate with your event system to publish the event
            Debug.LogError($"[SaveService] Save failed event: {errorMessage} ({saveType})");
        }
        #endregion
    }
}
