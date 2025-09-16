using System;
using UnityEngine;

namespace GameFramework.SaveSystem.Utilities
{
    /// <summary>
    /// Utility class for generating unique IDs for saveable objects
    /// </summary>
    public static class UniqueIDGenerator
    {
        /// <summary>
        /// Generates a unique ID for saveable objects
        /// Format: {prefix}_{timestamp}_{random}
        /// </summary>
        public static string GenerateUniqueID(string prefix = "obj")
        {
            var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var random = UnityEngine.Random.Range(1000, 9999);
            return $"{prefix}_{timestamp}_{random}";
        }
        
        /// <summary>
        /// Validates that a unique ID has the expected format
        /// </summary>
        public static bool IsValidUniqueID(string uniqueId)
        {
            if (string.IsNullOrEmpty(uniqueId)) return false;
            
            var parts = uniqueId.Split('_');
            return parts.Length == 3 && 
                   !string.IsNullOrEmpty(parts[0]) && 
                   long.TryParse(parts[1], out _) && 
                   int.TryParse(parts[2], out _);
        }
    }
}