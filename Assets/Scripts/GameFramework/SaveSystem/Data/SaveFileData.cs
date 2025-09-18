using System;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace GameFramework.SaveSystem.Data
{
    /// <summary>
    /// Container for all save file data with clean JSON structure
    /// Uses direct field mapping for core system data and Dictionary for dynamic saveables
    /// Supports both static system data and runtime-generated saveable objects
    /// </summary>
    [System.Serializable]
    public class SaveFileData
    {
        #region Serialized Fields
        [SerializeField] public long SaveTimeTicks;
        [SerializeField] public bool WasAutoSave;
        
        // Core game data (always present with fixed SaveKeys)
        [SerializeField] public GameSessionSaveData GameSessionData;  // SaveKey: "GameSessionData"
        [SerializeField] public PlayerSaveData PlayerData;            // SaveKey: "PlayerData"
        
        // Dynamic saveable objects (SaveableBase instances with generated SaveKeys)
        [SerializeField] public SavedObjectEntry[] DynamicSaveableObjects;
        
        // Future extensions: Add new fields here as needed
        // [SerializeField] public List<EnemySaveData> Enemies;
        // [SerializeField] public InventorySaveData Inventory;
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
        #endregion

        #region Constructor
        public SaveFileData()
        {
            SaveTimeTicks = DateTime.Now.Ticks;
            WasAutoSave = false;
            DynamicSaveableObjects = new SavedObjectEntry[0]; // Initialize as empty array
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Sets save data by key - handles both static fields and dynamic saveable objects
        /// Static fields: GameSessionData, PlayerData (use reflection)
        /// Dynamic objects: SaveableBase instances (store in DynamicSaveableObjects array)
        /// </summary>
        public bool SetSaveData(string saveKey, object data)
        {
            try
            {
                // First, try to set as a static field (for core game data)
                var field = typeof(SaveFileData).GetField(saveKey, BindingFlags.Public | BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(this, data);
                    Debug.Log($"[SaveFileData] Set static field save data for key: {saveKey}");
                    return true;
                }
                
                // If not a static field, handle as dynamic saveable object
                return SetDynamicSaveData(saveKey, data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveFileData] Failed to set save data for key {saveKey}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sets dynamic saveable object data in the DynamicSaveableObjects array
        /// </summary>
        private bool SetDynamicSaveData(string saveKey, object data)
        {
            try
            {
                // Convert to list for easier manipulation
                var dynamicObjects = DynamicSaveableObjects?.ToList() ?? new List<SavedObjectEntry>();
                
                // Create SavedObjectData from the raw save data
                var savedObjectData = new SavedObjectData(data?.GetType().Name ?? "Unknown", data);
                
                // Look for existing entry with this key
                var existingEntry = dynamicObjects.FirstOrDefault(e => e.Key == saveKey);
                if (existingEntry != null)
                {
                    // Update existing entry
                    existingEntry.Value = savedObjectData;
                }
                else
                {
                    // Add new entry
                    dynamicObjects.Add(new SavedObjectEntry 
                    { 
                        Key = saveKey, 
                        Value = savedObjectData 
                    });
                }
                
                // Convert back to array
                DynamicSaveableObjects = dynamicObjects.ToArray();
                
                Debug.Log($"[SaveFileData] Set dynamic save data for key: {saveKey}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveFileData] Failed to set dynamic save data for key {saveKey}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets save data by key - handles both static fields and dynamic saveable objects
        /// </summary>
        public T GetSaveData<T>(string saveKey) where T : class
        {
            try
            {
                // First, try to get from static field (for core game data)
                var field = typeof(SaveFileData).GetField(saveKey, BindingFlags.Public | BindingFlags.Instance);
                if (field != null)
                {
                    return field.GetValue(this) as T;
                }
                
                // If not a static field, try to get from dynamic saveable objects
                return GetDynamicSaveData<T>(saveKey);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveFileData] Failed to get save data for key {saveKey}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets dynamic saveable object data from the DynamicSaveableObjects array
        /// </summary>
        private T GetDynamicSaveData<T>(string saveKey) where T : class
        {
            try
            {
                if (DynamicSaveableObjects == null)
                {
                    return null;
                }
                
                // Find the entry with matching key
                var entry = DynamicSaveableObjects.FirstOrDefault(e => e.Key == saveKey);
                if (entry?.Value != null)
                {
                    // Use SavedObjectData's generic method to deserialize
                    return entry.Value.GetData<T>();
                }
                
                Debug.LogWarning($"[SaveFileData] No dynamic save data found for key: {saveKey}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveFileData] Failed to get dynamic save data for key {saveKey}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Validates that all expected save data is present
        /// </summary>
        public bool ValidateData()
        {
            bool isValid = true;

            // Validate static/core data
            if (GameSessionData == null)
            {
                Debug.LogError("[SaveFileData] GameSessionData is null");
                isValid = false;
            }

            if (PlayerData == null)
            {
                Debug.LogError("[SaveFileData] PlayerData is null");
                isValid = false;
            }

            // Validate dynamic saveable objects array
            if (DynamicSaveableObjects != null)
            {
                int validDynamicObjects = 0;
                foreach (var entry in DynamicSaveableObjects)
                {
                    if (entry != null && !string.IsNullOrEmpty(entry.Key) && entry.Value != null)
                    {
                        validDynamicObjects++;
                    }
                }
                Debug.Log($"[SaveFileData] Found {validDynamicObjects} valid dynamic saveable objects");
            }
            else
            {
                Debug.LogWarning("[SaveFileData] DynamicSaveableObjects is null");
            }

            return isValid;
        }

        /// <summary>
        /// Gets all dynamic save keys for debugging purposes
        /// </summary>
        public string[] GetAllDynamicSaveKeys()
        {
            if (DynamicSaveableObjects == null) return new string[0];
            
            return DynamicSaveableObjects
                .Where(entry => entry != null && !string.IsNullOrEmpty(entry.Key))
                .Select(entry => entry.Key)
                .ToArray();
        }

        /// <summary>
        /// Gets count of dynamic saveable objects
        /// </summary>
        public int GetDynamicObjectCount()
        {
            return DynamicSaveableObjects?.Length ?? 0;
        }
        #endregion
    }
}
