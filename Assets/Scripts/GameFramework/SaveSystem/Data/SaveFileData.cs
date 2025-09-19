using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameFramework.SaveSystem.Utilities;

namespace GameFramework.SaveSystem.Data
{
    /// <summary>
    /// New clean save file data structure with dynamic runtime object storage.
    /// Uses unified SerializedRuntimeObject collection that can store any type of RuntimeObjectSaveData.
    /// Automatically extensible - no code changes needed for new saveable types!
    /// Maintains compatibility with PlayerData and GameSessionData as requested.
    /// </summary>
    [System.Serializable]
    public class SaveFileData
    {
        #region Serialized Fields
        [Header("Save File Metadata")]
        [SerializeField] public long SaveTimeTicks;
        [SerializeField] public bool WasAutoSave;
        
        [Header("Core Game Data")]
        // Core game data (always present with fixed SaveKeys) - UNCHANGED as requested
        [SerializeField] public GameSessionSaveData GameSessionData;  // SaveKey: "GameSessionData"
        [SerializeField] public PlayerSaveData PlayerData;            // SaveKey: "PlayerData"
        
        [Header("Runtime Objects")]
        // Dynamic runtime object storage - automatically handles ANY saveable type!
        [SerializeField] public List<SerializedRuntimeObject> RuntimeObjects = new List<SerializedRuntimeObject>();
        
        [Header("Debug Info")]
        [SerializeField, ReadOnly] private int _totalRuntimeObjects = 0;
        [SerializeField, ReadOnly] private string _lastUpdated = "";
        #endregion

        #region Public Properties
        /// <summary>
        /// Helper property to get DateTime from ticks
        /// </summary>
        public DateTime SaveTime 
        { 
            get => new DateTime(SaveTimeTicks);
            set => SaveTimeTicks = value.Ticks;
        }
        
        /// <summary>
        /// Total number of runtime objects in this save file
        /// </summary>
        public int TotalRuntimeObjects 
        { 
            get 
            {
                UpdateDebugInfo();
                return RuntimeObjects?.Count ?? 0;
            }
        }
        #endregion

        #region Constructor
        public SaveFileData()
        {
            SaveTimeTicks = DateTime.Now.Ticks;
            WasAutoSave = false;
            RuntimeObjects = new List<SerializedRuntimeObject>();
            UpdateDebugInfo();
        }
        #endregion

