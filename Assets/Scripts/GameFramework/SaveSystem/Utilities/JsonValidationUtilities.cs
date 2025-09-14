using System;
using System.Threading.Tasks;

namespace GameFramework.Utilities
{
    /// <summary>
    /// Static utility class for JSON validation operations
    /// Provides lightweight validation methods for JSON content without full deserialization
    /// </summary>
    public static class JsonValidationUtilities
    {
        /// <summary>
        /// Validates JSON structure for GameSession loading without full deserialization
        /// Performs lightweight checks for required fields and basic structure
        /// </summary>
        /// <param name="json">JSON string to validate</param>
        /// <returns>True if JSON appears to contain valid GameSession structure</returns>
        public static bool ValidateGameSessionJsonStructure(string json)
        {
            if (string.IsNullOrEmpty(json)) return false;

            try
            {
                // Check for required fields (lightweight check)
                return json.Contains("\"playerName\"") &&
                       json.Contains("\"currentScene\"") &&
                       json.Length > 100; // Basic size check
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Asynchronously validates JSON structure for GameSession
        /// </summary>
        /// <param name="json">JSON string to validate</param>
        /// <returns>Task containing true if JSON is valid</returns>
        public static async Task<bool> ValidateGameSessionJsonStructureAsync(string json)
        {
            return await Task.Run(() => ValidateGameSessionJsonStructure(json));
        }

        /// <summary>
        /// Validates basic JSON syntax without parsing content
        /// </summary>
        /// <param name="json">JSON string to validate</param>
        /// <returns>True if JSON syntax appears valid</returns>
        public static bool ValidateJsonSyntax(string json)
        {
            if (string.IsNullOrEmpty(json)) return false;

            try
            {
                json = json.Trim();
                
                // Basic JSON syntax checks
                var startsCorrectly = json.StartsWith("{") || json.StartsWith("[");
                var endsCorrectly = json.EndsWith("}") || json.EndsWith("]");
                var hasMinimumLength = json.Length >= 2;
                
                return startsCorrectly && endsCorrectly && hasMinimumLength;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates that JSON contains specific required fields
        /// </summary>
        /// <param name="json">JSON string to validate</param>
        /// <param name="requiredFields">Array of field names that must be present</param>
        /// <returns>True if all required fields are found</returns>
        public static bool ValidateRequiredFields(string json, params string[] requiredFields)
        {
            if (string.IsNullOrEmpty(json) || requiredFields == null) return false;

            try
            {
                foreach (var field in requiredFields)
                {
                    var fieldPattern = $"\"{field}\"";
                    if (!json.Contains(fieldPattern))
                    {
                        return false;
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Performs comprehensive JSON validation including syntax and structure
        /// </summary>
        /// <param name="json">JSON string to validate</param>
        /// <returns>ValidationResult containing success status and details</returns>
        public static JsonValidationResult ValidateJsonComprehensively(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return new JsonValidationResult(false, "JSON is null or empty");
            }

            // Check basic syntax
            if (!ValidateJsonSyntax(json))
            {
                return new JsonValidationResult(false, "Invalid JSON syntax");
            }

            // Check GameSession structure
            if (!ValidateGameSessionJsonStructure(json))
            {
                return new JsonValidationResult(false, "Missing required GameSession fields");
            }

            return new JsonValidationResult(true, "JSON validation passed");
        }
    }

    /// <summary>
    /// Result of comprehensive JSON validation
    /// </summary>
    public readonly struct JsonValidationResult
    {
        public readonly bool IsValid;
        public readonly string Message;

        public JsonValidationResult(bool isValid, string message)
        {
            IsValid = isValid;
            Message = message ?? string.Empty;
        }
    }
}
