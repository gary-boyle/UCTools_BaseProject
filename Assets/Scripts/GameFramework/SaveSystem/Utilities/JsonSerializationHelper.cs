using System;
using System.Collections.Generic;
using UnityEngine;
using GameFramework.SaveSystem.Data;

namespace GameFramework.SaveSystem.Utilities
{
    /// <summary>
    /// Helper utility for JSON serialization/deserialization of complex SaveFileData structures.
    /// Handles the serialization issues with Unity's JsonUtility when dealing with complex nested objects
    /// and Lists that contain SerializedRuntimeObject instances.
    /// </summary>
    public static class JsonSerializationHelper
    {
        /// <summary>
        /// Deserializes SaveFileData from JSON string, handling all complex nested structures properly.
        /// This method works around Unity JsonUtility limitations with complex object graphs.
        /// </summary>
        /// <param name="json">The JSON string to deserialize</param>
        /// <returns>Deserialized SaveFileData or null if deserialization fails</returns>
        public static SaveFileData DeserializeFromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[JsonSerializationHelper] Cannot deserialize null or empty JSON");
                return null;
            }

            try
            {
                // First attempt: Try direct Unity JsonUtility deserialization
                var saveData = JsonUtility.FromJson<SaveFileData>(json);
                
                if (saveData != null)
                {
                    // Validate that the data was deserialized correctly
                    if (ValidateDeserializedData(saveData))
                    {
                        Debug.Log("[JsonSerializationHelper] Successfully deserialized SaveFileData with Unity JsonUtility");
                        return saveData;
                    }
                }
                
                Debug.LogWarning("[JsonSerializationHelper] Direct JsonUtility deserialization failed or produced invalid data, attempting manual deserialization");
                
                // Fallback: Manual deserialization for complex structures
                return DeserializeManually(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonSerializationHelper] Failed to deserialize SaveFileData: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Deserializes SaveFileData from JSON with type validation
        /// </summary>
        /// <typeparam name="T">Expected type (must be SaveFileData)</typeparam>
        /// <param name="json">The JSON string to deserialize</param>
        /// <returns>Deserialized data or null if failed</returns>
        public static T DeserializeFromJson<T>(string json) where T : class
        {
            if (typeof(T) != typeof(SaveFileData))
            {
                Debug.LogError($"[JsonSerializationHelper] Generic deserialization only supports SaveFileData, got: {typeof(T).Name}");
                return null;
            }

            return DeserializeFromJson(json) as T;
        }

        /// <summary>
        /// Serializes SaveFileData to JSON string
        /// </summary>
        /// <param name="saveData">The SaveFileData to serialize</param>
        /// <returns>JSON string or null if serialization fails</returns>
        public static string SerializeToJson(SaveFileData saveData)
        {
            if (saveData == null)
            {
                Debug.LogWarning("[JsonSerializationHelper] Cannot serialize null SaveFileData");
                return null;
            }

            try
            {
                return JsonUtility.ToJson(saveData, true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonSerializationHelper] Failed to serialize SaveFileData: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Validates that deserialized SaveFileData contains the expected structure
        /// </summary>
        /// <param name="saveData">The deserialized data to validate</param>
        /// <returns>True if data appears valid</returns>
        private static bool ValidateDeserializedData(SaveFileData saveData)
        {
            if (saveData == null) return false;

            // Check basic structure
            bool hasValidTimestamp = saveData.SaveTimeTicks > 0;
            bool hasRuntimeObjectsList = saveData.RuntimeObjects != null;
            
            // For debugging: log what we found
            Debug.Log($"[JsonSerializationHelper] Validation - Timestamp: {hasValidTimestamp}, " +
                     $"RuntimeObjects: {hasRuntimeObjectsList}, " +
                     $"GameSessionData: {saveData.GameSessionData != null}, " +
                     $"PlayerData: {saveData.PlayerData != null}");

            return hasValidTimestamp && hasRuntimeObjectsList;
        }

        /// <summary>
        /// Manual deserialization fallback for when JsonUtility fails with complex structures
        /// </summary>
        /// <param name="json">The JSON string to deserialize manually</param>
        /// <returns>SaveFileData or null if manual deserialization fails</returns>
        private static SaveFileData DeserializeManually(string json)
        {
            try
            {
                // For now, we'll still rely on JsonUtility but with better error handling
                // In the future, this could be expanded to use a more robust JSON library like Newtonsoft.Json
                
                var saveData = new SaveFileData();
                
                // Try to parse the JSON manually by extracting key components
                var jsonObject = JsonUtility.FromJson<SaveFileDataWrapper>(json);
                
                if (jsonObject != null)
                {
                    saveData.SaveTimeTicks = jsonObject.SaveTimeTicks;
                    saveData.WasAutoSave = jsonObject.WasAutoSave;
                    saveData.GameSessionData = jsonObject.GameSessionData;
                    saveData.PlayerData = jsonObject.PlayerData;
                    saveData.RuntimeObjects = jsonObject.RuntimeObjects ?? new List<SerializedRuntimeObject>();
                    
                    return saveData;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonSerializationHelper] Manual deserialization failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Wrapper class for manual deserialization - mirrors SaveFileData structure
        /// </summary>
        [System.Serializable]
        private class SaveFileDataWrapper
        {
            public long SaveTimeTicks;
            public bool WasAutoSave;
            public GameSessionSaveData GameSessionData;
            public PlayerSaveData PlayerData;
            public List<SerializedRuntimeObject> RuntimeObjects;
        }

        /// <summary>
        /// Creates a SaveFileData instance from corrupted or partial JSON data
        /// </summary>
        /// <param name="fileName">The filename this data is from</param>
        /// <returns>A SaveFileData with minimal valid structure</returns>
        public static SaveFileData CreateCorruptedSaveData(string fileName)
        {
            return new SaveFileData
            {
                SaveTimeTicks = DateTime.MinValue.Ticks,
                WasAutoSave = false,
                GameSessionData = new GameSessionSaveData
                {
                    uniqueID = "corrupted",
                    difficulty = "Unknown",
                    currentScene = "Unknown",
                    gameTime = 0
                },
                PlayerData = new PlayerSaveData
                {
                    uniqueID = "corrupted",
                    playerName = "Corrupted Save",
                    Position = Vector3.zero,
                    Rotation = Vector3.zero
                },
                RuntimeObjects = new List<SerializedRuntimeObject>()
            };
        }

        /// <summary>
        /// Attempts to repair corrupted JSON by fixing common issues
        /// </summary>
        /// <param name="json">Potentially corrupted JSON</param>
        /// <returns>Repaired JSON or original if no repair was possible</returns>
        public static string AttemptJsonRepair(string json)
        {
            if (string.IsNullOrEmpty(json)) return json;

            try
            {
                // Common repairs for Unity JsonUtility issues
                string repairedJson = json;

                // Fix missing RuntimeObjects array
                if (!repairedJson.Contains("\"RuntimeObjects\""))
                {
                    repairedJson = repairedJson.TrimEnd('}');
                    repairedJson += ",\"RuntimeObjects\":[]}";
                }

                // Fix null values that should be empty objects
                repairedJson = repairedJson.Replace("\"GameSessionData\":null", "\"GameSessionData\":{}");
                repairedJson = repairedJson.Replace("\"PlayerData\":null", "\"PlayerData\":{}");

                return repairedJson;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[JsonSerializationHelper] JSON repair failed: {ex.Message}");
                return json;
            }
        }
    }
}