        #region Runtime Object Management
        /// <summary>
        /// Adds or updates a runtime object's save data using the unified storage system.
        /// Automatically handles any type of RuntimeObjectSaveData without needing code changes.
        /// </summary>
        /// <param name="saveData">The runtime object save data</param>
        /// <returns>True if the object was added or updated successfully</returns>
        public bool SetRuntimeObjectData(RuntimeObjectSaveData saveData)
        {
            if (saveData == null || string.IsNullOrEmpty(saveData.uniqueID))
            {
                Debug.LogError("[SaveFileDataV2] Cannot set runtime object data - null data or missing unique ID");
                return false;
            }

            try
            {
                // Find existing object by unique ID
                var existingIndex = RuntimeObjects.FindIndex(obj => obj.uniqueID == saveData.uniqueID);
                
                if (existingIndex >= 0)
                {
                    // Update existing object
                    bool updateSuccess = RuntimeObjects[existingIndex].UpdateFrom(saveData);
                    if (updateSuccess)
                    {
                        Debug.Log($"[SaveFileDataV2] Updated runtime object: {saveData.uniqueID} ({saveData.typeName})");
                        UpdateDebugInfo();
                        return true;
                    }
                    else
                    {
                        Debug.LogError($"[SaveFileDataV2] Failed to update runtime object: {saveData.uniqueID}");
                        return false;
                    }
                }
                else
                {
                    // Add new object
                    var serializedObject = new SerializedRuntimeObject(saveData);
                    if (serializedObject.IsValid())
                    {
                        RuntimeObjects.Add(serializedObject);
                        Debug.Log($"[SaveFileDataV2] Added new runtime object: {saveData.uniqueID} ({saveData.typeName})");
                        UpdateDebugInfo();
                        return true;
                    }
                    else
                    {
                        Debug.LogError($"[SaveFileDataV2] Failed to serialize runtime object: {saveData.uniqueID}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveFileDataV2] Error setting runtime object data: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets all runtime objects deserialized from the unified storage.
        /// This provides a unified way to access all runtime objects regardless of their specific type.
        /// </summary>
        /// <returns>List containing all deserialized runtime objects</returns>
        public List<RuntimeObjectSaveData> GetAllRuntimeObjects()
        {
            var allObjects = new List<RuntimeObjectSaveData>();

            if (RuntimeObjects != null)
            {
                foreach (var serializedObj in RuntimeObjects)
                {
                    var deserializedObj = serializedObj.Deserialize();
                    if (deserializedObj != null)
                    {
                        allObjects.Add(deserializedObj);
                    }
                    else
                    {
                        Debug.LogWarning($"[SaveFileDataV2] Failed to deserialize runtime object: {serializedObj.uniqueID}");
                    }
                }
            }

            return allObjects;
        }

        /// <summary>
        /// Gets a runtime object by unique ID from the unified storage.
        /// </summary>
        /// <param name="uniqueID">The unique ID to search for</param>
        /// <returns>The deserialized runtime object if found, null otherwise</returns>
        public RuntimeObjectSaveData GetRuntimeObjectByID(string uniqueID)
        {
            if (string.IsNullOrEmpty(uniqueID))
                return null;

            var serializedObj = RuntimeObjects?.FirstOrDefault(obj => obj.uniqueID == uniqueID);
            return serializedObj?.Deserialize();
        }
        
        /// <summary>
        /// Gets runtime object save data by unique ID and type using the unified storage
        /// </summary>
        /// <typeparam name="T">The type of runtime save data</typeparam>
        /// <param name="uniqueID">The unique ID of the object</param>
        /// <returns>The save data, or null if not found or wrong type</returns>
        public T GetRuntimeObjectData<T>(string uniqueID) where T : RuntimeObjectSaveData
        {
            if (string.IsNullOrEmpty(uniqueID))
                return null;

            try
            {
                var serializedObj = RuntimeObjects?.FirstOrDefault(obj => obj.uniqueID == uniqueID);
                if (serializedObj == null)
                    return null;

                var deserializedObj = serializedObj.Deserialize();
                return deserializedObj as T;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveFileDataV2] Error getting runtime object data: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Gets all runtime objects of a specific type from the unified storage
        /// </summary>
        /// <typeparam name="T">The type of runtime save data</typeparam>
        /// <returns>List of objects of the specified type</returns>
        public List<T> GetAllRuntimeObjectsOfType<T>() where T : RuntimeObjectSaveData
        {
            var results = new List<T>();

            try
            {
                if (RuntimeObjects != null)
                {
                    foreach (var serializedObj in RuntimeObjects)
                    {
                        var deserializedObj = serializedObj.Deserialize();
                        if (deserializedObj is T typedObj)
                        {
                            results.Add(typedObj);
                        }
                    }
                }

                return results;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveFileDataV2] Error getting runtime objects of type: {ex.Message}");
                return new List<T>();
            }
        }
        
        /// <summary>
        /// Removes a runtime object by unique ID from the unified storage
        /// </summary>
        /// <param name="uniqueID">The unique ID of the object to remove</param>
        /// <returns>True if the object was found and removed</returns>
        public bool RemoveRuntimeObject(string uniqueID)
        {
            if (string.IsNullOrEmpty(uniqueID))
                return false;

            try
            {
                int removedCount = RuntimeObjects.RemoveAll(obj => obj.uniqueID == uniqueID);
                
                if (removedCount > 0)
                {
                    Debug.Log($"[SaveFileDataV2] Removed runtime object: {uniqueID}");
                    UpdateDebugInfo();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveFileDataV2] Error removing runtime object: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Validation
        /// <summary>
        /// Validates that all expected save data is present and properly structured
        /// </summary>
        public bool ValidateData()
        {
            bool isValid = true;

            // Validate core data (unchanged from original requirements)
            if (GameSessionData == null)
            {
                Debug.LogError("[SaveFileDataV2] GameSessionData is null");
                isValid = false;
            }

            if (PlayerData == null)
            {
                Debug.LogError("[SaveFileDataV2] PlayerData is null");
                isValid = false;
            }

            // Validate runtime objects
            int totalObjects = TotalRuntimeObjects;
            Debug.Log($"[SaveFileDataV2] Found {totalObjects} runtime objects");
            
            if (RuntimeObjects != null)
            {
                int validCount = 0;
                int invalidCount = 0;
                var typeGroups = new Dictionary<string, int>();

                foreach (var serializedObj in RuntimeObjects)
                {
                    if (serializedObj.IsValid())
                    {
                        validCount++;
                        
                        // Count objects by type for debugging
                        if (!typeGroups.ContainsKey(serializedObj.typeName))
                            typeGroups[serializedObj.typeName] = 0;
                        typeGroups[serializedObj.typeName]++;
                    }
                    else
                    {
                        invalidCount++;
                        Debug.LogWarning($"[SaveFileDataV2] Invalid serialized object: {serializedObj.uniqueID}");
                    }
                }

                Debug.Log($"[SaveFileDataV2] Runtime Objects: {validCount} valid, {invalidCount} invalid");
                
                // Log type breakdown
                foreach (var typeGroup in typeGroups)
                {
                    Debug.Log($"[SaveFileDataV2] - {typeGroup.Key}: {typeGroup.Value} objects");
                }
                
                if (invalidCount > 0)
                    isValid = false;
            }

            return isValid;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Updates debug information fields
        /// </summary>
        private void UpdateDebugInfo()
        {
            _totalRuntimeObjects = RuntimeObjects?.Count ?? 0;
            _lastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
        
        /// <summary>
        /// Gets type statistics for debugging purposes
        /// </summary>
        /// <returns>Dictionary mapping type names to counts</returns>
        public Dictionary<string, int> GetTypeStatistics()
        {
            var stats = new Dictionary<string, int>();
            
            if (RuntimeObjects != null)
            {
                foreach (var serializedObj in RuntimeObjects)
                {
                    if (!string.IsNullOrEmpty(serializedObj.typeName))
                    {
                        if (!stats.ContainsKey(serializedObj.typeName))
                            stats[serializedObj.typeName] = 0;
                        stats[serializedObj.typeName]++;
                    }
                }
            }
            
            return stats;
        }
        #endregion
    }
}
