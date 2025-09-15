using System;
using UnityEngine;
using GameFramework.SaveSystem.Data;

namespace GameFramework.SaveSystem.Utilities
{
    /// <summary>
    /// Static utility class for JSON serialization operations
    /// Provides centralized, consistent JSON handling with error management
    /// Uses Unity's JsonUtility for performance and compatibility
    /// </summary>
    public static class JsonSerializationHelper
    {
        /// <summary>
        /// Serializes an object to JSON string with pretty formatting
        /// </summary>
        /// <typeparam name="T">Type of object to serialize</typeparam>
        /// <param name="obj">Object to serialize</param>
        /// <param name="prettyPrint">Enable human-readable formatting</param>
        /// <returns>JSON string or null if serialization fails</returns>
        public static string SerializeToJson<T>(T obj, bool prettyPrint = true)
        {
            try
            {
                if (obj == null)
                {
                    Debug.LogWarning("[JsonSerializationHelper] Attempted to serialize null object");
                    return null;
                }

                string json = JsonUtility.ToJson(obj, prettyPrint);
                
                if (string.IsNullOrEmpty(json))
                {
                    Debug.LogError($"[JsonSerializationHelper] Serialization resulted in empty string for type: {typeof(T).Name}");
                    return null;
                }

                return json;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonSerializationHelper] Failed to serialize {typeof(T).Name}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Deserializes JSON string to object of specified type
        /// </summary>
        /// <typeparam name="T">Type to deserialize to</typeparam>
        /// <param name="json">JSON string to deserialize</param>
        /// <returns>Deserialized object or default(T) if deserialization fails</returns>
        public static T DeserializeFromJson<T>(string json)
        {
            try
            {
                if (string.IsNullOrEmpty(json))
                {
                    Debug.LogWarning("[JsonSerializationHelper] Attempted to deserialize null or empty JSON string");
                    return default(T);
                }

                T result = JsonUtility.FromJson<T>(json);
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonSerializationHelper] Failed to deserialize to {typeof(T).Name}: {ex.Message}");
                return default(T);
            }
        }

        /// <summary>
        /// Validates if a string is valid JSON format
        /// </summary>
        /// <param name="json">JSON string to validate</param>
        /// <returns>True if valid JSON, false otherwise</returns>
        public static bool IsValidJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return false;

            try
            {
                // Attempt to parse as generic object to validate structure
                JsonUtility.FromJson<object>(json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to deserialize with error handling
        /// Returns success status and result via out parameter
        /// </summary>
        public static bool TryDeserialize<T>(string json, out T result)
        {
            result = default(T);
            
            if (string.IsNullOrEmpty(json))
                return false;

            try
            {
                result = JsonUtility.FromJson<T>(json);
                return result != null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonSerializationHelper] Deserialization failed: {ex.Message}");
                return false;
            }
        }
    }
}
