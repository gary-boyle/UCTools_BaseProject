using System;
using System.Threading.Tasks;
using UnityEngine;
using GameFramework.DataStructures;

namespace GameFramework.Utilities
{
    /// <summary>
    /// Static utility class for GameSession serialization and deserialization operations
    /// Handles all JSON conversion for save/load operations with proper error handling
    /// </summary>
    public static class GameSessionSerializer
    {
        /// <summary>
        /// Serializes a GameSession to JSON string
        /// </summary>
        /// <param name="session">The game session to serialize</param>
        /// <param name="prettyPrint">Whether to format JSON with indentation</param>
        /// <returns>JSON string representation of the game session</returns>
        /// <exception cref="ArgumentNullException">Thrown when session is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when serialization fails</exception>
        public static string SerializeToJson(GameSession session, bool prettyPrint = true)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            try
            {
                return JsonUtility.ToJson(session, prettyPrint);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to serialize GameSession to JSON: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Deserializes a GameSession from JSON string
        /// </summary>
        /// <param name="json">JSON string to deserialize</param>
        /// <returns>Deserialized GameSession object</returns>
        /// <exception cref="ArgumentException">Thrown when JSON is null or empty</exception>
        /// <exception cref="InvalidOperationException">Thrown when deserialization fails</exception>
        public static GameSession DeserializeFromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentException("JSON string cannot be null or empty", nameof(json));

            try
            {
                return JsonUtility.FromJson<GameSession>(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to deserialize GameSession from JSON: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Asynchronously serializes a GameSession to JSON string
        /// </summary>
        /// <param name="session">The game session to serialize</param>
        /// <param name="prettyPrint">Whether to format JSON with indentation</param>
        /// <returns>Task containing JSON string representation</returns>
        public static async Task<string> SerializeToJsonAsync(GameSession session, bool prettyPrint = true)
        {
            return await Task.Run(() => SerializeToJson(session, prettyPrint));
        }

        /// <summary>
        /// Asynchronously deserializes a GameSession from JSON string
        /// </summary>
        /// <param name="json">JSON string to deserialize</param>
        /// <returns>Task containing deserialized GameSession object</returns>
        public static async Task<GameSession> DeserializeFromJsonAsync(string json)
        {
            return await Task.Run(() => DeserializeFromJson(json));
        }

        /// <summary>
        /// Validates that a GameSession can be successfully serialized and deserialized
        /// </summary>
        /// <param name="session">GameSession to validate</param>
        /// <returns>True if serialization round-trip succeeds</returns>
        public static bool ValidateSerializationRoundTrip(GameSession session)
        {
            if (session == null) return false;

            try
            {
                var json = SerializeToJson(session);
                var deserialized = DeserializeFromJson(json);
                return deserialized != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
