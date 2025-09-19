using System;
using UnityEngine;

namespace GameFramework.SaveSystem.Data
{
    /// <summary>
    /// Serialized wrapper for RuntimeObjectSaveData that enables dynamic storage in SaveFileDataV2.
    /// This allows SaveFileDataV2 to store any type of RuntimeObjectSaveData without requiring
    /// specific typed collections for each saveable type.
    /// </summary>
    [System.Serializable]
    public class SerializedRuntimeObject
    {
        [Header("Object Identity")]
        public string uniqueID;
        public string typeName;
        public string saveDataTypeName; // Full type name of the RuntimeObjectSaveData
        
        [Header("Serialized Data")]
        [TextArea(3, 10)]
        public string jsonData; // JSON serialization of the RuntimeObjectSaveData
        
        [Header("Debug Info")]
        [SerializeField, ReadOnly] private int dataLength = 0;
        
        /// <summary>
        /// Default constructor for serialization
        /// </summary>
        public SerializedRuntimeObject()
        {
            uniqueID = string.Empty;
            typeName = string.Empty;
            saveDataTypeName = string.Empty;
            jsonData = string.Empty;
        }
        
        /// <summary>
        /// Creates a SerializedRuntimeObject from RuntimeObjectSaveData
        /// </summary>
        /// <param name="saveData">The RuntimeObjectSaveData to serialize</param>
        public SerializedRuntimeObject(RuntimeObjectSaveData saveData)
        {
            if (saveData == null)
            {
                throw new ArgumentNullException(nameof(saveData));
            }
            
            uniqueID = saveData.uniqueID;
            typeName = saveData.typeName;
            saveDataTypeName = saveData.GetType().FullName;
            
            try
            {
                jsonData = JsonUtility.ToJson(saveData);
                dataLength = jsonData.Length;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SerializedRuntimeObject] Failed to serialize save data for {uniqueID}: {ex.Message}");
                jsonData = string.Empty;
                dataLength = 0;
            }
        }
        
        /// <summary>
        /// Deserializes the stored data back to a RuntimeObjectSaveData instance
        /// </summary>
        /// <returns>The deserialized RuntimeObjectSaveData, or null if deserialization fails</returns>
        public RuntimeObjectSaveData Deserialize()
        {
            if (string.IsNullOrEmpty(jsonData) || string.IsNullOrEmpty(saveDataTypeName))
            {
                Debug.LogWarning($"[SerializedRuntimeObject] Cannot deserialize object {uniqueID} - missing data or type name");
                return null;
            }
            
            try
            {
                // Try to find the type from the full name first
                Type saveDataType = Type.GetType(saveDataTypeName);
                
                // If that fails, try to find it using SaveableTypeRegistry
                if (saveDataType == null && !string.IsNullOrEmpty(typeName))
                {
                    saveDataType = GameFramework.SaveSystem.Utilities.SaveableTypeRegistry.GetSaveDataType(typeName);
                }
                
                if (saveDataType == null)
                {
                    Debug.LogError($"[SerializedRuntimeObject] Could not find type for {typeName} ({saveDataTypeName})");
                    return null;
                }
                
                // Deserialize the JSON data
                var saveData = (RuntimeObjectSaveData)JsonUtility.FromJson(jsonData, saveDataType);
                
                if (saveData == null)
                {
                    Debug.LogError($"[SerializedRuntimeObject] Failed to deserialize JSON for {uniqueID}");
                    return null;
                }
                
                // Ensure the deserialized data has the correct identity information
                saveData.uniqueID = uniqueID;
                saveData.typeName = typeName;
                
                return saveData;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SerializedRuntimeObject] Error deserializing object {uniqueID}: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Updates the serialized data from a RuntimeObjectSaveData instance
        /// </summary>
        /// <param name="saveData">The new save data to serialize</param>
        /// <returns>True if update was successful</returns>
        public bool UpdateFrom(RuntimeObjectSaveData saveData)
        {
            if (saveData == null)
            {
                Debug.LogError("[SerializedRuntimeObject] Cannot update from null save data");
                return false;
            }
            
            // Verify this is the same object
            if (saveData.uniqueID != uniqueID)
            {
                Debug.LogError($"[SerializedRuntimeObject] UniqueID mismatch: expected {uniqueID}, got {saveData.uniqueID}");
                return false;
            }
            
            try
            {
                // Update the serialized data
                typeName = saveData.typeName;
                saveDataTypeName = saveData.GetType().FullName;
                jsonData = JsonUtility.ToJson(saveData);
                dataLength = jsonData.Length;
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SerializedRuntimeObject] Failed to update serialized data for {uniqueID}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Validates that this serialized object has valid data
        /// </summary>
        /// <returns>True if the object is valid</returns>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(uniqueID) && 
                   !string.IsNullOrEmpty(typeName) && 
                   !string.IsNullOrEmpty(saveDataTypeName) && 
                   !string.IsNullOrEmpty(jsonData);
        }
        
        /// <summary>
        /// Gets a display-friendly summary of this object
        /// </summary>
        /// <returns>Summary string</returns>
        public override string ToString()
        {
            return $"SerializedRuntimeObject: {uniqueID} ({typeName}) - {dataLength} bytes";
        }
    }
 
}
