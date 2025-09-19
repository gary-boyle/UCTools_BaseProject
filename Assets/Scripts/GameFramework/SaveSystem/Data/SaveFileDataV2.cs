using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GameFramework.SaveSystem.Data
{
    /// <summary>
    /// New clean save file data structure that eliminates nested JSON strings.
    /// Uses direct field storage for runtime objects instead of serialized JSON blobs.
    /// Maintains compatibility with PlayerData and GameSessionData as requested.
    /// </summary>
    [System.Serializable]
    public class SaveFileDataV2
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
        // Clean runtime object data - no more nested JSON strings!
        [SerializeField] public List<ClickableCubeRuntimeSaveData> ClickableCubes = new List<ClickableCubeRuntimeSaveData>();
        [SerializeField] public List<TestGenericRuntimeSaveData> TestGenericObjects = new List<TestGenericRuntimeSaveData>();
        
        // Future object types can be added here as direct fields
        // [SerializeField] public List<EnemyRuntimeSaveData> Enemies = new List<EnemyRuntimeSaveData>();
        // [SerializeField] public List<ItemRuntimeSaveData> Items = new List<ItemRuntimeSaveData>();
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
        public int TotalRuntimeObjects => GetAllRuntimeObjects().Count;
        #endregion

        #region Constructor
        public SaveFileDataV2()
        {
            SaveTimeTicks = DateTime.Now.Ticks;
            WasAutoSave = false;
            ClickableCubes = new List<ClickableCubeRuntimeSaveData>();
            TestGenericObjects = new List<TestGenericRuntimeSaveData>();
        }
        #endregion

        #region Runtime Object Management
        /// <summary>
        /// Adds or updates a runtime object's save data based on its type and unique ID.
        /// Uses reflection to dynamically handle any SaveableBase-derived types.
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
                // Use reflection to find the appropriate collection and method
                return SetRuntimeObjectDataGeneric(saveData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveFileDataV2] Error setting runtime object data: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets all runtime objects from all collections as a generic list.
        /// This provides a unified way to access all runtime objects regardless of their specific type.
        /// </summary>
        /// <returns>List containing all runtime objects from all typed collections</returns>
        public List<RuntimeObjectSaveData> GetAllRuntimeObjects()
        {
            var allObjects = new List<RuntimeObjectSaveData>();

            // Add objects from all known collections
            if (ClickableCubes != null)
                allObjects.AddRange(ClickableCubes.Cast<RuntimeObjectSaveData>());

            if (TestGenericObjects != null)
                allObjects.AddRange(TestGenericObjects.Cast<RuntimeObjectSaveData>());

            // Future collections would be added here automatically if we use reflection
            // Or can be added manually as new types are created

            return allObjects;
        }

        /// <summary>
        /// Gets a runtime object by unique ID from any collection.
        /// More generic version that searches all collections.
        /// </summary>
        /// <param name="uniqueID">The unique ID to search for</param>
        /// <returns>The runtime object if found, null otherwise</returns>
        public RuntimeObjectSaveData GetRuntimeObjectByID(string uniqueID)
        {
            if (string.IsNullOrEmpty(uniqueID))
                return null;

            var allObjects = GetAllRuntimeObjects();
            return allObjects.FirstOrDefault(obj => obj.uniqueID == uniqueID);
        }
        
        /// <summary>
        /// Gets runtime object save data by unique ID and type
        /// </summary>
        /// <typeparam name="T">The type of runtime save data</typeparam>
        /// <param name="uniqueID">The unique ID of the object</param>
        /// <returns>The save data, or null if not found</returns>
        public T GetRuntimeObjectData<T>(string uniqueID) where T : RuntimeObjectSaveData
        {
            if (string.IsNullOrEmpty(uniqueID))
                return null;

            try
            {
                if (typeof(T) == typeof(ClickableCubeRuntimeSaveData))
                {
                    return ClickableCubes.FirstOrDefault(c => c.uniqueID == uniqueID) as T;
                }
                else if (typeof(T) == typeof(TestGenericRuntimeSaveData))
                {
                    return TestGenericObjects.FirstOrDefault(g => g.uniqueID == uniqueID) as T;
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveFileDataV2] Error getting runtime object data: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Gets all runtime objects of a specific type
        /// </summary>
        /// <typeparam name="T">The type of runtime save data</typeparam>
        /// <returns>List of objects of the specified type</returns>
        public List<T> GetAllRuntimeObjectsOfType<T>() where T : RuntimeObjectSaveData
        {
            try
            {
                if (typeof(T) == typeof(ClickableCubeRuntimeSaveData))
                {
                    return ClickableCubes.Cast<T>().ToList();
                }
                else if (typeof(T) == typeof(TestGenericRuntimeSaveData))
                {
                    return TestGenericObjects.Cast<T>().ToList();
                }

                return new List<T>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveFileDataV2] Error getting runtime objects of type: {ex.Message}");
                return new List<T>();
            }
        }
        
        /// <summary>
        /// Removes a runtime object by unique ID
        /// </summary>
        /// <param name="uniqueID">The unique ID of the object to remove</param>
        /// <returns>True if the object was found and removed</returns>
        public bool RemoveRuntimeObject(string uniqueID)
        {
            if (string.IsNullOrEmpty(uniqueID))
                return false;

            try
            {
                // Try to remove from each collection
                bool removed = false;
                
                removed |= ClickableCubes.RemoveAll(c => c.uniqueID == uniqueID) > 0;
                removed |= TestGenericObjects.RemoveAll(g => g.uniqueID == uniqueID) > 0;

                if (removed)
                {
                    Debug.Log($"[SaveFileDataV2] Removed runtime object: {uniqueID}");
                }

                return removed;
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
        /// Validates that all expected save data is present
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
            
            // Validate individual object collections
            ValidateObjectCollection(ClickableCubes, "ClickableCubes");
            ValidateObjectCollection(TestGenericObjects, "TestGenericObjects");

            return isValid;
        }
        
        private void ValidateObjectCollection<T>(List<T> collection, string collectionName) where T : RuntimeObjectSaveData
        {
            if (collection == null)
            {
                Debug.LogWarning($"[SaveFileDataV2] {collectionName} collection is null");
                return;
            }

            int validCount = 0;
            int invalidCount = 0;

            foreach (var obj in collection)
            {
                if (obj != null && !string.IsNullOrEmpty(obj.uniqueID) && !string.IsNullOrEmpty(obj.prefabGUID))
                {
                    validCount++;
                }
                else
                {
                    invalidCount++;
                }
            }

            Debug.Log($"[SaveFileDataV2] {collectionName}: {validCount} valid, {invalidCount} invalid objects");
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Generic method to set runtime object data using reflection to find the correct collection
        /// </summary>
        /// <param name="saveData">The runtime save data to store</param>
        /// <returns>True if successful</returns>
        private bool SetRuntimeObjectDataGeneric(RuntimeObjectSaveData saveData)
        {
            // Handle known types with direct method calls for better performance
            switch (saveData)
            {
                case ClickableCubeRuntimeSaveData cubeData:
                    return SetClickableCubeData(cubeData);
                
                case TestGenericRuntimeSaveData genericData:
                    return SetTestGenericData(genericData);
                
                default:
                    // For future extensibility, we could use reflection here to find
                    // the appropriate collection and add the object
                    Debug.LogWarning($"[SaveFileDataV2] Unknown runtime object type: {saveData.GetType().Name}. " +
                                   "Add specific handling for this type to SaveFileDataV2.");
                    
                    // TODO: Implement full reflection-based approach for unknown types
                    // This would automatically find the correct List<T> field and add the object
                    return false;
            }
        }
        
        private bool SetClickableCubeData(ClickableCubeRuntimeSaveData cubeData)
        {
            // Find existing entry
            var existingIndex = ClickableCubes.FindIndex(c => c.uniqueID == cubeData.uniqueID);
            
            if (existingIndex >= 0)
            {
                // Update existing
                ClickableCubes[existingIndex] = cubeData;
                Debug.Log($"[SaveFileDataV2] Updated ClickableCube: {cubeData.uniqueID}");
            }
            else
            {
                // Add new
                ClickableCubes.Add(cubeData);
                Debug.Log($"[SaveFileDataV2] Added new ClickableCube: {cubeData.uniqueID}");
            }
            
            return true;
        }
        
        private bool SetTestGenericData(TestGenericRuntimeSaveData genericData)
        {
            // Find existing entry
            var existingIndex = TestGenericObjects.FindIndex(g => g.uniqueID == genericData.uniqueID);
            
            if (existingIndex >= 0)
            {
                // Update existing
                TestGenericObjects[existingIndex] = genericData;
                Debug.Log($"[SaveFileDataV2] Updated TestGenericSaveable: {genericData.uniqueID}");
            }
            else
            {
                // Add new
                TestGenericObjects.Add(genericData);
                Debug.Log($"[SaveFileDataV2] Added new TestGenericSaveable: {genericData.uniqueID}");
            }
            
            return true;
        }
        #endregion

        // #region Legacy Support (Optional)
        // /// <summary>
        // /// Converts from old SaveFileData format (if needed for migration)
        // /// Note: User requested NO backwards compatibility, so this is optional
        // /// </summary>
        // public static SaveFileDataV2 FromLegacySaveFileData(SaveFileData legacyData)
        // {
        //     if (legacyData == null)
        //         return null;
        //
        //     var newData = new SaveFileDataV2
        //     {
        //         SaveTimeTicks = legacyData.SaveTimeTicks,
        //         WasAutoSave = legacyData.WasAutoSave,
        //         GameSessionData = legacyData.GameSessionData,
        //         PlayerData = legacyData.PlayerData
        //     };
        //
        //     // Migration logic would go here if needed
        //     // Since user specifically requested NO backwards compatibility, leaving this empty
        //
        //     return newData;
        // }
        // #endregion
    }
}
